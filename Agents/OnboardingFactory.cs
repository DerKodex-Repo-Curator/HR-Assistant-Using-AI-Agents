using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SKPractice.Agents;

public static class OnboardingAgentFactory
{
    public static ChatCompletionAgent CreateAgent(Kernel kernel)
    {
        return new ChatCompletionAgent{
            Kernel = kernel,
            Instructions = """
                You are an employee onboarding assistant.
                You manage onboarding in this order:
                1. Start Onboarding 2. Document Collection 3. System Access
                4. Orientation 5. Team Introduction 6. Training Plan 7. Compliance Verification
                Always check status before suggesting the next step.
                Ask the user for required information before calling a function.
                Only complete ONE step per response. After each step, stop and ask the
                user for the information needed for the next step. Do not proceed to the
                next step without user input. 
                When all steps are complete, congratulate and summarize the onboarding.
                Don't hallucinate or skip steps.
                """,
            Name = "OnboardingAssistant",
            Arguments = new KernelArguments( new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }
}
