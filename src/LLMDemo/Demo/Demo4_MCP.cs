using LLMDemos.Demo.MCP;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;

#pragma warning disable SKEXP0010

namespace LLMDemos.Demo
{
    class Demo4_MCP
    {
        public static async Task RunAsync(string modelId, string apiKey, Uri endPoint)
        {
            // Prepare and build kernel
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(modelId, endPoint, apiKey);

            Kernel kernel = builder.Build();

            // Create an MCPClient for the GitHub server
            var githubMCPClient = await McpDotNetExtensions.GetGitHubToolsAsync().ConfigureAwait(false);
            //var mcpClient = await McpDotNetExtensions.GetFilesystemToolsAsync(new[] { @"D:\Download" }).ConfigureAwait(false);
            //var mcpClient = await McpDotNetExtensions.GetDockerToolsAsync().ConfigureAwait(false);

            // Retrieve the list of tools available on the GitHub server
            var githubTools = await githubMCPClient.ListToolsAsync().ConfigureAwait(false);
            //var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            //foreach (var tool in tools.Tools)
            //{
            //    Console.WriteLine($"{tool.Name}: {tool.Description}");
            //}
            //Console.WriteLine();

            // Add the MCP tools as Kernel functions
            var githubFunctions = await githubMCPClient.MapToFunctionsAsync().ConfigureAwait(false);
            kernel.Plugins.AddFromFunctions("GitHub", githubFunctions);
            //var functions = await mcpClient.MapToFunctionsAsync().ConfigureAwait(false);
            //kernel.Plugins.AddFromFunctions("Docker", functions);

            // Enable automatic function calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            // Test using GitHub tools
            //var prompt = "Summarize the last four commits to the microsoft/semantic-kernel repository?";
            //var result = await kernel.InvokePromptAsync(prompt, new(executionSettings)).ConfigureAwait(false);

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var message = "你好！";
            //var message = $"请翻译这个MCP功能清理，并格式化为用户友好的文本：{string.Join(",", tools.Tools.Select(m => m.Name + ":" + m.Description))}";
            Console.WriteLine($"Me:{message}");
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("请使用中文回复。");
            chatHistory.AddUserMessage(message);

            var llmAnswer = new StringBuilder();
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("LLM:");

                llmAnswer.Clear();
                await foreach (StreamingChatMessageContent chatUpdate in chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel))
                {
                    // Access the response update via StreamingChatMessageContent.Content property
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(chatUpdate.Content);

                    // Alternatively, the response update can be accessed via the StreamingChatMessageContent.Items property
                    //Console.Write(chatUpdate.Items.OfType<StreamingTextContent>().FirstOrDefault());

                    llmAnswer.Append(chatUpdate.Content);
                }
                chatHistory.AddAssistantMessage(llmAnswer.ToString()); // 流式回复结束后添加到历史记录中

                Console.WriteLine();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"Me:");
                var nextMessage = Console.ReadLine();
                if (string.IsNullOrEmpty(nextMessage))
                {
                    break;
                }
                chatHistory.AddUserMessage(nextMessage);
            }
        }
    }
}
