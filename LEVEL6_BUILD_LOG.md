# Level 6 build log - Azure integration

Running notes while doing `LEVEL6_AZURE_SPEC.md`. Open items live at the TOP so nothing
gets lost between sessions.

================================================================
## >>> OPEN ITEMS - THINGS STILL TO DO OR DECIDE <<<
================================================================

- [x] **BILLING ALERT - DONE 2026-09-09.** There was NONE before today, despite the cost
      guard rule saying to keep one. Subscription is **PayAsYouGo with spendingLimit: Off**,
      so nothing stops charges automatically - the alert is the only warning.
      Created `budget-monthly-20`: AUD 20/month, notifications to
      `cues.chive_3e@icloud.com` at actual>50%, actual>90%, and FORECAST>100%.
      The forecast one is the useful alert - it fires on trajectory, before the money is
      spent, so a forgotten resource surfaces early rather than at the invoice.
      Verify any time with:
      az rest --method get --url https://management.azure.com/subscriptions/33c50466-99f6-4396-b754-085462b4948c/providers/Microsoft.Consumption/budgets?api-version=2023-05-01

- [ ] **AI Search is currently KEY-ONLY auth.** The create call returned
      `"authOptions": {"apiKeyOnly": {}}`. `DefaultAzureCredential` (step 4 of the spec)
      WILL BE REJECTED until Entra ID auth is enabled on the service. Do not treat that
      403 as a mystery - this is the cause.
      THE FIX (run at step 4, not before - see the 403 first, that is the exercise):
      ```
      az search service update --name srch-skpractice-33c50466 --resource-group rg-skpractice-level6 --auth-options aadOrApiKey --aad-auth-failure-mode http401WithBearerChallenge
      ```
      `aadOrApiKey` allows BOTH Entra ID and keys - the practical setting while switching
      over, since a key is still available as a fallback. `aad` is Entra-only and stricter,
      closer to what production would use. Know the difference; it is a fair question.

- [ ] **DOCKER DESKTOP UPDATE DEFERRED (2026-09-09).** It asked to update while Qdrant
      was running; postponed because an update restarts the daemon and stops running
      containers. Do it before T5 (AKS). Backup/restore steps and a full inventory:
      D:/Training/Docker/DOCKER_UPDATE_BACKUP_RESTORE.md
      KEY FACT: the `qdrant` container has NO VOLUME MOUNT, so the hr_policies
      collection lives in its writable layer and dies with the container. Use
      `docker commit` to preserve it, or recreate Qdrant with a named volume.

- [ ] **TEARDOWN when the level is finished:**
      `az group delete --name rg-skpractice-level6 --yes --no-wait`

- [ ] **SK version decision.** Currently on 1.73.0; latest is 1.80.1. Spec recommends
      swapping to Azure FIRST on 1.73.0, then upgrading as a separate step, so a break has
      one cause.

- [ ] Update `STATE.md` - its Level 6 line still says "Blocker: Azure subscription was
      cancelled". FALSE as of 2026-09-09, the subscription is Enabled.

- [ ] Update `README.md` "Honest gaps" - it says Azure was never done. Will be wrong once
      this level lands.

================================================================
## WHAT IS PROVISIONED (as at 2026-09-09)
================================================================

```
subscription   33c50466-99f6-4396-b754-085462b4948c  "Azure subscription 1"
               PayAsYouGo_2014-09-01, spendingLimit: Off, month-to-date 0.00
tenant         2ad5deb9-2a56-4945-a114-afe73fa2e6b0
login          cues.chive_3e@icloud.com
region         australiaeast

resource group rg-skpractice-level6                      CREATED
AI Search      srch-skpractice-33c50466                   CREATED, sku=free, running
                 semanticSearch: free  <- semantic re-ranking IS available
                 authOptions: apiKeyOnly  <- see open item above
Azure OpenAI   aoai-skpractice-33c50466                   CREATED, sku S0, Succeeded
  ENDPOINT       https://aoai-skpractice-33c50466.openai.azure.com/
                 ^ THIS is what the code needs in step 2
  deployment     gpt-5-mini  (2025-08-07, GlobalStandard)  CREATED, Succeeded
                 ^ NOT gpt-4o-mini (deprecating), NOT gpt-5.4-mini (zero quota).
                   See "the three gates" finding below.
  deployment     text-embedding-3-small (v1, Standard)     CREATED, Succeeded
```

STEP 1 IS COMPLETE. Verify any time with:
az cognitiveservices account deployment list --name aoai-skpractice-33c50466 --resource-group rg-skpractice-level6 -o table

### WHAT THE CODE NEEDS IN STEP 2
```
endpoint                  https://aoai-skpractice-33c50466.openai.azure.com/
chat deployment name      gpt-5-mini              <- replaces the "gpt-4o-mini" string
embedding deployment name text-embedding-3-small  <- unchanged, 1536 dims
search endpoint           https://srch-skpractice-33c50466.search.windows.net
```
The chat DEPLOYMENT NAME changing from gpt-4o-mini to gpt-5-mini is the concrete example
of the deployment-name-vs-model-name point: Program.cs refers to what YOU named the
deployment, not to a model.

CLI TIP: these create calls dump hundreds of lines of JSON. Add `--output none` to silence
them (you only see output if it FAILS), or
`--query "{endpoint:properties.endpoint, state:properties.provisioningState}" -o table`
for just the useful fields.

### Commands still to run (step 1 of the spec)
```
az cognitiveservices account create --name aoai-skpractice-33c50466 \
  --resource-group rg-skpractice-level6 --location australiaeast \
  --kind OpenAI --sku S0 --custom-domain aoai-skpractice-33c50466 --yes

az cognitiveservices account deployment create --name aoai-skpractice-33c50466 \
  --resource-group rg-skpractice-level6 --deployment-name gpt-4o-mini \
  --model-name gpt-4o-mini --model-version 2024-07-18 --model-format OpenAI \
  --sku-name GlobalStandard --sku-capacity 10

az cognitiveservices account deployment create --name aoai-skpractice-33c50466 \
  --resource-group rg-skpractice-level6 --deployment-name text-embedding-3-small \
  --model-name text-embedding-3-small --model-version 1 --model-format OpenAI \
  --sku-name Standard --sku-capacity 10
```
`--custom-domain` is REQUIRED - Azure OpenAI needs it for the endpoint URL, and
DefaultAzureCredential does not work without it.

================================================================
## FINDINGS - things learned or verified today
================================================================

**Providers had to be registered first.** `Microsoft.CognitiveServices` came back
`NotRegistered`. That is a one-time per-subscription registration, NOT an access denial -
easy to misread as "Azure OpenAI is not approved for me". Fixed with
`az provider register --namespace Microsoft.CognitiveServices` (and `Microsoft.Search`).
Took a few minutes to move Registering -> Registered.

**Both required models ARE available in australiaeast:**
```
gpt-4o-mini             2024-07-18   GlobalStandard
text-embedding-3-small  1            Standard
```
Same two models already used against OpenAI directly, so cost stays roughly flat.

**Azure OpenAI is not available on free-tier subscriptions.** PayAsYouGo is required. There
is no cheaper route for this build.

**Deployment name vs model name are different things.** Your code refers to the DEPLOYMENT
name. They have been made identical here for simplicity, but they need not be.

**gpt-4o-mini IS REFUSED FOR NEW DEPLOYMENTS (2026-09-09).** The error:
```
(ServiceModelDeprecating) The model 'Format:OpenAI,Name:gpt-4o-mini,Version:2024-07-18'
is in deprecating state and cannot be used for new deployments.
```
THE INTERESTING PART: `az cognitiveservices model list` HAPPILY LISTS gpt-4o-mini for
australiaeast, and its published deprecation date is 2027-04-14 - almost two years away.
It is still refused. So "appears in the model list" does NOT mean "can be deployed", and
the published deprecation date is not the date new deployments stop. Check by ATTEMPTING
the deployment; the list is not authoritative.

**THEN gpt-5.4-mini FAILED TOO - a DIFFERENT gate.** The error:
```
(InsufficientQuota) This operation require 10 new capacity in quota One Thousand Tokens
Per Minute - gpt-5.4-mini - GlobalStandard, which is bigger than the current available
capacity 0. The current quota usage is 0 and the quota limit is 0.
```
Nothing wrong with the account. Azure allocates TPM quota PER MODEL PER REGION, and most
models sit at a limit of 0 until you request an increase.

**THE THREE GATES - this is the actual lesson.** "Is the model available?" has three
separate answers, and they are checked in different places:
```
1  LISTED       az cognitiveservices model list      does the model exist in the region
2  DEPLOYABLE   only found by ATTEMPTING it          not blocked by deprecation
3  HAS QUOTA    az cognitiveservices usage list      TPM limit > 0 for that model+sku
```
gpt-4o-mini passed 1, failed 2. gpt-5.4-mini passed 1 and 2, failed 3. Only checking the
model list tells you almost nothing.

CHECK QUOTA BEFORE PICKING A MODEL:
az cognitiveservices usage list --location australiaeast -o table

REPLACEMENT FINALLY CHOSEN: `gpt-5-mini` version `2025-08-07`, sku GlobalStandard.
It had 500 (thousand TPM) quota - the highest of any OpenAI chat model in this
subscription/region. Everything else in the OpenAI family was 0 or tiny.
NOTE: the `AIServices.*` quota entries (DeepSeek, Llama, Phi, Mistral, grok, etc) are a
DIFFERENT resource kind and are NOT usable from an `--kind OpenAI` account.

**THE EMBEDDING MODEL MUST NOT CHANGE.** `text-embedding-3-small` v1 is still current
(deprecates 2028-02-09). `HRPolicyRecord` declares `[VectorStoreVector(1536)]`, which is
that model's dimensionality. Swapping to a different embedding model with different
dimensions would break the index and force re-embedding everything. This is worth
understanding rather than just obeying: the vector dimension is baked into the index.

**AI Search Free tier: one per subscription.** Free was available and taken. Basic is
roughly USD 75/month - if Free had failed on quota, that was a STOP-and-ask, not a
substitute.

### Cost cleanup done 2026-09-09
Two resource groups left over from pre-EE Azure practice were deleted:
```
rg-sbpractice              sbpractice-deepak-33c50466  Basic     (idle = ~free)
                           sbpractice-std-33c50466     Standard  (~USD 10/month IDLE)
rg-mi-keyvault-practice    kv-practice-33c50466        Key Vault (free at rest)
```
The Standard Service Bus namespace was the only one actually costing money while idle.
NOTE: Key Vault soft-delete is on by default, so `kv-practice-33c50466` stays recoverable
for 90 days and THE NAME STAYS RESERVED. Creating a vault with that exact name will fail
until it is purged.

LESSON, and it is the point of the cost guard: a paid resource sat there forgotten for
months. `spendingLimit: Off` means nothing would have stopped it.

================================================================
## STEP 2 - what actually happened
================================================================

DONE 2026-09-09. Chat and embeddings now come from Azure OpenAI; Qdrant is still the store.

THE CODE CHANGE (Program.cs, the Kernel.CreateBuilder chain):
```
AddOpenAIChatCompletion("gpt-4o-mini", apiKey)
AddOpenAIEmbeddingGenerator("text-embedding-3-small", apiKey)
        becomes
AddAzureOpenAIChatCompletion(deploymentName: "gpt-5-mini",
                             endpoint: "https://aoai-skpractice-33c50466.openai.azure.com/",
                             apiKey: apiKey)
AddAzureOpenAIEmbeddingGenerator(deploymentName: "text-embedding-3-small",
                                 endpoint: "https://aoai-skpractice-33c50466.openai.azure.com/",
                                 apiKey: apiKey)
```

VERIFIED: mode 2, asked "how much annual leave do I get after 6 years", got back
"Section 4.2 - Employees with 5+ years of service receive 25 days of paid annual leave
per year" plus the carry-over and accrual detail. Correct section, correct answer.

### THINGS THAT WENT WRONG, AND WHY (the useful part)

**1. The OpenAI method has no endpoint; the Azure one does.** Starting from
`AddOpenAIChatCompletion`, IntelliSense showed apiKey as the SECOND parameter, which looked
like the endpoint was unwanted. It was simply the wrong method. OpenAI has ONE endpoint
(api.openai.com) and you name a MODEL; Azure OpenAI gives you your OWN endpoint and you
name a DEPLOYMENT you created. That difference is the whole reason the signatures differ.

**2. Relative path + wrong working directory = "API key not found".**
```
Unhandled exception. System.InvalidOperationException: OpenAI API key not found.
```
The key was fine. `File.Exists("SecretKey.md")` resolves against the WORKING DIRECTORY, and
the run was launched from D:/Training/Azure/Practice. `dotnet run --project <path>` says
what to BUILD, not where to RUN FROM - the process inherits the shell's cwd.
FIX: cd into the project folder first, then `dotnet run`.
THE REAL LESSON: a relative path resolves against the working directory, NOT the assembly
location. Locally those are usually the same so it never shows up; in a container or under
a service host they differ and config silently goes missing. The robust form is
`Path.Combine(AppContext.BaseDirectory, "SecretKey.md")`.

**3. gpt-5-mini worked with no parameter changes.** It is a reasoning model and
gpt-4o-mini was not, so an unsupported-parameter error (temperature is the usual one) was
plausible. It did not happen.

### A MISMATCH THAT IS CURRENTLY HARMLESS - understand it before step 3
The vectors in Qdrant were created in APRIL by **OpenAI's** text-embedding-3-small.
Queries are now embedded by **Azure's** deployment of the same model. Same model, same
1536 dimensions, so the vectors are comparable and retrieval works.
But it IS a mismatch, and it is the kind that fails silently rather than loudly - you would
see slightly worse retrieval, not an exception. Step 3 re-ingests into Azure AI Search,
which removes it. Worth knowing that embedding-model drift is a real production hazard:
change the embedding model and every stored vector must be regenerated.

================================================================
## STEP 3 - what actually happened
================================================================

DONE 2026-09-09. The vector store is now Azure AI Search. Qdrant is out of the path.

VERIFIED ON THE AZURE SIDE (not just trusting the app's console message). The index was
created from the C# attributes:
```
INDEX: hr-policies
   Id            Edm.String                          <- [VectorStoreKey]
   SectionText   Edm.String                          <- [VectorStoreData]
   Embedding     Collection(Edm.Single)  dims=1536   <- [VectorStoreVector(1536)]
   vectorSearch profile: EmbeddingProfile
```
Note what that means: the attributes on HRPolicyRecord were translated into an Azure AI
Search index definition INCLUDING a vector search profile that was never written by hand.
That is what Microsoft.Extensions.VectorData buys - the same record class drove Qdrant and
Azure, with only the KEY TYPE differing.

Console output changed from "already ingested, skipping" to "Ingested 16 sections into
Azure AI Search" - proof the index really was empty and really was populated. Retrieval
then returned Section 4.2 correctly.

### THE CHANGES
```
Models/HRPolicyRecord.cs     [VectorStoreKey] ulong Id  ->  string Id
Services/HRPolicyService.cs  VectorStoreCollection<ulong, ...>  ->  <string, ...>
                             GetAsync(0ul)          ->  GetAsync("0")
                             for (ulong i = 0; i < (ulong)sections.Length; i++)
                                                    ->  for (int i = 0; ...)
                             Id = i                 ->  Id = i.ToString()
Program.cs                   QdrantCollection<...>(QdrantClient("localhost", 6334),
                                                   "hr_policies", true)
                                       ->  AzureAISearchCollection<string, HRPolicyRecord>(
                                               new SearchIndexClient(
                                                   new Uri("https://srch-skpractice-33c50466.search.windows.net"),
                                                   new AzureKeyCredential(searchKey)),
                                               "hr-policies")
```
Constructor confirmed from the package XML docs, not guessed:
`AzureAISearchCollection(SearchIndexClient, string name, AzureAISearchCollectionOptions = null)`
The Qdrant version's third argument (`true`, its create-if-missing flag) has no equivalent
here - `EnsureCollectionExistsAsync()` in the ingest method already handles creation.

### FINDINGS

**The ulong -> string key change was the whole migration cost.** Azure AI Search document
keys MUST be strings; Qdrant accepted numeric keys. Everything else was mechanical. THE
LESSON: a storage abstraction hides the API surface but NOT the underlying store's
constraints. The code compiled fine as `QdrantCollection<string, ...>` even though Qdrant
would have rejected string keys at runtime - so the type system did not protect against
that, only the migration direction happened to be safe.

**Azure AI Search key CHARACTERS are restricted too** - letters, digits, underscore, dash,
equals only. Plain numeric strings ("0".."15") are fine. Using a section heading as the id
would have been rejected for containing spaces and full stops.

**A package downgrade blocked the install first:**
```
error NU1605: Detected package downgrade: Microsoft.Extensions.VectorData.Abstractions
from 10.1.0 to 10.0.1
```
The project pinned 10.0.1 DIRECTLY; the AzureAISearch connector needs >= 10.1.0. A direct
reference wins over a transitive one, and NuGet treats the resulting downgrade as an error
rather than silently breaking the connector's contract. FIX: bump the direct reference to
10.1.0.

**The ingest guard is still fragile.** It only checks whether document "0" exists. A
partial ingest leaves document 0 present and the rest missing, and the guard then reports
"already ingested". Not fixed - noted because it is the kind of check that hides a
half-populated index.

**Qdrant is now unused** but the container is still running and the
Connectors.Qdrant package is still referenced. Both can go. The old `hr_policies`
collection in Qdrant (OpenAI-era vectors from April) is now dead data - which also means
the OpenAI-vs-Azure embedding mismatch noted in step 2 is gone: these 16 vectors were
generated by the Azure deployment.

================================================================
## STEP 4 - what actually happened
================================================================

DONE 2026-09-09. No API key is read anywhere in the code path now. SecretKey.md and
SearchKey.md still exist on disk but nothing uses them.

### THE 403, SEEN BEFORE ASSIGNING ROLES (this was the exercise)
```
Microsoft.Extensions.VectorData.VectorStoreException: Call to vector store failed.
 ---> Azure.RequestFailedException: Service request failed. Status: 403 (Forbidden)
   at Azure.Search.Documents.SearchClient.GetDocumentInternal[T](...)
   at SKPractice.Services.HRPolicyService.IngestDocumentAsync() ... line 25
```
READ WHAT IT ACTUALLY SAYS - this is the useful bit:
- It is a **403 Forbidden**, not a 401. So DefaultAzureCredential SUCCESSFULLY GOT A TOKEN
  and Azure AI Search ACCEPTED it. Authentication worked. AUTHORISATION did not.
  A 401 would have meant the credential chain found nothing.
- The failing call is `GetDocumentAsync` at HRPolicyService.cs line 25 - the ingest guard's
  DOCUMENT READ. So the DATA plane refused before the control plane was ever reached.
That distinction (authenticated but not authorised, data plane vs control plane) is the
whole point of the step.

### THE THREE ROLES AND WHY EACH ONE
```
Search Index Data Contributor   on the SEARCH service  - DATA plane: read/write documents
                                                         INSIDE an index. This is the one
                                                         the 403 was about.
Search Service Contributor      on the SEARCH service  - CONTROL plane: create/delete
                                                         indexes. EnsureCollectionExists
                                                         needs it.
Cognitive Services OpenAI User  on the OPENAI account  - separate resource, separate role.
                                                         Would have failed next.
```

### THE GUEST-ACCOUNT TRAP
`--assignee cues.chive_3e@icloud.com` FAILED:
```
Cannot find user or service principal in graph database for 'cues.chive_3e@icloud.com'
```
The account is a GUEST (external) identity. Its real UPN is
`cues.chive_3e_icloud.com#EXT#@cueschive3eicloud.onmicrosoft.com` - the sign-in email is
NOT the directory identity.
FIX: use the object id and state the principal type, which skips the graph lookup entirely:
```
az ad signed-in-user show --query id -o tsv        ->  ab952b7c-63ac-4c55-a812-ccc9db7458dd
az role assignment create --assignee-object-id <id> --assignee-principal-type User --role "<role>" --scope <resource id>
```
(NOTE: the directory displayName on this account is "Ravi Malhotra", not Deepak. That is
just how the tenant was set up; it is the identity the role assignments attach to.)

### AI SEARCH HAD TO BE SWITCHED OFF KEY-ONLY FIRST
The service was created with `authOptions: {apiKeyOnly: {}}`, which rejects Entra tokens
outright. Fixed with:
```
az search service update --name srch-skpractice-33c50466 --resource-group rg-skpractice-level6 --auth-options aadOrApiKey --aad-auth-failure-mode http401WithBearerChallenge
```
`aadOrApiKey` accepts BOTH, so a key remains available as a fallback. `aad` alone is the
stricter, production-shaped setting. `disableLocalAuth` is still False.
WATCH OUT: the CLI response after that update did NOT echo `semanticSearch`, which looked
like it had been cleared. It had not - a direct ARM GET confirmed `semanticSearch: free` is
still set. Do not trust an update response's omissions; verify with a GET.

### THE CODE CHANGE, AND A REAL API INCONSISTENCY
```
new AzureKeyCredential(searchKey)   ->   new DefaultAzureCredential()
apiKey: <string>                    ->   credentials: new DefaultAzureCredential()
```
THE PARAMETER NAME IS NOT THE SAME ON BOTH METHODS, in the same package version:
```
AddAzureOpenAIChatCompletion       credentials   (PLURAL)
AddAzureOpenAIEmbeddingGenerator   credential    (SINGULAR)
```
Confirmed by reading the package XML docs, not by guessing. Two compile errors came from
this: CS0119 (wrote the type name without `new`), then CS1503 (kept `apiKey:` as the
parameter name, which pinned the compiler to the STRING overload). LESSON: named arguments
make a call readable but they also PIN you to one overload - passing positionally lets the
compiler pick the right overload from the argument types.

### WHY THIS IS WORTH MORE THAN A KEY IN A FILE
The same code authenticates locally (via the Azure CLI login) and would authenticate in
Azure via Managed Identity, with no branch and no key in either place. There is no secret
to leak, rotate, or accidentally commit - which matters for this repo specifically, since
an OpenAI key was already stripped from its history with git-filter-repo in May 2026.

VERIFIED: run came back "HR Policy already ingested, skipping" (the guard's read now
succeeds), then mode 2 returned Section 4.2, 25 days, with accrual and carry-over detail.

================================================================
## EXPECTED COST FOR THIS LEVEL
================================================================
```
AI Search          Free tier              $0     50MB / 3 indexes; 16 sections is nothing
Azure OpenAI       gpt-4o-mini            cents  same model as before
                   text-embedding-3-small cents  16 sections, one-off embed
```
Should land well under a dollar. If it does not, something is wrong - check for a stray
resource.

================================================================
## PROGRESS AGAINST THE SPEC
================================================================
```
STEP 1  provision resources           DONE 2026-09-09
STEP 2  swap models to Azure OpenAI   DONE 2026-09-09, verified working
STEP 3  swap Qdrant to AI Search      DONE 2026-09-09, verified on the Azure side
STEP 4  DefaultAzureCredential        DONE 2026-09-09, 403 seen first as intended
STEP 5  hybrid search experiment      not started  <- the only part that is NOT a port
```

## Related
`LEVEL6_AZURE_SPEC.md` - the spec being followed
`../SEMANTIC_KERNEL_BRUSHUP.md` - what Levels 1-5 built
`D:/Training/InterviewPrep/PRIORITY_PLAN_REVISED_2026-09-08.md` - this is T1
