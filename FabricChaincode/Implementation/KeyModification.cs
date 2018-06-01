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

using System;
using Google.Protobuf;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class KeyModification : IKeyModification
    {
        private readonly ByteString value;

        public KeyModification(Protos.Ledger.QueryResult.KeyModification km)
        {
            TxId = km.TxId;
            value = km.Value;
            Timestamp = km.Timestamp?.ToDateTime();
            IsDeleted = km.IsDelete;
        }


        public string TxId { get; }
        public byte[] Value => value.ToByteArray();
        public string StringValue => value.ToStringUtf8();
        public DateTime? Timestamp { get; }
        public bool IsDeleted { get; }
    }
}