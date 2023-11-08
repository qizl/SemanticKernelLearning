// See https://aka.ms/new-console-template for more information
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using System.Text;

//Create Kernel builder
var builder = new KernelBuilder();

// Configure AI service credentials used by the kernel
var (useAzureOpenAI, model, azureEndpoint, apiKey, orgId) = Settings.LoadFromFile();

if (useAzureOpenAI)
    builder.WithAzureChatCompletionService(model, azureEndpoint, apiKey);
else
    builder.WithOpenAIChatCompletionService(model, apiKey, orgId);

IKernel kernel = builder.Build();

// Load the FunPlugin from the Plugins Directory
var funPluginFunctions = kernel.ImportSemanticFunctionsFromDirectory("Skills", "Learning");

var myContext = new ContextVariables();
var histories = new StringBuilder();
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("Say anything to start practicing English.");
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Me：");
    var input = Console.ReadLine();
    myContext.Set("history", histories.ToString());
    myContext.Set("input", input);

    var result = await kernel.RunAsync(myContext, funPluginFunctions["LearningEnglishSkill"]);
    histories.AppendLine(input);
    histories.AppendLine(result.GetValue<string>());

    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine($"AI：{result}");
}