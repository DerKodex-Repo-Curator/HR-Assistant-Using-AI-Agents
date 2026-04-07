
namespace SKPractice.Models;

public class OnboardingState
{
        // Employee info
    public string EmployeeName { get; set; }
    public string Department { get; set; }
    public string Role { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? OrientationDate { get; set; }
    public string HiringManager { get; set; }

    // Step completion flags (6 booleans)
    public bool DocumentsCollected { get; set; }
    public bool SystemAccessProvisioned { get; set; }
    public bool OrientationCompleted { get; set; }
    public bool TeamIntroductionDone { get; set; }
    public bool TrainingPlanCreated { get; set; }
    public bool ComplianceVerified { get; set; }

    // Intermediate data
    public List<string> CollectedDocuments { get; set; } = new List<string>();
    public List<string> ProvisionedSystems { get; set; } = new List<string>();
    public List<string> TrainingTopics { get; set; } = new List<string>();
    public string AssignedBuddy { get; set; } = string.Empty;

    // Computed properties
 public int CompletedStepCount =>
      new[] 
      { 
        DocumentsCollected, SystemAccessProvisioned, OrientationCompleted,
        TeamIntroductionDone, TrainingPlanCreated, ComplianceVerified
      }.Count(s => s);
    public int TotalSteps = 6;           //{(always 6)}
    public bool IsComplete => CompletedStepCount == TotalSteps;
    
    public string GetStatusSummary()
    {
        var status = $"Onboarding progress for {EmployeeName}: {CompletedStepCount}/{TotalSteps} steps complete\n.";
        status += "Steps:\n";
        status += $"  1. Document Collection:     {(DocumentsCollected ? "DONE" : "PENDING")}\n";
        status += $"  2. System Access:           {(SystemAccessProvisioned ? "DONE" : "PENDING")}\n";
        status += $"- 3. Orientation Completed:   {(OrientationCompleted ? "DONE" : "PENDING")}\n";
        status += $"- 4. Team Introduction:       {(TeamIntroductionDone ? "DONE" : "PENDING")}\n";
        status += $"- 5. Training Plan:           {(TrainingPlanCreated ? "DONE" : "PENDING")}\n";
        status += $"- 6. Compliance Verification: {(ComplianceVerified ? "DONE" : "PENDING")}\n";
        return status;
    }     
}
