using System.Collections.Generic;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class Office
    {
        [ProtoMember(1)]
        public string ShortName { get; set; }

        [ProtoMember(2)]
        public string LongName { get; set; }

        [ProtoMember(3)]
        public float Rating { get; set; }
        
        [ProtoMember(4)]
        public long Id { get; set; }

        [ProtoMember(5)]
        public long RatingCount { get; set; } = 0;

        [ProtoMember(6)]
        public long SuggestionsCount { get; set; }
    }

    [ProtoContract]
    class Offices : Packet<Offices>
    {
        [ProtoMember(1)]
        public List<Office> Items { get; set; } = new List<Office>();
    }

    // beware of fragmentation
    [ProtoContract]
    class OfficeInfo : Packet<OfficeInfo>
    {
        [ProtoMember(1)]
        public long Id { get; set; }
        
        [ProtoMember(2)]
        public List<OfficeRating> Ratings { get; set; }
        
        [ProtoMember(3)]
        public List<Suggestion> Suggestions { get; set; }
    }
    
}
