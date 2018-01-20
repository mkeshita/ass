using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class Shutdown : Packet<Shutdown>
    {
    }
}
