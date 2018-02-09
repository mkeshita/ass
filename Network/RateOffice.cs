using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class RateOffice : Packet<RateOffice>
    {
        [ProtoMember(1)]
        public int Session { get; set; }
        
        [ProtoMember(2)]
        public long OfficeId { get; set; }
        
        [ProtoMember(3)]
        public int Rating { get; set; }
        
        [ProtoMember(4)]
        public string Message { get; set; }

        [ProtoMember(5)]
        public bool IsPrivate { get; set; }

        [ProtoMember(6)]
        public long ReturnCount { get; set; } = -1;
    }

    [ProtoContract]
    class RateOfficeResult : Packet<RateOfficeResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
    }
}
