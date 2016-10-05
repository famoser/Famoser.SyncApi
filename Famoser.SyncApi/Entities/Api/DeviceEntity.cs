﻿using System;
using Famoser.SyncApi.Entities.Api.Base;

namespace Famoser.SyncApi.Entities.Api
{
    public class DeviceEntity : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime LastRequestDateTime { get; set; }
    }
}
