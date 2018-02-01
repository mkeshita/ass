using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class Desktop : Packet<Desktop>
    {
        [ProtoMember(1)]
        public string IP { get; set; }

        [ProtoMember(2)]
        public int Port { get; set; }
        
    }
}
