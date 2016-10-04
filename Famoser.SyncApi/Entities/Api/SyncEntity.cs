﻿using System;
using Famoser.SyncApi.Entities.Api.Base;

namespace Famoser.SyncApi.Entities.Api
{
    public class SyncEntity : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid CollectionId { get; set; }
        public string Identifier { get; set; }
        public string GroupIdentifier { get; set; }
    }
}
