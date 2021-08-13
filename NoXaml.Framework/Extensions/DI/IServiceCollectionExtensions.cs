using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NoXaml.Framework.Components;

namespace NoXaml.Framework.Extensions.DI
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddNoXamlComponents(this IServiceCollection services)
        {
            var viewsToRegister = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.IsSubclassOf(typeof(NoXamlComponent)) ||
                    t.IsSubclassOf(typeof(NoXamlWindow))
                )
                .ToList()
            ;

            foreach (var view in viewsToRegister)
                services.AddTransient(view);

            return services;
        }
    }
}
