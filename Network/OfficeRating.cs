using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class OfficeRating
    {
        [ProtoMember(1)]
        public long OfficeId { get; set; }
        
        [ProtoMember(2)]
        public int Rating { get; set; }
        
        [ProtoMember(3)]
        public string Message { get; set; }
        
        [ProtoMember(5)]
        public string StudentName { get; set; }
    }
}
