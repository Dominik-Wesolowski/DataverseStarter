using Company.Crm.Application.Shared.Extensions;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;

namespace Company.Crm.Plugins.Shared
{
    // One object per invocation. Never store it on the cached plugin instance.
    public class PluginExecution : ILocalPluginContext
    {
        private readonly ILocalPluginContext _platform;
        private readonly Lazy<IOrganizationService> _adminService;

        public PluginExecution(IServiceProvider provider, string unsecureConfig = null, string secureConfig = null)
            : this(new LocalPluginContext(provider), unsecureConfig, secureConfig) { }

        public PluginExecution(ILocalPluginContext platform, string unsecureConfig = null, string secureConfig = null)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            ServiceProvider = platform.ServiceProvider;
            UnsecureConfig = unsecureConfig;
            SecureConfig = secureConfig;
            _adminService = new Lazy<IOrganizationService>(() => OrgServiceFactory.CreateOrganizationService(null));
        }

        protected PluginExecution(PluginExecution source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _platform = source._platform;
            _adminService = source._adminService;
            ServiceProvider = source.ServiceProvider;
            UnsecureConfig = source.UnsecureConfig;
            SecureConfig = source.SecureConfig;
        }

        public IServiceProvider ServiceProvider { get; internal set; }
        public IPluginExecutionContext Context => _platform.PluginExecutionContext;
        public IOrganizationService OrgService => _platform.PluginUserService;
        public IOrganizationService OrgServiceAdmin => _adminService.Value;
        public IOrganizationServiceFactory OrgServiceFactory => _platform.OrgSvcFactory;
        public ITracingService TracingService => _platform.TracingService;
        public string UnsecureConfig { get; }
        public string SecureConfig { get; }
        public bool IsCreate => IsMessage("Create");
        public bool IsUpdate => IsMessage("Update");
        public bool IsDelete => IsMessage("Delete");
        public bool IsMessage(string message) => string.Equals(Context.MessageName, message, StringComparison.OrdinalIgnoreCase);

        public object Target => Context.InputParameters.TryGetValue(PluginConstants.Target, out var value) ? value : null;
        public Entity TargetEntity => Target as Entity;
        public EntityReference TargetReference => Target as EntityReference;
        public Entity PreImage => GetPreImage();
        public Entity PostImage => GetPostImage();
        public Entity GetPreImage(string alias = PluginConstants.PreImage) => GetImage(Context.PreEntityImages, alias);
        public Entity GetPostImage(string alias = PluginConstants.PostImage) => GetImage(Context.PostEntityImages, alias);
        public Entity GetRequiredPreImage(string alias = PluginConstants.PreImage) =>
            GetPreImage(alias) ?? throw new InvalidPluginExecutionException("Required preimage is missing: " + alias);
        public Entity GetRequiredPostImage(string alias = PluginConstants.PostImage) =>
            GetPostImage(alias) ?? throw new InvalidPluginExecutionException("Required postimage is missing: " + alias);
        private static Entity GetImage(EntityImageCollection images, string alias) =>
            images.TryGetValue(alias, out var image) ? image : null;

        // Effective values for reading. Common mutable column values are copied; specialized objects may be shared.
        // This is not a universal deep clone. Modify TargetEntity to persist PreOperation changes.
        // The postimage is authoritative after the operation. Explicit nulls override older values.
        public Entity FullTarget
        {
            get
            {
                var sources = new[] { PreImage, TargetEntity, PostImage }.Where(entity => entity != null).ToArray();
                if (sources.Length == 0) return null;
                var merged = new Entity(sources[0].LogicalName);
                foreach (var source in sources)
                {
                    Require(string.Equals(merged.LogicalName, source.LogicalName, StringComparison.OrdinalIgnoreCase),
                        "Cannot merge images from different tables.");
                    if (source.Id != Guid.Empty) merged.Id = source.Id;
                    foreach (var attribute in source.Attributes) merged[attribute.Key] = DataverseValue.Copy(attribute.Value);
                }
                return merged;
            }
        }

        public T Get<T>() => ServiceProvider.GetRequiredService<T>();
        public PluginExecution<TEntity> ToEntity<TEntity>() where TEntity : Entity => new PluginExecution<TEntity>(this);
        public bool TryGetInputParameter<T>(string key, out T value) => TryGetValue(Context.InputParameters, key, out value);
        public T GetRequiredInput<T>(string key)
        {
            if (TryGetInputParameter<T>(key, out var value)) return value;
            throw new InvalidPluginExecutionException("Required input parameter is missing or has an invalid type: " + key);
        }
        public void SetOutput(string key, object value) => Context.OutputParameters[key] = value;
        public void SetSharedVariable<T>(string key, T value) => Context.SharedVariables[key] = value;
        public bool TryGetSharedVariable<T>(string key, out T value) => TryGetValue(Context.SharedVariables, key, out value);
        // Explicitly reads only the immediate parent, never silently searches ancestors.
        public bool TryGetParentSharedVariable<T>(string key, out T value)
        {
            var parent = Context.ParentContext;
            if (parent != null) return TryGetValue(parent.SharedVariables, key, out value);
            value = default(T);
            return false;
        }
        public bool HasSharedVariable(string key) => Context.SharedVariables.Contains(key);
        private static bool TryGetValue<T>(ParameterCollection values, string key, out T value)
        {
            if (values.TryGetValue(key, out var raw) && raw is T typed) { value = typed; return true; }
            value = default(T);
            return false;
        }
        public void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidPluginExecutionException(message);
        }

        // An absent preimage cannot prove that an attribute changed.
        public bool IsChanged(string attribute)
        {
            if (TargetEntity == null || !TargetEntity.Contains(attribute)) return false;
            if (IsCreate) return true;
            Require(PreImage != null, "IsChanged requires the PreImage image for " + attribute + ".");
            return !DataverseValue.AreEqual(TargetEntity[attribute], PreImage.Contains(attribute) ? PreImage[attribute] : null);
        }
        public bool HasChangedAny(params string[] attributes) => attributes.Any(IsChanged);

        public void Trace(string message, [CallerMemberName] string method = null) => _platform.Trace(message, method);
        IPluginExecutionContext ILocalPluginContext.PluginExecutionContext => Context;
        IOrganizationService ILocalPluginContext.PluginUserService => OrgService;
        public IOrganizationService InitiatingUserService => _platform.InitiatingUserService;
        IOrganizationServiceFactory ILocalPluginContext.OrgSvcFactory => OrgServiceFactory;
        public IServiceEndpointNotificationService NotificationService => _platform.NotificationService;
        public ILogger Logger => _platform.Logger;
    }
}
