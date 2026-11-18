# Shared helpers

Entity helpers live in Application/Shared/Extensions and work without a plugin context.
GetRequiredAttribute rejects absent/null values and incompatible types; false and zero are valid.
TryGetAliasedValue unwraps AliasedValue without conversions. Incompatible types throw.
TryGetEffectiveValue prefers Target over the preimage, including explicit null. Its boolean indicates presence;
an explicit null maps to default(T). GetEffectiveValue returns only the effective value.

BuildPatch(before, desired, fields) compares selected fields on the same persisted record.
The caller must retrieve/register those baseline columns; omitted baseline values represent null.
Desired fields that are absent are skipped, explicit null clears a value. No query or update runs automatically.
Skip OrganizationService.Update when patch.Attributes.Count is zero. Use an independent baseline snapshot:
SDK ToEntity conversions can share the original Attributes collection.
Common mutable SDK values are copied; this is not a universal deep clone.

EnvironmentVariableReader caches successful lookups, including missing values, within its scoped instance.
Faults are not cached. Never register the reader as a singleton.
Optional typed reads return null only when no value exists; empty or invalid text throws.
Required reads reject blank text. Boolean accepts true/false, decimal uses an invariant dot separator,
GUID uses Guid.TryParse. Errors contain the schema name, never the configured value.
Secret variables remain unsupported by this text reader.

Plugin context offers optional and required named pre/post images, with typed variants.
TryGetSharedVariable reads the current context; TryGetParentSharedVariable reads only the immediate parent.
Neither searches arbitrary ancestors.
