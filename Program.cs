using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AutomationBot;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    private static readonly string token = "8000158802:AAHpfi5UsfQddRNIvC1dIYqeXUD6MON-ZSw";

    private static string FilePath = "text.json";
    public static Dictionary<string, string>? Phrases { get; private set; }

    public static string groupId = "-1256";
    public static string groupName = "";



    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHostedService<MyBackgroundService>();
        var app = builder.Build();
        app.Run();

    }
}
