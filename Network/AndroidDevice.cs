using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProtoBuf;

namespace norsu.ass.Network
{
    [ProtoContract]
    class AndroidDevice : Packet<AndroidDevice>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [ProtoMember(1)]
        public string IP { get; set; }
        
        [ProtoMember(2)]
        public int Port { get; set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
