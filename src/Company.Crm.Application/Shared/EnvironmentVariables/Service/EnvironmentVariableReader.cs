using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Application.Shared.EnvironmentVariables.Service
{
    // Reads under the plugin execution user. No cross-user/process-wide cache.
    public sealed class EnvironmentVariableReader : ServiceBase, IEnvironmentVariableReader
    {
        public EnvironmentVariableReader(IOrganizationService organizationService) : base(organizationService) { }

        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string GetValue(string schemaName)
        {
            if (string.IsNullOrWhiteSpace(schemaName)) throw new ArgumentException("A schema name is required.", nameof(schemaName));
            if (_cache.TryGetValue(schemaName, out var cached)) return cached;
            var value = LoadValue(schemaName);
            _cache[schemaName] = value;
            return value;
        }

        private string LoadValue(string schemaName)
        {
            if (string.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentException("A schema name is required.", nameof(schemaName));
            var definitions = new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = new ColumnSet("defaultvalue", "type"),
                TopCount = 2
            };
            definitions.Criteria.AddCondition("schemaname", ConditionOperator.Equal, schemaName);
            var found = OrganizationService.RetrieveMultiple(definitions).Entities;
            if (found.Count == 0) return null;
            if (found.Count > 1)
                throw new InvalidPluginExecutionException("Environment variable definition is ambiguous: " + schemaName);
            var definition = found[0];
            // Secret variables require a dedicated secret retrieval mechanism.
            if (definition.GetAttributeValue<OptionSetValue>("type")?.Value == 100000005)
                throw new InvalidPluginExecutionException("Secret environment variables cannot be read as text.");

            var values = new QueryExpression("environmentvariablevalue")
            {
                ColumnSet = new ColumnSet("value"),
                TopCount = 2
            };
            values.Criteria.AddCondition("environmentvariabledefinitionid", ConditionOperator.Equal, definition.Id);
            values.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            var current = OrganizationService.RetrieveMultiple(values).Entities;
            if (current.Count > 1)
                throw new InvalidPluginExecutionException("Multiple current environment variable values: " + schemaName);
            // Explicit empty string overrides a default; absent/null current value falls back.
            return current.SingleOrDefault()?.GetAttributeValue<string>("value")
                ?? definition.GetAttributeValue<string>("defaultvalue");
        }

        public string GetRequiredValue(string schemaName)
        {
            var value = GetValue(schemaName);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidPluginExecutionException("Required environment variable is missing or empty: " + schemaName);
            return value;
        }

        public bool? GetBoolean(string schemaName) => ParseOptional<bool>(schemaName, ParseBoolean);
        public decimal? GetDecimal(string schemaName) => ParseOptional<decimal>(schemaName, ParseDecimal);
        public Guid? GetGuid(string schemaName) => ParseOptional<Guid>(schemaName, ParseGuid);
        public bool GetRequiredBoolean(string schemaName) => ParseBoolean(GetRequiredValue(schemaName), schemaName);
        public decimal GetRequiredDecimal(string schemaName) => ParseDecimal(GetRequiredValue(schemaName), schemaName);
        public Guid GetRequiredGuid(string schemaName) => ParseGuid(GetRequiredValue(schemaName), schemaName);

        private T? ParseOptional<T>(string schemaName, Func<string, string, T> parse) where T : struct
        {
            var value = GetValue(schemaName);
            return value == null ? (T?)null : parse(value, schemaName);
        }

        private static bool ParseBoolean(string value, string name)
        {
            if (bool.TryParse(value, out var parsed)) return parsed;
            throw InvalidValue(name, "boolean (true/false)");
        }

        private static decimal ParseDecimal(string value, string name)
        {
            if (decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint
                | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            throw InvalidValue(name, "decimal using a dot separator");
        }

        private static Guid ParseGuid(string value, string name)
        {
            if (Guid.TryParse(value, out var parsed)) return parsed;
            throw InvalidValue(name, "GUID");
        }

        private static InvalidPluginExecutionException InvalidValue(string name, string expected) =>
            new InvalidPluginExecutionException("Invalid environment variable: " + name + ". Expected " + expected + ".");
    }
}
