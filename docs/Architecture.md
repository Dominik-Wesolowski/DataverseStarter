# Architecture

## Projects and dependencies

Plugins references Application and EarlyBound; Application references EarlyBound.
The backend deliberately uses the Dataverse SDK. No separate Domain, Infrastructure
or Dataverse project is introduced without an actual boundary to enforce.
See Structure.md for the complete folder tree.

## Entities and Shared

Entities/<Entity> contains business code: thin plugin entry points in Plugins,
services and optional repositories in Application. Shared contains reusable bases,
context helpers, configuration readers, DI and SDK extensions. Shared does not mean
one global Utils class. Entity-specific helpers stay with the entity.
Tests mirror the same Entities/Shared split; test fixtures live in Shared/Support.

## Application services

Shared/IService and ServiceBase expose Retrieve<TEntity>, RetrievePage<TEntity> and
Execute<TResponse>. IOrganizationService is injected through constructors.
RetrievePage returns Records, MoreRecords and PagingCookie from a single request;
the caller explicitly advances QueryExpression.PageInfo. No unbounded retrieve-all
loop or implicit all-columns query is used by the base service.

Shared/DependencyInjection in Plugins scans the entire Application namespace for
public concrete types whose namespaces end in .Service or .Repository. Matching
interfaces use I + class name in the same namespace. The contract's Shared namespace
does not restrict scanning to Shared. This also works after assembly merging.
Only registration metadata is cached. Each invocation gets its own DI container/scope,
disposed on success and on failure. SDK services use the step user, not implicit SYSTEM.

## Plugin runtime

Company.Crm.Plugins.Shared.PluginBase directly inherits Company.Crm.Plugins.PluginBase,
the untouched PAC-generated class physically stored in Shared/Generated/PluginBase.cs.
No adapter or second public IPlugin.Execute implementation is introduced.
Concrete plugins pass their type in a short constructor so PAC's entry/exit traces
identify the actual plugin. PAC owns platform initialization and base tracing.
Our override adds registration checks, guards, scoped DI and application exception handling.
Shared/Generated/provenance.json records the original generator hash, checked during builds.
Shared/Registration contains the unchanged Sparkle registration attribute and enums.

PluginBase<TEntity> provides its nested PluginContext with TEntity already bound:

```csharp
public sealed class PreCreateAccountPlugin : PluginBase<Account>
{
    public PreCreateAccountPlugin() : base(typeof(PreCreateAccountPlugin)) { }

    protected override void Execute(PluginContext context)
    {
        context.Get<IAccountService>().NormalizeAccountName(context.TargetEntity);
    }
}
```

The context implementation lives in Shared/Context while its public namespace remains
Company.Crm.Plugins.Shared. It exposes Context, OrgService, lazy OrgServiceAdmin,
typed TargetEntity/images, parameters and shared variables. Platform compatibility aliases
are explicit ILocalPluginContext members. Never store invocation state on plugin instances.
Non-generic Shared.PluginBase supports EntityReference targets and Custom APIs.
When using that base inside a Plugins child namespace, explicitly select the Shared base
to distinguish it from the original PAC non-generic PluginBase.

## Entity values and models

Shared/Extensions contains EntityExtensions and DataverseValue. FullTarget merges
PreImage -> Target -> PostImage and copies common mutable SDK values, not arbitrary
object graphs. Persist PreOperation changes through TargetEntity. Explicit nulls win
over preimage values. IsChanged requires a preimage outside Create and compares
multi-select choices as sets, binary content and lookup identity rather than display labels.
Depth-based business guards are not shipped.

EarlyBound/Generated/Entities/account.cs is initially a labelled minimal OOTB test model.
PAC intentionally overwrites it when generating real target-environment metadata and
CrmServiceContext. Account.EntityLogicalName and Account.Fields replace schema constants.
No second fallback Account class is kept.

## Build and template

Sparkle owns registration/authentication; model generation uses the active PAC profile.
The ILRepack artifact merges application, models and DI dependencies, leaving SDK external.
The output is signed, checked against the runtime manifest and tested as a merged DLL.
The template excludes real environment models and local keys/artifacts; its optional Account
variant includes only the minimal example model. TestTemplate.ps1 creates and checks both
variants under new names without CRM credentials. CI runs the same local checks and never deploys.
