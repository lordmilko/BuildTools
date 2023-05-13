﻿using System;

namespace BuildTools
{
    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider) => (T) serviceProvider.GetService(typeof(T));
    }
}