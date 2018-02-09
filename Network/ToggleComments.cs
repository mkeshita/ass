using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class ToggleComments : Packet<ToggleComments>
    {
        [ProtoMember(1)]
        public long SuggestionId { get; set; }
        [ProtoMember(2)]
        public long UserId { get; set; }
    }

    [ProtoContract]
    class ToggleCommentsResult : Packet<ToggleCommentsResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }

        [ProtoMember(2)]
        public bool AllowComments { get; set; }
    }
}
