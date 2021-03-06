﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bymyslf.AuthenticationSessionStore.Extensions;
using Bymyslf.AuthenticationSessionStore.Utils;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace Bymyslf.AuthenticationSessionStore
{
    public class InMemoryAuthenticationSessionStore : IAuthenticationSessionStore
    {
        private readonly IAuthenticationTicketSerializer serializer;
        private readonly ConcurrentDictionary<string, string> store;
        private readonly Timer garbageCollectTimer;

        public InMemoryAuthenticationSessionStore(IAuthenticationTicketSerializer serializer)
        {
            Guard.Against<ArgumentNullException>(serializer.IsNull(), "serializer can't be null");

            this.serializer = serializer;
            this.store = new ConcurrentDictionary<string, string>();
            this.garbageCollectTimer = new Timer(new TimerCallback(this.GarbageCollect), null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
        }

        public Task RemoveAsync(string key)
        {
            Guard.Against<ArgumentNullException>(key.IsNull(), "RemoveAsync - key can't be null");

            string value;
            this.store.TryRemove(key, out value);
            return Task.FromResult(0);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            Guard.Against<ArgumentNullException>(key.IsNull(), "RenewAsync - key can't be null");
            Guard.Against<ArgumentNullException>(ticket.IsNull(), "RenewAsync - ticket can't be null");

            var entry = this.store.FirstOrDefault(ent => ent.Key == key);
            if (entry.IsNotNull())
            {
                var oldValue = entry.Value;
                var json = this.serializer.Serialize(ticket);
                this.store.TryUpdate(key, json, oldValue);
            }

            return Task.FromResult(0);
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            Guard.Against<ArgumentNullException>(key.IsNull(), "RetrieveAsync - key can't be null");

            var entry = this.store.FirstOrDefault(ent => ent.Key == key);
            if (entry.IsNotNull())
            {
                return Task.FromResult(this.serializer.Deserialize(entry.Value));
            }

            return Task.FromResult((AuthenticationTicket)null);
        }

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            Guard.Against<ArgumentNullException>(ticket.IsNull(), "StoreAsync - ticket can't be null");

            var key = Guid.NewGuid().ToString("N");
            var json = this.serializer.Serialize(ticket);
            this.store.TryAdd(key, json);
            return Task.FromResult(key);
        }

        private void GarbageCollect(object state)
        {
            string value;
            var now = DateTimeOffset.Now.ToUniversalTime();
            foreach (var entry in this.store)
            {
                var ticket = this.serializer.Deserialize(entry.Value);
                var expiresAt = ticket.Properties.ExpiresUtc;
                if (expiresAt < now)
                {
                    this.store.TryRemove(entry.Key, out value);
                }
            }
        }
    }
}