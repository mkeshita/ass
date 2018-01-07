using System.Collections.Generic;
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

        [ProtoMember(6)]
        public long UserId { get; set; }
    }

    [ProtoContract]
    class Comments : Packet<Comments>
    {
        [ProtoMember(1)]
        public long SuggestionId { get; set; }
        
        [ProtoMember(2)]
        public List<Comment> Items { get; set; } = new List<Comment>();
    }

    [ProtoContract]
    class GetComments : Packet<GetComments>
    {
        [ProtoMember(1)]
        public long SuggestionId { get; set; }
        
        [ProtoMember(2)]
        public int Session { get; set; }
    }

    [ProtoContract]
    class AddComment : Packet<AddComment>
    {
        [ProtoMember(1)]
        public long SuggestionId { get; set; }
        
        [ProtoMember(2)]
        public int Session { get; set; }
        
        [ProtoMember(3)]
        public string Message { get; set; }
    }
}
