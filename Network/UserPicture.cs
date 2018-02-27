using System;
using System.Collections.Generic;
using System.Text;
#if !__ANDROID__
using norsu.ass.Models;
#endif
using ProtoBuf;

namespace norsu.ass.Network
{
    
    [ProtoContract]
    class UserPicture : Packet<UserPicture>
    {
        [ProtoMember(1)]
        public long UserId { get; set; }

        [ProtoMember(2)]
        public byte[] Picture { get; set; }

        [ProtoMember(3)]
        public long Revision { get; set; }
    }

    [ProtoContract]
    class GetUsers : Packet<GetUsers>
    {
        [ProtoMember(1)]
        public int Page { get; set; }

        [ProtoMember(2)]
        public long HighestId { get; set; }
    }

    [ProtoContract]
    class GetUsersResult : Packet<GetUsersResult>
    {
        [ProtoMember(1)]
        public int Page { get; set; }

        [ProtoMember(2)]
        public int Count { get; set; }

        [ProtoMember(3)]
        public List<UserInfo> Users { get; set; } = new List<UserInfo>();

        [ProtoMember(4)]
        public int Pages { get; set; }
    }

    [ProtoContract]
    class UserInfo : Packet<UserInfo>
    {
        [ProtoMember(1)]
        public long Id { get; set; }

        [ProtoMember(2)]
        public byte[] Picture { get; set; }

        [ProtoMember(3)]
        public string Username { get; set; }

        [ProtoMember(4)]
        public string Password { get; set; }

        [ProtoMember(5)]
        public string Firstname { get; set; }

        [ProtoMember(6)]
        public string Lastname { get; set; }

        [ProtoMember(7)]
        public int Access { get; set; }

        [ProtoMember(8)]
        public string Description { get; set; }

        [ProtoMember(9)]
        public bool IsAnonymous { get; set; }

        [ProtoMember(10)]
        public string StudentId { get; set; }

        [ProtoMember(11)]
        public long PictureRevision { get; set; }

        [ProtoMember(12)]
        public Statuses Status { get; set; }

        [ProtoMember(13)]
        public string StatusMessage { get; set; }
    }

    [ProtoContract]
    class GetPicture : Packet<GetPicture>
    {
        [ProtoMember(1)]
        public int Session { get; set; }

        [ProtoMember(2)]
        public long UserId { get; set; }

        [ProtoMember(3)]
        public long Revision { get; set; }
    }

    [ProtoContract]
    class SetPicture : Packet<SetPicture>
    {
        [ProtoMember(1)]
        public long Id { get; set; }

        [ProtoMember(2)]
        public byte[] Picture { get; set; }
    }

    [ProtoContract]
    class SetPictureResult : Packet<SetPictureResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
    }

    [ProtoContract]
    class ResetPassword : Packet<ResetPassword>
    {
        [ProtoMember(1)]
        public long Id { get; set; }
    }

    [ProtoContract]
    class ChangePassword : Packet<ChangePassword>
    {
        [ProtoMember(1)]
        public string Current { get; set; }
        [ProtoMember(2)]
        public string NewPassword { get; set; }
        [ProtoMember(3)]
        public long Id { get; set; }
    }

    [ProtoContract]
    class ChangePasswordResult : Packet<ChangePasswordResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
    }

    [ProtoContract]
    class ResetPasswordResult : Packet<ResetPasswordResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
    }

    [ProtoContract]
    class DeleteUser : Packet<DeleteUser>
    {
        [ProtoMember(1)]
        public long Id { get; set; }
    }

    [ProtoContract]
    class DeleteUserResult : Packet<DeleteUserResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
    }

    [ProtoContract]
    class SaveUser : Packet<SaveUser>
    {
        [ProtoMember(1)]
        public UserInfo User { get; set; }
    }

    [ProtoContract]
    class SaveUserResult : Packet<SaveUserResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public long Id { get; set; }
    }

}
