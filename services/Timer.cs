using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutomationBot;
using DatabaseServiceNameSpace;
using LessonNotificationNameSpace;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class MyBackgroundService : BackgroundService
{
    private readonly ILogger<MyBackgroundService> _logger;
    private readonly Bot _bot;
    private readonly DatabaseService _databaseService;
    private readonly List<LessonNotification> _notifications;

    public MyBackgroundService(ILogger<MyBackgroundService> logger)
    {
        _logger = logger;

        string token = "8000158802:AAHpfi5UsfQddRNIvC1dIYqeXUD6MON-ZSw";
        var cts = new CancellationTokenSource();

        _databaseService = new DatabaseService("GIRLBOSS", "IFNTUNG_SCHEDULE");
        _bot = new Bot(token, cts, _databaseService);

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bot is running...");

        await _databaseService.OpenConnectionAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

                List<string> userIds = await _databaseService.GetTelegramUsernamesAsync();
                foreach (var userId in userIds)

                {

                    bool isConnected = await _databaseService.GetUserConnection(userId.ToString());
                    if (isConnected)
                    {
                        string fakeTime = "15:50";
                        TimeSpan fakeParsedTime = TimeSpan.Parse(fakeTime);
                        var notifications = await _databaseService.GetNotificationDataAsync();

                        foreach (var (telegramId, lessonName, lessonTime) in notifications)
                        {
                            if (lessonTime.TimeOfDay == fakeParsedTime)
                            {
                                string message = $"Reminder: The lesson '{lessonName}' is scheduled for {lessonTime}.";
                                await _bot.SendTextMessageAsync(telegramId, message);

                                string updateQuery = $@"
            UPDATE ul
            SET ul.notified = 1
            FROM User_Lessons ul
            INNER JOIN Users u ON ul.user_id = u.id
            INNER JOIN Lessons l ON ul.lesson_id = l.id
            WHERE u.telegram_id = '{telegramId}'
              AND l.name = '{lessonName}';";

                                try
                                {
                                    await _databaseService.ExecuteQueryAsync(updateQuery);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error updating User_Lessons: {ex.Message}");
                                }
                            }
                        }


                    }


                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in notification loop: {ex.Message}");
            }

            await Task.Delay(5000, stoppingToken); // Check every 5 seconds
        }

        _logger.LogInformation("Background service stopping.");
    }
}
