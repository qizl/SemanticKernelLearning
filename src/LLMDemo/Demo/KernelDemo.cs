using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0010

namespace DeepSeekDemo.Demo
{
    internal class KernelDemo
    {
        public async static Task Run(string modelId, Uri endPoint)
        {
            var builder = Kernel.CreateBuilder();

            builder.AddOpenAIChatCompletion(modelId, endPoint, null);

            var kernel = builder.Build();

            var prompt = @"{{$input}} One line TLDR with the fewest words.";

            var summarize = kernel.CreateFunctionFromPrompt(prompt, executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 100 });

            string text1 = @"
                1st Law of Thermodynamics - Energy cannot be created or destroyed.
                2nd Law of Thermodynamics - For a spontaneous process, the entropy of the universe increases.
                3rd Law of Thermodynamics - A perfect crystal at zero Kelvin has zero entropy.";

            string text2 = @"
                1. An object at rest remains at rest, and an object in motion remains in motion at constant speed and in a straight line unless acted on by an unbalanced force.
                2. The acceleration of an object depends on the mass of the object and the amount of force applied.
                3. Whenever one object exerts a force on another object, the second object exerts an equal and opposite on the first.";

            Console.WriteLine(await kernel.InvokeAsync(summarize, new() { ["input"] = text1 }));

            Console.WriteLine(await kernel.InvokeAsync(summarize, new() { ["input"] = text2 }));

            // Output:
            //   Energy conserved, entropy increases, zero entropy at 0K.
            //   Objects move in response to forces.

            Console.ReadKey();
        }
    }
}
