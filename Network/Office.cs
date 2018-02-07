using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class Office : INotifyPropertyChanged
    {
       
        private string _ShortName;

        [ProtoMember(1)]
        public string ShortName
        {
            get => _ShortName;
            set
            {
                if(value == _ShortName)
                    return;
                _ShortName = value;
                OnPropertyChanged(nameof(ShortName));
            }
        }
        
       
        private string _LongName;

        [ProtoMember(2)]
        public string LongName
        {
            get => _LongName;
            set
            {
                if(value == _LongName)
                    return;
                _LongName = value;
                OnPropertyChanged(nameof(LongName));
            }
        }

        private float _Rating;

        [ProtoMember(3)]
        public float Rating
        {
            get => _Rating;
            set
            {
                if(value == _Rating)
                    return;
                _Rating = value;
                OnPropertyChanged(nameof(Rating));
            }
        }

        [ProtoMember(4)]
        public long Id { get; set; }

        private long _RatingCount;

        public long RatingCount
        {
            get => _RatingCount;
            set
            {
                if(value == _RatingCount)
                    return;
                _RatingCount = value;
                OnPropertyChanged(nameof(RatingCount));
            }
        }

        private long _SuggestionsCount;


        [ProtoMember(6)]
        public long SuggestionsCount
        {
            get => _SuggestionsCount;
            set
            {
                if(value == _SuggestionsCount)
                    return;
                _SuggestionsCount = value;
                OnPropertyChanged(nameof(SuggestionsCount));
            }
        }

        private byte[] _Picture;

        public byte[] Picture
        {
            get => _Picture;
            set
            {
                if(value == _Picture)
                    return;
                _Picture = value;
                OnPropertyChanged(nameof(Picture));
            }
        }

        

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [ProtoContract]
    class SetOfficePicture : Packet<SetOfficePicture>
    {
        [ProtoMember(1)]
        public long Id { get; set; }
        [ProtoMember(2)]
        public byte[] Picture { get; set; }
    }

    [ProtoContract]
    class SetOfficePictureResult : Packet<SetOfficePictureResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
    }

    [ProtoContract]
    class AddOfficeAdmin : Packet<AddOfficeAdmin>
    {
        [ProtoMember(1)]
        public long OfficeId { get; set; }
        [ProtoMember(2)]
        public long UserId { get; set; }
    }

    [ProtoContract]
    class AddOfficeAdminResult : Packet<AddOfficeAdminResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }

        [ProtoMember(2)]
        public long Id { get; set; }
    }

    [ProtoContract]
    class DeleteOffice : Packet<DeleteOffice>
    {
        [ProtoMember(1)]
        public long Id { get; set; }
    }

    [ProtoContract]
    class DeleteOfficeResult : Packet<DeleteOfficeResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
    }

    [ProtoContract]
    class SaveOffice : Packet<SaveOffice>
    {
        [ProtoMember(1)]
        public long Id { get; set; }
        [ProtoMember(2)]
        public string ShortName { get; set; }
        [ProtoMember(3)]
        public string LongName { get; set; }
    }

    [ProtoContract]
    class SaveOfficeResult : Packet<SaveOfficeResult>
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
        [ProtoMember(2)]
        public long Id { get; set; }
    }
    
    [ProtoContract]
    class Offices : Packet<Offices>
    {
        [ProtoMember(1)]
        public List<Office> Items { get; set; } = new List<Office>();
    }

    // beware of fragmentation
    [ProtoContract]
    class OfficeInfo : Packet<OfficeInfo>
    {
        [ProtoMember(1)]
        public long Id { get; set; }
        
        [ProtoMember(2)]
        public List<OfficeRating> Ratings { get; set; }
        
        [ProtoMember(3)]
        public List<Suggestion> Suggestions { get; set; }
    }
    
}
