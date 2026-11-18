using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Company.Crm.Application.Shared
{
    // Shared contract. Resolve the feature interface (e.g. IAccountService) from DI.
    public interface IService
    {
        TEntity Retrieve<TEntity>(Guid id, ColumnSet columns) where TEntity : Entity, new();
        EntityPage<TEntity> RetrievePage<TEntity>(QueryExpression query) where TEntity : Entity, new();
        TResponse Execute<TResponse>(OrganizationRequest request) where TResponse : OrganizationResponse;
    }
}
