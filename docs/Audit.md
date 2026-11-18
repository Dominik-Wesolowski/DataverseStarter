# Audit implementation

The repository now implements the following audit decisions:

- RetrievePage<TEntity> returns Records, MoreRecords and PagingCookie; there is no implicit retrieve-all loop.
- IsChanged compares multi-select choices as sets, binary values by content and lookups by identity/alternate key.
- FullTarget copies Money, OptionSetValue, OptionSetValueCollection, EntityReference, byte[] and AliasedValue.
  Specialized object graphs are not a universal deep clone; write business changes to TargetEntity.
- PAC runtime remains original and hash-validated. Our framework base directly inherits from the original PAC base and passes the plugin type through its constructor.
- Platform aliases on PluginExecution are explicit ILocalPluginContext implementations.
- Entity metadata is cached per generic type. Request services/context are not globally cached.
- MaxDepth was removed. Use filtering attributes, change detection and idempotent operations.
- Customer-specific models were removed. ModelBuilder generates CrmServiceContext with target-environment models.
- Account examples are optional. Source defaults to IncludeAccountExample=false.
- TestTemplate.ps1 creates fresh projects in an isolated template hive and verifies the default project end to end.
- CI builds/tests the backend and, for the template source, exercises template generation without CRM credentials.

Still intentionally outside local verification: actual registration/execution in a Dataverse sandbox,
and target-environment metadata generation. The optional Account example builds offline against its minimal OOTB model.
CI does not deploy. The initial Account model is a replaceable example, not customer metadata.

Sparkle and ILRepack remain the selected deployment path. Microsoft recommends dependent assembly packages
and does not support ILMerge; evaluate that alternative separately before a long-term platform decision:
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/build-and-package

Direct inheritance: Shared.PluginBase inherits the original PAC PluginBase.
There is no adapter or second IPlugin.Execute implementation. Concrete plugins pass their type
with a short constructor, e.g. public MyPlugin() : base(typeof(MyPlugin)) { }.
Configuration is forwarded only when needed. PAC owns platform initialization and entry/exit tracing.
