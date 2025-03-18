using LLMDemos.Demo.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMDemos.Demo
{
    class Demo5_Agent
    {
        public static async Task RunAgentAsync(string modelId, string apiKey, Uri endPoint)
        {
            // Prepare and build kernel
            var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            builder.AddOpenAIChatCompletion(modelId, endPoint, apiKey);
#pragma warning restore SKEXP0010 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            Kernel kernel = builder.Build();

            // Define the agent
            ChatCompletionAgent agent = new()
            {
                Name = "Parrot", // 鹦鹉
                Instructions = "Repeat the user message in the voice of a pirate and then end with a parrot sound.", // 用海盗的声音重复用户消息，然后以鹦鹉的声音结束。
                Kernel = kernel,
            };

            /// Create the chat history to capture the agent interaction.
            ChatHistory chat = [];
            chat.AddSystemMessage("请使用中文回复");

            // Respond to user input
            await InvokeAgentAsync("Fortune favors the bold."); // 运气偏爱勇敢的人。
            await InvokeAgentAsync("I came, I saw, I conquered."); // 我来了，我看见了，我征服了。
            await InvokeAgentAsync("Practice makes perfect."); // 熟能生巧。

            // Local function to invoke agent and display the conversation messages.
            async Task InvokeAgentAsync(string input)
            {
                ChatMessageContent message = new(AuthorRole.User, input);
                chat.Add(message);
                WriteAgentChatMessage(message);

                await foreach (ChatMessageContent response in agent.InvokeAsync(chat))
                {
                    chat.Add(response);

                    WriteAgentChatMessage(response);
                }
            }

            Console.ReadKey();
        }

        public static async Task RunAgentGroupChatAsync(IConfigurationRoot config, string modelId, string apiKey, Uri endPoint)
        {
            // Prepare and build kernel
            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton<IConfiguration>(config);
#pragma warning disable SKEXP0010 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            builder.AddOpenAIChatCompletion(modelId, endPoint, apiKey);
#pragma warning restore SKEXP0010 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            Kernel kernel = builder.Build();
            //builder.Plugins.AddFromType<DemoPlugin>(nameof(DemoPlugin));

            // Define the agents
            ChatCompletionAgent agentReviewer = new()
            {
                Instructions =
                    $@"                        
                    You are an art director who has opinions about copywriting born of a love for David Ogilvy.
                    The goal is to determine if the given copy is acceptable to print.
                    If so, state that it is approved.
                    If not, provide insight on how to refine suggested copy without example.                            
                    请使用中文回复
                    ", // 你是一位艺术总监，对文案有着源于对大卫·奥格威的热爱。目标是确定给定的文案是否可以印刷。如果可以，请说明已批准。如果不行，请提供如何改进建议文案的见解，但不提供示例。
                Name = "ArtDirector", // 艺术总监
                Kernel = kernel,
            };

            ChatCompletionAgent agentWriter = new()
            {
                Instructions =
                    $@"
                    You are a copywriter with ten years of experience and are known for brevity and a dry humor.
                    The goal is to refine and decide on the single best copy as an expert in the field.
                    Only provide a single proposal per response.
                    You're laser focused on the goal at hand.
                    Don't waste time with chit chat.
                    Consider suggestions when refining an idea.
                    请使用中文回复
                    ", // 你是一位有十年经验的文案撰写人，以简洁和干涩的幽默著称。目标是作为该领域的专家，改进并决定最佳文案。每次回复只提供一个建议。你专注于手头的目标。不要浪费时间闲聊。在改进想法时考虑建议。
                Name = "CopyWriter", // 文案撰写人
                Kernel = kernel,
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            };
            // Initialize plugin and add to the agent's Kernel (same as direct Kernel usage).
            //agentWriter.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<DemoPlugin>(nameof(DemoPlugin)));
            //agentWriter.Kernel.Plugins.AddFromType<DemoPlugin>(nameof(DemoPlugin));
            agentWriter.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<DemoPlugin>(nameof(DemoPlugin), builder.Services.BuildServiceProvider()));

            // Create a chat for agent interaction.
#pragma warning disable SKEXP0110 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            AgentGroupChat chat = new(agentWriter, agentReviewer)
            {
                ExecutionSettings = new()
                {
                    // Here a TerminationStrategy subclass is used that will terminate when
                    // an assistant message contains the term "approve".
                    TerminationStrategy = new ApprovalTerminationStrategy()
                    {
                        // Only the art-director may approve.
                        Agents = [agentReviewer],
                        // Limit total number of turns
                        MaximumIterations = 10,
                    }
                }
            };
#pragma warning restore SKEXP0110 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。

            // Invoke chat and display messages.
            ChatMessageContent input = new(AuthorRole.User, "concept: 联网搜一下女星倪妮的最新活动，依此写一段文案。"); // 概念：用蛋盒制作的地图。
            chat.AddChatMessage(input);
            WriteAgentChatMessage(input);

            await foreach (ChatMessageContent response in chat.InvokeAsync())
            {
                WriteAgentChatMessage(response);
            }

            Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");

            Console.ReadKey();
        }

        /// <summary>
        /// Common method to write formatted agent chat content to the console.
        /// </summary>
        private static void WriteAgentChatMessage(ChatMessageContent message)
        {
            // Include ChatMessageContent.AuthorName in output, if present.
#pragma warning disable SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            string authorExpression = message.Role == AuthorRole.User ? string.Empty : $" - {message.AuthorName ?? "*"}";
#pragma warning restore SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            // Include TextContent (via ChatMessageContent.Content), if present.
            string contentExpression = string.IsNullOrWhiteSpace(message.Content) ? string.Empty : message.Content;
            bool isCode = message.Metadata?.ContainsKey("code") ?? false;
            string codeMarker = isCode ? "\n  [CODE]\n" : " ";
            Console.WriteLine($"\n# {message.Role}{authorExpression}:{codeMarker}{contentExpression}");

            // Provide visibility for inner content (that isn't TextContent).
            foreach (KernelContent item in message.Items)
            {
#pragma warning disable SKEXP0110 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                if (item is AnnotationContent annotation)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {annotation.Quote}: File #{annotation.FileId}");
                }
                else if (item is FileReferenceContent fileReference)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] File #{fileReference.FileId}");
                }
                else if (item is ImageContent image)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.DataUri ?? $"{image.Data?.Length} bytes"}");
                }
                else if (item is FunctionCallContent functionCall)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {functionCall.Id}");
                }
                else if (item is FunctionResultContent functionResult)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId} - {System.Text.Json.JsonSerializer.Serialize(functionResult.Result) ?? "*"}");
                }
#pragma warning restore SKEXP0110 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            }

            if (message.Metadata?.TryGetValue("Usage", out object? usage) ?? false)
            {
#pragma warning disable OPENAI001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                if (usage is OpenAI.Assistants.RunStepTokenUsage assistantUsage)
                {
                    WriteUsage(assistantUsage.TotalTokenCount, assistantUsage.InputTokenCount, assistantUsage.OutputTokenCount);
                }
                else if (usage is Azure.AI.Projects.RunStepCompletionUsage agentUsage)
                {
                    WriteUsage(agentUsage.TotalTokens, agentUsage.PromptTokens, agentUsage.CompletionTokens);
                }
                else if (usage is OpenAI.Chat.ChatTokenUsage chatUsage)
                {
                    WriteUsage(chatUsage.TotalTokenCount, chatUsage.InputTokenCount, chatUsage.OutputTokenCount);
                }
#pragma warning restore OPENAI001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            }

            void WriteUsage(long totalTokens, long inputTokens, long outputTokens)
            {
                Console.WriteLine($"  [Usage] Tokens: {totalTokens}, Input: {inputTokens}, Output: {outputTokens}");
            }
        }

#pragma warning disable SKEXP0110 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
        private sealed class ApprovalTerminationStrategy : TerminationStrategy
#pragma warning restore SKEXP0110 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
        {
            // Terminate when the final message contains the term "approve"
            protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
                => Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }
}
