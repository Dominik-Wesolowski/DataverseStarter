using Company.Crm.Plugins.Shared;
using Company.Crm.Plugins.Tests.Shared.Support;
using Microsoft.Xrm.Sdk;
using Moq;
using Xunit;

namespace Company.Crm.Plugins.Tests.Shared
{
    public class ContextHelperTests
    {
        [Fact]
        public void RequiredNamedImagesAreTypedAndValidateTable()
        {
            var fixture = new PluginFixture();
            fixture.PreImages.Add("original", new Entity("account") { ["name"] = "before" });
            fixture.PostImages.Add("wrong", new Entity("contact"));
            var context = new PluginExecution(fixture.Provider.Object).ToEntity<GenericBasePluginTests.TestAccount>();
            Assert.IsType<GenericBasePluginTests.TestAccount>(context.GetRequiredPreImage("original"));
            Assert.Equal("before", context.GetPreImage("original")["name"]);
            Assert.Contains("missing", Assert.Throws<InvalidPluginExecutionException>(() => context.GetRequiredPostImage("missing")).Message);
            Assert.Throws<InvalidPluginExecutionException>(() => context.GetRequiredPostImage("wrong"));
        }

        [Fact]
        public void ParentSharedVariablesAreExplicitAndDoNotSearchGrandparents()
        {
            var fixture = new PluginFixture();
            fixture.SharedVariables["source"] = "current";
            var parent = new Mock<IPluginExecutionContext>();
            parent.SetupGet(p => p.SharedVariables).Returns(new ParameterCollection { ["source"] = "parent" });
            var grandparent = new Mock<IPluginExecutionContext>();
            grandparent.SetupGet(p => p.SharedVariables).Returns(new ParameterCollection { ["ancestor"] = true });
            parent.SetupGet(p => p.ParentContext).Returns(grandparent.Object);
            fixture.Execution.SetupGet(p => p.ParentContext).Returns(parent.Object);
            var context = new PluginExecution(fixture.Provider.Object);
            Assert.True(context.TryGetSharedVariable<string>("source", out var current));
            Assert.Equal("current", current);
            Assert.True(context.TryGetParentSharedVariable<string>("source", out var inherited));
            Assert.Equal("parent", inherited);
            Assert.False(context.TryGetParentSharedVariable<bool>("ancestor", out _));
            Assert.False(context.TryGetParentSharedVariable<int>("source", out _));
            fixture.Execution.SetupGet(p => p.ParentContext).Returns((IPluginExecutionContext)null);
            Assert.False(context.TryGetParentSharedVariable<string>("source", out _));
        }
    }
}
