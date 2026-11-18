# Entities and Shared

Business code is grouped by entity. Cross-entity infrastructure and tools live in Shared.
There are no global Services/Repositories folders and no generic Utils class.

```text
src/
  Company.Crm.Application/
    Entities/
      Accounts/
        Service/
        Repository/
    Shared/
      IService.cs
      ServiceBase.cs
      EntityPage.cs
      EnvironmentVariables/
        Service/
  Company.Crm.Plugins/
    Entities/
      Accounts/
        PreCreateAccountPlugin.cs
        PreUpdateAccountPlugin.cs
    Shared/
      PluginBase.cs
      PluginBase.Generic.cs
      PluginConstants.cs
      Context/
      DependencyInjection/
      Extensions/
      Guards/
      Registration/
      Generated/
  Company.Crm.EarlyBound/
    builderSettings.json
    Generated/
      Entities/
        account.cs
      OptionSets/
tests/
  Company.Crm.Application.Tests/
    Entities/Accounts/
      Service/
      Repository/
      Model/
    Shared/
      ServiceBaseTests.cs
      EnvironmentVariables/Service/
  Company.Crm.Plugins.Tests/
    Entities/Accounts/
    Shared/
      Support/
```

Accounts, Contacts and other entity folders stay under Entities. Entity-specific helpers
also stay there; only genuinely reusable code belongs in Shared. Services and optional
repositories remain together under their entity. Cross-cutting environment configuration
belongs in Application/Shared/EnvironmentVariables.

Namespaces follow the two areas: Application.Entities.Accounts.Service,
Application.Shared, Plugins.Entities.Accounts and Plugins.Shared. Shared context/base
classes retain a small Plugins.Shared API; Guards and DependencyInjection have their
own subnamespaces. SDK extension helpers stay in the Shared API for easy consumption.

The original PAC PluginBase is moved physically to Shared/Generated without editing
its content or namespace. Our Shared.PluginBase inherits directly from that generated
class. Generator destinations, provenance validation and template exclusions use the new path.
The DI scanner covers both Entities and Shared inside Application, including merged builds.

EarlyBound follows PAC output structure and does not receive an artificial Shared folder.
The initial account.cs is a minimal test/example model containing only accountid, name,
accountnumber, telephone1, emailaddress1, websiteurl and description. PAC deliberately
overwrites it when real models are generated. There is no duplicate AccountSchema or
fallback Account class. Previous environment metadata is outside source in artifacts/model-backup.

The default template omits entity examples. --include-account-example includes the
Account feature and minimal model, so both variants build/test without CRM authentication.
