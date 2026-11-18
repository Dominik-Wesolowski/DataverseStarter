using System;
using Company.Crm.Plugins.Shared;
using Company.Crm.Plugins.Tests.Shared.Support;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Company.Crm.Plugins.Tests.Shared
{
    public class PluginContextTests
    {
        [Fact]
        public void FullTargetPreservesIdAndMergesPostImageLastWithoutMutatingTarget()
        {
            var id = Guid.NewGuid();
            var target = new Entity("account", id) { ["name"] = null };
            var fixture = new PluginFixture("Update", target);
            fixture.PreImages.Add("PreImage", new Entity("account", id) { ["name"] = "old", ["telephone1"] = "123" });
            fixture.PostImages.Add("PostImage", new Entity("account", id) { ["telephone1"] = "456" });
            var context = new PluginExecution(fixture.Provider.Object);
            var snapshot = context.FullTarget;
            Assert.Equal(id, snapshot.Id);
            Assert.Null(snapshot["name"]);
            Assert.Equal("456", snapshot["telephone1"]);
            snapshot["name"] = "snapshot only";
            Assert.Null(target["name"]);
            Assert.False(target.Contains("telephone1"));
        }

        [Fact]
        public void ChangedDistinguishesMissingAttributesUnchangedValuesAndExplicitNull()
        {
            var target = new Entity("account") { ["name"] = null, ["telephone1"] = "123" };
            var fixture = new PluginFixture("Update", target);
            fixture.PreImages.Add("PreImage", new Entity("account") { ["name"] = "old", ["telephone1"] = "123" });
            var context = new PluginExecution(fixture.Provider.Object);
            Assert.True(context.IsChanged("name"));
            Assert.False(context.IsChanged("telephone1"));
            Assert.False(context.IsChanged("address1_city"));
            Assert.True(context.HasChangedAny("telephone1", "name"));
        }

        [Fact]
        public void MissingPreImageDoesNotSilentlyClaimAChange()
        {
            var context = new PluginExecution(new PluginFixture("Update", new Entity("account") { ["name"] = "x" }).Provider.Object);
            Assert.Null(context.PreImage);
            Assert.Contains("PreImage", Assert.Throws<InvalidPluginExecutionException>(() => context.IsChanged("name")).Message);
        }

        [Fact]
        public void DeleteExposesReferenceAndSharedVariablesWithoutRequiringEntityTarget()
        {
            var fixture = new PluginFixture("Delete");
            var reference = new EntityReference("account", Guid.NewGuid());
            fixture.Inputs.Add("Target", reference);
            var context = new PluginExecution(fixture.Provider.Object);
            Assert.True(context.IsDelete);
            Assert.Same(reference, context.TargetReference);
            Assert.Null(context.TargetEntity);
            context.SetSharedVariable("done", true);
            Assert.True(context.TryGetSharedVariable<bool>("done", out var done));
            Assert.True(done);
            Assert.False(context.TryGetSharedVariable<Guid>("done", out _));
        }

        [Fact]
        public void TypedExecutionSharesConfigurationAndLazilyCreatesAdminServiceOnce()
        {
            var fixture = new PluginFixture();
            var context = new PluginExecution(fixture.Provider.Object, "unsecure", "secure");
            var typed = context.ToEntity<GenericBasePluginTests.TestAccount>();
            Assert.Equal("unsecure", typed.UnsecureConfig);
            Assert.Equal("secure", typed.SecureConfig);
            fixture.Factory.Verify(factory => factory.CreateOrganizationService(null), Moq.Times.Never);
            Assert.Same(context.OrgServiceAdmin, typed.OrgServiceAdmin);
            fixture.Factory.Verify(factory => factory.CreateOrganizationService(null), Moq.Times.Once);
        }
    }
}
