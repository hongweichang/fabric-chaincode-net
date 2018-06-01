/*
Copyright IBM 2017 All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

         http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using Google.Protobuf;
using Hyperledger.Fabric.Protos.Ledger.QueryResult;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Implementation
{
    [TestClass]
    public class KeyValueTest
    {
        [TestMethod]
        public void TestKeyValueImpl()
        {
            var _=new KeyValue(new KV {Key = "key", Value = ByteString.CopyFromUtf8("value")});
        }

        [TestMethod]
        public void TestGetKey()
        {
            KeyValue kv = new KeyValue(new KV {Key = "key", Value = ByteString.CopyFromUtf8("value")});
            Assert.AreEqual(kv.Key, "key");
        }


        [TestMethod]
        public void TestGetValue()
        {
            KeyValue kv = new KeyValue(new KV {Key = "key", Value = ByteString.CopyFromUtf8("value")});
            CollectionAssert.AreEqual(kv.Value, "value".ToBytes());
        }

        [TestMethod]
        public void TestGetStringValue()
        {
            KeyValue kv = new KeyValue(new KV {Key = "key", Value = ByteString.CopyFromUtf8("value")});
            Assert.AreEqual(kv.StringValue, "value");
        }
    }
}