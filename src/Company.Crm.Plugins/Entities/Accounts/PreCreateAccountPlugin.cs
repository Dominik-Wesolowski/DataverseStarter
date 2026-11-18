using Company.Crm.Application.Entities.Accounts.Service;
using Company.Crm.EarlyBound.Generated;
using Company.Crm.Plugins.Shared;
using Company.Crm.Plugins.Shared.Guards;

namespace Company.Crm.Plugins.Entities.Accounts
{
    [CrmPluginRegistration(
        message: MessageNameEnum.Create, 
        entityLogicalName: Account.EntityLogicalName, 
        stage: StageEnum.PreOperation,
        executionMode: ExecutionModeEnum.Synchronous,
        filteringAttributes:"", 
        stepName: "Company.Crm: normalize account name (Create)",
        executionOrder: 1, 
        isolationModel: IsolationModeEnum.Sandbox)]
    [RequiresTarget]
    public sealed class PreCreateAccountPlugin() : PluginBase<Account>(typeof(PreCreateAccountPlugin))
    {
        protected override void Execute(PluginContext context)
        {
            context.Get<IAccountService>().NormalizeAccountName(context.TargetEntity);
        }
    }
}
