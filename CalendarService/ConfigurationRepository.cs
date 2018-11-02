using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarService
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly CalendarServiceContext context;
        internal static readonly ConcurrentDictionary<string, SemaphoreSlim> ConfigSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        public ConfigurationRepository(CalendarServiceContext context)
        {
            this.context = context;
        }

        private void FillTokens(StoredConfiguration config, TokenResponse tokens)
        {
            config.RefreshToken = tokens.refresh_token;
            config.AccessToken = tokens.access_token;
            config.ExpiresIn = DateTime.Now.AddSeconds(tokens.expires_in);
        }

        private async Task<string> AddTokens(string userId, TokenResponse tokens, string type)
        {
            var storedConfiguration = new StoredConfiguration()
            {
                Id = Guid.NewGuid().ToString(),
                Type = type
            };
            FillTokens(storedConfiguration, tokens);
            var user = await context.Users.Include(v => v.Configurations).FirstOrDefaultAsync(v => v.Id == userId);
            if (null == user)
            {
                user = new User()
                {
                    Id = userId,
                    Configurations = new List<StoredConfiguration>() {
                        storedConfiguration
                    }
                };
                await context.Users.AddAsync(user);
            }
            else
            {
                if (null == user.Configurations)
                {
                    user.Configurations = new List<StoredConfiguration>();
                }
                user.Configurations.Add(storedConfiguration);
            }
            await context.SaveChangesAsync();
            return storedConfiguration.Id;
        }

        public async Task<string> AddMicrosoftTokens(string userId, TokenResponse tokens)
        {
            return await AddTokens(userId, tokens, CalendarType.Microsoft);
        }

        public async Task<string> AddGoogleTokens(string userId, TokenResponse tokens)
        {
            return await AddTokens(userId, tokens, CalendarType.Google);
        }

        public async Task CreateConfigState(string userId, string state, string redirectUri)
        {
            var configState = new StoredConfigState()
            {
                RedirectUri = redirectUri,
                State = state,
                StoredTime = DateTime.Now
            };
            var user = await context.Users.Include(v => v.ConfigStates).FirstOrDefaultAsync(v => v.Id == userId);
            if (null == user)
            {
                user = new User()
                {
                    Id = userId,
                    ConfigStates = new List<StoredConfigState>() {
                        configState
                    }
                };
                await context.Users.AddAsync(user);
            }
            else
            {
                if (null == user.ConfigStates)
                {
                    user.ConfigStates = new List<StoredConfigState>();
                }
                user.ConfigStates.Add(configState);
            }
            await context.SaveChangesAsync();
        }

        public async Task<ConfigState> GetConfigState(string state)
        {
            var storedState = await context.ConfigStates.Where(v => v.State == state).SingleOrDefaultAsync();
            if (null != storedState)
            {
                var expired = (storedState.StoredTime + new TimeSpan(0, 1, 30)) < DateTime.Now;
                context.ConfigStates.Remove(storedState);
                await context.SaveChangesAsync();
                if (expired)
                {
                    return null;
                }
                return new ConfigState()
                {
                    RedirectUri = storedState.RedirectUri,
                    UserId = storedState.UserId
                };
            }
            return null;
        }

        public async Task<StoredConfiguration> GetConfiguration(string configId)
        {
            return await context.Configurations.Include(v => v.SubscribedFeeds)
                .ThenInclude(v => v.Notification).Where(v => v.Id == configId).SingleOrDefaultAsync();
        }

        public async Task<StoredConfiguration[]> GetConfigurations(string userid)
        {
            if (!(await context.Users.AnyAsync(v => v.Id == userid)))
            {
                return null;
            }
            return await context.Configurations.Include(v => v.SubscribedFeeds)
                .ThenInclude(v => v.Notification).Where(v => v.UserId == userid).ToArrayAsync();
        }

        public async Task<StoredConfiguration> RefreshTokens(string id, TokenResponse tokens)
        {
            var config = await context.Configurations.Where(v => v.Id == id).SingleOrDefaultAsync();
            FillTokens(config, tokens);
            await context.SaveChangesAsync();
            return config;
        }

        public async Task<bool> RemoveConfig(string userId, string configId)
        {
            var token = await context.Configurations.Where(v => v.UserId == userId && v.Id == configId).SingleOrDefaultAsync();
            if (null != token)
            {
                context.Configurations.Remove(token);
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> SetFeeds(string userId, string id, string[] feedIds)
        {
            var config = await context.Configurations.Include(v => v.SubscribedFeeds).Where(v => v.UserId == userId && v.Id == id).SingleOrDefaultAsync();
            if (null != config)
            {
                foreach (var feed in feedIds)
                {
                    if (!config.SubscribedFeeds.Any(v => v.FeedId == feed))
                    {
                        config.SubscribedFeeds.Add(new StoredFeed()
                        {
                            Id = Guid.NewGuid().ToString(),
                            FeedId = feed
                        });
                    }
                }
                foreach (var feed in config.SubscribedFeeds)
                {
                    if (!feedIds.Any(v => v == feed.FeedId))
                    {
                        context.Feeds.Remove(feed);
                    }
                }
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task UpdateNotification(string configId, string feedId, NotificationInstallation result)
        {
            var feed = await context.Feeds.Where(v => v.ConfigurationId == configId && feedId == v.Id).SingleAsync();
            if (null == feed.Notification)
            {
                feed.Notification = new StoredNotification();
            }
            else if (feed.Notification.NotificationId != result.NotificationId)
            {
                context.Notifications.Remove(feed.Notification);
                await context.SaveChangesAsync();
                feed.Notification = new StoredNotification();
            }
            feed.Notification.Expires = result.Expires;
            feed.Notification.NotificationId = result.NotificationId;
            feed.Notification.ProviderNotificationId = result.ProviderNotifiactionId;
            await context.SaveChangesAsync();
        }

        public async Task<string> GetUserIdByNotificationAsync(string notificationId)
        {
            return await context.Notifications.Where(v => v.NotificationId == notificationId).Select(v => v.Feed).Select(v => v.Configuration)
                .Select(v => v.User).Select(v => v.Id).SingleOrDefaultAsync();
        }
    }
}
