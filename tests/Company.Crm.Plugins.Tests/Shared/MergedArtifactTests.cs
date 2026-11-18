using Company.Crm.Application.Shared;
using Company.Crm.Plugins.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Company.Crm.Plugins.Tests.Shared
{
    public class MergedArtifactTests
    {
        [Fact]
        public void RunsMergedApplicationAndDependencyInjectionWithExternalSdk()
        {
            var artifact = typeof(Company.Crm.Plugins.Shared.PluginBase).Assembly;
            Assert.Same(artifact, typeof(ServiceBase).Assembly);
            Assert.Same(artifact, typeof(IServiceCollection).Assembly);
            Assert.NotSame(artifact, typeof(IOrganizationService).Assembly);
        }
    }
}
