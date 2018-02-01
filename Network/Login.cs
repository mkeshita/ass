using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class LoginResult : Packet<LoginResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }

        [ProtoMember(2)]
        public Student Student { get; set; }
        
        [ProtoMember(3)]
        public int Session { get; set; }
        
        public LoginResult() { }

        public LoginResult(Student student, int session)
        {
            Student = student;
            Success = Student != null;
            Session = session;
        }
    }

    [ProtoContract]
    class DesktopLoginResult : Packet<DesktopLoginResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
        [ProtoMember(3)]
        public UserInfo User { get; set; }
    }
    [ProtoContract]
    class DesktopLoginRequest : Packet<DesktopLoginRequest>
    {
        [ProtoMember(1)]
        public string Username { get; set; }
        [ProtoMember(2)]
        public string Password { get; set; }
    }
    
    [ProtoContract]
    class LoginRequest : Packet<LoginRequest>
    {
        [ProtoMember(1)]
        public string Username { get; set; }
        
        [ProtoMember(2)]
        public string Password { get; set; }
        
        [ProtoMember(3)]
        public bool Annonymous { get; set; }
    }
}
