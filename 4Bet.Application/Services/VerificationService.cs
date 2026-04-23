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
    
    // 1. Додаємо репозиторій запитів
    private readonly IVerificationRepository _verificationRequestRepository;

    public VerificationService(
        IConfiguration config, 
        FourBetDbContext context, 
        IAuthRepository authRepository,
        IVerificationRepository verificationRequestRepository) // 2. Інжектимо в конструктор
    {
        _context = context;
        _authRepository = authRepository;
        _verificationRequestRepository = verificationRequestRepository;

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
}