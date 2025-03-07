using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0010

namespace DeepSeekDemo.Demo
{
    internal class LLMDemo
    {
        public async static Task Run(string modelId, Uri endPoint)
        {
            var chatCompletionService = new OpenAIChatCompletionService(modelId, endPoint);

            var message = "你好";
            Console.WriteLine($"Me:{message}");
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("请使用中文与我对话。");
            chatHistory.AddUserMessage($"{message}<think>\\n\\n</think>");

            while (true)
            {
                Console.WriteLine("LLM:");
                //var reply = await chatCompletionService.GetChatMessageContentAsync(chatHistory);
                //Console.WriteLine(reply);

                // Start streaming chat based on the chat history
                await foreach (StreamingChatMessageContent chatUpdate in chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory))
                {
                    // Access the response update via StreamingChatMessageContent.Content property
                    Console.Write(chatUpdate.Content);

                    // Alternatively, the response update can be accessed via the StreamingChatMessageContent.Items property
                    //Console.Write(chatUpdate.Items.OfType<StreamingTextContent>().FirstOrDefault());
                }

                Console.WriteLine();
                Console.WriteLine();
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
