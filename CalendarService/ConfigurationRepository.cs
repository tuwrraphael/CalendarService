using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarService
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly CalendarServiceContext context;

        public ConfigurationRepository(CalendarServiceContext context)
        {
            this.context = context;
        }

        public async Task<string> AddMicrosoftTokens(string userId, TokenResponse tokens)
        {
            var storedToken = new StoredToken()
            {
                Id = Guid.NewGuid().ToString(),
                AccessToken = tokens.access_token,
                RefreshToken = tokens.refresh_token,
                ExpiresIn = DateTime.Now.AddMilliseconds(tokens.expires_in),
                Type = "microsoft"
            };
            var user = await context.Users.Include(v => v.Tokens).FirstOrDefaultAsync(v => v.Id == userId);
            if (null == user)
            {
                user = new User()
                {
                    Id = userId,
                    Tokens = new List<StoredToken>() {
                        storedToken
                    }
                };
                await context.Users.AddAsync(user);
            }
            else
            {
                if (null == user.Tokens)
                {
                    user.Tokens = new List<StoredToken>();
                }
                user.Tokens.Add(storedToken);
            }
            await context.SaveChangesAsync();
            return storedToken.Id;
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
                var expired = (storedState.StoredTime + new TimeSpan(0, 0, 30)) < DateTime.Now;
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

        public async Task<bool> RemoveConfig(string userId, string configId)
        {
            var token = await context.Tokens.Where(v => v.UserId == userId && v.Id == configId).SingleOrDefaultAsync();
            if (null != token)
            {
                context.Tokens.Remove(token);
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
