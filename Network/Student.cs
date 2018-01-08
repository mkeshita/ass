using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class Student
    {
        [ProtoMember(1)]
        public string StudentId { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public string Course { get; set; }
        
        [ProtoMember(4)]
        public bool IsAnonymous { get; set; }

        [ProtoMember(5)]
        public string UserName { get; set; }

        [ProtoMember(6)]
        public long Id { get; set; }
    }
}
