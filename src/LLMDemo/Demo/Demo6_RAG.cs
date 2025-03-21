using LLMDemos.Models;
using LLMDemos.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;
using System.Runtime.Serialization;
using System.Text;

namespace LLMDemos.Demo
{
    class Demo6_RAG
    {
        public static async Task RunAsync(IConfigurationRoot config, string modelId, string apiKey, Uri endPoint)
        {
            // Prepare and build kernel
            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton<IConfiguration>(config);
#pragma warning disable SKEXP0010 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            builder.AddOpenAIChatCompletion(modelId, endPoint, apiKey);
#pragma warning restore SKEXP0010 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。

            var embedingGenerator = new OllamaEmbeddingGenerator(new Uri(config["Embeddings:Ollama:EndPoint"]!), config["Embeddings:Ollama:ModelId"]);
            builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(embedingGenerator);

            var vectorStore = new QdrantVectorStore(new QdrantClient(host: config["VectorStores:Qdrant:Host"]!, port: int.Parse(config["VectorStores:Qdrant:Port"]!), apiKey: config["VectorStores:Qdrant:ApiKey"]));

            var ragConfig = config.GetSection("RAG");
            // Get the unique key genrator
            var uniqueKeyGenerator = new UniqueKeyGenerator<Guid>(() => Guid.NewGuid());
            // Get the collection in qdrant
            var ragVectorRecordCollection = vectorStore.GetCollection<Guid, TextSnippet<Guid>>(ragConfig["CollectionName"]!);
            builder.Services.AddSingleton(ragVectorRecordCollection);

            // Get the PDF loader
            var pdfLoader = new PdfDataLoader<Guid>(uniqueKeyGenerator, ragVectorRecordCollection, embedingGenerator);

            //Console.WriteLine("Loading the PDF data into vector store...");
            //var pdfFilePath = ragConfig["PdfFileFolder"]!;
            //var pdfFiles = Directory.GetFiles(pdfFilePath);
            //try
            //{
            //    foreach (var pdfFile in pdfFiles)
            //    {
            //        Console.WriteLine($"Start Loading PDF into vector store: {pdfFile}");
            //        await pdfLoader.LoadPdf(
            //            pdfFile,
            //            int.Parse(ragConfig["DataLoadingBatchSize"]!),
            //            int.Parse(ragConfig["DataLoadingBetweenBatchDelayInMilliseconds"]!));
            //        Console.WriteLine($"Finished Loading PDF into vector store: {pdfFile}");
            //    }
            //    Console.WriteLine($"All PDFs loaded into vector store succeed!");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Failed to load PDFs: {ex.Message}");
            //    return;
            //}
            //finally
            //{
            //    Console.WriteLine("Finished loading the PDF data into vector store...");
            //}

            //var vectorSearchTool = new VectorDataSearcher<Guid>(ragVectorRecordCollection, embedingGenerator);
            builder.Plugins.AddFromType<VectorDataSearcher<Guid>>(nameof(VectorDataSearcher<Guid>));
            builder.Plugins.AddFromType<DemoPlugin>(nameof(DemoPlugin));
            Kernel kernel = builder.Build();

            var settings = new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            };

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var message = "你好。";
            Console.WriteLine($"Me:{message}");
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage($"你是一个专业的AI聊天机器人，为客户提供信息咨询服务。请使用插件（{nameof(VectorDataSearcher<Guid>)}）从向量数据库中获取相关信息来回答用户提出的问题。输出要求：请在回复中引用相关信息的地方包括对相关信息的引用。");
            chatHistory.AddUserMessage(message);

            var llmAnswer = new StringBuilder();
            while (true)
            {
                Console.Write("LLM:");

                await foreach (StreamingChatMessageContent chatUpdate in chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel))
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
