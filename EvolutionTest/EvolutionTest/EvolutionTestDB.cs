using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace EvolutionTest
{
	public class BotInfo
	{

	}

	public class EvolutionTestDB
	{
		public static readonly string TableName = "BotInfo";
		private string connectionString;

		public EvolutionTestDB(string dbName)
		{
			string dbPath = Path.Combine(Directory.GetCurrentDirectory(), $"{dbName}.sqlite");
			connectionString = "DataSource=" + dbPath;
			
			CreateTable();
		}

		private void CreateTable()
		{
			using (var connection = new SQLiteConnection(connectionString))
			{
				connection.Open();
				SQLiteCommand cmd = connection.CreateCommand();

				cmd.CommandText =
					$"CREATE TABLE IF NOT EXISTS {TableName} " +
					$"(tick INT, id INT, info TEXT)";
				cmd.ExecuteNonQuery();
			}
		}

		public void Insert(BotInfo[] bots, int tick)
		{
			string commandText = $"INSERT INTO {TableName} (tick, id, info) VALUES";

			/*int count = 0;
			foreach (BotInfo botInfo in bots)
			{
				string info = JsonConvert.SerializeObject(botInfo);
				commandText += $" ({tick}, {count}, '{info}'),";
				count++;
			}

			commandText = commandText.TrimEnd(',');*/

			string info = JsonConvert.SerializeObject(bots);
			commandText += $" ({tick}, {tick}, '{info}')";

			using (var connection = new SQLiteConnection(connectionString))
			{
				connection.Open();
				SQLiteCommand cmd = connection.CreateCommand();

				cmd.CommandText = commandText;
				cmd.ExecuteNonQuery();
			}
			
		}

		public virtual BotInfo[] Select(int tick)
		{
			using (var connection = new SQLiteConnection(connectionString))
			{
				connection.Open();
				SQLiteCommand cmd = connection.CreateCommand();

				cmd.CommandText = $"SELECT * FROM {TableName} WHERE tick = {tick}";

				using (SQLiteDataReader reader = cmd.ExecuteReader())
				{
					reader.Read();
					{
						BotInfo[] row = JsonConvert.DeserializeObject<BotInfo[]>((string)reader["info"]);
						return row;
					}
				}
			}
		}
	}
}
