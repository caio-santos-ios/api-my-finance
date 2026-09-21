using api_finances.src.Interfaces;
using api_finances.src.Repository;
using api_finances.src.Services;
using api_finances.src.Infraestructure;
using api_finances.src.Helpers;
using CloudinaryDotNet;

namespace api_finances.src.Configuration
{
    public static class Build
    {
        public static void AddBuilderConfiguration(this WebApplicationBuilder builder)
        {
            AppDbContext.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "";
            AppDbContext.DatabaseName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "";
            AppDbContext.IsSSL = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IS_SSL"))
                && Convert.ToBoolean(Environment.GetEnvironmentVariable("IS_SSL"));
        }

        public static void AddContext(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<AppDbContext>();
        }

        public static void AddBuilderHelpers(this WebApplicationBuilder builder)
        {
            builder.Services.AddHttpClient<MailHelper>();
            builder.Services.AddTransient<UploadHelper>();
        }

        public static void AddBuilderServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<IAuthService, AuthService>();
            
            builder.Services.AddTransient<IUserService, UserService>();
            builder.Services.AddTransient<IUserRepository, UserRepository>();

            builder.Services.AddTransient<ICategoryService, CategoryService>();
            builder.Services.AddTransient<ICategoryRepository, CategoryRepository>();
            
            builder.Services.AddTransient<IDashboardService, DashboardService>();
            builder.Services.AddTransient<IDashboardRepository, DashboardRepository>();
            
            builder.Services.AddTransient<IOperationService, OperationService>();
            builder.Services.AddTransient<IOperationRepository, OperationRepository>();
            
            builder.Services.AddTransient<IBankService, BankService>();
            builder.Services.AddTransient<IBankRepository, BankRepository>();


            Account account = new(
                Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME"),
                Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY"),
                Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            );
            builder.Services.AddSingleton(new Cloudinary(account));
        }
    }
}