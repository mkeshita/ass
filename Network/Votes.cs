using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class GetVotes : Packet<GetVotes>
    {
        [ProtoMember(1)]
        public long SuggestionId { get; set; }
        [ProtoMember(2)]
        public long HighestId { get; set; }
    }

    [ProtoContract]
    class Votes : Packet<Votes>
    {
        [ProtoMember(1)]
        public List<Vote> List { get; set; } = new List<Vote>();
    }
    
    [ProtoContract]
    class Vote : Packet<Vote>
    {
        [ProtoMember(1)]
        public long SuggestionId { get; set; }
        
        [ProtoMember(2)]
        public bool DownVote { get; set; }

        [ProtoMember(3)]
        public long UserId { get; set; }
        
        [ProtoMember(4)]
        public long Id { get; set; }
        
    }
}
