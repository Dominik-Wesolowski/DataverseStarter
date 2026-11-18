using System;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Application.Shared.Extensions
{
    public static class DataverseValue
    {
        public static bool AreEqual(object left, object right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null) return false;
            if (left is OptionSetValueCollection choices && right is OptionSetValueCollection otherChoices)
                return new System.Collections.Generic.HashSet<int>(choices.Select(choice => choice.Value))
                    .SetEquals(otherChoices.Select(choice => choice.Value));
            if (left is byte[] bytes && right is byte[] otherBytes) return bytes.SequenceEqual(otherBytes);
            if (left is EntityReference reference && right is EntityReference otherReference)
                return string.Equals(reference.LogicalName, otherReference.LogicalName, StringComparison.OrdinalIgnoreCase)
                    && reference.Id == otherReference.Id
                    && (reference.Id != Guid.Empty || reference.KeyAttributes.Count == otherReference.KeyAttributes.Count
                        && reference.KeyAttributes.All(key => otherReference.KeyAttributes.TryGetValue(key.Key, out var value)
                            && AreEqual(key.Value, value)));
            return Equals(left, right);
        }

        // Copy common mutable column values. Strings and value types are already safe to share.
        // Specialized/custom objects are intentionally not represented as a universal deep clone.
        public static object Copy(object value)
        {
            if (value is Money money) return new Money(money.Value);
            if (value is OptionSetValue choice) return new OptionSetValue(choice.Value);
            if (value is OptionSetValueCollection choices)
                return new OptionSetValueCollection(choices.Select(item => new OptionSetValue(item.Value)).ToList());
            if (value is byte[] bytes) return (byte[])bytes.Clone();
            if (value is EntityReference reference)
            {
                var copy = new EntityReference(reference.LogicalName, reference.Id) { Name = reference.Name, RowVersion = reference.RowVersion };
                foreach (var key in reference.KeyAttributes) copy.KeyAttributes[key.Key] = Copy(key.Value);
                return copy;
            }
            if (value is AliasedValue aliased) return new AliasedValue(aliased.EntityLogicalName, aliased.AttributeLogicalName, Copy(aliased.Value));
            return value;
        }
    }
}
