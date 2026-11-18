using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Company.Crm.Plugins.Shared
{
    public class PluginExecution<TEntity> : PluginExecution where TEntity : Entity
    {
        public PluginExecution(PluginExecution source) : base(source) { }
        public PluginExecution(IServiceProvider provider, string unsecureConfig = null, string secureConfig = null)
            : base(provider, unsecureConfig, secureConfig) { }
        public new TEntity TargetEntity => Convert(base.TargetEntity);
        public new TEntity PreImage => Convert(base.PreImage);
        public new TEntity PostImage => Convert(base.PostImage);
        public new TEntity GetPreImage(string alias = PluginConstants.PreImage) => Convert(base.GetPreImage(alias));
        public new TEntity GetPostImage(string alias = PluginConstants.PostImage) => Convert(base.GetPostImage(alias));
        public new TEntity GetRequiredPreImage(string alias = PluginConstants.PreImage) => Convert(base.GetRequiredPreImage(alias));
        public new TEntity GetRequiredPostImage(string alias = PluginConstants.PostImage) => Convert(base.GetRequiredPostImage(alias));
        public TEntity FullTargetEntity => Convert(FullTarget);

        private static readonly EntityLogicalNameAttribute Metadata = (EntityLogicalNameAttribute)Attribute.GetCustomAttribute(
            typeof(TEntity), typeof(EntityLogicalNameAttribute), true);

        private static TEntity Convert(Entity entity)
        {
            if (entity == null) return null;
            if (typeof(TEntity) == typeof(Entity)) return (TEntity)entity;
            var metadata = Metadata;
            if (metadata == null) throw new InvalidPluginExecutionException("The requested type is not an early-bound entity: " + typeof(TEntity).FullName);
            if (!string.Equals(entity.LogicalName, metadata.LogicalName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidPluginExecutionException("Expected " + metadata.LogicalName + ", received " + entity.LogicalName + ".");
            return entity.ToEntity<TEntity>();
        }
    }
}
