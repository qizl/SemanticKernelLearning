using LLMDemos.Demo;
using Microsoft.Extensions.Configuration;
using System.Net;

var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json")
    .Build();

var modelId = config["LLM:ModelId"]!;
var apiKey = config["LLM:ApiKey"]!;
var endPoint = new Uri(config["LLM:EndPoint"]!);

//await Demo1_LLM.Run(modelId, apiKey, endPoint);
//await Demo1_LLM.RunEAI(modelId, apiKey, endPoint);

//await Demo2_Kernel_CreateFunctionFromPrompt.RunAsync(modelId, apiKey, endPoint);
//await Demo3_Kernel_CreateFunctionFromMethod.RunAsync(config, modelId, apiKey, endPoint);

//await Demo4_MCP.RunAsync(modelId, apiKey, endPoint);

//await Demo5_Agent.RunAgentAsync(modelId, apiKey, endPoint);
//await Demo5_Agent.RunAgentGroupChatAsync(config, modelId, apiKey, endPoint);

await Demo6_RAG.RunAsync(config, modelId, apiKey, endPoint);