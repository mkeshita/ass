using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using norsu.ass.Models;
using norsu.ass.Server.Properties;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;

namespace norsu.ass.Network
{
    class Server : INotifyPropertyChanged
    {
        private Server()
        {
            var serializer = DPSManager.GetDataSerializer<ProtobufSerializer>();
            NetworkComms.DisableLogging();
            NetworkComms.DefaultSendReceiveOptions = new SendReceiveOptions(serializer,
                NetworkComms.DefaultSendReceiveOptions.DataProcessors, NetworkComms.DefaultSendReceiveOptions.Options);

            NetworkComms.AppendGlobalIncomingPacketHandler<AndroidDevice>(AndroidDevice.Header, HandShakeHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<LoginRequest>(LoginRequest.Header, LoginHandler);
            
            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            
        }

        private Dictionary<int, User> Sessions { get; } = new Dictionary<int, User>();
        private Random SessionID = new Random();
        
        private async void LoginHandler(PacketHeader packetheader, Connection connection, LoginRequest request)
        {
            var dev = Devices.FirstOrDefault(d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());
            
            if (dev == null) return;
            
            User user = null;
            if (request.Annonymous)
            {

                if (Settings.Default.AllowAnnonymousUser)
                {
                    var usr = new User()
                    {
                        Username = DateTime.Now.Ticks.ToString(),
                        Firstname = request.Username,
                        IsAnnonymous = true,
                        Access = User.AccessLevels.Student,
                    };
                    usr.Save();

                    user = usr;
                }

            }
            else
            {
                user = User.Cache.FirstOrDefault(x => x.Username.ToLower() == request.Username.ToLower());
                
            }

            var result = new LoginResult();
            if(user != null)
            {
                var sid = SessionID.Next(7, int.MaxValue);

                while (Sessions.ContainsKey(sid))
                    sid = SessionID.Next(7, int.MaxValue);

                Sessions.Add(sid, user);

                result = new LoginResult(new Student() {Name = request.Username}, sid);
            }
            await result.Send(dev.IP, dev.Port);
        }

        private async void HandShakeHandler(PacketHeader packetHeader, Connection connection, AndroidDevice ad)
        {
            var dev = Devices.FirstOrDefault(x => x.IP == ad.IP);
            if (dev == null)
            {
                Devices.Add(ad);
                dev = ad;
            }
            dev.Port = ad.Port;
            
            var serverInfo = new ServerInfo()
            {
                AllowAnnonymous = Settings.Default.AllowAnnonymousUser,
                AllowPrivateSuggestions = Settings.Default.AllowUserPrivateSuggestion,
                AllowRegistration = Settings.Default.AllowAndroidRegistration,
                CanDeleteSuggestion = Settings.Default.UserCanDeleteOwnSuggestion,
                CanEditSuggestion = Settings.Default.UserCanEditOwnSuggestion,
                FullnameRequired = Settings.Default.RequireUserFullname,
                ReplyDepth = Settings.Default.ReplyDepth,
            };

            var localEPs = Connection.AllExistingLocalListenEndPoints();
            
            var ip = new IPEndPoint(IPAddress.Parse(ad.IP), ad.Port);

            foreach (var localEP in localEPs[ConnectionType.UDP])
            {
                var lEp = localEP as IPEndPoint;

                if (lEp == null)
                    continue;
                if (!ip.Address.IsInSameSubnet(lEp.Address))
                    continue;

                serverInfo.IP = lEp.Address.ToString();
                serverInfo.Port = lEp.Port;
                await serverInfo.Send(ip);
                break;
            }
        }

        ~Server()
        {
            NetworkComms.Shutdown();
        }

        public void Start()
        {
            Connection.StartListening(ConnectionType.UDP, new IPEndPoint(IPAddress.Any, 0), true);
            OnPropertyChanged(nameof(LocalEndPoints));
        }

        public void Stop()
        {
            Connection.StopListening();
        }

        private static Server _instance;
        public static Server Instance => _instance ?? (_instance = new Server());
        
        public static readonly ObservableCollection<AndroidDevice> Devices = new ObservableCollection<AndroidDevice>();

        public List<EndPoint> LocalEndPoints => Connection.AllExistingLocalListenEndPoints()[ConnectionType.UDP];

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
