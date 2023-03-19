using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetHelp.Pipelines;

internal static class Extensions
{
        public static IServiceCollection Clone(this IServiceCollection? services)
        {
                var clone = new ServiceCollection();
                if (services != null)
                {
                        foreach (var service in services)
                        {
                                clone.Add(service);
                        }
                }
                return clone;
        }
}
