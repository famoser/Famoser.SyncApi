﻿using System;

namespace Famoser.SyncApi.Api.Communication.Request.Base
{
    public class BaseRequest
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public string AuthorizationCode { get; set; }
    }
}
