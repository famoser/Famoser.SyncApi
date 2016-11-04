﻿using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Famoser.SyncApi.Api;
using Famoser.SyncApi.Api.Communication.Entities;
using Famoser.SyncApi.Api.Communication.Request;
using Famoser.SyncApi.Api.Configuration;
using Famoser.SyncApi.Enums;
using Famoser.SyncApi.Helpers;
using Famoser.SyncApi.Managers;
using Famoser.SyncApi.Managers.Interfaces;
using Famoser.SyncApi.Models.Information;
using Famoser.SyncApi.Models.Interfaces.Base;
using Famoser.SyncApi.Repositories.Interfaces.Base;
using Famoser.SyncApi.Services.Interfaces;
using Famoser.SyncApi.Services.Interfaces.Authentication;
using Famoser.SyncApi.Storage.Cache;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace Famoser.SyncApi.Repositories.Base
{
    public abstract class PersistentRepository<TModel> : BasePersistentRepository<TModel>, IPersistentRespository<TModel>
        where TModel : IUniqueSyncModel
    {
        protected readonly IManager<TModel> Manager;
        protected CacheEntity<TModel> CacheEntity;
        protected readonly ApiInformation ApiInformation;
        private readonly IApiConfigurationService _apiConfigurationService;
        private readonly IApiStorageService _apiStorageService;

        protected PersistentRepository(IApiConfigurationService apiConfigurationService, IApiStorageService apiStorageService)
            : base(apiConfigurationService)
        {
            _apiConfigurationService = apiConfigurationService;
            _apiStorageService = apiStorageService;

            Manager = _apiConfigurationService.GetManager<TModel>();
            ApiInformation = apiConfigurationService.GetApiInformations();
        }

        protected ApiClient GetAuthApiClient()
        {
            return new ApiClient(ApiInformation.Uri);
        }

        public Task<TModel> GetAsync()
        {
            return ExecuteSafe(async () =>
            {
                if (_apiConfigurationService.StartSyncAutomatically())
                    await SyncAsync();

                return Manager.GetModel();
            });
        }

        public Task<bool> SaveAsync()
        {
            return ExecuteSafe(async () =>
            {
                if (CacheEntity.ModelInformation.PendingAction == PendingAction.None
                    || CacheEntity.ModelInformation.PendingAction == PendingAction.Delete
                    || CacheEntity.ModelInformation.PendingAction == PendingAction.Read)
                {
                    CacheEntity.ModelInformation.VersionId = Guid.NewGuid();
                    CacheEntity.ModelInformation.PendingAction = PendingAction.Update;
                }
                await SaveCacheAsync();
                return true;
            });
        }

        public Task<bool> RemoveAsync()
        {
            return ExecuteSafe(async () =>
            {
                if (CacheEntity.ModelInformation.PendingAction != PendingAction.Create)
                {
                    CacheEntity.ModelInformation.PendingAction = PendingAction.Create;
                }
                await SaveCacheAsync();
                return true;
            });
        }

        protected async Task SaveCacheAsync()
        {
            try
            {
                await _apiStorageService.SaveCacheEntityAsync<CacheEntity<TModel>>();
                if (_apiConfigurationService.CanUseWebConnection())
                    await SyncInternalAsync();
            }
            catch (Exception ex)
            {
                ExceptionLogger?.LogException(ex, this);
            }
        }

        public CacheInformations GetCacheInformations()
        {
            return CacheEntity.ModelInformation;
        }
    }
}
