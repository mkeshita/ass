using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class RegistrationRequest : Packet<RegistrationRequest>
    {
        [ProtoMember(1)]
        public string Username { get; set; }
        
        [ProtoMember(2)]
        public string Password { get; set; }
        
        [ProtoMember(3)]
        public string Name { get; set; }
        
        [ProtoMember(4)]
        public string Course { get; set; }
    }

    [ProtoContract]
    class RegistrationResult : Packet<RegistrationResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        
        [ProtoMember(2)]
        public string Message { get; set; }
        
        [ProtoMember(3)]
        public int Session { get; set; }
        
    }
}
