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
            using (var conn = new SqliteConnection(GetDataSource()))
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
            }

            return string.Empty;
        }

        public static string[] GetQuotes(string keyword = null)
        {
            keyword = keyword.IsNullOrEmptyReplace("%");
            var response = new List<string>();

            var query = keyword == "%"
                ? @$" select quote from quotes; "
                : @$" select quote from quotes where quote like '%{keyword}%'; ";

            using (var conn = new SqliteConnection(GetDataSource()))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = query;

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

        private static string GetDataSource()
        {
            return "Data Source=/host/bot.db";
        }
    }
}
