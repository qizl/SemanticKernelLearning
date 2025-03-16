using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;

#pragma warning disable SKEXP0010

namespace LLMDemos.Demo
{
    internal class Demo1_LLM
    {
        public async static Task Run(string modelId, string apiKey, Uri endPoint)
        {
            var chatCompletionService = new OpenAIChatCompletionService(modelId, endPoint, apiKey);

            var message = "你好";
            Console.WriteLine($"Me:{message}");
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("请使用中文回复。");
            chatHistory.AddUserMessage(message);

            var llmAnswer = new StringBuilder();
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("LLM:");
                //var reply = await chatCompletionService.GetChatMessageContentAsync(chatHistory);
                //Console.WriteLine(reply);

                llmAnswer.Clear();
                // Start streaming chat based on the chat history
                await foreach (StreamingChatMessageContent chatUpdate in chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory))
                {
                    // Access the response update via StreamingChatMessageContent.Content property
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

        public async static Task RunEAI(string modelId, string apiKey, Uri endPoint)
        {
            var apiKeyCredential = new ApiKeyCredential(apiKey);
            var aiClientOptions = new OpenAIClientOptions
            {
                Endpoint = endPoint
            };
            var aiClient = new OpenAIClient(apiKeyCredential, aiClientOptions)
                .AsChatClient(modelId);

            var message = "你好";
            Console.WriteLine($"Me:{message}");
            var chatHistory = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, "请使用中文回复。"),
                new ChatMessage(ChatRole.User, message)
            };

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("LLM:");

                // Start streaming chat based on the chat history
                await foreach (var chatUpdate in aiClient.GetStreamingResponseAsync(chatHistory))
                {
                    // Access the response update via StreamingChatMessageContent.Content property
                    Console.Write(chatUpdate);

                    // Alternatively, the response update can be accessed via the StreamingChatMessageContent.Items property
                    //Console.Write(chatUpdate.Items.OfType<StreamingTextContent>().FirstOrDefault());
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
                chatHistory.Add(new ChatMessage(ChatRole.User, nextMessage));
            }
        }
    }
}
