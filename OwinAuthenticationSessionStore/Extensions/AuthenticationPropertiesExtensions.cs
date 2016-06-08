﻿using System;
using Microsoft.Owin.Security;

namespace OwinAuthenticationSessionStore.Extensions
{
    public static class AuthenticationPropertiesExtensions
    {
        public static TimeSpan ExpiresUtcToTimeSpanOrDefault(this AuthenticationProperties properties, int @default)
        {
            if (properties.ExpiresUtc.HasValue)
            {
                var expiresUtc = properties.ExpiresUtc.Value;
                return expiresUtc.DateTime.Subtract(DateTimeOffset.Now);
            }

            return TimeSpan.FromMinutes(@default);
        }
    }
}
