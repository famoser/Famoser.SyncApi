﻿using System;
using System.Threading.Tasks;
using Famoser.FrameworkEssentials.Services;
using Famoser.SyncApi.Api.Communication.Request.Base;
using Famoser.SyncApi.Api.Communication.Response.Base;
using Newtonsoft.Json;
using Famoser.SyncApi.Services.Interfaces;

namespace Famoser.SyncApi.Api.Base
{
    public class BaseApiClient : IDisposable
    {
        private readonly Uri _baseUri;
        private readonly RestService _restService;
        private readonly IApiTraceService _logger;

        public BaseApiClient(Uri baseUri, IApiTraceService service)
        {
            _baseUri = baseUri;
            _logger = service;
            _restService = new RestService(null, true, _logger);
        }

        private Uri GetUri(string node)
        {
            return new Uri(_baseUri.AbsoluteUri + "1.0/" + node);
        }

        protected virtual async Task<T> DoApiRequestAsync<T>(BaseRequest request, string node = "") where T : BaseResponse, new()
        {
            var response = await _restService.PostJsonAsync(GetUri(node), JsonConvert.SerializeObject(request));
            if (response != null)
            {
                var rawResponse = await response.GetResponseAsStringAsync();
                var obj = JsonConvert.DeserializeObject<T>(rawResponse);
                if (obj != null)
                {
                    obj.RequestFailed = !response.IsRequestSuccessfull;
                    if (obj.RequestFailed)
                    {
                        _logger.TraceFailedRequest(request, node, obj.ServerMessage);
                    }
                    else
                    {
                        _logger.TraceSuccessfulRequest(request, node);
                    }
                    return obj;
                }
                return new T()
                {
                    ServerMessage = "Server responded with an " + response.HttpResponseMessage.StatusCode,
                    RequestFailed = true
                };
            }
            return new T()
            {
                ServerMessage = "Server could not be found",
                RequestFailed = true
            };
        }

        private bool _isDisposed;
        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
                if (disposing)
                    _restService.Dispose();
            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
