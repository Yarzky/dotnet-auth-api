using AuthSystem.Infrastructure.Extensions;
using AuthSystem.Application.Extensions;
using AuthSystem.Api.Middleware;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using AuthSystem.Application.DTOs;
using AuthSystem.Application.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using dotenv.net;

DotEnv.Load(options: new DotEnvOptions(
      envFilePaths: new[] {"../.env", ".env", "./.env"}
      ));

var builder = WebApplication.CreateBuilder(args);

// 1. JWT Configuration from Environment (Padronizado)
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
?? builder.Configuration["JWT_SECRET_KEY"] 
?? throw new InvalidOperationException("JWT Secret Key is not configured.");
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ??
builder.Configuration["JwtSettings:Issuer"];
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ??
builder.Configuration["JwtSettings:Audience"];

builder.Configuration["JWT_SECRET_KEY"] = secretKey;
builder.Configuration["JwtSettings:Issuer"] = issuer;
builder.Configuration["JwtSettings:Audience"] = audience;

// 2. Database Connection from Environment (Opcional, mas recomendado)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
  var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
  var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5434";
  var db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "auth_db";
  var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
  var pass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";

  connectionString =
    $"Host={host};Port={port};Database={db};Username={user};Password={pass}";
  builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}





// Add services to the container.
builder.Services.AddRateLimiter(options =>
    {
    options.AddFixedWindowLimiter("auth-limit", opt =>
        {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
        });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });
builder.Services.AddControllers()
  .ConfigureApiBehaviorOptions(options =>
      {
      options.InvalidModelStateResponseFactory = context =>
      {
      var errors = context.ModelState.Values
      .SelectMany(v => v.Errors)
      .Select(e => e.ErrorMessage)
      .ToList();

      var response = new AuthResponse<object>(
          false,
          Error: new AuthError("VALIDATION_ERROR", string.Join(" ", errors))
          );

      return new BadRequestObjectResult(response);
      };
      });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Configure JWT Authentication
//var secretKey = builder.Configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT Secret Key is not configured.");
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
    {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
.AddJwtBearer(options =>
    {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ValidateIssuer = true,
    ValidIssuer = issuer,
    ValidateAudience = true,
    ValidAudience = audience,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero
    };
    });

builder.Services.AddAuthorization(options =>
    {
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()));
    options.AddPolicy("UserOnly", policy => policy.RequireRole(UserRole.User.ToString()));
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    {
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
        Title = "JWT Auth System API",
        Version = "v1",
        Description = "A standalone authentication and authorization service."
        });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
        });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
        {
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
        Id = "Bearer"
        }
        },
        Array.Empty<string>()
        }
        });
    });

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
