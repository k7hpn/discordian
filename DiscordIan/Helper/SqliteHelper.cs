using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;

namespace DiscordIan.Helper
{
    public class SqliteHelper
    {
        public static int GetTableCount(string table)
        {
            using (var conn = new SqliteConnection("Data Source=/db/bot.db"))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText =
                    @$" select count(*) from {table}; ";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var count = reader.GetString(0);

                        if (int.TryParse(count, out int response))
                        {
                            return response;
                        }
                    }
                }

                throw new Exception("Value not returned");
            }
        }

        public static void InsertWeather(string id, string name, string location)
        {
            using (var conn = new SqliteConnection("Data Source=/db/bot.db"))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText =
                    @$" delete from weather where id = {id}; insert into weather (id, name, location) values ({id}, '{name}', '{location}'); ";

                command.ExecuteNonQuery();
                conn.Close();
            }
        }

        public static string SelectWeatherDefault(string id)
        {
            using (var conn = new SqliteConnection("Data Source=/db/bot.db"))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText =
                    @$" select location from weather where id = {id} LIMIT 1; ";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader.GetString(0);
                    }
                }

                throw new KeyNotFoundException("Value not returned");
            }
        }
    }
}
