using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using mezstu_backend.Models;
using mezstu_backend.Providers;
using Microsoft.Owin;
using MongoDB.Driver;
using Owin;
using Telegram.Bot.Types;

[assembly: OwinStartupAttribute(typeof(mezstu_backend.Startup))]
namespace mezstu_backend
{
    public partial class Startup
    {

        public void Configuration(IAppBuilder app)
        {
            BotHelper.Context.BotHandleCommands();
            ConfigureAuth(app);   
        }

   
    }
}
