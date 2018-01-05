using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class Suggestion
    {
        [ProtoMember(1)]
        public long OfficeId { get; set; }
        
        [ProtoMember(2)]
        public string Title { get; set; }
        
        [ProtoMember(3)]
        public string Body { get; set; }
        
        [ProtoMember(4)]
        public string StudentName { get; set; }

        [ProtoMember(5)]
        public int Likes { get; set; }
        
        [ProtoMember(6)]
        public int Dislikes { get; set; }
    }
}
