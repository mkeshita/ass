using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class UserPicture : Packet<UserPicture>
    {
        [ProtoMember(1)]
        public long UserId { get; set; }
        
        [ProtoMember(2)]
        public byte[] Picture { get; set; }
    }

    [ProtoContract]
    class GetPicture : Packet<GetPicture>
    {
        [ProtoMember(1)]
        public int Session { get; set; }
        [ProtoMember(2)]
        public long UserId { get; set; }
    }
}
