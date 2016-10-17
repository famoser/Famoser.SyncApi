﻿using System;
using System.Threading.Tasks;
using Famoser.FrameworkEssentials.Helpers;
using Famoser.FrameworkEssentials.Logging.Interfaces;
using Famoser.SyncApi.Api.Configuration;
using Famoser.SyncApi.Clients;
using Famoser.SyncApi.Enums;
using Famoser.SyncApi.Managers;
using Famoser.SyncApi.Managers.Interfaces;
using Famoser.SyncApi.Repositories.Interfaces.Base;
using Famoser.SyncApi.Services.Interfaces;
using Famoser.SyncApi.Storage.Cache;

namespace Famoser.SyncApi.Repositories.Base
{
    public abstract class PersistentRepository<TModel> : IPersistentRespository<TModel>
    {
        protected readonly IManager<TModel> Manager = new Manager<TModel>();
        protected CacheEntity<TModel> CacheEntity;
        protected readonly ApiInformationEntity ApiInformationEntity;

        protected PersistentRepository(IApiConfigurationService apiConfigurationService)
        {
            ApiInformationEntity = apiConfigurationService.GetApiInformations();
        }

        protected AuthApiClient GetAuthApiClient()
        {
            return new AuthApiClient(ApiInformationEntity.Uri);
        }

        protected abstract Task<bool> SyncInternalAsync();
        protected abstract Task<bool> InitializeAsync();
        public Task<TModel> GetAsync()
        {
            return ExecuteSafe(async () =>
            {
                await SyncInternalAsync();

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
                return await SyncInternalAsync();
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
                return await SyncInternalAsync();
            });
        }

        public Task<bool> SyncAsync()
        {
            return ExecuteSafe(async () => await SyncInternalAsync());
        }


        private IExceptionLogger _exceptionLogger;
        protected async Task<T> ExecuteSafe<T>(Func<Task<T>> func)
        {
            try
            {
                if (!await InitializeAsync())
                    return default(T);

                return await func();
            }
            catch (Exception ex)
            {
                _exceptionLogger?.LogException(ex, this);
            }
            return default(T);
        }
    }
}
