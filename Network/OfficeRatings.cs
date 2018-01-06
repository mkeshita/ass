﻿using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class OfficeRatings : Packet<OfficeRatings>
    {
        [ProtoMember(1)]
        public long OfficeId { get; set; }
        [ProtoMember(2)]
        public List<OfficeRating> Ratings { get; set; } = new List<OfficeRating>();
        [ProtoMember(3)]
        public float Rating { get; set; }
    }
}
