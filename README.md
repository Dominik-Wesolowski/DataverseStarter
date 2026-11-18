# Dataverse Starter

**Start with business logic. Keep the plugin plumbing ready.**

A reusable .NET template for Microsoft Dataverse plugins: typed execution context,
automatic dependency injection, Sparkle deployment and a signed, merged assembly
from a single build command.

Create a project under your own name, add your entities and services, and build a DLL
ready for Plugin Registration Tool. No company-specific configuration or CRM credentials included.

## What you get

- **PAC-generated foundation** — the original Microsoft PluginBase stays unmodified; a thin wrapper adds typed context and scoped DI.
- **One build, one deployment assembly** — ILRepack merges application and DI dependencies, signs the DLL and validates the result. Microsoft SDK assemblies remain external.
- **Entity-first structure** — plugins, services and repositories have predictable homes under Entities; reusable code lives under Shared.
- **Automatic service registration** — matching interfaces and implementations are discovered by convention.
- **Tests through the entry point** — exercise IPlugin.Execute, guards, real DI, services and repositories with a mocked Dataverse boundary.
- **An optional Account example** — Create/Update plugins, service, repository, minimal OOTB model and tests.

## Create your project

You need Windows, a current .NET SDK, Node.js/npm and Power Platform CLI (`pac`).
Plugins target .NET Framework 4.6.2; tests target 4.7.2. The SDK version is not pinned.

Clone this repository, then install the local template from its root:

```powershell
dotnet new install .
```

Create a clean project:

```powershell
dotnet new dataverse-starter -n XYZ -o D:\XYZ
```

Or include the working Account example:

```powershell
dotnet new dataverse-starter -n XYZ -o D:\XYZ --include-account-example
```

The name is yours: `XYZ` produces `XYZ.sln`, `XYZ.Plugins`, `XYZ.Application`
and `XYZ.EarlyBound`. Use `XYZ.Crm` if you want that prefix instead.

```powershell
cd D:\XYZ
npm run setup
npm run check
```

`setup` creates the local signing key if missing and generates the original runtime with
`pac plugin init`. Setup and local tests do not require a CRM connection.
Build does not create a key automatically: run setup before your first build.
Keep the signing key stable once an assembly has been deployed; it is ignored by Git.

To update the installed template after editing this source repository:

```powershell
dotnet new install . --force
```

## Everyday commands

| Command | Purpose |
| --- | --- |
| `npm run setup` | Initialize signing and generate the PAC runtime. |
| `npm run build` | Build **Debug**, merge, sign and validate the plugin DLL. |
| `npm run build:release` | Build and validate **Release** explicitly. |
| `npm test` | Run application and plugin tests in Debug. |
| `npm run test:artifact` | Run plugin tests against the merged Debug DLL; build first. |
| `npm run check` | Build, run source tests and test the merged DLL. |
| `npm run model:generate` | Generate early-bound models from the active PAC environment. |
| `npm run plugins:register` | Register the Debug assembly and steps using Sparkle. |
| `npm run runtime:generate` | Regenerate the original PAC runtime and its provenance manifest. |
| `npm run plugins:init` | Create a signing key only if missing; also called by setup. |

## Your deployment DLL

After `npm run build`, select this file in Plugin Registration Tool:

```text
artifacts/plugins/Debug/XYZ.Plugins.dll
```

Release output goes to `artifacts/plugins/Release/XYZ.Plugins.dll`.
In this template source, the assembly prefix is `Company.Crm`.

For Sparkle registration:

```powershell
npm run plugins:register
# After npm run build:release:
npm run plugins:register -- /p:release
```

Sparkle manages its own authentication. It does not reuse the active PAC login.
Neither setup nor check deploys anything.

## Project layout

```text
src/
  XYZ.Plugins/
    Entities/Accounts/         Plugin entry points
    Shared/                   Context, bases, guards, DI and registration
  XYZ.Application/
    Entities/Accounts/
      Service/                Business logic and its interface
      Repository/             Dataverse queries and their interface
    Shared/                   Service base, entity helpers and configuration
  XYZ.EarlyBound/
    Generated/                Replaceable early-bound models
    builderSettings.json      PAC model generation settings
tests/
  XYZ.Application.Tests/
  XYZ.Plugins.Tests/
scripts/                      Setup, generation, deployment and validation
build/                        Merge configuration and local signing key
```

Account folders are included only when requested. To add a feature, follow the same
`Entities/<Entity>/Service` and `Repository` layout. Public concrete classes in those
Application namespaces are registered as scoped services through a matching
`I<ClassName>` interface in the same namespace. Missing contracts fail explicitly.

## Writing plugins

Declare the entity type once. The inherited PluginContext provides the typed target:

```csharp
public sealed class PreCreateAccountPlugin()
    : PluginBase<Account>(typeof(PreCreateAccountPlugin))
{
    protected override void Execute(PluginContext context)
    {
        context.Get<IAccountService>().NormalizeAccountName(context.TargetEntity);
    }
}
```

Add the Sparkle registration attribute and guards for your step; the optional Account
example shows the complete registration. The non-generic Shared.PluginBase also supports
EntityReference targets and Custom APIs.

Write PreOperation changes to TargetEntity. FullTargetEntity is a merged reading snapshot
(PreImage → Target → PostImage), with explicit nulls taking precedence. Required named
images have dedicated accessors. IsChanged requires a preimage outside Create.
Never store invocation state on a plugin instance: Dataverse can reuse it concurrently.

Shared helpers include required and aliased attribute reads, effective values, selected-field
patches and typed environment-variable reads. Configuration caching is scoped to one reader
instance. BuildPatch performs no SDK operation: provide an independent baseline with the
compared columns loaded, and skip Update when the patch has no attributes.
ServiceBase exposes explicit paging rather than an unbounded retrieve-all loop.

## Generate real models

Review `src/XYZ.EarlyBound/builderSettings.json`, then choose the target PAC profile:

```powershell
pac auth list
pac auth select --index <profile-index>
npm run model:generate
```

The optional Account model is a minimal example for offline builds and tests.
PAC intentionally overwrites it with metadata from your environment.
Review generated files before committing them.

## Testing

Focused unit tests cover services and helpers. PluginScenario runs the public plugin
entry point with the generated runtime, registration guards and real application composition.
Inspect Target snapshots, traces, outputs, shared variables and SDK calls, including failures.
Only the platform boundary is mocked. These tests do not emulate server-side transactions,
rollback, security or automatic step dispatch; verify those behaviors in Dataverse.

The source template can also verify both generated project variants locally:

```powershell
powershell.exe -NoProfile -File scripts/TestTemplate.ps1
```

This source-only script is excluded from generated projects. No hosted CI workflow is included.

## Acknowledgements

The execution API builds on ideas from
[CrmBasePluginFramework](https://github.com/Dominik-Wesolowski/CrmBasePluginFramework).
Its retained license notice is in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).
The Microsoft runtime and Sparkle registration source retain their original notices.
