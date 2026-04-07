# Level 5 — Interview Q&A: Multi-Step Workflow Agent

## Q1: If you removed all preconditions from the plugin and relied only on agent instructions to enforce step order, what would happen?

**Answer:** The agent would call functions in rapid succession with hallucinated data, trying to complete all steps without stopping for user input. Business rules must be enforced in code, not prompts. Prompts are best-effort guidance — the LLM may ignore them. Code-level validation is guaranteed.

**Interview tip:** This demonstrates understanding of the boundary between LLM reasoning (unreliable) and deterministic code (reliable). In production, critical business rules always go in code.

---

## Q2: What's the limitation of storing state in a private field like `_state` in the plugin?

**Answer:**
1. **No persistence** — state lives in memory only. If the app crashes or restarts, all progress is lost. In production, store state in a database so the agent can resume where it left off.
2. **Single-user only** — one `_state` instance means one onboarding at a time. For concurrent users, you'd need a `Dictionary<string, OnboardingState>` keyed by employee ID.
3. **No audit trail** — no record of when each step was completed or by whom.

**Interview tip:** This maps to the NewRez JD requirement "state tracking to support autonomous decision-making." Real systems use database-backed state with timestamps and user attribution.

---

## Q3: How does the LLM know which KernelFunction to call when FunctionChoiceBehavior.Auto() is set?

**Answer:** The LLM sees:
1. **Function names** — e.g., `StartOnboarding`, `CollectDocuments`
2. **[Description] attributes** — on the function and each parameter. This is the LLM's only guide to understanding what the function does and what inputs it needs.
3. **Parameter types** — string, bool, etc.
4. **System instructions** — the agent's instruction prompt.

It does NOT see: internal code, precondition logic, or the state object. It's like an API consumer reading Swagger docs — only the signature and descriptions.

**How it works under the hood:** Semantic Kernel serializes function name, description, and parameter schema into OpenAI's function-calling format. The LLM returns a JSON object with function name and arguments. SK deserializes and executes the function.

**Interview tip:** Good `[Description]` attributes are critical. Vague descriptions = LLM guesses wrong. This is a form of prompt engineering at the tool level.

---

## Q4: What's the difference between Level 4 (HR Policy) and Level 5 (Onboarding) agents?

**Answer:**
- **Level 4**: Stateless — every function searches documents and returns results. No memory between function calls beyond chat history.
- **Level 5**: Stateful — the plugin manages `OnboardingState` across multiple turns. Functions have preconditions that depend on prior function calls. The agent orchestrates a multi-step workflow, not just Q&A.

**Interview tip:** This is the progression from a simple RAG assistant to an autonomous workflow agent — exactly what "agentic workflow design" means on job descriptions.

---

## Q5: How do you control agent autonomy — preventing it from doing too much at once?

**Answer:** Three layers:
1. **Code preconditions** — functions return errors if called out of order (guaranteed enforcement)
2. **Agent instructions** — "complete one step per response, stop and ask user" (best-effort, LLM may ignore)
3. **Approval-based loops** (production pattern) — each tool call requires explicit user confirmation before execution

In practice, LLMs still batch calls despite instructions. For critical workflows, use approval gates in the agent loop, not just prompt instructions.

---

## Q6: Why use `AddFromObject(new OnboardingPlugin())` instead of `AddFromType<OnboardingPlugin>()`?

**Answer:** `AddFromObject` registers a specific instance. `AddFromType` creates a new instance per call. Since `OnboardingPlugin` holds state (`_state` field), you need one shared instance across the entire conversation. If SK created a new instance each time, state would be lost between function calls.

**Interview tip:** This is the same concept as DI lifetimes — Singleton vs Transient. `AddFromObject` = Singleton behavior. `AddFromType` = Transient behavior.
