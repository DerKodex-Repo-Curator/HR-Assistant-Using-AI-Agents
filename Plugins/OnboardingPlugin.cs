using Microsoft.SemanticKernel;
using SKPractice.Models;
using System.ComponentModel;

namespace SKPractice.Plugins;

public class OnboardingPlugin
{
    private readonly OnboardingState _state = new();
    private readonly Dictionary<string, string> _buddyMap = new()
    {
        { "Engineering", "Sarah Chen" },
        { "Marketing", "James Patel" },
        { "Finance", "Priya Sharma" },
        { "HR", "Tom Wilson" }
    };
    
    [KernelFunction, Description("Get the current onboarding status and progress")]
    public string GetStatus()
    {
        if (string.IsNullOrEmpty(_state.EmployeeName))
        {
            return "No onboarding in progress. Start with EmployeeName, Department, Role, StartDate, and HiringManager information.";
        }
        return $"Onboarding Status: {_state.GetStatusSummary()}";
    }
    [KernelFunction, Description("Start onboarding for a new employee")]
    public string StartOnboarding(
        [Description("Employee full name")] string employeeName,
        [Description("Department")] string department,
        [Description("Job role/title")] string role,
        [Description("Start date")] string startDate,
        [Description("Hiring manager name")] string hiringManager)
    {
        var startingDate =  DateTime.Parse(startDate);
        if (startingDate < DateTime.Now)
        {
            return "Start date cannot be in the past. Please provide a valid current or future date.";
        }
        _buddyMap.TryGetValue(department, out var buddy);
        _state.EmployeeName = employeeName;
        _state.Department = department;
        _state.Role = role;
        _state.StartDate = startingDate;
        _state.HiringManager = hiringManager;
        _state.AssignedBuddy = buddy ?? "Sarah Chen";
        return $"Onboarding started for {employeeName} ({role},{department}). \n Start date: {startDate}.  Hiring manager: {hiringManager}. \n Assigned buddy: {_state.AssignedBuddy}.\nNext step: Document Collection.";
    }

    [KernelFunction, Description("Collect employee documents")]
    public string CollectDocuments(
        [Description("List of document names")] string documents
    )
    {
        if (_state.DocumentsCollected)
        {
            return "Documents already collected.";
        }
        if (string.IsNullOrEmpty(_state.EmployeeName))
        {
            return "Please start onboarding first.";
        }
        if (string.IsNullOrEmpty(documents) || documents.Split(',').Length < 3)
        {
            return "Please provide at least 3 document names.";
        }
        var docList = documents.Split(',').Select(d => d.Trim()).ToList();
        _state.CollectedDocuments = docList;
        _state.DocumentsCollected = true;
        return "Documents collected. Next step: System Access Provisioning.";
    }

    [KernelFunction, Description("Provision system access for the employee")]
    public string ProvisionSystemAccess( 
        [Description("System Names")] string systemNames
    )
    {
        if(!_state.DocumentsCollected)
        {
            return "Documents not collected yet.";
        }
        if (_state.SystemAccessProvisioned)
        {
            return "System access already provisioned.";
        }
        var systemList = systemNames.Split(',').Select(s => s.Trim()).ToList();
        _state.ProvisionedSystems = systemList;
        _state.SystemAccessProvisioned = true;
        return "System access provisioned. Next step: Complete Orientation.";
    }

    [KernelFunction, Description("Complete Orientation")]
    public string CompleteOrientation(
        [Description("Orientation Date")] string orientationDate,
        [Description("Handbook Acknowledged")] bool handbookAcknowledged,
        [Description("Acknowledged by") ] string acknowledgedBy
    )
    {
        if (_state.OrientationCompleted)
        {
            return "Orientation already completed.";
        }
        if (!_state.SystemAccessProvisioned)
        {
            return "System access not provisioned yet.";
        }
        if (string.IsNullOrEmpty(acknowledgedBy))
        {
            
            return $"Are you sure, acknowledgement is provided by {_state.EmployeeName}?";
        }
        if (acknowledgedBy != _state.EmployeeName)
        {
            return $"Are you sure, acknowledgement is provided by {_state.EmployeeName}?";
        }
        if (string.IsNullOrEmpty(orientationDate))
        {
            return "Orientation date is required.";
        }
        var orientDate = DateTime.Parse(orientationDate);
        if (orientDate < _state.StartDate)
        {
            return "Orientation date cannot be on or before start date.";
        }
        _state.OrientationDate = orientDate;
        _state.OrientationCompleted = true;
        return handbookAcknowledged ? "Next step: Team Introduction. Onboarding is finished." : "Orientation completed, warning: handbook not acknowledged.";
    }

    [KernelFunction, Description("Complete Team Introduction")]
    public string CompleteTeamIntroduction()
    {
        if (!_state.OrientationCompleted)
        {
            return "Orientation not completed yet.";
        }
        if (_state.TeamIntroductionDone)
        {
            return "Team introduction already completed.";
        }
        _state.TeamIntroductionDone = true;
        return $"Team introduction is facilitated by {_state.AssignedBuddy}. Next Step is Training Plan.";
    }

    [KernelFunction, Description("Complete Training Plan")]
    public string CompleteTrainingPlan(
        [Description("Training Topics")] string trainingTopics
    ){
        if (!_state.TeamIntroductionDone)
        {
            return "Team introduction not completed yet.";
        }
        if (string.IsNullOrEmpty(trainingTopics))
        {
            return "Training topics are required.";
        }
        if (_state.TrainingPlanCreated)
        {
            return "Training plan already completed.";
        }
        var topics = trainingTopics.Split(',').Select(t => t.Trim()).ToList();
        _state.TrainingTopics = topics;
        var schedule = topics.Select((t, i) => $"{t}: {_state.StartDate.AddDays(7 * (i + 1)):yyyy-MM-dd}").ToList();
        _state.TrainingPlanCreated = true;
        return $"Training plan is completed.\n {string.Join("\n", schedule)}\n Next Step: Compliance Verification.";
    }

    [KernelFunction, Description("Complete Compliance Verification")]
    public string RunComplianceVerification()
    {
        if (_state.ComplianceVerified)
        {
            return "Compliance verification already completed.";
        }
        var missing = new List<string>();
        if (!_state.DocumentsCollected) missing.Add("Document Collection");
        if (!_state.SystemAccessProvisioned) missing.Add("System Access");
        if (!_state.OrientationCompleted) missing.Add("Orientation");
        if (!_state.TeamIntroductionDone) missing.Add("Team Introduction");
        if (!_state.TrainingPlanCreated) missing.Add("Training Plan");

        if (missing.Count > 0)
            return $"Cannot verify compliance. Missing steps: {string.Join(", ", missing)}";
        _state.ComplianceVerified = true;
        return $"Compliance verification passed. Onboarding complete for {_state.EmployeeName}.";
    }
}
