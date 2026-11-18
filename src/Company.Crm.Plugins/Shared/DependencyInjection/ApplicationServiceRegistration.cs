using System;
using System.Linq;
using Company.Crm.Application.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Crm.Plugins.Shared.DependencyInjection
{
    internal static class ApplicationServiceRegistration
    {
        // Cache type metadata only. No request context or resolved service is cached.
        private static readonly Lazy<Registration[]> Registrations =
            new Lazy<Registration[]>(Discover);

        internal static void AddApplicationServices(this IServiceCollection services)
        {
            foreach (var registration in Registrations.Value)
                services.AddScoped(registration.Contract, registration.Implementation);
        }

        private static Registration[] Discover()
        {
            var contractNamespace = typeof(IService).Namespace;
            var applicationNamespace = contractNamespace.Substring(0, contractNamespace.LastIndexOf('.')) + ".";
            var registrations = typeof(IService).Assembly.GetTypes()
                .Where(type => type.IsClass && type.IsPublic && !type.IsAbstract && !type.ContainsGenericParameters)
                .Where(type => type.Namespace != null &&
                    type.Namespace.StartsWith(applicationNamespace, StringComparison.Ordinal) &&
                    (type.Namespace.EndsWith(".Service", StringComparison.Ordinal) ||
                     type.Namespace.EndsWith(".Repository", StringComparison.Ordinal)))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type =>
                {
                    var contract = type.GetInterfaces().SingleOrDefault(candidate =>
                        candidate.Name == "I" + type.Name && candidate.Namespace == type.Namespace);
                    if (contract == null)
                        throw new InvalidOperationException("Missing matching interface for application component: " +
                            type.FullName + ". Expected I" + type.Name + " in the same namespace.");
                    return new Registration(contract, type);
                }).ToArray();

            var duplicate = registrations.GroupBy(item => item.Contract).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException("Multiple default implementations for: " + duplicate.Key.FullName);
            return registrations;
        }

        private sealed class Registration
        {
            internal Type Contract { get; }
            internal Type Implementation { get; }
            internal Registration(Type contract, Type implementation)
            {
                Contract = contract;
                Implementation = implementation;
            }
        }
    }
}
