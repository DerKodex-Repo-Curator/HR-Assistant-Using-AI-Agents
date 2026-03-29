using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SKPractice.Agents;

public static class HRPolicyAgentFactory
{
    public static ChatCompletionAgent CreateAgent(Kernel kernel)
    {
        return new ChatCompletionAgent{
            Kernel = kernel,
            Instructions = """
            You are an HR policy assistant,  
            Only answer from the policy documents (use the tools) 
            Include the section number in your answer,
            If you can't find the answer, say so — don't make things up
            Remember what the user said earlier in the conversation,
            Don't hallucinate.
            """,
            Name = "HRAssistant",
            Arguments = new KernelArguments( new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }
}