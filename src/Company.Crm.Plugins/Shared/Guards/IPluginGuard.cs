namespace Company.Crm.Plugins.Shared.Guards
{
    public interface IPluginGuard
    {
        bool Passes(ILocalPluginContext context);
    }
}
