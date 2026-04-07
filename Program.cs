using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Responses;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.AI;
using System.Reflection;
using SKPractice.Services;
using SKPractice.Plugins;
using SKPractice.Models;
using SKPractice.Agents;
using Microsoft.SemanticKernel.Agents;

// ============================================================
// STEP 1: Create the Kernel (the DI container for AI)
// Think of this like Program.cs where you register services
// ============================================================
//var apiKey = "REDACTED_OPENAI_KEY";
var apiKey = File.ReadAllText(@"D:\Training\AILearning\MicrosoftSemanticKernel\SecretKey.md").Trim();
var hrPolicyDocumentPath = "Documents/HRPolicy.txt";

#pragma warning disable SKEXP0070, SKEXP0010, SKEXP0001
var builder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o-mini", apiKey)
    .AddOpenAIEmbeddingGenerator("text-embedding-3-small", apiKey);  // cheap model, good for practice;

builder.Services.AddSingleton<IHRPolicyService>(sp =>new HRPolicyService(
    hrPolicyDocumentPath, 
    sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
    new QdrantCollection<ulong, HRPolicyRecord>(new Qdrant.Client.QdrantClient("localhost", 6334), "hr_policies", true)));    
builder.Plugins.AddFromType<HRPolicyPlugin>();
var kernel = builder.Build();
var hrPolicyService = kernel.Services.GetRequiredService<IHRPolicyService>();
await hrPolicyService.IngestDocumentAsync();
// ============================================================
// STEP 2: Register Plugins (your C# code that AI can call)
// These are just regular C# classes with decorated methods
// The AI reads the [Description] to know WHEN to call each one
// ============================================================
kernel.Plugins.AddFromType<TimePlugin>();
kernel.Plugins.AddFromType<WeatherPlugin>();
kernel.Plugins.AddFromType<CalculatorPlugin>();
kernel.Plugins.AddFromObject(new OnboardingPlugin());
Console.WriteLine("Choose a mode:");
Console.WriteLine("1. Chat (direct LLM with plugins)");
Console.WriteLine("2. HR Policy Agent (with conversation memory)");
Console.WriteLine("3. Onboarding Assistant Agent(with conversation memory)");
Console.Write("Enter 1 or 2 or 3: ");
var mode = Console.ReadLine()?.Trim();
if(mode == "2")
{
    //Agent mode - Level 4 (new)
    var agent = HRPolicyAgentFactory.CreateAgent(kernel);
    AgentThread thread = new ChatHistoryAgentThread();
    Console.WriteLine("\n=== HR Policy Assistant ===");
    Console.WriteLine("Welcome! How can I help you with HR Policy query today?:");
    while (true)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase)) 
            break;
        var message = new ChatMessageContent(AuthorRole.User, input);
        Console.WriteLine($"HR Assistant: ");
        await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, thread))
        {
            Console.Write(response.Content);
        }
        Console.WriteLine("\n");
    }
}
else if(mode == "3")
{
    //Agent mode - Level 4 (new)
    var agent = OnboardingAgentFactory.CreateAgent(kernel);
    AgentThread thread = new ChatHistoryAgentThread();
    Console.WriteLine("\n=== Employee Onboarding Assistant ===");
    Console.WriteLine("Welcome! To start onboarding, please provide:");
    Console.WriteLine("  - Employee name");
    Console.WriteLine("  - Department");
    Console.WriteLine("  - Role");
    Console.WriteLine("  - Start date");
    Console.WriteLine("  - Hiring manager");
    Console.WriteLine("Type 'exit' to quit.\n");
    while (true)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase)) 
            break;
        var message = new ChatMessageContent(AuthorRole.User, input);
        Console.WriteLine($"Onboarding Assistant: ");
        await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, thread))
        {
            Console.Write(response.Content);
        }
        Console.WriteLine("\n");
    }
}
else
{
    // ============================================================
    // STEP 3: Enable Auto Function Calling
    // This tells the AI: "You can call any registered plugin
    // function whenever you think it's needed"
    // ============================================================
    var settings = new OpenAIPromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    // ============================================================
    // STEP 4: Chat Loop
    // The AI will automatically decide which plugins to call
    // based on what the user asks
    // ============================================================
    var chatService = kernel.GetRequiredService<IChatCompletionService>();
    var chatHistory = new ChatHistory();

    chatHistory.AddSystemMessage(
        "You are a helpful assistant. When users ask about time, weather, or calculations, " +
        "use the available tools to get accurate answers. Be concise.");

    Console.WriteLine("=== Semantic Kernel Practice ===");
    Console.WriteLine("Try asking:");
    Console.WriteLine("  - What time is it in New York?");
    Console.WriteLine("  - What's the weather in Dallas?");
    Console.WriteLine("  - Calculate 15% tax on $2500");
    Console.WriteLine("  - What's the weather AND time in London?");
    Console.WriteLine("Type 'exit' to quit.\n");

    while (true)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        chatHistory.AddUserMessage(input);

        var response = await chatService.GetChatMessageContentAsync(
            chatHistory, settings, kernel);

        Console.WriteLine($"AI: {response.Content}\n");
        chatHistory.AddAssistantMessage(response.Content ?? "");
    }    
}









