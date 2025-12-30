using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDBHelpers
{
    /// <summary>
    /// Interface which lets a class indicate that it has an ID which can be used to store it in MongoDB
    /// </summary>
    public interface IHasIdentifier<TId, out TSelf> where TSelf : IHasIdentifier<TId, TSelf>
    {
        /// <summary>
        /// The identifier
        /// </summary>
        TId Id { get; }

        /// <summary>
        /// Returns this object with the <see cref="Id"/> set to <paramref name="id"/>.
        /// </summary>
        TSelf WithId(TId id);
    }

    /// <summary>
    /// Extensions, mostly on <see cref="IMongoCollection"/>, to make queries/update easier.
    /// </summary>
    public static class MongoDbExtensions
    {
        /// <summary>
        /// Insert or update the given <paramref name="obj"/> into the <paramref name="collection"/>.
        /// </summary>
        /// <returns>
        /// True if the object was inserted or updated, false if it already existed and there were no updates to make.
        /// This is NOT a mark of success.
        /// </returns>
        public static async Task<bool> UpsertAsync<TObject, TId>(this IMongoCollection<TObject> collection, TObject obj)
            where TObject : IHasIdentifier<TId, TObject>
        {
            if (EqualityComparer<TId>.Default.Equals(obj.Id, default))
            {
                // If the ID has the default value, then we always want to do an insert
                // and let Mongo create the ID and assign it back to the C# object.
                await collection.InsertOneAsync(obj);
                return true;
            }
            else
            {
                // If the ID is not empty, that's still no guarantee that the object is in the db
                // (since we might have generated the id programmatically)
                // Therefore, we need to do an upsert (rather than something like a replace).
                ReplaceOneResult? result = await collection.ReplaceOneAsync(o => o.Id!.Equals(obj.Id), obj, new ReplaceOptions { IsUpsert = true });
                return result.ModifiedCount > 0;
            }
        }

        /// <summary>
        /// Insert or update the given <paramref name="obj"/> into the <paramref name="collection"/>.
        /// </summary>
        /// <returns>
        /// True if the object was inserted or updated, false if it already existed and there were no updates to make.
        /// This is NOT a mark of success.
        /// </returns>
        [Obsolete("Use in favor of UpsertAsync if possible")]
        public static bool Upsert<TObject, TId>(this IMongoCollection<TObject> collection, TObject obj)
            where TObject : IHasIdentifier<TId, TObject>
        {
            if (EqualityComparer<TId>.Default.Equals(obj.Id, default))
            {
                // If the ID has the default value, then we always want to do an insert
                // and let Mongo create the ID and assign it back to the C# object.
                collection.InsertOne(obj);
                return true;
            }
            else
            {
                // If the ID is not empty, that's still no guarantee that the object is in the db
                // (since we might have generated the id programmatically)
                // Therefore, we need to do an upsert (rather than something like a replace).
                ReplaceOneResult result = collection.ReplaceOne(o => o.Id!.Equals(obj.Id), obj, new ReplaceOptions { IsUpsert = true });
                return result.ModifiedCount > 0;
            }
        }

        /// <summary>
        /// Delete an object with the given  <paramref name="id"/> from the <paramref name="collection"/>.
        /// </summary>
        public static async Task<DeleteResult> DeleteAsync<TObject, TId>(this IMongoCollection<TObject> collection, TId id)
            where TObject : IHasIdentifier<TId, TObject> => await collection.DeleteOneAsync(Builders<TObject>.Filter.Eq("_id", id));

        /// <summary>
        /// Update the given <paramref name="obj"/> into the <paramref name="collection"/>.
        /// </summary>
        /// <remarks>
        /// Note that this replaces the whole object, so it's not good for limited property list updated
        /// </remarks>
        public static async Task UpdateAsync<TObject, TId>(this IMongoCollection<TObject> collection, TObject obj)
            where TObject : IHasIdentifier<TId, TObject> => await collection.ReplaceOneAsync(Builders<TObject>.Filter.Eq("_id", obj.Id), obj);

        /// <summary>
        /// Find all objects in the given <paramref name="collection"/>.
        /// </summary>
        [Obsolete("Use in favor of FindAllAsync if possible")]
        public static List<TObject> FindAll<TObject>(this IMongoCollection<TObject> collection) => collection.Find(_ => true).ToList();

        /// <summary>
        /// Find all objects in the given <paramref name="collection"/>.
        /// </summary>
        public static async Task<List<TObject>> FindAllAsync<TObject>(this IMongoCollection<TObject> collection) => await (await collection.FindAsync(_ => true)).ToListAsync();

        /// <summary>
        /// Find an object with the given <paramref name="id"/> in the given <paramref name="collection"/>.
        /// </summary>
        public static TObject? FindById<TObject, TId>(this IMongoCollection<TObject> collection, TId id) => collection.Find(Builders<TObject>.Filter.Eq("_id", id)).FirstOrDefault();

        /// <summary>
        /// Find an object with the given <paramref name="id"/> in the given <paramref name="collection"/>.
        /// </summary>
        public static async Task<TObject?> FindByIdAsync<TObject, TId>(this IMongoCollection<TObject> collection, TId id) => (await collection.FindAsync(Builders<TObject>.Filter.Eq("_id", id))).FirstOrDefault();

        /// <summary>
        /// Find an object that matches the given <paramref name="filter"/> in the given <paramref name="collection"/>.
        /// </summary>
        public static async Task<List<TObject>> FindByConditionAsync<TObject>(this IMongoCollection<TObject> collection, Expression<Func<TObject, bool>> filter) => await (await collection.FindAsync<TObject>(filter)).ToListAsync();

        /// <summary>
        /// Returns a collection based on a distinct field as specified in <paramref name="distinct"/>, and ordered by <paramref name="orderedBy"/>.
        /// </summary>
        public static async Task<List<TObject>> FindDistinctOrderedByDescending<TObject, TKey>(this IMongoCollection<TObject> collection, Expression<Func<TObject, object>> orderedBy, Expression<Func<TObject, TKey>> distinct)
        {
            return await collection.Aggregate() // Create an aggregate collection (where we can chain expressions)
                .SortByDescending(orderedBy) // Order as the user desires
                .Group(distinct, g => g.First()) // Create a group based on a field, and then select the first item in the group
                .ToListAsync();
        }
    }
}
