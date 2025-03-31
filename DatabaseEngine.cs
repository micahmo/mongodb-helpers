using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDBHelpers
{
    public class DatabaseEngine
    {
        public static string? ConnectionString { get; set; }

        public static IMongoDatabase DatabaseInstance
        {
            get
            {
                return _databaseInstance ??= new Func<IMongoDatabase>(() =>
                {
                    var clientSettings = MongoClientSettings.FromConnectionString(ConnectionString);
                    clientSettings.AllowInsecureTls = ConnectionString?.Contains("tlsAllowInvalidCertificates=true") == true;
                    MongoClient mongoClient = new MongoClient(clientSettings);
                    return mongoClient.GetDatabase("yth");
                })();
            }
        }
        private static IMongoDatabase? _databaseInstance;

        public static string? TestConnection()
        {
            try
            {
                DatabaseInstance.ListCollectionNames();
                return default;
            }
            catch (Exception ex)
            {
                _databaseInstance = null;
                return ex.Message;
            }
        }

        public static async Task<string?> TestConnectionAsync()
        {
            string? result = default;

            await Task.Run(() =>
            {
                try
                {
                    DatabaseInstance.ListCollectionNames();
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
            });

            if (!string.IsNullOrEmpty(result))
            {
                _databaseInstance = null;
            }

            return result;
        }

        public static void Reset()
        {
            _databaseInstance = null;
        }
    }
}
