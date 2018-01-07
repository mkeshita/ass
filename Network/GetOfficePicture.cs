using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class OfficePicture  : Packet<OfficePicture>
    {
        [ProtoMember(1)]
        public long OfficeId { get; set; }
        [ProtoMember(2)]
        public byte[] Picture { get; set; }
    }

    [ProtoContract]
    class GetOfficePicture : Packet<GetOfficePicture>
    {
        [ProtoMember(1)]
        public long OfficeId { get; set; }

        [ProtoMember(2)]
        public int Session { get; set; }
    }
}
