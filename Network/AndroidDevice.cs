using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class AndroidDevice : Packet<AndroidDevice>
    {
       
        [ProtoMember(1)]
        public string IP { get; set; }
        
        [ProtoMember(2)]
        public int Port { get; set; }
        
    }
}
