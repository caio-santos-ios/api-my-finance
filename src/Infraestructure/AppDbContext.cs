using MongoDB.Driver;
using api_finances.src.Models;

namespace api_finances.src.Infraestructure
{
    public class AppDbContext
    {
        public static string? ConnectionString { get; set; }
        public static string? DatabaseName { get; set; }
        public static bool IsSSL { get; set; }
        private IMongoDatabase Database { get; }

        public AppDbContext()
        {
            try
            {
                MongoClientSettings mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(ConnectionString));
                if (IsSSL)
                {
                    mongoClientSettings.SslSettings = new SslSettings
                    {
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                    };
                }

                var mongoClient = new MongoClient(mongoClientSettings);
                Database = mongoClient.GetDatabase(DatabaseName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to connect to database. Error: {ex.Message}");
            }
        }

        public IMongoCollection<User> Users => Database.GetCollection<User>("users");
        public IMongoCollection<Category> Categories => Database.GetCollection<Category>("categories");
        public IMongoCollection<Bank> Banks => Database.GetCollection<Bank>("banks");
        public IMongoCollection<Operation> Operations => Database.GetCollection<Operation>("operations");
        public IMongoCollection<Attachment> Attachments => Database.GetCollection<Attachment>("attachments");
        public IMongoCollection<Importation> Importations => Database.GetCollection<Importation>("importations");
    }
}

