using ClaimLens.Application;
using ClaimLens.Infrastructure;
using ClaimLens.Infrastructure.Auth;
using ClaimLens.Infrastructure.Persistence;
using ClaimLens.Api.Errors;
using ClaimLens.Api.Hubs;
using ClaimLens.Application.Investigations;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    {
        throw new InvalidOperationException("JWT secret is not configured.");
    }

    jwtSecret = "dev-only-claimlens-signing-key-not-for-production-use";
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "claimlens";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "claimlens-web";

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.PostConfigure<JwtSettings>(settings =>
{
    settings.Secret = jwtSecret;
    settings.Issuer = jwtIssuer;
    settings.Audience = jwtAudience;
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtAccessTokenIssuer.SigningKey(jwtSecret),
            ValidateLifetime = true,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            code = "RATE_LIMITED",
            message = "Too many requests. Try again shortly."
        }, cancellationToken);
    };
    options.AddPolicy("auth", httpContext => FixedWindow(httpContext, "RateLimiting:AuthPermitLimit", 30));
    options.AddPolicy("create-investigation", httpContext => FixedWindow(httpContext, "RateLimiting:CreatePermitLimit", 20));
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<IInvestigationProgress, SignalRInvestigationProgress>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var webOrigin = builder.Configuration["WebOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy.WithOrigins(webOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetService<ClaimLensDbContext>();
    if (database is null)
    {
        throw new InvalidOperationException("Database connection string is not configured.");
    }

    database.Database.Migrate();
    if (app.Environment.IsDevelopment())
    {
        await DevelopmentReviewerSeed.EnsureAsync(scope.ServiceProvider, CancellationToken.None);
        await DevelopmentAdminSeed.EnsureAsync(scope.ServiceProvider, CancellationToken.None);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    }

    await next();
});
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<InvestigationsHub>("/hubs/investigations");

app.Run();

static RateLimitPartition<string> FixedWindow(HttpContext httpContext, string permitKey, int fallbackPermit)
{
    var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
    var permit = configuration.GetValue(permitKey, fallbackPermit);
    var windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 60);
    var partition = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permit,
        Window = TimeSpan.FromSeconds(windowSeconds),
        QueueLimit = 0
    });
}

public partial class Program;
