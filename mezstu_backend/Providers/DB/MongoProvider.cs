using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using mezstu_backend.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace mezstu_backend.Providers.DB
{
    public class MongoProvider : Provider
    {
        private readonly MongoDB.Driver.MongoClient MongoClient;
        private readonly MongoDB.Driver.IMongoDatabase MongoDatabase;
        

        private const string MongoDatabaseName = "mezstu";
        private string MongoCollectionName = "test";
        private const string ArgExceptionString = "Connection String must be not null value";

       public MongoProvider(string connectionString)
        {
           if (!string.IsNullOrEmpty(connectionString))
           {
               MongoClient = new MongoClient(connectionString);
               MongoDatabase = MongoClient.GetDatabase(MongoDatabaseName);
           }
           else
               throw new ArgumentNullException(ArgExceptionString);
        }

        private bool ValidateConnection()
        {
            if (MongoClient != null && MongoDatabase != null)
                return true;
            return false;
        }

        private IMongoCollection<T> GetMongoCollection<T>(string collectionName)
        {
            return this.MongoDatabase.GetCollection<T>(collectionName);
        }

        public void SetSessionMongoCollectionName(string collectionName)
        {
            if (!string.IsNullOrEmpty(collectionName))
                this.MongoCollectionName = collectionName;
        }

        public override void Push<T>(T instance)
        {
            if (this.ValidateConnection())
                     this?.GetMongoCollection<T>(this.MongoCollectionName)?.InsertOneAsync(instance);  
        }

        

        public override async Task<IEnumerable<T>> Pop<T>(Expression<Func<T, bool>> predicate)
        {
            return await this?.GetMongoCollection<T>(this.MongoCollectionName)?.Find(predicate)?.ToListAsync() ?? null;
        }

        public override void Replace<T>(Expression<Func<T, bool>> predicate, T instance)
        {
            if (this.ValidateConnection())
            {
                this?.GetMongoCollection<T>(this.MongoCollectionName)?.ReplaceOneAsync(predicate, instance);
            }
        }

        public async Task<IEnumerable<T>>  FullTextSearch<T>(string phrase, int skip = 0, int limit = 3)
        {
            if (this.ValidateConnection())
            {
                //var filter = new BsonDocument(new Dictionary<string, object>()
                //{
                //    ["$text"] = new BsonDocument("$search", phrase),
                //    ["$score"] = new BsonDocument("$meta", "textScore"),
                //});

                //var textSearchQueryExact = Query.Matches("Text", phrase);
                //var textSearchQueryFullText = Query.Text(phrase);
                //var textSearchQuery = Query.Or(textSearchQueryFullText, textSearchQueryExact);
                

                //return await this?.GetMongoCollection<T>(this.MongoCollectionName)?.Find(textSearchQuery)

                var F = Builders<T>.Filter.Text(phrase);
                var P = Builders<T>.Projection.MetaTextScore("TextMatchScore");
                var S = Builders<T>.Sort.MetaTextScore("TextMatchScore");
                return await GetMongoCollection<T>(MongoCollectionName).Find(F).Project<T>(P).Sort(S).Skip(skip).Limit(limit).ToListAsync();
            }
            return null;
        }
    }
}