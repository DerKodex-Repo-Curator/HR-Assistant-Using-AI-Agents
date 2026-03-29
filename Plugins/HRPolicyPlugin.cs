using System.ComponentModel;
using Microsoft.SemanticKernel;
using SKPractice.Services;

namespace SKPractice.Plugins;
public class HRPolicyPlugin
{
    private readonly IHRPolicyService _hrPolicyService;
    public HRPolicyPlugin(IHRPolicyService hrPolicyService)
    {
        _hrPolicyService = hrPolicyService;
    }
    [KernelFunction, Description("Searches company HR policies including leave, sick days,"
    +"work from home, probation, health insurance, retiremet, overtime, training budget,"
    +" and performance reviews. Use when employee asks any question about company policies or HR procedurs.")]
    public async Task<string>  GetHRPolicySection(
        [Description("The HR topic to look up by reading HR Policy Manual")] string topic)
    {
        return await _hrPolicyService.SearchPolicyAsync(topic);
    }
}