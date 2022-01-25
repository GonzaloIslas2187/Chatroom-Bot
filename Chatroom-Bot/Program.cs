using Chatroom_Bot.Entities;
using Chatroom_Bot.Mapper;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TinyCsvParser;

namespace Chatroom_Bot
{
    internal class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static HubConnection hubConnection;
        private static Uri uri;

        static async Task Main(string[] args)
        {
            await CreateConnection();

            await ReadMessages();

            Console.WriteLine("Starting...");
            Console.WriteLine("Press enter to finish...");
            Console.ReadLine();
        }

        private static async Task CreateConnection()
        {
            Uri.TryCreate("https://localhost:5001/api", UriKind.RelativeOrAbsolute, out uri);

            try
            {
                var loginBot = await Login();
                hubConnection = new HubConnectionBuilder().WithUrl(uri + "/Chatroom", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(loginBot.Token);
                }).Build();
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid API");
                await CreateConnection();
            }
        }

        private static async Task<LoginInfo> Login()
        {
            var body = new
            {
                Email = "TestBot@Testbot.com",
                Password = "Password1!"
            };

            var todoItemJson = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(uri + "/Credential/Login", todoItemJson);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var loginResult = JsonSerializer.Deserialize<LoginInfo>(result, options);

            Console.WriteLine("Token: " + loginResult.Token);
            Console.WriteLine("UserName: " + loginResult.UserName);

            return loginResult;
        }

        private static async Task ReadMessages()
        {
            hubConnection.On<Message>("MessageBot", async (message) =>
            {
                var response = await GetResponseToCommand(message);
                await hubConnection.InvokeAsync("PublishMessage", response, cancellationToken: default);
            });

            await hubConnection.StartAsync();
        }

        private static async Task<string> GetResponseToCommand(Message message)
        {
            var messageContent = message.Content.Split('=');

            switch (messageContent[0])
            {
                case "/stock":
                    return await CallApi(messageContent[1]);
                case "/help":
                default:
                    return "Type /stock=<stock_code>";
            }
        }

        private static async Task<string> CallApi(string filename)
        {
            var url = string.Format("https://stooq.com/q/l/?s={0}&f=sd2t2ohlcv&h&e=csv", filename);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/csv"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            await using var content = await response.Content.ReadAsStreamAsync();

            var csvParserOptions = new CsvParserOptions(true, ',');
            var csvMapper = new StockMapper();
            var csvParser = new CsvParser<Stock>(csvParserOptions, csvMapper);

            var details = csvParser.ReadFromStream(content, Encoding.ASCII)
                            .Select(r => r.Result)
                            .First();

            if (details.Close != "N/D")
            {
                return $"{details.Symbol.ToUpper()} quote is ${details.Close} per share";
            }

            return "Invalid Stock code";
        }
    }
}