using Company.Crm.Application.Entities.Accounts.Service;
using Company.Crm.EarlyBound.Generated;
using Company.Crm.Plugins.Shared;
using Company.Crm.Plugins.Shared.Guards;

namespace Company.Crm.Plugins.Entities.Accounts
{
    [CrmPluginRegistration(
        message: MessageNameEnum.Update,
        entityLogicalName: Account.EntityLogicalName,
        stage: StageEnum.PreOperation,
        executionMode: ExecutionModeEnum.Synchronous,
        filteringAttributes: Account.Fields.Name,
        stepName: "Company.Crm: normalize account name (Update)",
        executionOrder: 1,
        isolationModel: IsolationModeEnum.Sandbox,
        Image1Name = PluginConstants.PreImage,
        Image1Attributes = Account.Fields.Name,
        Image1Type = ImageTypeEnum.PreImage)]
    [RequiresTarget]
    public sealed class PreUpdateAccountPlugin() : PluginBase<Account>(typeof(PreUpdateAccountPlugin))
    {
        protected override void Execute(PluginContext context)
        {
            context.Require(context.PreImage != null, "Required PreImage is missing.");
            context.Get<IAccountService>().NormalizeAccountName(context.TargetEntity, context.PreImage);
        }
    }
}
