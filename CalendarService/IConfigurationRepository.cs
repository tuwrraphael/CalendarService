﻿using System.Threading.Tasks;

namespace CalendarService
{
    public interface IConfigurationRepository
    {
        Task<bool> RemoveConfig(string userId, string configId);
        Task<ConfigState> GetConfigState(string state);
        Task<string> AddMicrosoftTokens(string userId, TokenResponse tokens);
        Task CreateConfigState(string userId, string state, string redirectUri);
        /// <returns>null if user does not exist</returns>
        Task<StoredConfiguration[]> GetConfigurations(string userid);
        Task<StoredConfiguration> GetConfiguration(string configId);
        Task<StoredConfiguration> RefreshTokens(string id, TokenResponse tokens);
        Task<bool> SetFeeds(string v, string id, string[] feedIds);
        Task UpdateNotification(string configId, string feedId, NotificationInstallation result);
        Task<string> GetUserIdByNotificationAsync(string notificationId);
        Task<string> AddGoogleTokens(string userId, TokenResponse tokenResponse);
    }
}