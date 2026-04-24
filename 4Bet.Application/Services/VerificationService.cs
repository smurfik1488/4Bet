using System.Security.Cryptography;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
// Додай using для твоїх моделей, де лежить VerificationRequest (наприклад, _4Bet.Domain.Entities)
// using _4Bet.Domain.Entities; 
using Microsoft.Extensions.Configuration;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;

namespace _4Bet.Application.Services;

public class VerificationService : IVerificationService
{
    private readonly BlobContainerClient _containerClient;
    private readonly DocumentAnalysisClient _analysisClient;
    private readonly FourBetDbContext _context;
    private readonly IAuthRepository _authRepository;
    private readonly IVerificationRepository _verificationRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    
    // 1. Додаємо репозиторій запитів
    private readonly IVerificationRepository _verificationRequestRepository;

    public VerificationService(
        IConfiguration config, 
        FourBetDbContext context, 
        IAuthRepository authRepository,
        IVerificationRepository verificationRequestRepository, IVerificationRepository verificationRepository, IEmailService emailService, IEmailVerificationRepository emailVerificationRepository) // 2. Інжектимо в конструктор
    {
        _context = context;
        _authRepository = authRepository;
        _verificationRequestRepository = verificationRequestRepository;
        _verificationRepository = verificationRepository;
        _emailService = emailService;
        _emailVerificationRepository = emailVerificationRepository;

        // Налаштовуємо клієнта для Сховища
        var storageConn = config["Azure:Storage:ConnectionString"];
        var containerName = config["Azure:Storage:ContainerName"];
        _containerClient = new BlobServiceClient(storageConn).GetBlobContainerClient(containerName);

        // Налаштовуємо клієнта для ШІ
        var aiKey = config["Azure:DocumentIntelligence:Key"];
        var aiEndpoint = config["Azure:DocumentIntelligence:Endpoint"];
        _analysisClient = new DocumentAnalysisClient(new Uri(aiEndpoint), new AzureKeyCredential(aiKey));
    }

    public async Task<string> VerifyAgeAsync(Stream fileStream, string fileName, Guid userId)
    {
        // 1. Завантаження в Blob Storage
        var blobClient = _containerClient.GetBlobClient($"{userId}/{Guid.NewGuid()}{Path.GetExtension(fileName)}");
        await blobClient.UploadAsync(fileStream);
        fileStream.Position = 0; 

        // 2. Аналіз через Azure AI
        AnalyzeDocumentOperation operation = await _analysisClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-idDocument", fileStream);
        AnalyzeResult result = operation.Value;

        var document = result.Documents.FirstOrDefault();
        
        // 3. Відправка реквесту на ручну перевірку, якщо ШІ сумнівається
        if (document == null || document.Confidence < 0.8) 
        {
            var request = new VerificationRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DocumentUrl = blobClient.Uri.ToString(), // Зберігаємо посилання на розмите/замазане фото
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _verificationRequestRepository.AddAsync(request);
            
            return "PENDING_REVIEW";
        }

        // 4. Витягуємо дату народження, якщо все ок
        if (document.Fields.TryGetValue("DateOfBirth", out DocumentField? dobField) && dobField.FieldType == DocumentFieldType.Date)
        {
            DateTime birthDate = dobField.Value.AsDate().DateTime;
            int age = DateTime.Today.Year - birthDate.Year;
            if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;

            if (age >= 21)
            {
                var user = await _authRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.IsBdVerified = true;
                    // Оскільки у тебе тут є доступ до _context, це спрацює, 
                    // але правильніше було б зробити _authRepository.UpdateAsync(user), якщо такий метод є
                    await _context.SaveChangesAsync(); 
                    return "SUCCESS";
                }
            }
            else 
            {
                return "TOO_YOUNG";
            }
        }

        // Якщо дати народження не знайдено на якісному фото — теж відправляємо адміну
        var missingDataRequest = new VerificationRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentUrl = blobClient.Uri.ToString(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await _verificationRequestRepository.AddAsync(missingDataRequest);

        return "PENDING_REVIEW"; // Замінив "DATA_NOT_FOUND" на відправку адміну
    }
    
    public async Task GenerateAndSendCodeAsync(User user)
    {
        // Generate a secure 6-digit code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        // Save it to the database (expires in 15 minutes)
        var request = new EmailVerificationRequest
        {
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        
        await _emailVerificationRepository.AddAsync(request); // Assuming you have an Add method

        // Send the email
        var subject = "Your 4Bet Verification Code";
        var body = $"<h2>Hello!</h2><p>Your verification code is: <strong>{code}</strong></p><p>This code expires in 15 minutes.</p>";

        if (user.Email != null) await _emailService.SendEmailAsync(user.Email, subject, body);
    }

    // 2. RECEIVE, CHECK, AND VERIFY
    public async Task<bool> VerifyCodeAsync(string email, string code)
    {
        // Find the user
        var user = await _authRepository.GetByEmailAsync(email);
        if (user == null) return false;

        // Find their active verification code in the DB
        var verificationRequest = await _emailVerificationRepository.GetLatestCodeForUserAsync(user.Id);
        
        if (verificationRequest == null) return false;

        // Check if code matches AND is not expired
        if (verificationRequest.Code == code && verificationRequest.ExpiresAt > DateTime.UtcNow)
        {
            // Success! Mark user as verified
            user.IsEmailVerified = true;
            await _authRepository.UpdateAsync(user); 

            // Optional: Delete the code from DB so it can't be used again
            await _emailVerificationRepository.DeleteAsync(verificationRequest);
            await _emailVerificationRepository.InvalidateOldCodesAsync(user.Id);

            return true;
        }

        return false; // Code was wrong or expired
    }
    public async Task ResendCodeAsync(string email)
    {
        // 1. Find the user
        var user = await _authRepository.GetByEmailAsync(email);
    
        // If user doesn't exist or is already verified, do nothing (for security/spam prevention)
        if (user == null || user.IsEmailVerified) 
        {
            return; 
        }

        // 2. Delete any old codes so they don't get confused
        await _emailVerificationRepository.InvalidateOldCodesAsync(user.Id);

        // 3. Reuse our existing method to generate, save, and email the new code!
        await GenerateAndSendCodeAsync(user);
    }
}