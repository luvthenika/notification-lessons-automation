using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using GroupManagerNamespace;
using ScheduleManagerNamespace;
using DatabaseServiceNameSpace;
using System.Text.RegularExpressions;

namespace AutomationBot
{
    class Bot
    {
        private readonly TelegramBotClient _bot;
        private readonly CancellationTokenSource _cts;
        private readonly DatabaseService _databaseService;
        private static Bot _instance;

        public Bot(string token, CancellationTokenSource cts, DatabaseService databaseService)
        {
            _bot = new TelegramBotClient(token);
            _cts = cts;
            _databaseService = databaseService;

            _bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                cancellationToken: _cts.Token
            );
        }

        public static Bot GetInstance(string token, CancellationTokenSource cts, DatabaseService databaseService)
        {
            if (_instance == null)
            {
                _instance = new Bot(token, cts, databaseService);
            }
            return _instance;
        }
        public async Task SendTextMessageAsync(string chatId, string message)
        {
            await _bot.SendTextMessageAsync(chatId: chatId, text: message);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException apiException)
            {
                Console.WriteLine($"Telegram API Error:\n[{apiException.ErrorCode}]\n{apiException.Message}");
            }
            else
            {
                Console.WriteLine($"Unexpected Error: {exception.Message}");
            }
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message is not null)
                {
                    await OnMessage(update.Message);
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is not null)
                {
                    await OnCallbackQuery(update.CallbackQuery);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while processing update: {ex.Message}");
            }
        }

        private async Task OnMessage(Message msg)
        {
            if (msg.Text is not { } text || string.IsNullOrWhiteSpace(text))
            {
                await _bot.SendTextMessageAsync(msg.Chat.Id, "Invalid input. Please use /start to begin.");
                return;
            }

            if (text.StartsWith('/'))
            {
                await OnCommand(text, msg);
            }
            else
            {
                await HandleGroupConnection(msg, text);
            }
        }

        private async Task HandleGroupConnection(Message msg, string groupName)
        {
            try
            {
                bool isUserConnected = await _databaseService.GetUserConnection(msg.From.Id.ToString());

                if (isUserConnected)
                {
                    await _bot.SendTextMessageAsync(msg.Chat.Id, "You are already connected. Use /change to update your group.");
                    return;
                }

                GroupManager groupManager = new GroupManager();
                string groupId = await groupManager.getGroupValueId(groupName);

                string query = $"UPDATE Users SET connected = 1, groupName = '{groupName}', groupId = {groupId} WHERE telegram_id = {msg.From.Id}";


                await _databaseService.ExecuteQueryAsync(query);

                await _bot.SendTextMessageAsync(msg.Chat.Id, "You have successfully connected!");

                ScheduleManager scheduleManager = new ScheduleManager();
                List<Dictionary<string, string>> schedule = await scheduleManager.GetScheduleValue(groupId);
                foreach (var entry in schedule)
                {
                    string lessonName = entry["lesson_description"];
                    string lessonTime = entry["lesson_time"];
                    string pattern = @"\d{1,2}:\d{2}";
                    MatchCollection matches = Regex.Matches(lessonTime, pattern);
                    string startTime = matches[0].Value; // 8:00
                    DateTime lessonDateTime = DateTime.Today.Add(TimeSpan.Parse(startTime)); // Combines today's date with the time
                    string formattedDateTime = lessonDateTime.ToString("yyyy-MM-dd HH:mm");
                    if(lessonName.Length < 1){
                        continue;
                    }
                    string inserLessonsQuery = $"INSERT INTO Lessons (name, time) OUTPUT INSERTED.id VALUES ('{lessonName}', '{formattedDateTime}')";
                    int lessonId;

                    try
                    {
                        lessonId = await _databaseService.ExecuteQueryAndReturnIdAsync(inserLessonsQuery);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inserting lesson: {ex.Message}");
                        continue;
                    }

                    List<int> userIds = await _databaseService.GetUserIdsAsync();

                    foreach (int userId in userIds)
                    {
                        string inserLessonsAndUserQuery = $"INSERT INTO User_Lessons (user_id, lesson_id) VALUES ({userId}, {lessonId})";
                        try
                        {
                            await _databaseService.ExecuteQueryAsync(inserLessonsAndUserQuery);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error inserting into User_Lessons: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine(schedule);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleGroupConnection: {ex.Message}");
                await _bot.SendTextMessageAsync(msg.Chat.Id, "An error occurred while connecting. Please try again.");
            }
        }

        private async Task OnCommand(string command, Message msg)
        {
            switch (command.ToLower())
            {
                case "/start":
                    await _bot.SendTextMessageAsync(msg.Chat.Id, """
                        <b><u>Welcome to the Education Automation Bot</u></b>:
                        Here are the commands you can use:
                        /connect - Connect to the bot by entering your academic group.
                        /group - View your academic group.
                        /change - Change your academic group.
                        /stop - Stop Education Automation Bot.
                        """,
                        parseMode: ParseMode.Html,
                        replyMarkup: new ReplyKeyboardRemove()
                    );

                    try
                    {
                        string insertQuery = $"INSERT INTO Users (telegram_username, telegram_id, connected) VALUES ('{msg.From.Username}', {msg.From.Id.ToString()}, 0)";

                        await _databaseService.ExecuteQueryAsync(insertQuery);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while inserting user: {ex.Message}");
                    }
                    break;

                case "/connect":
                    await _bot.SendTextMessageAsync(msg.Chat.Id, "Please enter your academic group:");
                    break;

                case "/change":
                    await _bot.SendTextMessageAsync(msg.Chat.Id, "Please enter the new group you want to connect to:");
                    break;

                case "/stop":
                    await _bot.SendTextMessageAsync(msg.Chat.Id, "Thank you for using this bot. Goodbye!");
                    string query = "DELETE FROM Users";
                    await _databaseService.ExecuteQueryAsync(query);
                    break;

                default:
                    await _bot.SendTextMessageAsync(msg.Chat.Id, "Unknown command. Please use /start to see available options.");
                    break;
            }
        }

        private async Task OnCallbackQuery(CallbackQuery callbackQuery)
        {
            await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, $"You selected: {callbackQuery.Data}");
            await _bot.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Callback data: {callbackQuery.Data}");
        }
    }
}
