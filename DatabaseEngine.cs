using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDBHelpers
{
    /// <summary>
    /// This class represents the engine that can be used to interact with MongoDB
    /// </summary>
    public class DatabaseEngine
    {
        /// <summary>
        /// The MongoDB connection string. Set before accessing the <see cref="DatabaseInstance"/>.
        /// </summary>
        public static string? ConnectionString { get; set; }

        /// <summary>
        /// The MongoDB instance. Set the <see cref="ConnectionString"/> before accessing.
        /// </summary>
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

        /// <summary>
        /// Check whether we can connect to MongoDB with the given <see cref="ConnectionString"/>
        /// </summary>
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

        /// <summary>
        /// Check whether we can connect to MongoDB with the given <see cref="ConnectionString"/>
        /// </summary>
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

        /// <summary>
        /// Reset the instance so we can reconnect with a new <see cref="ConnectionString"/>
        /// </summary>
        public static void Reset()
        {
            _databaseInstance = null;
        }
    }
}
