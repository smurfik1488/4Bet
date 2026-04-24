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
// 1. Added the missing namespace for the external services
using _4Bet.Infrastructure.ExternalServices; 

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("4betDBConnection");

// 2. Реєструємо DbContext
builder.Services.AddDbContext<_4Bet.Infrastructure.Data.FourBetDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddAutoMapper(_ => { }, typeof(MappingProfile).Assembly);

// Scoped Services & Repositories
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<ISportService, SportService>();
builder.Services.AddScoped<IAdminVerificationService, AdminVerificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISportParserService, SportParserService>();


builder.Services.AddScoped<IVerificationRepository, VerificationRepository>();
builder.Services.AddScoped<ISportRepository, SportRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();




// 3. Added the HTTP Client for the Parser
builder.Services.AddHttpClient<ISportParserService, SportParserService>(client =>
{
    client.BaseAddress = new Uri("https://api.the-odds-api.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 4. Added the Background Worker that will run automatically
builder.Services.AddHostedService<SportDataUpdateWorker>();

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
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

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

app.UseHttpsRedirection();

app.Run();