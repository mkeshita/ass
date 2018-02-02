using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class OfficeRating : Packet<OfficeRating>
    {
        [ProtoMember(1)]
        public long OfficeId { get; set; }
        
        [ProtoMember(2)]
        public int Rating { get; set; }
        
        [ProtoMember(3)]
        public string Message { get; set; }
        
        [ProtoMember(4)]
        public bool IsAnonymous { get; set; }
        
        [ProtoMember(5)]
        public string StudentName { get; set; }
        
        [ProtoMember(6)]
        public bool MyRating { get; set; }
        
        [ProtoMember(7)]
        public bool IsPrivate { get; set; }

        [ProtoMember(8)]
        public long UserId { get; set; }

        [ProtoMember(9)]
        public long Id { get; set; }
    }
}
