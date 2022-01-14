using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

namespace DiscordIan.Helper
{
    public class SqliteHelper
    {
        public static int GetTableCount(string table)
        {
            using (var conn = new SqliteConnection(GetDataSource()))
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
            ScrubInput(location);

            using (var conn = new SqliteConnection(GetDataSource()))
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
            ScrubInput(id);

            var cmd = @$" select location from weather where id = {id} LIMIT 1; ";

            return GetOneValue(cmd);
        }

        public static string[] GetQuotes(string keyword = null)
        {
            ScrubInput(keyword);
            keyword = keyword.IsNullOrEmptyReplace("%");

            var cmd = keyword == "%"
                ? @$" select quote from quotes; "
                : @$" select quote from quotes where quote like '%{keyword}%'; ";

            return GetManyValues(cmd);
        }

        private static string GetDataSource()
        {
            return "Data Source=/host/bot.db";
        }

        private static void ScrubInput(string input)
        {
            if (input.Contains(";")
                || input.Contains("drop")
                || input.Contains("delete")
                || input.Contains("'")
                || input.Contains("update")
                || input.Contains("insert"))
            {
                throw new Exception("Assfuckery detected.");
            }
        }

        private static string GetOneValue(string cmd)
        {
            using (var conn = new SqliteConnection(GetDataSource()))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = cmd;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader.GetString(0);
                    }
                }
            }

            return string.Empty;
        }

        private static string[] GetManyValues(string cmd)
        {
            var response = new List<string>();

            using (var conn = new SqliteConnection(GetDataSource()))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = cmd;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        response.Add(reader.GetString(0));
                    }
                }
            }

            return response.ToArray();
        }
    }
}
