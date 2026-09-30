using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.LocalSubs
{
    internal sealed class ScopedServiceProvider : IServiceProvider
    {
        private readonly IServiceScope _serviceScope;

        public ScopedServiceProvider(IServiceScope serviceScope)
        {
            _serviceScope = serviceScope;
        }

        public object? GetService(Type serviceType)
        {
            return _serviceScope.ServiceProvider.GetService(serviceType);
        }
    }
}
