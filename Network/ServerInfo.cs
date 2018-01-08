using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class ServerInfo : Packet<ServerInfo>
    {
        [ProtoMember(1)]
        public string IP { get; set; }
        [ProtoMember(2)]
        public int Port { get; set; }
        [ProtoMember(3)]
        public bool AllowAnnonymous { get; set; }
        
        [ProtoMember(4)]
        public bool FullnameRequired { get; set; }
        
        [ProtoMember(5)]
        public bool AllowRegistration { get; set; }
        
        [ProtoMember(6)]
        public bool AllowPrivateSuggestions { get; set; }
        
        [ProtoMember(7)]
        public bool CanDeleteSuggestion { get; set; }
        
        [ProtoMember(8)]
        public bool CanEditSuggestion { get; set; }
        
        [ProtoMember(9)]
        public int ReplyDepth { get; set; }
    }
}
