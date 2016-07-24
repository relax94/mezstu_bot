using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using Telegram.Bot;

namespace mezstu_backend.Models
{
    public class Answer
    {
        public ObjectId _id { get; set; }
        public Telegram.Bot.Types.User User { get; set; }

        public int PublicId
        {
            get; set; }
        public long ChatId { get; set; }
        public char AnswerChar { get; set; }
        public DateTime AnswerDateTime { get; set; }

    }
}