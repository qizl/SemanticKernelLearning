using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace LLMDemos.Plugins
{
    internal class DemoPlugin
    {
        private readonly IConfiguration _config;

        public DemoPlugin(IConfiguration config)
        {
            _config = config;
        }

        [KernelFunction("get_weather_for_city")]
        [Description("获取指定城市的天气")]
        public string GetWeatherForCity(string cityName)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"（{nameof(GetWeatherForCity)}：正在查询天气...{cityName}）");

            return $"{cityName} 25°,天气晴朗。";
        }

        [KernelFunction("save_to_database")]
        [Description("将聊天记录保存到数据库")]
        public bool Save2Db(string content)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"（{nameof(Save2Db)}：正在将内容保存到数据库...{content}）");

            return true;
        }

        [KernelFunction("search_web")]
        [Description("搜索网络数据")]
        public async Task<string> SearhWebAsync(string keywords, int resultCount = 3)
        {
            var defaultForegroundColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine($"（{nameof(SearhWebAsync)}：正在搜索网络数据...{keywords}）");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["BochaAI:ApiKey"]}");

            var requestBody = new
            {
                query = keywords,
                freshness = "noLimit",
                summary = true,
                count = resultCount
            };

            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(new Uri(_config["BochaAI:EndPoint"]!), jsonContent);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"（{nameof(SearhWebAsync)}：{content}）");
            Console.ForegroundColor = defaultForegroundColor;

            return content;
        }

        [KernelFunction("get_web_uri_content")]
        [Description("获取网页内容")]
        public async Task<string> GetUriContentAsync(string uri)
        {
            var defaultForegroundColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.Write($"（{nameof(GetUriContentAsync)}：正在获取网页内容...{uri}）");

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"（{nameof(GetUriContentAsync)}：{content}）");
            Console.ForegroundColor = defaultForegroundColor;

            return content;
        }
    }
}
