using System;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Application.Shared.Extensions
{
    public static class EntityExtensions
    {
        public static T GetRequiredAttribute<T>(this Entity entity, string attribute)
        {
            Validate(entity, attribute);
            if (!entity.Attributes.TryGetValue(attribute, out var raw) || raw == null)
                throw new InvalidPluginExecutionException("Required attribute is missing: " + attribute);
            return Read<T>(raw, attribute);
        }

        public static bool TryGetAliasedValue<T>(this Entity entity, string attribute, out T value)
        {
            Validate(entity, attribute);
            value = default(T);
            if (!entity.Attributes.TryGetValue(attribute, out var raw) || raw == null) return false;
            if (!(raw is AliasedValue alias))
                throw new InvalidPluginExecutionException("Expected an aliased attribute: " + attribute);
            if (alias.Value == null) return false;
            value = Read<T>(alias.Value, attribute);
            return true;
        }

        // True means present, including an explicit null. Null maps to default(T), as in the SDK.
        public static bool TryGetEffectiveValue<T>(this Entity target, Entity preImage, string attribute, out T value)
        {
            Validate(target, attribute);
            object raw;
            var present = target.Attributes.TryGetValue(attribute, out raw)
                || (preImage != null && preImage.Attributes.TryGetValue(attribute, out raw));
            value = !present || raw == null ? default(T) : Read<T>(raw, attribute);
            return present;
        }

        public static T GetEffectiveValue<T>(this Entity target, Entity preImage, string attribute)
        {
            target.TryGetEffectiveValue<T>(preImage, attribute, out var value);
            return value;
        }

        // Caller must load the compared columns in before. Missing baseline attributes represent null.
        // Only explicitly supplied desired attributes are considered. No database calls are performed.
        public static Entity BuildPatch(this Entity before, Entity desired, params string[] attributes)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (desired == null) throw new ArgumentNullException(nameof(desired));
            if (before.Id == Guid.Empty || before.Id != desired.Id
                || !string.Equals(before.LogicalName, desired.LogicalName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Patch entities must identify the same persisted record.", nameof(desired));
            if (attributes == null || attributes.Length == 0)
                throw new ArgumentException("Select the attributes to compare.", nameof(attributes));
            var patch = new Entity(before.LogicalName, before.Id);
            foreach (var attribute in attributes)
            {
                Validate(desired, attribute);
                if (!desired.Attributes.TryGetValue(attribute, out var next)) continue;
                before.Attributes.TryGetValue(attribute, out var previous);
                if (!DataverseValue.AreEqual(previous, next)) patch[attribute] = DataverseValue.Copy(next);
            }
            return patch;
        }

        private static T Read<T>(object value, string attribute)
        {
            if (value is T typed) return typed;
            throw new InvalidPluginExecutionException("Attribute has an unexpected type: " + attribute);
        }

        private static void Validate(Entity entity, string attribute)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(attribute)) throw new ArgumentException("An attribute name is required.", nameof(attribute));
        }
    }
}
