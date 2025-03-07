using LLMDemos.Demo;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json")
    .Build();

var modelId = config["LLM:ModelId"];
var apiKey = config["LLM:ApiKey"];
var endPoint = new Uri(config["LLM:EndPoint"]);

await LLMDemo.RunEAI(modelId, apiKey, endPoint);
//await LLMDemo.Run(modelId, endPoint);

//await KernelDemo.Run(modelId, endPoint);
