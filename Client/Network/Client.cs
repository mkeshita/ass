using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;

namespace norsu.ass.Network
{
    class Client
    {
        private Client()
        {
            if (_started)
                return;
            _started = true;

            NetworkComms.EnableLogging(new LiteLogger(LiteLogger.LogMode.ConsoleOnly));

            NetworkComms.IgnoreUnknownPacketTypes = true;
            var serializer = DPSManager.GetDataSerializer<NetworkCommsDotNet.DPSBase.ProtobufSerializer>();

            NetworkComms.DefaultSendReceiveOptions = new SendReceiveOptions(serializer,
                NetworkComms.DefaultSendReceiveOptions.DataProcessors, NetworkComms.DefaultSendReceiveOptions.Options);

            NetworkComms.AppendGlobalIncomingPacketHandler<ServerInfo>(ServerInfo.Header, ServerInfoReceived);

            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);

            PeerDiscovery.OnPeerDiscovered += OnPeerDiscovered;
            Connection.StartListening(ConnectionType.UDP, new IPEndPoint(IPAddress.Any, 0));

            PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
        }

        ~Client()
        {
            Stop();
        }

        private static Client _instance;
        public static Client Instance => _instance ?? (_instance = new Client());
        
        private static bool _started = false;

        public static void Start()
        {
            Instance._Start();
        }
        
        private void _Start()
        {
           
        }

        public static void Stop()
        {
            Connection.StopListening();
            NetworkComms.Shutdown();
        }

        private static ServerInfo _server;

        public static ServerInfo Server
        {
            get
            {
                return _server;
            }
            set
            {
                _server = value;
            }
        }

        private void ServerInfoReceived(PacketHeader packetheader, Connection connection, ServerInfo incomingobject)
        {
            Server = incomingobject;
        }

        private async void OnPeerDiscovered(ShortGuid peeridentifier, Dictionary<ConnectionType, List<EndPoint>> endPoints)
        {
            var info = new AndroidDevice();

            var eps = endPoints[ConnectionType.UDP];
            var localEPs = Connection.AllExistingLocalListenEndPoints();

            foreach(var value in eps)
            {
                var ip = value as IPEndPoint;
                if(ip?.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                foreach(var localEP in localEPs[ConnectionType.UDP])
                {
                    var lEp = (IPEndPoint)localEP;
                    if(!ip.Address.IsInSameSubnet(lEp.Address))
                        continue;
                    info.IP = lEp.Address.ToString();
                    info.Port = lEp.Port;
                    await info.Send(ip);
                }
            }
        }

        public static async Task FindServer()
        {
            await Instance._FindServer();
        }
        
        private async Task _FindServer()
        {
            var start = DateTime.Now;
            PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            while ((DateTime.Now - start).TotalSeconds < 7)
            {
                if (Server != null)
                    break;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public static async Task<RegistrationResult> Register(string username, string password, string name,string course)
        {
            return await Instance._Register(username, password, name, course);
        }
        private async Task<RegistrationResult> _Register(string username, string password, string name, string course)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            RegistrationResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<RegistrationResult>(RegistrationResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(RegistrationResult.Header);
                    result = r;
                });

            await new RegistrationRequest()
            {
                Username = username,
                Password = password,
                Name=name,
                Course= course,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<LoginResult> Login(string username, string password,bool annonymous = false)
        {
            return await Instance._Login(username, password, annonymous);
        }
        
        private int Session { get; set; }
        
        private string _Username { get; set; }
        public static string Username => Instance._Username;
        
        private async Task<LoginResult> _Login(string username, string password, bool annonymous = false)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            LoginResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<LoginResult>(LoginResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(LoginResult.Header);
                    result = r;
                    Session = r.Success ? r.Session : 0;
                    _Username = r.Student.Name;
                });
            
            await new LoginRequest()
            {
                Username = username,
                Password = password, //very secure :D
                Annonymous = annonymous
            }.Send(Server.IP,Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<Offices> GetOffices()
        {
            return await Instance._GetOffices();
        }
        private async Task<Offices> _GetOffices()
        {
            if (Server == null) await _FindServer();
            if (Server == null) return null;

            Offices result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<Offices>(Offices.Header,
                (h, c, res) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(Offices.Header);
                    result = res;
                });

            await Packet.Send(Requests.GET_OFFICES,Server.IP,Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<OfficeRatings> GetRatings(long officeId)
        {
            return await Instance._GetRatings(officeId);
        }
        private async Task<OfficeRatings> _GetRatings(long officeId)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;
            
            OfficeRatings result = null;

            NetworkComms.AppendGlobalIncomingPacketHandler<OfficeRatings>(OfficeRatings.Header,
                (h, c, res) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(OfficeRatings.Header);
                    result = res;
                });

            await new GetRatings()
            {
                OfficeId = officeId,
                Session = Session
            }.Send(Server.IP, Server.Port);
            
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<OfficeRatings> RateOffice(long officeId, int rating, string message, bool isPrivate)
        {
            return await Instance._RateOffice(officeId, rating, message, isPrivate);
        }
        private async Task<OfficeRatings> _RateOffice(long officeId, int rating, string message, bool isPrivate = false)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            OfficeRatings result = null;

            NetworkComms.AppendGlobalIncomingPacketHandler<OfficeRatings>(OfficeRatings.Header,
                (h, c, res) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(OfficeRatings.Header);
                    result = res;
                });
            
            await new RateOffice()
            {
                IsPrivate = isPrivate,
                Message = message,
                OfficeId = officeId,
                Rating = rating,
                Session = Session,
            }.Send(Server.IP, Server.Port);
            
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }
    }
}