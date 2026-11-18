using System;
using Company.Crm.Application.Shared.Extensions;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Company.Crm.Application.Tests.Shared.Extensions
{
    public class EntityHelperTests
    {
        [Fact]
        public void ExplicitNullOverridesImageAndRemainsPresent()
        {
            var target = new Entity("account") { ["name"] = null };
            var image = new Entity("account") { ["name"] = "before" };
            Assert.True(target.TryGetEffectiveValue<string>(image, "name", out var value));
            Assert.Null(value);
            Assert.False(target.TryGetEffectiveValue<string>(image, "missing", out _));
        }

        [Fact]
        public void RequiredValuesAcceptFalseAndZeroButRejectNullAndWrongType()
        {
            var entity = new Entity("account") { ["flag"] = false, ["count"] = 0, ["empty"] = null };
            Assert.False(entity.GetRequiredAttribute<bool>("flag"));
            Assert.Equal(0, entity.GetRequiredAttribute<int>("count"));
            Assert.Throws<InvalidPluginExecutionException>(() => entity.GetRequiredAttribute<string>("empty"));
            Assert.Throws<InvalidPluginExecutionException>(() => entity.GetRequiredAttribute<int>("flag"));
        }

        [Fact]
        public void AliasesAreUnwrappedWithoutCoercion()
        {
            var entity = new Entity("account") { ["contact.name"] = new AliasedValue("contact", "name", "Ada") };
            Assert.True(entity.TryGetAliasedValue<string>("contact.name", out var value));
            Assert.Equal("Ada", value);
            Assert.False(entity.TryGetAliasedValue<string>("missing", out _));
            Assert.Throws<InvalidPluginExecutionException>(() => entity.TryGetAliasedValue<int>("contact.name", out _));
        }

        [Fact]
        public void PatchIncludesOnlySelectedChangesAndCopiesMutableValues()
        {
            var id = Guid.NewGuid();
            var before = new Entity("account", id) { ["name"] = "old", ["phone"] = "123", ["amount"] = new Money(1) };
            var desired = new Entity("account", id) { ["name"] = null, ["amount"] = new Money(2), ["unselected"] = "skip" };
            var patch = before.BuildPatch(desired, "name", "phone", "amount");
            Assert.Equal(2, patch.Attributes.Count);
            Assert.Null(patch["name"]);
            Assert.False(patch.Contains("phone"));
            ((Money)patch["amount"]).Value = 3;
            Assert.Equal(2, ((Money)desired["amount"]).Value);
            Assert.Equal("old", before["name"]);
        }

        [Fact]
        public void PatchIgnoresChoiceOrderAndRejectsDifferentRecords()
        {
            var before = new Entity("account", Guid.NewGuid()) { ["choices"] = new OptionSetValueCollection { new OptionSetValue(1), new OptionSetValue(2) } };
            var desired = new Entity("account", before.Id) { ["choices"] = new OptionSetValueCollection { new OptionSetValue(2), new OptionSetValue(1) } };
            Assert.Empty(before.BuildPatch(desired, "choices").Attributes);
            desired.Id = Guid.NewGuid();
            Assert.Throws<ArgumentException>(() => before.BuildPatch(desired, "choices"));
        }
    }
}
