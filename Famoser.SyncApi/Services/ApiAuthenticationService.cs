﻿using System;
using System.Threading.Tasks;
using Famoser.SyncApi.Api.Communication.Entities;
using Famoser.SyncApi.Api.Communication.Request;
using Famoser.SyncApi.Api.Communication.Request.Base;
using Famoser.SyncApi.Api.Configuration;
using Famoser.SyncApi.Api.Enums;
using Famoser.SyncApi.Containers;
using Famoser.SyncApi.Enums;
using Famoser.SyncApi.Helpers;
using Famoser.SyncApi.Models.Information;
using Famoser.SyncApi.Models.Interfaces;
using Famoser.SyncApi.Repositories.Interfaces;
using Famoser.SyncApi.Services.Interfaces;
using Famoser.SyncApi.Services.Interfaces.Authentication;
using Famoser.SyncApi.Storage.Roaming;
using Nito.AsyncEx;

namespace Famoser.SyncApi.Services
{
    public class ApiAuthenticationService : IApiAuthenticationService
    {
        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly IApiUserAuthenticationService _apiUserAuthenticationService;
        private readonly IApiDeviceAuthenticationService _apiDeviceAuthenticationService;
        private readonly ApiInformation _apiInformation;

        public ApiAuthenticationService(IApiConfigurationService apiConfigurationService, IApiUserAuthenticationService apiUserAuthenticationService, IApiDeviceAuthenticationService apiDeviceAuthenticationService)
        {
            _apiUserAuthenticationService = apiUserAuthenticationService;
            _apiUserAuthenticationService.SetAuthenticationService(this);

            _apiDeviceAuthenticationService = apiDeviceAuthenticationService;
            _apiDeviceAuthenticationService.SetAuthenticationService(this);

            _apiInformation = apiConfigurationService.GetApiInformations();
        }

        private bool IsAuthenticated()
        {
            return IsInitialized() && _apiRoamingEntity.AuthenticationState == AuthenticationState.Authenticated && _deviceModel.GetAuthenticationState() == AuthenticationState.Authenticated;
        }

        private bool IsInitialized()
        {
            return _apiRoamingEntity != null && _deviceModel != null;
        }

        private DateTime _lastRefresh = DateTime.MinValue;
        private async Task ReInitializeAsync()
        {
            using (await _asyncLock.LockAsync())
            {
                if (_lastRefresh < DateTime.Now - TimeSpan.FromSeconds(2))
                {
                    _apiRoamingEntity = await _apiUserAuthenticationService.GetApiRoamingEntityAsync();
                    _deviceModel = await _apiDeviceAuthenticationService.GetDeviceAsync(_apiRoamingEntity);
                    _lastRefresh = DateTime.Now;
                }
            }
        }

        private ApiRoamingEntity _apiRoamingEntity;
        private IDeviceModel _deviceModel;
        public async Task<bool> IsAuthenticatedAsync()
        {
            if (IsAuthenticated())
                return true;
            await ReInitializeAsync();
            return IsAuthenticated();
        }

        public async Task<T> CreateRequestAsync<T>(string identifier, int messageCount = 0) where T : BaseRequest, new()
        {
            if (!IsInitialized())
            {
                await ReInitializeAsync();
                if (!IsInitialized())
                    return null;
            }

            var request = new T
            {
                AuthorizationCode = AuthorizationHelper.GenerateAuthorizationCode(_apiInformation, _apiRoamingEntity, messageCount),
                UserId = _apiRoamingEntity.UserId,
                DeviceId = _deviceModel.GetId(),
                ApplicationId = _apiInformation.ApplicationId,
                Identifier = identifier
            };

            return request;
        }

        public async Task<T> CreateRequestAsync<T, TCollection>(string identifier) where T : SyncEntityRequest, new() where TCollection : ICollectionModel
        {
            var req = await CreateRequestAsync<T>(identifier);
            if (_apiCollectionRepositoryContainer.Contains<TCollection>())
            {
                var ss = _apiCollectionRepositoryContainer.Get<TCollection>();
                if (ss != null)
                {
                    var collections = await ss.GetAllAsync();
                    foreach (var collection in collections)
                    {
                        req.CollectionEntities.Add(new CollectionEntity()
                        {
                            Id = collection.GetId(),
                            OnlineAction = OnlineAction.ConfirmAccess
                        });
                    }
                }
            }
            return req;
        }

        public async Task<CacheInformations> CreateModelInformationAsync()
        {
            if (!IsInitialized())
            {
                await ReInitializeAsync();
                if (!IsInitialized())
                    return null;
            }

            var mi = new CacheInformations
            {
                Id = Guid.NewGuid(),
                VersionId = Guid.NewGuid(),
                UserId = _apiRoamingEntity.UserId,
                DeviceId = _deviceModel.GetId(),
                CreateDateTime = DateTime.Now,
                PendingAction = PendingAction.Create
            };
            return mi;
        }

        private readonly ApiCollectionRepositoryContainer _apiCollectionRepositoryContainer = new ApiCollectionRepositoryContainer();
        public void RegisterCollectionRepository<TCollection>(IApiCollectionRepository<TCollection> repository) where TCollection : ICollectionModel
        {
            _apiCollectionRepositoryContainer.Add(repository);
        }

        public Guid? TryGetDeviceId()
        {
            if (IsAuthenticated())
            {
                return _deviceModel?.GetId();
            }
            return null;
        }

        private readonly ApiRepositoryContainer _apiRepositoryContainer = new ApiRepositoryContainer();
        public void RegisterRepository<TSyncModel, TCollection>(IApiRepository<TSyncModel, TCollection> repository) where TSyncModel : ISyncModel where TCollection : ICollectionModel
        {
            _apiRepositoryContainer.Add(repository);
        }

        public void UnRegisterCollectionRepository<TCollection>(IApiCollectionRepository<TCollection> repository) where TCollection : ICollectionModel
        {
            _apiCollectionRepositoryContainer.Remove<TCollection>();
        }

        public void UnRegisterRepository<TSyncModel, TCollection>(IApiRepository<TSyncModel, TCollection> repository) where TSyncModel : ISyncModel where TCollection : ICollectionModel
        {
            _apiRepositoryContainer.Remove<TSyncModel, TCollection>();
        }

        public async Task CleanUpAfterUserRemoveAsync()
        {
            await _apiDeviceAuthenticationService.CleanUpDeviceAsync();

            _apiRoamingEntity = null;
            await CleanUpAfterDeviceRemoveAsync();
        }

        public async Task CleanUpAfterDeviceRemoveAsync()
        {
            foreach (var value in _apiCollectionRepositoryContainer.GetAll())
            {
                dynamic repo = value;
                await repo.CleanUpAsync();
            }

            foreach (var value in _apiRepositoryContainer.GetAll())
            {
                dynamic repo = value;
                await repo.CleanUpAsync();
            }

            //reset all auth
            _deviceModel = null;
            _lastRefresh = DateTime.MinValue;
        }

        public async Task CleanUpAfterCollectionRemoveAsync<TCollection>(TCollection collection) where TCollection : ICollectionModel
        {
            foreach (var value in _apiRepositoryContainer.GetAll<TCollection>())
            {
                dynamic repo = value;
                await repo.RemoveAllFromCollectionAsync(collection);
            }
        }
    }
}
