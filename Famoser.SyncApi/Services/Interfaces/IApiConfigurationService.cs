﻿using System;
using System.Threading.Tasks;
using Famoser.SyncApi.Api.Configuration;
using Famoser.SyncApi.Models.Interfaces;

namespace Famoser.SyncApi.Services.Interfaces
{
    public interface IApiConfigurationService
    {
        ApiInformationEntity GetApiInformations();
        Task<TDevice> GetDeviceObjectAsync<TDevice>();
        Task<TUser> GetUserObjectAsync<TUser>(); 
        string GetFileName(string proposedFilename, Type objectType = null);
    }
}
