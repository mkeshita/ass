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
        
        [ProtoMember(3)]
        public long Revision { get; set; }
    }

    [ProtoContract]
    class GetUsers : Packet<GetUsers>
    {
        [ProtoMember(1)]
        public int Page { get; set; }
        
        [ProtoMember(2)]
        public long HighestId { get; set; }
    }
    
    [ProtoContract]
    class GetUsersResult : Packet<GetUsersResult>
    {
        [ProtoMember(1)]
        public int Page { get; set; }
        [ProtoMember(2)]
        public int Count { get; set; }
        [ProtoMember(3)]
        public List<UserInfo> Users { get; set; } = new List<UserInfo>();
        [ProtoMember(4)]
        public int Pages { get; set; }
    }

    [ProtoContract]
    class UserInfo : Packet<UserInfo>
    {
        [ProtoMember(1)]
        public long Id { get; set; }

        [ProtoMember(2)]
        public byte[] Picture { get; set; }

        [ProtoMember(3)]
        public string Username { get; set; }

        [ProtoMember(4)]
        public string Password { get; set; }

        [ProtoMember(5)]
        public string Firstname { get; set; }

        [ProtoMember(6)]
        public string Lastname { get; set; }

        [ProtoMember(7)]
        public int Access { get; set; }

        [ProtoMember(8)]
        public string Description { get; set; }

        [ProtoMember(9)]
        public bool IsAnonymous { get; set; }

        [ProtoMember(10)]
        public string StudentId { get; set; }

        [ProtoMember(11)]
        public long PictureRevision { get; set; }
    }

    [ProtoContract]
    class GetPicture : Packet<GetPicture>
    {
        [ProtoMember(1)]
        public int Session { get; set; }
        [ProtoMember(2)]
        public long UserId { get; set; }

        [ProtoMember(3)]
        public long Revision { get; set; }
    }
}
