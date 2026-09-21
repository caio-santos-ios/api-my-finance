using api_finances.src.Configuration;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using api_finances.src.Models.Base;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Services.AddEndpointsApiExplorer();
builder.AddBuilderConfiguration();
builder.AddContext();
builder.AddBuilderServices();
builder.AddBuilderHelpers();
builder.Services.AddControllers();
builder.WebHost.UseUrls("http://0.0.0.0:5140");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    string secretKey = Environment.GetEnvironmentVariable("SECRET_KEY") ?? "";
    string issuer    = Environment.GetEnvironmentVariable("ISSUER")     ?? "";
    string audience  = Environment.GetEnvironmentVariable("AUDIENCE")   ?? "";

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = issuer,
        ValidAudience            = audience,
        ClockSkew                = TimeSpan.FromMinutes(5),
        IssuerSigningKey         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(secretKey)
        )
    };
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        List<string> errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
            .ToList();

        ResponseApi<dynamic> response = new ()
        {
            Message = errors[0],
            StatusCode = 400,
        };

        return new BadRequestObjectResult(response);
    };
});

var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "http://localhost:3000").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppPolicy", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AppPolicy");

var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();