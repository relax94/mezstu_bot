using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace mezstu_backend.Models
{
    public class Question
    {
        public ObjectId _id;
        public int PublicId;
        public string Text;
        public DateTime CreatedAt;

        [BsonIgnoreIfNull]
        public double? TextMatchScore { get; set; }
    }
}