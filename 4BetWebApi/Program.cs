using System.Text;
using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using _4Bet.Application.Services;
using _4Bet.Infrastructure.IRepositories;
using _4Bet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using _4Bet.Application.Mappings;
using Microsoft.OpenApi.Models;
using _4Bet.Infrastructure.ExternalServices; 

// 1. ДОДАНО: Юзінги для роботи SignalR
using _4BetWebApi.Hubs;
using _4BetWebApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// 2. ДОДАНО: Налаштування CORS (Обов'язково для WebSockets/SignalR)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            return;
        }

        // Safe fallback for local development if no config provided.
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 3. ДОДАНО: Реєстрація самого SignalR
builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("4betDBConnection");

builder.Services.AddDbContext<_4Bet.Infrastructure.Data.FourBetDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddAutoMapper(_ => { }, typeof(MappingProfile).Assembly);

// Scoped Services & Repositories
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<ISportService, SportService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IBetService, BetService>();
builder.Services.AddScoped<IAdminVerificationService, AdminVerificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IVerificationRepository, VerificationRepository>();
builder.Services.AddScoped<ISportRepository, SportRepository>();
builder.Services.AddScoped<IBetRepository, BetRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();

// 4. ДОДАНО: Реєстрація сервісу для пушу сповіщень на клієнт
builder.Services.AddTransient<ISportNotificationService, SignalRNotificationService>();


// --- HTTP Clients ---
builder.Services.AddHttpClient<ISportParserService, SportParserService>(client =>
{
    client.BaseAddress = new Uri("https://api.the-odds-api.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("TeamLogoProxy", static client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; 4Bet/1.0)");
    client.Timeout = TimeSpan.FromSeconds(20);
});



// --- Background Workers ---
builder.Services.AddHostedService<SportDataUpdateWorker>(); // Підтягує pre-match матчі/коефи з The Odds API
builder.Services.AddHostedService<OddsLiveUpdateWorker>(); // Оновлює коефи та пушить лайв-зміни по SignalR
builder.Services.AddHostedService<BetSettlementWorker>(); // Закриває pending ставки по завершених матчах


// Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Authentication & Authorization Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR browser clients send JWT via access_token query for WebSockets/SSE.
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/matchHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 5. ДОДАНО: Застосування CORS (Обов'язково перед мапінгом)
app.UseCors("FrontendCorsPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 6. ДОДАНО: Відкриваємо шлях для підключення фронтенду до вебсокетів
app.MapHub<MatchHub>("/matchHub");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger(); 
    app.UseSwaggerUI(); 
}

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.Run();