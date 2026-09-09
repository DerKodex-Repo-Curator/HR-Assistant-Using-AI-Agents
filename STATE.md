# Stream: SKPractice — Semantic Kernel + .NET + Azure

**Source:** Hands-on extension of Microsoft Semantic Kernel docs + Microsoft Learn AI modules
**Status:** Active — Levels 1-5 done, **REPO PUBLISHED to GitHub Sun 2026-05-18 Sydney**, currently being used as portfolio anchor in active job applications (USYD Senior Innovation Developer cited the repo URL in cover letter on Tue 2026-05-19 Sydney)
**Started:** 2026-03-29 (Level 1)
**Last touched (code):** 2026-04-07 (Level 5 completion)
**Last touched (publishing + README):** Sun 2026-05-18 Sydney
**Migrated to operating model:** 2026-05-04
**Pre-operating-model history:** `D:\Training\Archive\2026-05-04_PreOperatingModel\track1_progress.md`

---

## Current position

- **Tier:** Building (Levels 1-5 done — Foundation territory completed) — **now also portfolio-stage** (repo public)
- **Public URL:** https://github.com/Dee4GitHub/HR-Assistant-Using-AI-Agents
- **Active level:** Level 6 (Azure integration) — NOT STARTED
- **Last knowledge check:** None yet (knowledge checks were not part of the old system; will introduce on Level 6 onwards)
- **Review queue:** Empty (no checks done)
- **Project location:** `D:\Training\AILearning\MicrosoftSemanticKernel\SKPractice` (local, in sync with `origin/main`)
- **Tech:** .NET 10, Semantic Kernel v1.73.0, OpenAI (gpt-4o-mini), Qdrant (Docker localhost:6334)

## What shipped to public (Sun 2026-05-18 Sydney)

- Git history rewritten via `git-filter-repo` v2.47.0 to strip a commented-out OpenAI API key (`sk-proj-2VzZ8Pf...`) from all 5 prior commits. Verified clean: 0 occurrences across history, working tree, and packfiles. Old key NOT rotated (per Deepak's risk assessment — Anthropic API exposure was bounded by published retention policies; key is a personal learning OpenAI account, not production).
- `Program.cs` line 22 changed from hardcoded absolute Windows path to env-var-first resolution: `Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? File.ReadAllText("SecretKey.md")` with InvalidOperationException + actionable message if neither set.
- `SecretKey.md` copied from parent path to project root, added to `.gitignore` (line 8) so it's never committed.
- `.env.example` created with `OPENAI_API_KEY=sk-proj-replace-with-your-real-key` placeholder and optional model/Qdrant overrides.
- `.gitignore` expanded: secrets section (SecretKey.md, .env, .env.*, !.env.example, appsettings.Development.json), IDE artifacts, OS junk.
- `README.md` written in Deepak's voice (1567 words, 8 sections + repo layout + license): problem statement (HR policy Q&A + onboarding workflow), what works today (3 modes), setup (3 bash commands), stack table, architecture in plain English, design decision callout (business rules in C# not in prompt), what I learned, honest gaps, planned improvements (8 numbered items), repo layout, MIT license. ZERO unicode chars, ZERO banned AI jargon, ZERO references to n8n / Tobias / LinkedIn Learning (license discipline applied — n8n LinkedIn Learning materials are gated per Section 1(b) and CANNOT be republished).
- Public commit on top: `chore: prepare for public release` (commit `38531ea` originally, became `8fef71a` after one of the post-rewrite re-numberings — the actual current HEAD on origin/main is the most recent commit).
- GitHub repo created via `gh repo create Dee4GitHub/HR-Assistant-Using-AI-Agents --public`; pushed clean; browser-verified rendering on https://github.com/Dee4GitHub/HR-Assistant-Using-AI-Agents.

---

## Completed levels

### Level 1 — Kernel + Plugins + Auto Function Calling — DONE
- TimePlugin, WeatherPlugin, CalculatorPlugin, HRPolicyPlugin
- Auto function calling working

### Level 2 — Proper DI + file-based document search — DONE
- IHRPolicyService with constructor injection
- File-based document search

### Level 3 — Full RAG with Qdrant — DONE
- 16 sections embedded with text-embedding-3-small
- Vector search working

### Level 4 — ChatCompletionAgent + ChatHistoryAgentThread — DONE (2026-03-29)
- Refactored project into Plugins/, Services/, Models/, Agents/ folders
- HRPolicyAgentFactory creates agent with FunctionChoiceBehavior.Auto()
- Multi-turn memory working — tested with "6 years" context giving 25 days leave
- Streaming responses via InvokeStreamingAsync
- Mode menu in Program.cs (mode 1 = chat, mode 2 = agent)

### Level 5 — Multi-step workflow agent (Employee Onboarding) — DONE (2026-04-07)
- OnboardingState model with 6 step flags, computed properties, status summary
- OnboardingPlugin.cs: 8 KernelFunctions with precondition checks, buddy assignment, training schedule generation
- OnboardingAgentFactory.cs: ChatCompletionAgent with step-order instructions, one-step-per-response control
- Program.cs mode 3 with welcome message
- **Key learnings:** business rules in code not prompts, state management limitations (in-memory only), [Description] attributes as LLM's API docs, prompt tuning for agent autonomy control

---

## Remaining levels (active backlog)

### Level 6 — Azure integration — NOT STARTED
- Swap OpenAI → Azure OpenAI
- Swap Qdrant → Azure AI Search
- Code-first vector index
- DefaultAzureCredential
- **Blocker:** Azure subscription was cancelled, needs reactivation
- **Status check needed:** Confirm with Deepak whether Azure subscription is reactivated. If yes, start Level 6. If no, skip ahead to Level 7 (xUnit tests work without Azure).

### Level 7 — Unit tests (xUnit + Moq) — NOT STARTED
- Tests for plugins + services
- Mock IHRPolicyService for OnboardingPlugin tests
- Cover precondition checks, state transitions
- Target: 80%+ coverage

---

## Suggested next moves (when stream resumes)

**Option A:** Level 7 first (xUnit tests for existing Levels 1-5 code) — no Azure dependency, builds testing discipline, can be done immediately
**Option B:** Level 6 first (Azure integration) — requires Azure subscription reactivation, more aligned with CBA REQ250287's AWS+Azure context
**Option C:** Pivot to a new level: Level 8 — **migrate SKPractice to AWS Bedrock + Claude** — directly aligned with CBA REQ250287 RDIA Crew (AWS-first stack). This would be the highest-leverage move for the active job pipeline.

**My recommendation:** Option C (Bedrock migration), then Option A (tests). Level 6 (Azure) can wait until Azure subscription is justified by an Azure-specific role in pipeline.

---

## Knowledge check plan (going forward)

- After Level 6 (or Level 8 if pivoting): 5-question check on Azure OpenAI vs OpenAI vs Bedrock primitives, vector store APIs, authentication patterns
- After Level 7: 5-question check on xUnit + Moq patterns, mocking SK services, async test patterns
- Wrong answers → spaced repetition per operating model

---

## Score history

| Date | Check # | Score | Notes |
|---|---|---|---|
| (none — knowledge checks introduced at migration to operating model) | | | |

---

## Discussion notes / clarifications

*Migrated from `track1_progress.md`:*
- "Memory & State in AI Agents" was tracked as Gap 3 IN PROGRESS — completed via Levels 4-5
- "Microsoft Agent Framework" was tracked as Gap 4 — covered via ChatCompletionAgent in Level 4-5
- .NET Core deep-dive Q&A was a between-levels practice — paused, can resume any time

---

## Next session — pick up here

**Action:** Confirm Azure subscription state with Deepak. If reactivated, start Level 6. If not, decide between Level 7 (tests) or Level 8 (Bedrock pivot) per CBA REQ250287 alignment.
**Context I need to remember:** SKPractice has Levels 1-5 working, code at `D:\Training\AILearning\MicrosoftSemanticKernel\SKPractice`. Last level (5) was Employee Onboarding multi-step workflow agent. Project is in good shape — was paused at Level 5, not abandoned.

---

## If interrupted by interview / application

**Pause clean point:** End of any level, after the level's "Key learnings" are documented.
**To resume:** Read this file. Check Levels Done vs Levels Remaining. Pick up at first NOT STARTED.
