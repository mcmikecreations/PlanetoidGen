using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Documents;
using PlanetoidGen.Contracts.Repositories.Documents;
using PlanetoidGen.Domain.Models.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Documents
{
    public class DocumentBaseRepository<T> : IDocumentBaseRepository<T> where T : DocumentBase
    {
        protected IMongoDatabase Database { get; }
        protected IMongoCollection<T> Collection { get; }

        public DocumentBaseRepository(
            IOptions<DocumentDbOptions> dbOptions)
        {
            var collectionName = dbOptions.Value.CollectionName;

            var mongoClient = new MongoClient(
                dbOptions.Value.ConnectionString);

            Database = mongoClient.GetDatabase(
                dbOptions.Value.DatabaseName);

            var collectionExists = Database.ListCollectionNames().ToList().Contains(collectionName);
            if (collectionExists == false)
            {
                Database.CreateCollection(collectionName);
            }

            Collection = Database.GetCollection<T>(
                collectionName);
        }

        /// <summary>
        /// Gets all documents
        /// </summary>
        /// <returns>Success result with the list of the documents</returns>
        public virtual async ValueTask<Result<List<T>>> GetAll()
        {
            return await Execute(async () =>
            {
                var result = await Collection.Find(_ => true).ToListAsync();
                return Result<List<T>>.CreateSuccess(result);
            });
        }

        /// <summary>
        /// Gets a document by ID
        /// </summary>
        /// <param name="id">The ID of the document</param>
        /// <returns>Success result with model if the document was found; Success result with null value if the document was not found</returns>
        public virtual async ValueTask<Result<T>> GetById(string id)
        {
            return await Execute(async () =>
            {
                var result = await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
                return Result<T>.CreateSuccess(result);
            });
        }

        /// <summary>
        /// Creates a document
        /// </summary>
        /// <param name="item">The document model</param>
        /// <returns>Success result with 'true' value; Failure if the document cannot be created</returns>
        public virtual async ValueTask<Result<bool>> Create(T item)
        {
            return await Execute(async () =>
            {
                await Collection.InsertOneAsync(item);
                return Result<bool>.CreateSuccess(true);
            });
        }

        /// <summary>
        /// Updates a document by ID
        /// </summary>
        /// <param name="id">The ID of the document</param>
        /// <returns>Success result with 'true' value if the document was updated; Failure result if the document was not found or was not updated</returns>
        public virtual async ValueTask<Result<bool>> Update(string id, T item)
        {
            return await Execute(async () =>
            {
                var existResult = await Exists(id);

                if (!existResult.Success)
                {
                    return Result<bool>.CreateFailure(existResult);
                }
                else if (!existResult.Data)
                {
                    return Result<bool>.CreateFailure($"Record with id {id} was not found.");
                }

                var result = await Collection.ReplaceOneAsync(x => x.Id == id, item);

                if (result.MatchedCount < 1)
                {
                    return Result<bool>.CreateFailure($"Record with id {id} could not be updated.");
                }

                return Result<bool>.CreateSuccess(true);
            });
        }

        /// <summary>
        /// Removes a document by ID
        /// </summary>
        /// <param name="id">The ID of the document</param>
        /// <returns>Success result with 'true' value if the document was deleted; Failure result if the record was not found or was not deleted</returns>
        public virtual async ValueTask<Result<bool>> Remove(string id)
        {
            return await Execute(async () =>
            {
                var existResult = await Exists(id);

                if (!existResult.Success)
                {
                    return Result<bool>.CreateFailure(existResult);
                }
                else if (!existResult.Data)
                {
                    return Result<bool>.CreateFailure($"Record with id {id} was not found.");
                }

                var result = await Collection.DeleteOneAsync(x => x.Id == id);

                return Result<bool>.CreateSuccess(true);
            });
        }

        /// <summary>
        /// Removes documents by IDs
        /// </summary>
        /// <param name="ids">The list of IDs of the documents</param>
        /// <returns>Success result with deleted records count. Will not fail if some ID was not deleted</returns>
        public virtual async ValueTask<Result<int>> RemoveAll(IEnumerable<string> ids)
        {
            return await Execute(async () =>
            {
                var result = await Collection.DeleteManyAsync(x => ids.Contains(x.Id));
                return Result<int>.CreateSuccess((int)result.DeletedCount);
            });
        }

        /// <summary>
        /// Checks whether the document exists in the collection
        /// </summary>
        /// <param name="id">The ID of the document</param>
        /// <returns>Success result with the boolean value</returns>
        public virtual async ValueTask<Result<bool>> Exists(string id)
        {
            return await Execute(async () =>
            {
                var result = await Collection.Find(x => x.Id == id).CountDocumentsAsync();
                return Result<bool>.CreateSuccess(result > 0);
            });
        }

        protected async ValueTask<Result<R>> Execute<R>(Func<ValueTask<Result<R>>> func)
        {
            try
            {
                return await func();
            }
            catch (MongoException e)
            {
                return Result<R>.CreateFailure(e);
            }
        }

        protected async ValueTask<Result> Execute(Func<ValueTask<Result>> func)
        {
            try
            {
                return await func();
            }
            catch (MongoException e)
            {
                return Result.CreateFailure(e);
            }
        }
    }
}
