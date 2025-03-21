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
            Kernel kernel = builder.Build();

            bool isFunctionChoiceBehaviorAuto = true;
            var settings = new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = isFunctionChoiceBehaviorAuto ? FunctionChoiceBehavior.Auto() : FunctionChoiceBehavior.Auto(autoInvoke: false),
            };

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var message = "你好。";
            Console.WriteLine($"Me:{message}");
            var chatHistory = new ChatHistory();
            var promptTemplate = """
                    请使用下面的提示使用工具从向量数据库中获取相关信息来回答用户提出的问题：
                    {{#with (SearchPlugin-GetTextSearchResults question)}}  
                      {{#each this}}  
                        Value: {{Value}}
                        Link: {{Link}}
                        Score: {{Score}}
                        -----------------
                      {{/each}}
                    {{/with}}

                    输出要求：请在回复中引用相关信息的地方包括对相关信息的引用。
                    """;
            chatHistory.AddSystemMessage($"你是一个专业的AI聊天机器人，为客户提供信息咨询服务。{promptTemplate}");
            chatHistory.AddUserMessage(message);

            var llmAnswer = new StringBuilder();
            while (true)
            {
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

                        llmAnswer.Append(chatUpdate.Content);
                    }
                    chatHistory.AddAssistantMessage(llmAnswer.ToString()); // 流式回复结束后添加到历史记录中
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
                    var functionCalls = Microsoft.SemanticKernel.FunctionCallContent.GetFunctionCalls(result);
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

                                chatHistory.Add(result1);
                            }
                            catch (Exception ex)
                            {
                                // Adding function exception to the chat history.
                                chatHistory.Add(new Microsoft.SemanticKernel.FunctionResultContent(functionCall, ex).ToChatMessage());
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
}
