namespace Company.Crm.Application.Shared.EnvironmentVariables.Service
{
    public interface IEnvironmentVariableReader : IService
    {
        string GetValue(string schemaName);
        string GetRequiredValue(string schemaName);
        bool? GetBoolean(string schemaName);
        decimal? GetDecimal(string schemaName);
        System.Guid? GetGuid(string schemaName);
        bool GetRequiredBoolean(string schemaName);
        decimal GetRequiredDecimal(string schemaName);
        System.Guid GetRequiredGuid(string schemaName);
    }
}