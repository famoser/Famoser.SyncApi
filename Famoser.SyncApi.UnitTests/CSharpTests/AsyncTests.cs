﻿using System;
using System.Net;
using System.Threading.Tasks;
using Famoser.SyncApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Famoser.SyncApi.UnitTests.CSharpTests
{
    [TestClass]
    public class AsyncTests
    {
        [TestMethod]
        public async Task CheckForSameInstanceAsync()
        {
            var instance = await GetInstanceAsync();
            Assert.IsTrue(instance.Item1 == _collectionModel);


            var instance2 = await GetInstanceAsync();
            Assert.IsTrue(instance2.Item1 == _collectionModel);
        }

        private readonly CollectionModel _collectionModel = new CollectionModel();
        private async Task<Tuple<CollectionModel, byte[]>> GetInstanceAsync()
        {
            using (var client = new WebClient())
            {
                var res = await client.DownloadDataTaskAsync(new Uri("https://www.google.ch/?gws_rd=ssl"));
                return new Tuple<CollectionModel, byte[]>(_collectionModel, res);
            }
        }
    }
}
