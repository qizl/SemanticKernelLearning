using DeepSeekDemo.Demo;

var modelId = "qwen2.5-7b-instruct-1m";
//var modelId= "deepseek-r1-distill-qwen-14b";
var endPoint = new Uri("http://127.0.0.1:1234/v1");

await LLMDemo.Run(modelId, endPoint);

//await KernelDemo.Run(modelId, endPoint);
