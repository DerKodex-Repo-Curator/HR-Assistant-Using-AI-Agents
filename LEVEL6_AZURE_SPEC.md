# Level 6 - Azure integration. Step spec.

This is T1 in `D:/Training/InterviewPrep/PRIORITY_PLAN_REVISED_2026-09-08.md`.
Written 2026-09-09.

WHY THIS IS FIRST: Raghu (SarasAmerica resource manager) named "GenAI / RAG, Azure OpenAI,
Azure AI Search" as his first question. The US resume Professional Summary already claims
RAG work. You have the pipeline; it just runs against OpenAI and Qdrant instead of Azure.
This level closes that gap, and it is the LAST level of a project you already built.

PART 1 = what to do, in order.  PART 2 = why.  PART 3 = the questions to be able to answer.

================================================================
## PART 0 - BEFORE ANY CODE
================================================================

### 0.1 STATE.md is out of date - fix it
It says "Blocker: Azure subscription was cancelled, needs reactivation".
Checked 2026-09-09: `az account show` reports **Azure subscription 1, State: Enabled**.
The blocker is stale. Update that line when you start.

### 0.2 Azure CLI token has expired
Run this yourself (interactive, opens a browser):
```
az login --scope https://management.core.windows.net//.default
```

### 0.3 CONFIRM AZURE OPENAI ACCESS BEFORE ANYTHING ELSE
Azure OpenAI is not automatically available on every subscription. Check FIRST, because if
access is not granted the whole level changes shape:
```
az cognitiveservices account list --output table
az provider show --namespace Microsoft.CognitiveServices --query registrationState -o tsv
```
IF AZURE OPENAI IS NOT AVAILABLE: do not stall. Do Azure AI Search alone (swap the vector
store, keep OpenAI for embeddings and chat). That still covers two thirds of Raghu's first
ask, and the AI Search half is the part you have never done.

### 0.4 THE COST GUARD APPLIES (project_azure_cost_guard)
- ONE resource group. Name it so you can find it: `rg-skpractice-level6`.
- AI Search: **Free tier** if available (limits: 50MB, 3 indexes - your 16 sections are
  nothing). Basic if Free is taken.
- Azure OpenAI: gpt-4o-mini and text-embedding-3-small. Same models you already use, so
  cost stays roughly what it is now.
- **`az group delete` WHEN DONE.** Keep the billing alert on.
- Write the teardown command in your BUILD LOG on day one, not day three.

================================================================
## PART 1 - WHAT TO DO
================================================================

The good news, and notice this before you start: your code is ALREADY provider-neutral in
the two places that matter.

```
IHRPolicyService        2 methods only: IngestDocumentAsync, SearchPolicyAsync
HRPolicyRecord          uses Microsoft.Extensions.VectorData attributes
                        [VectorStoreKey] [VectorStoreData] [VectorStoreVector(1536)]
                        - these are NOT Qdrant-specific
```
So `HRPolicyService.cs` should barely change. What changes is `Program.cs` (what gets
registered) and the packages.

Do it in this order. Each step is runnable - do NOT do all five then run once.

----------------------------------------------------------------
### STEP 1 - Provision the Azure resources
----------------------------------------------------------------
```
resource group        rg-skpractice-level6
Azure OpenAI          one account, two DEPLOYMENTS:
                        gpt-4o-mini            (chat)
                        text-embedding-3-small (embeddings, 1536 dims)
Azure AI Search       one service, Free tier if available
```
OBSERVE, do not skip: in Azure OpenAI you deploy a MODEL and give the deployment a NAME.
Your code then refers to the DEPLOYMENT NAME, not the model name. They can differ. This
trips people up and it is a fair thing to be asked about.

Record in the build log: endpoint URLs, deployment names, the AI Search service name.
NOT the keys - see step 4.

----------------------------------------------------------------
### STEP 2 - Swap the chat and embedding models to Azure OpenAI
----------------------------------------------------------------
Packages (verified on NuGet 2026-09-09):
```
Microsoft.SemanticKernel                        1.80.1   (you are on 1.73.0)
Microsoft.SemanticKernel.Connectors.AzureAISearch 1.74.0-preview
Azure.Identity                                  1.21.0
```
NOTE: you are on SK 1.73.0. Decide whether to upgrade to 1.80.1 as part of this or stay
put. Upgrading mid-swap means two variables changing at once - if something breaks you will
not know which caused it. RECOMMENDATION: swap FIRST on 1.73.0, get it green, THEN upgrade
as a separate step.

In `Program.cs`, the builder currently reads:
```csharp
.AddOpenAIChatCompletion("gpt-4o-mini", apiKey)
.AddOpenAIEmbeddingGenerator("text-embedding-3-small", apiKey)
```
The Azure equivalents take a DEPLOYMENT NAME and an ENDPOINT as well as credentials. Find
the `AddAzure...` overloads - do not guess the signatures, let IntelliSense or the SK docs
show you. If completion is unreliable, the package's XML docs are the source of truth.

RUN IT AT THIS POINT. Chat and embeddings now come from Azure; Qdrant is still the store.
Two things changed, not four. If it breaks, the cause is in this step.

----------------------------------------------------------------
### STEP 3 - Swap Qdrant for Azure AI Search
----------------------------------------------------------------
Currently in Program.cs:
```csharp
new QdrantCollection<ulong, HRPolicyRecord>(
    new Qdrant.Client.QdrantClient("localhost", 6334), "hr_policies", true)
```
Replace with the Azure AI Search collection type from the connector package.

ONE THING WILL BITE YOU - think about it before you hit it. Your key is `ulong`:
```csharp
[VectorStoreKey] public ulong Id { get; set; }
```
Azure AI Search document keys are **strings**. Qdrant accepted a numeric key; AI Search
will not. So `HRPolicyRecord.Id` has to become a `string`, and `IngestDocumentAsync` has to
produce string ids instead of the `ulong i` loop counter.

That is a REAL portability lesson, not a nuisance: the vector-store abstraction hides the
API but NOT the underlying key-type constraints. Write that down - it is the kind of
concrete detail that shows you actually ran this rather than read about it.

RUN IT. Now the store has changed too.

----------------------------------------------------------------
### STEP 4 - DefaultAzureCredential instead of API keys
----------------------------------------------------------------
Right now you read a key from `OPENAI_API_KEY` or `SecretKey.md`. Azure lets you drop the
key entirely.

```csharp
new DefaultAzureCredential()
```
It tries a CHAIN of credential sources in order - environment variables, Managed Identity,
Visual Studio, Azure CLI, and others. Locally it will pick up your `az login`. Deployed to
Azure it would use Managed Identity. Same code, no key in either place.

YOU MUST ALSO GRANT RBAC or it will fail with a 403 that looks like a config error:
```
Cognitive Services OpenAI User        on the Azure OpenAI account
Search Index Data Contributor         on the AI Search service (write, for ingest)
Search Service Contributor            on the AI Search service (create the index)
```
OBSERVE: run it BEFORE assigning the roles and read the actual error. Then assign and run
again. Seeing the 403 and knowing which role fixed it is worth more than being told.

TERMINOLOGY, and this matters (it is in the USJobs CLAUDE.md as a standing rule): in Azure
it is **Managed Identity + RBAC + Entra ID**. "IAM user" is AWS. Do not mix them.

----------------------------------------------------------------
### STEP 5 - Hybrid search (the part that is genuinely new)
----------------------------------------------------------------
Everything above is a port. THIS step teaches you something you have not done.

Your current search is pure vector similarity - `SearchAsync(vector, top: 3)`. Azure AI
Search can do vector AND keyword (BM25) in one query, and merge the rankings.

WHY IT MATTERS, and this is the thing to be able to say out loud: pure vector search is
bad at EXACT tokens. Product codes, error numbers, policy section numbers, surnames. The
embedding of "Section 14" is close to the embedding of "Section 15". Keyword search nails
it; vector search does not. Hybrid gets both.

DELIBERATE EXPERIMENT - do this, do not skip it:
```
1  ask a CONCEPTUAL question   "how much leave do I get after 6 years"
2  ask an EXACT-TOKEN question "what does Section 14 say"
3  run both against PURE VECTOR search, record the retrieved sections
4  run both against HYBRID search, record again
5  write down which one changed and why
```
That comparison is the single most valuable artifact in this level. It is a result you
measured, not a claim you repeated.

OPTIONAL if time allows: semantic re-ranking on top of hybrid. Know what it is either way -
a second-stage model re-scores the top N results for actual relevance rather than
similarity.

================================================================
## PART 1B - DONE MEANS
================================================================
- [ ] STATE.md stale blocker line fixed
- [ ] Azure OpenAI access confirmed (or the fallback path taken and noted)
- [ ] Resources in ONE resource group, cheap tiers
- [ ] Chat + embeddings on Azure OpenAI, ran green
- [ ] Vector store on Azure AI Search, ran green
- [ ] The ulong -> string key change made and UNDERSTOOD
- [ ] DefaultAzureCredential working, no API key in the code path
- [ ] The 403 seen BEFORE roles were assigned, and the fixing role noted
- [ ] Hybrid vs pure-vector experiment run, both results written down
- [ ] README updated - the "Honest gaps" section currently says Azure was never done
- [ ] `az group delete --name rg-skpractice-level6` RUN, or a diary note for when
- [ ] BUILD LOG written: what surprised you, what broke

================================================================
## PART 2 - WHY
================================================================

## Why this is T1 and not the React build
Raghu named GenAI/RAG/Azure OpenAI/Azure AI Search as his FIRST item, and the US resume
already claims RAG in the Professional Summary. That is a claim already made in writing to
people who are marketing you right now. React/Redux is also a gap, but nobody has asked
about it this week.

## Why swap one thing at a time
Five changes at once gives you five suspects when it breaks. Steps 2, 3 and 4 each change
one layer - models, then store, then auth - and each is runnable. This is the same
discipline as the seven-step list endpoint in the Build 1 spec, and it is the habit that
makes debugging fast.

## Why DefaultAzureCredential is worth the extra RBAC pain
A key in a file is a key that can leak, and this repo already had one commit-stripped with
git-filter-repo in May. DefaultAzureCredential means there is no key to leak. The same code
authenticates locally via `az login` and in Azure via Managed Identity, with no branch.
That "same code in both places" property is the actual point.

## Why hybrid search is the part that matters
Everything else in this level is a port - the same pipeline pointed at different endpoints.
Hybrid search is a genuine capability you do not currently have, and it fixes a real failure
mode you can demonstrate. "Pure vector search could not find Section 14, hybrid could" is a
concrete finding.

================================================================
## PART 3 - BE ABLE TO ANSWER THESE WITHOUT NOTES
================================================================
1. What is the difference between Azure OpenAI and OpenAI directly? (data residency,
   private networking, RBAC/Entra instead of an API key, deployment names, regional quota)
2. Why is the key type a problem when moving Qdrant to AI Search? (ulong vs string, and
   what that says about how far a storage abstraction actually gets you)
3. What does DefaultAzureCredential actually do? (an ordered chain of credential sources;
   name at least three)
4. Which RBAC roles does this need and why two on the Search service? (data plane read/write
   versus control plane index creation)
5. What is hybrid search and when does pure vector search fail? (exact tokens - codes,
   numbers, names. Give the Section 14 example from your own experiment.)
6. What is semantic re-ranking and where does it sit? (second stage over the top N)
7. Your chunking splits on the word "Section ". What breaks with a real corpus?
   (this is already in your README's honest gaps - keep saying it, it is a good answer)

## Related
`D:/Training/AILearning/MicrosoftSemanticKernel/SEMANTIC_KERNEL_BRUSHUP.md` - what Levels
1-5 built, and how to talk about it.
