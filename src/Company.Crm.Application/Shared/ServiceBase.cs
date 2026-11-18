using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Company.Crm.Application.Shared
{
    public abstract class ServiceBase : IService
    {
        protected IOrganizationService OrganizationService { get; }

        protected ServiceBase(IOrganizationService organizationService)
        {
            OrganizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        public TEntity Retrieve<TEntity>(Guid id, ColumnSet columns) where TEntity : Entity, new()
        {
            if (id == Guid.Empty) throw new ArgumentException("A record id is required.", nameof(id));
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            var logicalName = GetLogicalName<TEntity>();
            return OrganizationService.Retrieve(logicalName, id, columns).ToEntity<TEntity>();
        }

        // One SDK request. The caller explicitly advances PageInfo using the returned metadata.
        public EntityPage<TEntity> RetrievePage<TEntity>(QueryExpression query) where TEntity : Entity, new()
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!string.Equals(query.EntityName, GetLogicalName<TEntity>(), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Query entity does not match the requested early-bound type.", nameof(query));
            var response = OrganizationService.RetrieveMultiple(query);
            var records = response.Entities.Select(entity => entity.ToEntity<TEntity>()).ToList().AsReadOnly();
            return new EntityPage<TEntity>(records, response.MoreRecords, response.PagingCookie);
        }

        public TResponse Execute<TResponse>(OrganizationRequest request) where TResponse : OrganizationResponse
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var response = OrganizationService.Execute(request);
            return response as TResponse ?? throw new InvalidOperationException(
                "The SDK response does not match " + typeof(TResponse).FullName + ".");
        }

        private static string GetLogicalName<TEntity>() where TEntity : Entity, new()
        {
            var metadata = (Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute)Attribute.GetCustomAttribute(
                typeof(TEntity), typeof(Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute), true);
            var name = metadata?.LogicalName;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Use an early-bound entity type with a logical name.");
            return name;
        }
    }
}
