using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class GetRatings : Packet<GetRatings>
    {
        [ProtoMember(1)]
        public int Session { get; set; }
        
        [ProtoMember(2)]
        public long OfficeId { get; set; }

        [ProtoMember(3)]
        public int Page { get; set; }
        
    }
}
