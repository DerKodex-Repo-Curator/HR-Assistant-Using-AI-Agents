# HR Assistant Using AI Agents

A small working prototype I built to learn Microsoft Semantic Kernel hands-on. It has two AI agents that solve common HR problems: answering employee policy questions, and walking a new hire through structured onboarding.

The point of this repo is not a finished product. The point is to take Semantic Kernel from "I read the docs" to "I have working code I can defend in a conversation." Two working agents and a plugin chat mode later, that goal is met.

## The problem I wanted to solve

Two real workflows that small HR teams burn hours on:

1. Employees ask the same policy questions over and over (How many sick days do I get? What is the probation period? Does the company match my retirement contribution?). HR answers them by reading the same policy document, again, by hand.

2. Onboarding a new starter has a known sequence of steps that must happen in order (document collection, system access, orientation, team intro, training plan, compliance check). When the steps live in someone's head, they slip. When they live in a checklist nobody owns, they slip differently.

Both are good candidates for an AI agent because the work is repetitive, the rules are well defined, and the answers come from existing documents and existing process. The agent does not need to be creative. It needs to be grounded, consistent, and refuse to make things up.

## What works today

Run the project and you get a console app with three modes:

- **Mode 1: Chat with tool plugins.** Direct LLM with four simple tools (time, weather, calculator, HR policy). The LLM picks the right tool based on the question. This is the basic plugin pattern.
- **Mode 2: HR Policy Agent.** An agent backed by a RAG pipeline over a real HR policy document. The document is split into sections, embedded with `text-embedding-3-small`, stored in Qdrant. When you ask "how much annual leave do I get after five years," the agent searches the vector store, cites the section number, and refuses to invent an answer if nothing matches.
- **Mode 3: Onboarding Assistant Agent.** A multi-step workflow agent. It walks the conversation through six onboarding steps in order, refuses to skip ahead, asks for the data each step needs, and validates inputs (start date must be in the future, orientation date must be after start date, at least three documents required, and so on).

The full setup runs locally. Qdrant comes up in Docker. The .NET app needs an OpenAI API key.

## Setup

You need .NET 10, Docker (for Qdrant), and an OpenAI API key.

```bash
# 1. Start Qdrant
docker run -d -p 6334:6334 qdrant/qdrant

# 2. Provide your OpenAI key (pick one)
# Option A: environment variable
export OPENAI_API_KEY=sk-proj-yourkey...
# Option B: file at the project root (gitignored)
echo "sk-proj-yourkey..." > SecretKey.md

# 3. Run
dotnet run
```

The code checks `OPENAI_API_KEY` first, then falls back to `SecretKey.md`. Pick whichever fits your workflow. The repo includes `.env.example` showing the expected shape.

## Stack

| Layer | Choice |
|---|---|
| Language and runtime | C# / .NET 10 |
| Agent framework | Microsoft Semantic Kernel 1.73 |
| LLM | OpenAI gpt-4o-mini |
| Embeddings | OpenAI text-embedding-3-small |
| Vector store | Qdrant (Docker, localhost:6334) |
| DI | Built-in Microsoft.Extensions.DependencyInjection |

I picked OpenAI because the OpenAI connector for SK is the most stable and the gpt-4o-mini price point is reasonable for repeated learning runs. Qdrant because it is open source, runs in one Docker command, and the SK Qdrant connector is first-class.

## Architecture in plain English

The Semantic Kernel acts like a service container for AI work, the same way `IServiceCollection` does for normal .NET. Plugins are C# classes whose methods are annotated with `[KernelFunction]` and `[Description]`. The agent reads those descriptions and decides which method to call based on what the user asked. That is the whole pattern.

For the HR Policy agent I added one extra layer. A `HRPolicyService` reads the policy text file, splits it by section, generates an embedding for each section using OpenAI, and stores the embedding in Qdrant. When the agent calls the search function, the service generates an embedding for the user's query, asks Qdrant for the top three nearest sections, and returns the matching text. The agent then frames an answer using only that text.

For the Onboarding agent the work is different. The state lives in a plain C# `OnboardingState` object with booleans for each completed step. The plugin functions check preconditions before they act ("you cannot collect documents before onboarding has started", "you cannot complete orientation before system access is provisioned"). The agent's system prompt tells it to handle one step per response and ask the user for what the next step needs.

## A design decision worth calling out

The Onboarding agent does its business rules in C# code, not in the system prompt. When I first built it I had the rules in the prompt ("make sure documents are collected before provisioning access, and the start date must be in the future"). The LLM mostly followed them. Mostly was not good enough. Moving the rules into the plugin functions means they fire deterministically every time, and the LLM cannot decide to be flexible about them.

This is the lesson I keep coming back to: the prompt is for tone, persona, and conversation shape. The code is for invariants. Treating them the other way around is how you get an agent that sometimes does the right thing.

## What I learned

- Semantic Kernel's DI story is the cleanest of the three frameworks I have looked at (SK, AutoGen.NET, Microsoft Agent Framework). For an enterprise .NET codebase it is the path of least resistance.
- `[Description]` strings on plugin methods are the LLM's API documentation. Vague descriptions mean the wrong tool gets called. Specific descriptions with examples are worth the time.
- A grounded agent that says "I cannot find that in the policy" is more useful than a confident agent that makes things up. The system prompt has to encourage that behaviour explicitly.
- Vector search returns results that look semantically close but are not always what the human meant. Top-three retrieval and letting the agent narrow from there worked better than top-one.

## Honest gaps

- Onboarding state is in-memory only. Restart the app and you lose progress. A real version would persist to a database.
- No tests yet. The next addition is a golden eval set (~20 representative questions with expected behaviours) wired into CI; see Planned Improvements below.
- The weather and calculator plugins return fake or trivial data. They exist to demonstrate the plugin pattern, not to be useful.
- No telemetry. Semantic Kernel has built-in OpenTelemetry hooks that I have not turned on yet.
- The HR policy document is small (a few sections). The retrieval would need re-thinking for a real-world corpus.

## Planned improvements

The roadmap for this project, in rough priority order:

1. Multi-agent orchestration. Turn the single HR agent into a small team: a recruiter agent for candidate-facing questions, the existing policy expert, and an escalation agent that decides when a question needs a human. Wire them together with `AgentGroupChat`.
2. Port the HR agent to Microsoft Agent Framework and to AutoGen.NET. Same behaviour, different frameworks. The point is a side-by-side comparison document on ergonomics, lines of code, and what each framework makes easy or hard.
3. Expose the plugins as a Model Context Protocol (MCP) server, so the same tools can be consumed by Claude Desktop, Cursor, or any other MCP-aware client.
4. Deploy the HR agent to Azure AI Foundry with real traces and cost dashboards.
5. Production hardening: a golden eval set wired into CI so prompt changes cannot silently regress quality, OpenTelemetry traces, structured logging, input guardrails, and per-user cost ceilings.
6. Persistent onboarding state. Right now the state is in-memory; restart the app and progress is lost. Add a database layer.
7. Expand the HR policy corpus. The current `HRPolicy.txt` is a single document with a handful of sections. Broaden coverage across compensation, benefits, leave, IT security, workplace safety, code of conduct, and performance reviews. With a larger and more realistic corpus the retrieval quality (chunking strategy, citation precision, top-k tuning) becomes measurable.
8. Tighten the system prompt. The current prompt is short and works. The next iteration adds explicit sections for role, persona, style, constraints, fallback wording, and a citation format with examples. Same intent, more rigorous structure.

Each of these is its own commit (or set of commits) and will land progressively.

## Repo layout

```
SKPractice/
  Program.cs                       # entry point, three-mode menu
  Plugins/                         # C# methods the LLM can call
    HRPolicyPlugin.cs              # delegates to HRPolicyService for RAG
    OnboardingPlugin.cs            # six-step workflow with precondition checks
    TimePlugin.cs                  # demo plugin
    WeatherPlugin.cs               # demo plugin (fake data)
    CalculatorPlugin.cs            # demo plugin
  Agents/
    HRPolicyAgentFactory.cs        # ChatCompletionAgent with anti-hallucination prompt
    OnboardingFactory.cs           # ChatCompletionAgent for the step-by-step flow
  Services/
    IHRPolicyService.cs
    HRPolicyService.cs             # ingest, embed, store, search via Qdrant
  Models/
    HRPolicyRecord.cs              # vector store record shape
    OnboardingState.cs             # in-memory state for the onboarding workflow
  Documents/
    HRPolicy.txt                   # source document for RAG
    OnboardingChecklist.txt
  RequirementDocuments/            # design notes I wrote before building
  .env.example
  .gitignore
```

## License

MIT. Use it, fork it, learn from it. If something here helps you, that is the point.
