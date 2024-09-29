using Npgsql;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        string connectionString = "Host=localhost;Username=postgres;Password=bduni23;Database=databaseJOKES";

        while (true)
        {
            Console.WriteLine("Enter joke type (general, programming, knock-knock) or 'exit' to quit:");
            string jokeType = Console.ReadLine()?.ToLower();

            if (jokeType == "exit"){break;}

            string apiUrl = jokeType switch
            {
                "programming" => "https://official-joke-api.appspot.com/jokes/programming/random",
                "general" => "https://official-joke-api.appspot.com/jokes/general/random",
                "knock-knock" => "https://official-joke-api.appspot.com/jokes/knock-knock/random",
                _ => "https://official-joke-api.appspot.com/random_joke"
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string responseBody = await client.GetStringAsync(apiUrl);
                    JToken joke = responseBody.StartsWith("[")
                        ? JArray.Parse(responseBody)[0]
                        : JObject.Parse(responseBody);

                    int jokeId = (int)joke["id"];
                    string type = joke["type"].ToString();
                    string setup = joke["setup"].ToString();
                    string punchline = joke["punchline"].ToString();

                    Console.WriteLine("Joke:");
                    Console.WriteLine(setup);
                    Console.WriteLine(punchline);

                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("INSERT INTO jokes (joke_id, type, setup, punchline) VALUES (@joke_id, @type, @setup, @punchline)", conn))
                        {
                            cmd.Parameters.AddWithValue("joke_id", jokeId);
                            cmd.Parameters.AddWithValue("type", type);
                            cmd.Parameters.AddWithValue("setup", setup);
                            cmd.Parameters.AddWithValue("punchline", punchline);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine("\nJokes from database:");
                    using (var conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("SELECT id, type, setup, punchline FROM jokes", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine($"ID: {reader["id"]}, Type: {reader["type"]}, Setup: {reader["setup"]}, Punchline: {reader["punchline"]}");
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Console.WriteLine($"JSON parsing error: {e.Message}");
            }
        }
    }
}
