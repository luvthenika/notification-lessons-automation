using Microsoft.Data.SqlClient;
namespace DatabaseServiceNameSpace
{
    public class DatabaseService : IDisposable
    {
        private readonly string _server;
        private readonly string _database;
        private SqlConnection? _connection;

        public DatabaseService(string server, string database)
        {
            _server = server;
            _database = database;

        }

        private string BuildConnectionString()
        {
            return $"Server={_server};Database={_database};Trusted_Connection=True;TrustServerCertificate=True;"; // Adjust as needed for authentication
        }

        public async Task OpenConnectionAsync()
        {
            if (_connection == null)
            {
                _connection = new SqlConnection(BuildConnectionString());
                await _connection.OpenAsync();
            }
            else if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        public async Task ExecuteQueryAsync(string query)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("Connection is not open.");
            }

            await using var command = new SqlCommand(query, _connection);
            await command.ExecuteNonQueryAsync();
        }
        public async Task<List<int>> GetUserIdsAsync()
        {
            var userIds = new List<int>();
            string query = "SELECT id FROM Users";

            try
            {
                using (var command = new SqlCommand(query, _connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            userIds.Add(reader.GetInt32(0)); // Assuming `id` is the first column
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user IDs: {ex.Message}");
            }

            return userIds;
        }
        public async Task<int> ExecuteQueryAndReturnIdAsync(string query)
        {
            int insertedId = -1; // Default value in case of failure

            try
            {
                using (var command = new SqlCommand(query, _connection))
                {
                    // Execute the query and retrieve the inserted ID
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            insertedId = reader.GetInt32(0); // Assuming `id` is the first column in the output
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query and retrieving ID: {ex.Message}");
            }

            return insertedId;
        }

        public async Task<List<string>> GetTelegramUsernamesAsync()
        {
            var usernames = new List<string>();
            string query = "SELECT telegram_id FROM Users";

            using (SqlConnection connection = new SqlConnection(BuildConnectionString()))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            usernames.Add(reader["telegram_id"].ToString());
                        }
                    }
                }
            }
            Console.WriteLine(usernames);
            return usernames;
        }
        public async Task<List<(string TelegramId, string LessonName, DateTime LessonTime)>> GetNotificationDataAsync()
        {
            var results = new List<(string TelegramId, string LessonName, DateTime LessonTime)>();
            string query = @"
        SELECT Users.telegram_id , Lessons.name , Lessons.time
FROM Users INNER JOIN User_Lessons ON Users.id = User_Lessons.user_id
INNER JOIN Lessons ON Lessons.id = User_Lessons.lesson_id
WHERE  User_Lessons.notified = 0 AND Users.Connected = 1
ORDER BY Lessons.time ASC;";

            try
            {
                using (var command = new SqlCommand(query, _connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string telegramId = reader["telegram_id"].ToString();
                            string lessonName = reader["name"].ToString();
                            DateTime lessonTime = DateTime.Parse(reader["time"].ToString());

                            results.Add((telegramId, lessonName, lessonTime));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
            }

            return results;
        }

        public async Task<bool> GetUserConnection(string telegramId)
        {
            bool connected = false;

            string query = "SELECT connected FROM Users WHERE telegram_id = @TelegramId";

            using (SqlConnection connection = new SqlConnection(BuildConnectionString()))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("telegramId", telegramId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            connected = reader.GetBoolean(reader.GetOrdinal("connected"));
                        }
                    }
                }
            }

            Console.WriteLine($"Connected: {connected}");
            return connected;
        }


        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}

