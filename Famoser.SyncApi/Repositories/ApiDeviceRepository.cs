﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Famoser.SyncApi.Api.Communication.Entities;
using Famoser.SyncApi.Api.Communication.Request;
using Famoser.SyncApi.Api.Configuration;
using Famoser.SyncApi.Clients;
using Famoser.SyncApi.Enums;
using Famoser.SyncApi.Helpers;
using Famoser.SyncApi.Managers;
using Famoser.SyncApi.Models.Interfaces;
using Famoser.SyncApi.Repositories.Base;
using Famoser.SyncApi.Repositories.Interfaces;
using Famoser.SyncApi.Services.Interfaces;
using Famoser.SyncApi.Services.Interfaces.Authentication;
using Famoser.SyncApi.Storage.Cache;
using Famoser.SyncApi.Storage.Cache.Entitites;
using Famoser.SyncApi.Storage.Roaming;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace Famoser.SyncApi.Repositories
{
    public class ApiDeviceRepository<TDevice, TUser> : PersistentRepository<TDevice>, IApiDeviceRepository<TDevice, TUser>, IApiDeviceAuthenticationService
        where TDevice : class, IDeviceModel
        where TUser : class, IUserModel
    {
        private readonly IApiStorageService _apiStorageService;
        private readonly IApiConfigurationService _apiConfigurationService;
        private readonly ApiClient _authApiClient;
        public ApiDeviceRepository(IApiConfigurationService apiConfigurationService, IApiStorageService apiStorageService) : base(apiConfigurationService)
        {
            _apiStorageService = apiStorageService;
            _apiConfigurationService = apiConfigurationService;

            _authApiClient = GetAuthApiClient();
        }

        private readonly AsyncLock _asyncLock = new AsyncLock();
        protected override async Task<bool> InitializeAsync()
        {
            using (await _asyncLock.LockAsync())
            {
                if (CacheEntity != null)
                    return true;

                //need this from authenticationservice
                if (_apiRoamingEntity == null)
                    return false;

                CacheEntity = await _apiStorageService.GetCacheEntity<TDevice>(GetModelCacheFilePath());
                if (CacheEntity.ModelInformation == null)
                {
                    CacheEntity.Model = await _apiConfigurationService.GetDeviceObjectAsync<TDevice>();
                    CacheEntity.ModelInformation = new ModelInformation()
                    {
                        Id = Guid.NewGuid(),
                        UserId = _apiRoamingEntity.UserId,
                        PendingAction = PendingAction.Create,
                        VersionId = Guid.NewGuid()
                    };
                    CacheEntity.Model.SetId(CacheEntity.ModelInformation.Id);
                    await _apiStorageService.SaveCacheEntityAsync<TDevice>();
                }

                return true;
            }
        }

        protected override async Task<bool> SyncInternalAsync()
        {
            if (CacheEntity.ModelInformation.PendingAction == PendingAction.None)
                return true;

            if (CacheEntity.ModelInformation.PendingAction == PendingAction.Create)
            {
                var resp = await _authApiClient.DoSyncRequestAsync(
                    AuthorizeRequest(ApiInformationEntity, _apiRoamingEntity, new AuthRequestEntity()
                    {
                        DeviceEntity = new DeviceEntity()
                        {
                            Id = CacheEntity.ModelInformation.Id,
                            OnlineAction = OnlineAction.Create,
                            VersionId = CacheEntity.ModelInformation.VersionId,
                            Content = JsonConvert.SerializeObject(CacheEntity.Model)
                        }
                    }));
                if (resp.RequestFailed)
                {
                    return false;
                }
            }
            // read is not valid action in this repo
            //else if (CacheEntity.ModelInformation.PendingAction == PendingAction.Read)
            //{
            //    var resp = await _authApiClient.DoRequestAsync(
            //        AuthorizeRequest(ApiInformationEntity, _apiRoamingEntity, new AuthRequestEntity()
            //        {
            //            UserEntity = new UserEntity()
            //            {
            //                Id = CacheEntity.ModelInformation.Id,
            //                OnlineAction = OnlineAction.Read
            //            }
            //        }));
            //    if (resp.IsSuccessfull)
            //    {
            //        Manager.Set(JsonConvert.DeserializeObject<TDevice>(resp.UserEntity.Content));
            //    }
            //    else
            //        return false;
            //}
            else if (CacheEntity.ModelInformation.PendingAction == PendingAction.Update)
            {
                var resp = await _authApiClient.DoSyncRequestAsync(
                    AuthorizeRequest(ApiInformationEntity, _apiRoamingEntity, new AuthRequestEntity()
                    {
                        DeviceEntity = new DeviceEntity()
                        {
                            Id = CacheEntity.ModelInformation.Id,
                            OnlineAction = OnlineAction.Update,
                            VersionId = CacheEntity.ModelInformation.VersionId,
                            Content = JsonConvert.SerializeObject(CacheEntity.Model)
                        }
                    }));
                if (resp.RequestFailed)
                {
                    return false;
                }
            }
            else if (CacheEntity.ModelInformation.PendingAction == PendingAction.Delete)
            {
                var resp = await _authApiClient.DoSyncRequestAsync(
                    AuthorizeRequest(ApiInformationEntity, _apiRoamingEntity, new AuthRequestEntity()
                    {
                        UserEntity = new UserEntity()
                        {
                            Id = CacheEntity.ModelInformation.Id,
                            OnlineAction = OnlineAction.Delete
                        }
                    }));
                if (resp.RequestFailed)
                {
                    return false;
                }

                //clean up
                CacheEntity.ModelInformation.PendingAction = PendingAction.None;
                return await _apiStorageService.EraseCacheEntityAsync<TUser>();
            }
            else
                return true;


            CacheEntity.ModelInformation.PendingAction = PendingAction.None;
            return await _apiStorageService.SaveCacheEntityAsync<TUser>();
        }


        private readonly AsyncLock _deviceLock = new AsyncLock();
        private bool _initializedDevices;
        //private CollectionCacheEntity<TDevice> _deviceCache;
        private async Task<bool> InitializeDevicesAsync()
        {
            using (await _deviceLock.LockAsync())
            {
                if (_initializedDevices)
                    return true;

                _initializedDevices = true;

                /*
                _deviceCache = await _apiStorageService.GetCollectionCacheEntity<TDevice>();
                if (_deviceCache.ModelInformations == null)
                {
                    _deviceCache.ModelInformations = new List<ModelInformation>();
                    _deviceCache.Models = new List<TDevice>();
                }
                
                //todo: implement device sync
                */
                return true;
            }
        }

        private readonly CollectionManager<TDevice> _deviceManager = new CollectionManager<TDevice>();
        public ObservableCollection<TDevice> GetAllLazy()
        {
#pragma warning disable 4014
            InitializeDevicesAsync();
#pragma warning restore 4014

            return _deviceManager.GetObservableCollection();
        }

        public Task<ObservableCollection<TDevice>> GetAllAsync()
        {
            return ExecuteSafe(async () =>
            {
                await InitializeDevicesAsync();

                return _deviceManager.GetObservableCollection();
            });
        }

        public Task<bool> UnAuthenticateAsync(TDevice device)
        {
            return ExecuteSafe(async () =>
            {
                var resp = await _authApiClient.UnAuthenticateRequestAsync(AuthorizeRequest(ApiInformationEntity, _apiRoamingEntity, new AuthRequestEntity()
                {
                    ClientMessage = device.GetId().ToString()
                }));
                return resp.IsSuccessfull;
            });
        }

        public Task<bool> AuthenticateAsync(TDevice device)
        {
            return ExecuteSafe(async () =>
            {
                var resp = await _authApiClient.AuthenticateRequestAsync(AuthorizeRequest(ApiInformationEntity, _apiRoamingEntity, new AuthRequestEntity()
                {
                    ClientMessage = device.GetId().ToString()
                }));
                return resp.IsSuccessfull;
            });
        }

        public Task<string> CreateNewAuthenticationCodeAsync()
        {
            return ExecuteSafe(async () =>
            {
                var resp = await _authApiClient.CreateAuthCodeRequestAsync(AuthorizeRequest(ApiInformationEntity, _apiRoamingEntity, new AuthRequestEntity()));
                return resp.IsSuccessfull ? resp.ServerMessage : default(string);
            });
        }

        public Task<bool> TryUseAuthenticationCodeAsync(string authenticationCode)
        {
            return ExecuteSafe(async () =>
            {
                var resp = await _authApiClient.DoSyncRequestAsync(AuthorizeRequest(ApiInformationEntity, _apiRoamingEntity, new AuthRequestEntity()
                {
                    ClientMessage = authenticationCode
                }));
                return resp.IsSuccessfull;
            });
        }

        private ApiRoamingEntity _apiRoamingEntity;
        public async Task<Guid?> GetAuthenticatedDeviceIdAsync(ApiRoamingEntity apiRoamingEntity)
        {
            _apiRoamingEntity = apiRoamingEntity;

            if (!await InitializeAsync())
                return null;

            if (Manager.GetModel().GetAuthenticationState() == AuthenticationState.Authenticated)
                return Manager.GetModel().GetId();
            return null;
        }

        private AuthRequestEntity AuthorizeRequest(ApiInformationEntity apiInformationEntity,
            ApiRoamingEntity apiRoamingInfo, AuthRequestEntity request)
        {
            request.AuthorizationCode = AuthorizationHelper.GenerateAuthorizationCode(apiInformationEntity, apiRoamingInfo);
            request.UserId = _apiRoamingEntity.UserId;
            request.DeviceId = CacheEntity.Model.GetId();
            return request;
        }
    }
}
