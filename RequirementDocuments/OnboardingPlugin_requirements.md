Onboarding Plugin Requirements.

  1. StartOnboarding
  - In: name, department, role, startDate, hiringManager
  - Out: confirmation with employee details, assigns a buddy (hardcode a
  list of 3-4 buddies, pick based on department)
  - Sets: all state fields, assigns buddy

  2. GetStatus
  - In: nothing
  - Out: calls _state.GetStatusSummary()

  3. CompleteDocCollection
  - In: list of document names (e.g., "passport, tax file number,
  superannuation form, bank details, emergency contact form")
  - Precondition: onboarding must be started
  - Out: confirms which docs collected, warns if fewer than 3 docs provided
  - Sets: DocumentsCollected = true, stores doc names in CollectedDocuments

  4. CompleteSystemAccess
  - In: list of systems (e.g., "email, VPN, Jira, GitHub, Slack")
  - Precondition: documents must be collected first
  - Out: confirms provisioned systems
  - Sets: SystemAccessProvisioned = true, stores in ProvisionedSystems

  5. CompleteOrientation
  - In: orientationDate, handbookAcknowledged (bool)
  - Precondition: system access done
  - Out: confirms orientation, warns if handbook not acknowledged
  - Sets: OrientationCompleted = true

  6. CompleteTeamIntro
  - In: none (uses assigned buddy from state)
  - Precondition: orientation done
  - Out: confirms team intro facilitated by buddy
  - Sets: TeamIntroductionDone = true

  7. CompleteTrainingPlan
  - In: list of training topics (e.g., "security awareness, code standards,
  CI/CD pipeline, architecture overview")
  - Precondition: team intro done
  - Out: confirms training plan created with topics and estimated dates
  (start date + 7/14/21/28 days)
  - Sets: TrainingPlanCreated = true

  8. RunComplianceVerification
  - In: none
  - Precondition: all 5 previous steps done
  - Out: checks all flags, returns pass/fail with details. If all done →
  sets ComplianceVerified = true. If gaps → lists what's missing.