using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using mezstu_backend.Models;
using mezstu_backend.Providers.DB;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace mezstu_backend.Providers
{

    public enum BotCommands
    {
        NextQuestion,
        NextQuestionByNumber,
        SetAnswer,
        FullTextSearch,
        GetAnswer,
        Answer,
        SeekCursor,
        UnknowCommand
    }


    public class BotHelper
    {

        private static BotHelper InstanceBotHelper;
        private MongoProvider mongoProvider;
        private IReplyMarkup answerButtons;
        private Telegram.Bot.TelegramBotClient botClient;
        private Dictionary<long, int> sessionsQuestions;
        private Dictionary<long, int> searchCursor;

        public static BotHelper Context => InstanceBotHelper ?? (InstanceBotHelper = new BotHelper());

        BotHelper()
        {
            mongoProvider = new MongoProvider("mongodb://mezstu:mezstu@ds023425.mlab.com:23425/mezstu");
            answerButtons = GetAnswerKeyboard();
            botClient = new Telegram.Bot.TelegramBotClient("259874311:AAHFFPUt99OCFxTkCWrppzO2NGAYOWxVrwU");
            sessionsQuestions = new Dictionary<long, int>();
            searchCursor = new Dictionary<long, int>();
        }



        public void BotHandleCommands()
        {
            botClient.StartReceiving();
            botClient.OnMessage += HandleBotCommand;
        }


        private async Task<IEnumerable<T>> GetDocument<T>(string collectionName, Expression<Func<T, bool>> exp)
        {
            //  var filter = new BsonDocument("PublicId", new BsonDocument("$eq", qn));
            mongoProvider.SetSessionMongoCollectionName(collectionName);
            var documents = await mongoProvider.Pop(exp);
            return documents;
        }

        private async Task<T> GetFirstDocument<T>(string collectionName, Expression<Func<T, bool>> exp)
        {
            return (await GetDocument<T>(collectionName, exp)).FirstOrDefault();
        }

        private async Task<Answer> GetAnswer(long userId, int questionNum)
        {
            return await GetFirstDocument<Answer>("answers", a => a.User.Id.Equals(userId) && a.PublicId.Equals(questionNum));
        }

        private async Task<string> GetRandomQuestionText(long chatId)
        {
            var rand = new Random().Next(750);
            sessionsQuestions[chatId] = rand;
            return (await GetFirstDocument<Question>("questions", q => q.PublicId.Equals(rand)))?.Text ??
                          "Question number range [1-750]";
        }

        private async Task<string> GetQuestionTextByNumber(long chatId, Command command)
        {
            if (command != null)
            {
                int numberOfQuestion = 1;
                if (command.Arguments.Length >= 2 && int.TryParse(command.Arguments[1], out numberOfQuestion))
                {
                    sessionsQuestions[chatId] = numberOfQuestion;
                    return (await GetFirstDocument<Question>("questions", q => q.PublicId.Equals(numberOfQuestion)))?.Text ??
                           "Question number range [1-750]";
                }
                else
                {
                    return await GetRandomQuestionText(chatId);
                }
            }
            return "Smth. wrong";
        }

        private void SendAnswer(long chatId, string text)
        {
            botClient.SendTextMessageAsync(chatId, text, false, false, 0, GetAnswerKeyboard());
        }

        private async void SetAnswer(Telegram.Bot.Types.Message telegramMessage, Command command)
        {
            long chatId = telegramMessage.Chat.Id;
            int userId = telegramMessage.From.Id;
            if (sessionsQuestions.ContainsKey(chatId))
            {
                int numberOfQuestion = sessionsQuestions[chatId];

                mongoProvider.SetSessionMongoCollectionName("answers");
                Answer answer = await GetAnswer(userId, numberOfQuestion);
                if (answer == null)
                {
                    answer = new Answer()
                    {
                        AnswerChar = command.CommandText[0],
                        ChatId = chatId,
                        PublicId = numberOfQuestion,
                        User = telegramMessage.From,
                        AnswerDateTime = DateTime.Now
                    };
                    mongoProvider.Push<Answer>(answer);
                    SendAnswer(chatId, $"Yours answer {answer.AnswerChar}");

                }
                else
                {
                    string infoMessage = $"Yours answer changed from {answer.AnswerChar} to {command.CommandText[0]}";
                    answer.AnswerChar = command.CommandText[0];
                    answer.AnswerDateTime = DateTime.Now;
                    mongoProvider.Replace(a => a.User.Id.Equals(userId) && a.PublicId.Equals(numberOfQuestion), answer);
                    SendAnswer(chatId, infoMessage);
                }
            }
        }

        private async void GetQuestionStats(long chatId, Command command)
        {
            int numberOfQuestion = 0;
            if (command != null && command.CommandVar == BotCommands.GetAnswer && command.Arguments.Length >= 2)
            {
                if (int.TryParse(command.Arguments[1], out numberOfQuestion))
                {
                    GetQuestionAnswersCore(chatId, numberOfQuestion);
                }
                else
                {
                    SendAnswer(chatId, "WROOOONG!!!!");
                }
            }
            else if (sessionsQuestions.ContainsKey(chatId))
            {
                numberOfQuestion = sessionsQuestions[chatId];
                GetQuestionAnswersCore(chatId, numberOfQuestion);
            }
        }

        private async void GetQuestionAnswersCore(long chatId, int numberOfQuestion)
        {
            if (chatId > 0 && numberOfQuestion > 0)
            {
                IEnumerable<Answer> answers =
                    await GetDocument<Answer>("answers", a => a.PublicId.Equals(numberOfQuestion));
                if (answers != null && answers.Any())
                {
                    string response = string.Join("\n",
                        answers.GroupBy(g => g.AnswerChar).Select(group => $"{group.Key} - {group.Count()} ({string.Join(",", group.Select(x => $"{x.User.FirstName} {x.User.LastName}"))})"));
                    SendAnswer(chatId, response);
                }
                else
                {
                    SendAnswer(chatId, "Answers exists!!!");
                }
            }
        }

        private async void FullTextSearch(long chatId, Command command)
        {
            mongoProvider.SetSessionMongoCollectionName("questions");
            int skip = searchCursor.ContainsKey(chatId) ? searchCursor[chatId] : 0;
            var response = await mongoProvider.FullTextSearch<Question>(command.Arguments[1]?.Trim(), skip);

            foreach (var question in response)
            {
                SendAnswer(chatId, $"{question.Text} \n\n SearchScore = {question.TextMatchScore}");
            }
            Seek(chatId);
            // TODO SEND TO CLIENT
        }

        private void Seek(long chatId) {
            if (searchCursor.ContainsKey(chatId))
            {
                searchCursor[chatId] += 3;
            }
            else 
                searchCursor.Add(chatId, 3);
        }


        private async void HandleBotCommand(object sender, MessageEventArgs eventArgs)
        {
            if (eventArgs != null)
            {
                long chatId = eventArgs?.Message?.Chat?.Id ?? 1;
                Command command = new Command(eventArgs?.Message?.Text);

                switch (command.CommandVar)
                {
                    case BotCommands.NextQuestion:
                        SendAnswer(chatId, (await GetRandomQuestionText(chatId)));
                        break;
                    case BotCommands.NextQuestionByNumber:
                        SendAnswer(chatId, (await GetQuestionTextByNumber(chatId, command)));
                        break;
                    case BotCommands.Answer:
                        SetAnswer(eventArgs.Message, command);

                        break;
                    case BotCommands.GetAnswer:
                        GetQuestionStats(chatId, command);
                        break;
                    case BotCommands.UnknowCommand:
                        SendAnswer(chatId, "Unknow command !!!");
                        break;
                    case BotCommands.FullTextSearch:
                        FullTextSearch(chatId, command);
                        break;
                    case BotCommands.SeekCursor:
                        FullTextSearch(chatId, command);
                        break;


                }
            }

        }

        private IReplyMarkup GetAnswerKeyboard()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[5][]
            {
                new KeyboardButton[1] {"А"},
                new KeyboardButton[1] {"Б"},
                new KeyboardButton[1] {"В"},
                new KeyboardButton[1] {"Г"},
                new KeyboardButton[1] {"Д"},
            });
        }



    }
}