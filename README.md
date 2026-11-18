# Company.Crm

Dataverse Starter with Sparkle (spkl), ILRepack, PAC generators and automatic DI.

## Build output

`npm run build` builds the solution, merges and signs the plugin, then validates the final DLL.
The PRT artifact is `artifacts/plugins/Debug/Company.Crm.Plugins.dll`.
Debug is the default. Use `npm run build:release` for Release.
The build does not generate a signing key. Run `npm run setup` once before the first build.

## Start

Prerequisites: Windows, the latest installed .NET SDK, Node.js/npm and Power Platform CLI (pac).
Use the .NET Framework targeting dependencies restored by the solution.

```powershell
npm run setup
npm run check
```

setup creates a local signing key and generates the untouched Microsoft plugin runtime
through pac plugin init. No CRM connection is needed for setup or framework tests.
The runtime manifest records the SHA-256 of the actual generator output.

The optional Account example includes a minimal OOTB model for tests and builds offline.
When connecting a real project, select the target PAC profile and replace the sample model:

```powershell
pac auth list
pac auth select --index <profile-index>
npm run model:generate
```

The generator reads metadata from the active PAC environment. Review builderSettings.json
before generation. PAC intentionally overwrites Generated/Entities/account.cs and generates
the remaining target-environment models. The initial Account is only a test/example model,
not full environment metadata. Template validation rejects exporting a full environment model.

## Structure

- src/Company.Crm.Application: Entities for services/repositories; Shared for bases and environment variables.
- src/Company.Crm.EarlyBound: PAC model settings and replaceable early-bound entity models.
- src/Company.Crm.Plugins: Entities for plugins; Shared for bases, context, DI, extensions and registration.
- tests: application and plugin tests, including tests against the merged assembly.
- scripts and build: generation, signing, Sparkle launcher and ILRepack configuration.

Application/Entities/<Entity>/Service and Repository use matching I<ClassName> interfaces.
The DI scanner registers matching implementations automatically within the Application assembly.

## Plugins

Declare the entity type once in PluginBase<Account>. Its inherited PluginContext binds Account:

```csharp
protected override void Execute(PluginContext context)
{
    var target = context.TargetEntity; // Account
}
```

PluginContext exposes Context, OrgService, lazy OrgServiceAdmin, target and images,
FullTargetEntity, typed input parameters, shared variables and Get<T>() for scoped DI.
FullTarget merges PreImage -> Target -> PostImage and copies common mutable SDK column values.
It is not a universal deep clone of specialized objects.
Write PreOperation changes to TargetEntity. An explicit null overrides the previous value.
Images are optional unless required explicitly. IsChanged compares multi-select choices as sets,
binary values by content and lookup identities without display labels. IsChanged requires PreImage outside Create.
Do not store per-invocation state on a plugin instance: Dataverse may reuse it concurrently.
PluginBase without an entity type supports EntityReference targets and Custom APIs.

Source builds default to IncludeAccountExample=false. The minimal model allows offline example tests:
dotnet test DataverseStarter.sln -c Debug -p:IncludeAccountExample=true.
An instantiated --include-account-example project enables it by default.

The optional Account feature is a demonstration that normalizes account names; it is not
included in the default template output. Replace it with actual business requirements.

## Commands

```powershell
npm run build
npm test
npm run build:release
npm run test:artifact
npm run check
```

check builds, tests, merges the signed plugin DLL and tests the merged artifact.
Output: artifacts/plugins/Debug/Company.Crm.Plugins.dll.
SDK assemblies remain external; application and DI dependencies are merged.

Registration uses Sparkle and is an explicit deployment action:

```powershell
npm run plugins:register
npm run plugins:register -- /p:release
```

Sparkle manages its own authentication; it does not inherit PAC's active login.
model:generate uses PAC's active profile. Neither setup nor check deploys anything.
Do not commit credentials or the local build/Plugin.snk key. Keep the key stable
for a deployed assembly's identity and provision it separately in CI.

## New company or solution

Install the source template, then create a separate project with dotnet new dataverse-starter
and a namespace-safe name. See the source repository's docs/TemplateAuthoring.md.
The generated solution already contains its final names; do not rename projects manually.

The execution API is inspired by https://github.com/Dominik-Wesolowski/CrmBasePluginFramework.
See docs/Architecture.md and docs/CrmBasePluginFramework-LICENSE.txt.

## Explicit paging

ServiceBase.RetrievePage<TEntity>(query) returns Records, MoreRecords and PagingCookie.
The caller sets the next query.PageInfo.PagingCookie and increments PageNumber explicitly.
No automatic unbounded reads run inside a synchronous plugin.

## Validation

The Windows CI workflow runs setup and check without deploying. Both template variants
can build offline; the Account variant ships its minimal test model. The template source
also runs scripts/TestTemplate.ps1 in an isolated dotnet hive. See docs/Audit.md in the source
repository for audit decisions and remaining sandbox validation.

Direct inheritance: Shared.PluginBase inherits the original PAC PluginBase.
There is no adapter or second IPlugin.Execute implementation. Concrete plugins pass their type
with a short constructor, e.g. public MyPlugin() : base(typeof(MyPlugin)) { }.
Configuration is forwarded only when needed. PAC owns platform initialization and entry/exit tracing.

SDK selection: global.json does not pin a version, so dotnet uses the latest installed SDK. CI installs the latest stable SDK. This does not change the plugin target framework (net462).

The folder responsibilities and naming decisions are documented in docs/Structure.md.
