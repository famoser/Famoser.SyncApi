﻿using System.Threading.Tasks;
using Famoser.SyncApi.Models.Interfaces;
using Famoser.SyncApi.Storage.Roaming;

namespace Famoser.SyncApi.Services.Interfaces.Authentication
{
    /// <summary>
    /// This service creates & authenticates a device against the api
    /// </summary>
    public interface IApiDeviceAuthenticationService
    {
        /// <summary>
        /// Get an authenticated device model
        /// </summary>
        /// <param name="apiRoamingEntity"></param>
        /// <returns></returns>
        Task<IDeviceModel> GetDeviceAsync(ApiRoamingEntity apiRoamingEntity);

        /// <summary>
        /// Set the authentication service
        /// </summary>
        /// <param name="apiAuthenticationService"></param>
        void SetAuthenticationService(IApiAuthenticationService apiAuthenticationService);

        /// <summary>
        /// clean up the service. This is called once the user is removed
        /// </summary>
        /// <returns></returns>
        Task<bool> CleanUpDeviceAsync();
    }
}
