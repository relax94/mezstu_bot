using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using mezstu_backend.Providers;

namespace mezstu_backend.Models
{
    public class Command
    {

        private Dictionary<string, BotCommands> commands = new Dictionary<string, BotCommands>()
        {
            ["/rand"] = BotCommands.NextQuestion,
            ["/search"] = BotCommands.NextQuestionByNumber,
            ["А"] = BotCommands.Answer,
            ["Б"] = BotCommands.Answer,
            ["В"] = BotCommands.Answer,
            ["Г"] = BotCommands.Answer,
            ["Д"] = BotCommands.Answer,
            ["/ftxt"] = BotCommands.FullTextSearch, 
            ["/seek"] = BotCommands.SeekCursor,
            ["/stat"] = BotCommands.GetAnswer
        };

    public Command(string fullCommandText)
        {
           if(!string.IsNullOrEmpty(fullCommandText))
                this.ParseCommand(fullCommandText);
        }

        private void ParseCommand(string input)
        {
            var iargs = input.Split(' ');
            if (iargs != null && iargs.Length > 0)
            {
                this.CommandText = iargs[0];
                this.CommandVar = ConvertToEnum(this.CommandText);
                if (this.CommandVar != BotCommands.FullTextSearch)
                    this.Arguments = iargs;
                else
                {
                    int firstIndex = input.IndexOf(' ');
                    if (firstIndex > 0)
                    {
                        this.Arguments = new string[2] {this.CommandText,input.Substring(firstIndex, input.Length - firstIndex)};
                    }
                }
            }
        }

        public string CommandText { get; set; }
        public BotCommands CommandVar { get; set; }
        public string[] Arguments { get; set; }

        private BotCommands ConvertToEnum(string commandString)
        {
            if(commands.ContainsKey(commandString))
                return commands[commandString];
            else 
                return BotCommands.UnknowCommand;
        }
    }
}