using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;

#pragma warning disable SKEXP0010

namespace LLMDemos.Demo
{
    internal class Demo3_Kernel_CreateFunctionFromMethod
    {
        public static async Task RunAsync(string modelId, string apiKey, Uri endPoint)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(modelId, endPoint, apiKey);
            builder.Plugins.AddFromType<DemoPlugin>(nameof(DemoPlugin));
            var kernel = builder.Build();

            //var demoInstance = new DemoPlugin();
            //kernel.ImportPluginFromFunctions("WeatherPlugin", new[]
            //{
            //     kernel.CreateFunctionFromMethod(demoInstance.GetWeatherForCity, nameof(demoInstance.GetWeatherForCity), "获取指定城市的天气")
            //});
            //kernel.ImportPluginFromFunctions("Save2DbPlugin", new[]
            //{
            //     kernel.CreateFunctionFromMethod(demoInstance.Save2Db, nameof(demoInstance.Save2Db), "将聊天记录保存到数据库。")
            //});

            bool isFunctionChoiceBehaviorAuto = false;
            //var settings = new OpenAIPromptExecutionSettings()
            //{
            //    Temperature = 0,
            //    //ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions, // 手动调用函数
            //    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions // 自动调用函数
            //};
            var settings = new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = isFunctionChoiceBehaviorAuto ? FunctionChoiceBehavior.Auto() : FunctionChoiceBehavior.Auto(autoInvoke: false),
            };

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var message = "你好。";
            Console.WriteLine($"Me:{message}");
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("请使用中文回复。");
            chatHistory.AddUserMessage(message);

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("LLM:");

                if (isFunctionChoiceBehaviorAuto)
                {
                    // 自动调用函数

                    await foreach (StreamingChatMessageContent chatUpdate in chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel))
                    {
                        // Access the response update via StreamingChatMessageContent.Content property
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(chatUpdate.Content);

                        // Alternatively, the response update can be accessed via the StreamingChatMessageContent.Items property
                        //Console.Write(chatUpdate.Items.OfType<StreamingTextContent>().FirstOrDefault());
                    }
                }
                else
                {
                    // 手动调用函数

                    var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);
                    if (!string.IsNullOrEmpty(result.Content))
                    {
                        Console.Write(result.Content);
                    }

                    // Adding AI model response containing chosen functions to chat history as it's required by the models to preserve the context.
                    chatHistory.Add(result);

                    // Check if the AI model has chosen any function for invocation.
                    var functionCalls = FunctionCallContent.GetFunctionCalls(result);
                    if (functionCalls.Any())
                    {
                        // Sequentially iterating over each chosen function, invoke it, and add the result to the chat history.
                        foreach (var functionCall in functionCalls)
                        {
                            try
                            {
                                // Invoking the function
                                var resultContent = await functionCall.InvokeAsync(kernel); // Executing each function.

                                // Adding the function result to the chat history
                                chatHistory.Add(resultContent.ToChatMessage());

                                var result1 = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write(result1);

                                //chatHistory.Add(result1);
                            }
                            catch (Exception ex)
                            {
                                // Adding function exception to the chat history.
                                chatHistory.Add(new FunctionResultContent(functionCall, ex).ToChatMessage());
                                // or
                                //chatHistory.Add(new FunctionResultContent(functionCall, "Error details that the AI model can reason about.").ToChatMessage());
                            }
                        }
                    }
                }

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

    internal class DemoPlugin
    {
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
    }
}
