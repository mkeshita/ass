using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class Comment
    {
        [ProtoMember(1)]
        public long SuggestionId { get; set; }
        
        [ProtoMember(2)]
        public string Message { get; set; }
        
        [ProtoMember(3)]
        public string Sender { get; set; }
        
        [ProtoMember(4)]
        public long Id { get; set; }
        
        [ProtoMember(5)]
        public long ParentId { get; set; }
    }
}
