using System.Collections.Generic;
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
        public long Likes { get; set; }
        
        [ProtoMember(6)]
        public int Dislikes { get; set; }

        [ProtoMember(7)]
        public bool Liked { get; set; }
        
        [ProtoMember(8)]
        public bool Disliked { get; set; }

        [ProtoMember(9)]
        public long Id { get; set; }

        public int RowId { get; set; }
        
        [ProtoMember(10)]
        public long UserId { get; set; }

        [ProtoMember(11)]
        public long Comments { get; set; }
        
        [ProtoMember(12)]
        public bool AllowComment { get; set; }
        
        [ProtoMember(13)]
        public long CommentsDisabledBy { get; set; }
    }

    [ProtoContract]
    class GetSuggestions : Packet<GetSuggestions>
    {
        [ProtoMember(1)]
        public int Session { get; set; }
        
        [ProtoMember(2)]
        public long OfficeId { get; set; }

        [ProtoMember(3)]
        public int Page { get; set; }
    }

    [ProtoContract]
    class Suggestions : Packet<Suggestions>
    {
        [ProtoMember(1)]
        public long OfficeId { get; set; }

        [ProtoMember(2)]
        public List<Suggestion> Items { get; set; } = new List<Suggestion>();

        [ProtoMember(3)]
        public long TotalCount { get; set; }
        
        [ProtoMember(4)]
        public int Page { get; set; }
        
        [ProtoMember(5)]
        public bool Full { get; set; }

        [ProtoMember(6)]
        public int Pages { get; set; }
    }

    [ProtoContract]
    class LikeSuggestion : Packet<LikeSuggestion>
    {
        [ProtoMember(1)]
        public int Session { get; set; }
        
        [ProtoMember(2)]
        public long SuggestionId { get; set; }
        
        [ProtoMember(3)]
        public bool Dislike { get; set; }
    }

    [ProtoContract]
    class Suggest : Packet<Suggest>
    {
        [ProtoMember(1)]
        public int Session { get; set; }
        
        [ProtoMember(2)]
        public string Subject { get; set; }
        
        [ProtoMember(3)]
        public string Body { get; set; }
        
        [ProtoMember(4)]
        public long OfficeId { get; set; }

        [ProtoMember(5)]
        public bool IsPrivate { get; set; }
    }

    [ProtoContract]
    class SuggestResult : Packet<SuggestResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }

        [ProtoMember(3)]
        public int TotalCount { get; set; }
    }


}
