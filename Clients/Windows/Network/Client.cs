using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using norsu.ass.Models;
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
           // return;

            NetworkComms.DisableLogging();
            //NetworkComms.EnableLogging();

            NetworkComms.IgnoreUnknownPacketTypes = true;
            var serializer = DPSManager.GetDataSerializer<NetworkCommsDotNet.DPSBase.ProtobufSerializer>();

            NetworkComms.DefaultSendReceiveOptions = new SendReceiveOptions(serializer,
                NetworkComms.DefaultSendReceiveOptions.DataProcessors, NetworkComms.DefaultSendReceiveOptions.Options);

            NetworkComms.AppendGlobalIncomingPacketHandler<ServerInfo>(ServerInfo.Header, ServerInfoReceived);

            NetworkComms.AppendGlobalIncomingPacketHandler<OfficePicture>(OfficePicture.Header,
                (h, c, res) =>
                {
                    if (res != null)
                        Messenger.Default.Broadcast(Messages.OfficePictureReceived, res);
                });

            NetworkComms.AppendGlobalIncomingPacketHandler<Shutdown>(Shutdown.Header, ShutdownHandler);

            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);

            PeerDiscovery.OnPeerDiscovered += OnPeerDiscovered;


            NetworkComms.AppendGlobalIncomingPacketHandler<byte[]>("PartialFileData", IncomingPartialFileData);

            NetworkComms.AppendGlobalIncomingPacketHandler<SendInfo>("PartialFileDataInfo",
                IncomingPartialFileDataInfo);

            NetworkComms.AppendGlobalConnectionCloseHandler(OnConnectionClose);

            PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);


            _Start();
        }

        private void StartEx()
        {
            
        }

        public bool DatabaseDownloaded { get; set; }

#region NetworkComms ExampleFileTransfer.WPF
        object syncRoot = new object();

        /// <summary>
        /// Data context for the GUI list box
        /// </summary>
        public ObservableCollection<ReceivedFile> receivedFiles { get; } = new ObservableCollection<ReceivedFile>();

        /// <summary>
        /// References to received files by remote ConnectionInfo
        /// </summary>
        Dictionary<ConnectionInfo, Dictionary<string, ReceivedFile>> receivedFilesDict =
            new Dictionary<ConnectionInfo, Dictionary<string, ReceivedFile>>();

        /// <summary>
        /// Incoming partial data cache. Keys are ConnectionInfo, PacketSequenceNumber. Value is partial packet data.
        /// </summary>
        Dictionary<ConnectionInfo, Dictionary<long, byte[]>> incomingDataCache =
            new Dictionary<ConnectionInfo, Dictionary<long, byte[]>>();

        /// <summary>
        /// Incoming sendInfo cache. Keys are ConnectionInfo, PacketSequenceNumber. Value is sendInfo.
        /// </summary>
        Dictionary<ConnectionInfo, Dictionary<long, SendInfo>> incomingDataInfoCache =
            new Dictionary<ConnectionInfo, Dictionary<long, SendInfo>>();


        private DateTime _lastReceived = DateTime.Now;
        private Task _timeOut;
        private bool _done = false;

        private void BytesReceived()
        {
            _lastReceived = DateTime.Now;
            if (_timeOut != null)
            {
                _timeOut = Task.Factory.StartNew(async () =>
                {
                    while ((DateTime.Now - _lastReceived).TotalMilliseconds < 2222)
                        await TaskEx.Delay(100);
                    var db = receivedFiles.FirstOrDefault();
                    // if (!(db?.IsCompleted??false))
                    // {
                    db?.SaveFileToDisk(awooo.DataSource);
                    DatabaseDownloaded = true;
                    awooo.Context.Post(d =>
                    {
                        lock (syncRoot)
                        {
                            var filesToRemove = receivedFiles.ToList();

                            foreach (ReceivedFile file in filesToRemove)
                            {
                                receivedFiles.Remove(file);
                                file.Close();
                            }

                        }
                    }, null);

                    Client.Send(new Database());
                    //}
                });
            }
        }

        /// <summary>
        /// Handles an incoming packet of type 'PartialFileData'
        /// </summary>
        /// <param name="header">Header associated with incoming packet</param>
        /// <param name="connection">The connection associated with incoming packet</param>
        /// <param name="data">The incoming data</param>
        private void IncomingPartialFileData(PacketHeader header, Connection connection, byte[] data)
        {
            try
            {
                SendInfo info = null;
                ReceivedFile file = null;

                //Perform this in a thread safe way
                lock(syncRoot)
                {
                    //Extract the packet sequence number from the header
                    //The header can also user defined parameters
                    long sequenceNumber = header.GetOption(PacketHeaderLongItems.PacketSequenceNumber);

                    if(incomingDataInfoCache.ContainsKey(connection.ConnectionInfo) && incomingDataInfoCache[connection.ConnectionInfo].ContainsKey(sequenceNumber))
                    {
                        //We have the associated SendInfo so we can add this data directly to the file
                        info = incomingDataInfoCache[connection.ConnectionInfo][sequenceNumber];
                        incomingDataInfoCache[connection.ConnectionInfo].Remove(sequenceNumber);

                        //Check to see if we have already received any files from this location
                        if(!receivedFilesDict.ContainsKey(connection.ConnectionInfo))
                            receivedFilesDict.Add(connection.ConnectionInfo, new Dictionary<string, ReceivedFile>());

                        //Check to see if we have already initialised this file
                        if(!receivedFilesDict[connection.ConnectionInfo].ContainsKey(info.Filename))
                        {
                            receivedFilesDict[connection.ConnectionInfo].Add(info.Filename, new ReceivedFile(info.Filename, connection.ConnectionInfo, info.TotalBytes));
                            awooo.Context.Post(d =>
                            {
                                receivedFiles.Add(receivedFilesDict[connection.ConnectionInfo][info.Filename]);
                            },null);
                        }

                        file = receivedFilesDict[connection.ConnectionInfo][info.Filename];
                    } else
                    {
                        //We do not yet have the associated SendInfo so we just add the data to the cache
                        if(!incomingDataCache.ContainsKey(connection.ConnectionInfo))
                            incomingDataCache.Add(connection.ConnectionInfo, new Dictionary<long, byte[]>());

                        incomingDataCache[connection.ConnectionInfo].Add(sequenceNumber, data);
                    }
                }

                //If we have everything we need we can add data to the ReceivedFile
                if(info != null && file != null && !file.IsCompleted)
                {
                    file.AddData(info.BytesStart, 0, data.Length, data);
                    BytesReceived();
                    //Perform a little clean-up
                    file = null;
                    data = null;
                    GC.Collect();
                } else if(info == null ^ file == null)
                    throw new Exception("Either both are null or both are set. Info is " + (info == null ? "null." : "set.") + " File is " + (file == null ? "null." : "set.") + " File is " + (file.IsCompleted ? "completed." : "not completed."));
            } catch(Exception ex)
            {
                //
            }
        }

        /// <summary>
        /// Handles an incoming packet of type 'PartialFileDataInfo'
        /// </summary>
        /// <param name="header">Header associated with incoming packet</param>
        /// <param name="connection">The connection associated with incoming packet</param>
        /// <param name="data">The incoming data automatically converted to a SendInfo object</param>
        private void IncomingPartialFileDataInfo(PacketHeader header, Connection connection, SendInfo info)
        {
            try
            {
                byte[] data = null;
                ReceivedFile file = null;

                //Perform this in a thread safe way
                lock(syncRoot)
                {
                    //Extract the packet sequence number from the header
                    //The header can also user defined parameters
                    long sequenceNumber = info.PacketSequenceNumber;

                    if(incomingDataCache.ContainsKey(connection.ConnectionInfo) && incomingDataCache[connection.ConnectionInfo].ContainsKey(sequenceNumber))
                    {
                        //We already have the associated data in the cache
                        data = incomingDataCache[connection.ConnectionInfo][sequenceNumber];
                        incomingDataCache[connection.ConnectionInfo].Remove(sequenceNumber);

                        //Check to see if we have already received any files from this location
                        if(!receivedFilesDict.ContainsKey(connection.ConnectionInfo))
                            receivedFilesDict.Add(connection.ConnectionInfo, new Dictionary<string, ReceivedFile>());

                        //Check to see if we have already initialised this file
                        if(!receivedFilesDict[connection.ConnectionInfo].ContainsKey(info.Filename))
                        {
                            receivedFilesDict[connection.ConnectionInfo].Add(info.Filename, new ReceivedFile(info.Filename, connection.ConnectionInfo, info.TotalBytes));
                            awooo.Context.Post(d =>
                            {
                                receivedFiles.Add(receivedFilesDict[connection.ConnectionInfo][info.Filename]);
                            }, null);
                        }

                        file = receivedFilesDict[connection.ConnectionInfo][info.Filename];
                    } else
                    {
                        //We do not yet have the necessary data corresponding with this SendInfo so we add the
                        //info to the cache
                        if(!incomingDataInfoCache.ContainsKey(connection.ConnectionInfo))
                            incomingDataInfoCache.Add(connection.ConnectionInfo, new Dictionary<long, SendInfo>());

                        incomingDataInfoCache[connection.ConnectionInfo].Add(sequenceNumber, info);
                    }
                }

                //If we have everything we need we can add data to the ReceivedFile
                if(data != null && file != null && !file.IsCompleted)
                {
                    file.AddData(info.BytesStart, 0, data.Length, data);
                    BytesReceived();
                    //Perform a little clean-up
                    file = null;
                    data = null;
                    GC.Collect();
                } else if(data == null ^ file == null)
                    throw new Exception("Either both are null or both are set. Data is " + (data == null ? "null." : "set.") + " File is " + (file == null ? "null." : "set.") + " File is " + (file.IsCompleted ? "completed." : "not completed."));
            } catch(Exception ex)
            {
                //If an exception occurs we write to the log window and also create an error file
                Debug.Print(ex.Message);
            }
        }

        /// <summary>
        /// If a connection is closed we clean-up any incomplete ReceivedFiles
        /// </summary>
        /// <param name="conn">The closed connection</param>
        private void OnConnectionClose(Connection conn)
        {
            ReceivedFile[] filesToRemove = null;

            lock(syncRoot)
            {
                //Remove any associated data from the caches
                incomingDataCache.Remove(conn.ConnectionInfo);
                incomingDataInfoCache.Remove(conn.ConnectionInfo);

                //Remove any non completed files
                if(receivedFilesDict.ContainsKey(conn.ConnectionInfo))
                {
                    filesToRemove = (from current in receivedFilesDict[conn.ConnectionInfo] where !current.Value.IsCompleted select current.Value).ToArray();
                    receivedFilesDict[conn.ConnectionInfo] = (from current in receivedFilesDict[conn.ConnectionInfo] where current.Value.IsCompleted select current).ToDictionary(entry => entry.Key, entry => entry.Value);
                }
            }
            
                awooo.Context.Post(d =>
                {
                lock(syncRoot)
                {
                    if(filesToRemove != null)
                    {
                        foreach(ReceivedFile file in filesToRemove)
                        {
                            receivedFiles.Remove(file);
                            file.Close();
                        }
                    }
                }
                },null);
            
        }

#endregion
        
        private void ShutdownHandler(PacketHeader packetHeader, Connection connection, Shutdown incomingObject)
        {
            Server = null;
            Messenger.Default.Broadcast(Messages.ServerShutdown);
        }

        public static async void Send(string header, object message)
        {
            if (Server == null)
                await FindServer();
            if (Server == null) return;

            await Packet.Send(header, message, Server.IP, Server.Port);
        }

        public static async void Send<T>(Packet<T> packet) where T : Packet<T>
        {
            if (Server == null)
                await FindServer();
            if (Server == null)
                return;
            await packet.Send(Server.IP, Server.Port);
        }

        public static async Task<bool> SendAsync<T>(Packet<T> packet) where T : Packet<T>
        {
            if (Server == null)
                await FindServer();
            if (Server == null)
                return false;
            await packet.Send(Server.IP, Server.Port);
            return true;
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
        
        private async void _Start()
        {
            while (true)
                try
                {
                    Connection.StartListening(ConnectionType.UDP, new IPEndPoint(IPAddress.Any, 0));
                    break;
                }
                catch (Exception e)
                {
                    await TaskEx.Delay(111);
                }

            while (true)
                try
                {
                    Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, 0));
                    break;
                }
                catch (Exception e)
                {
                    await TaskEx.Delay(111);
                }

            _started = true;
        }

        public static void Stop()
        {
            lock (Instance.syncRoot)
            {
                foreach (ReceivedFile file in Instance.receivedFiles)
                    file.Close();
            }
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

        public static Office SelectedOffice { get; set; }

        private void ServerInfoReceived(PacketHeader packetheader, Connection connection, ServerInfo incomingobject)
        {
            Server = incomingobject;
            Messenger.Default.Broadcast(Messages.ServerFound);
        }

        private async void OnPeerDiscovered(ShortGuid peeridentifier, Dictionary<ConnectionType, List<EndPoint>> endPoints)
        {
            var info = new Desktop();

            var eps = endPoints[ConnectionType.UDP];
            var localEPs = Connection.AllExistingLocalListenEndPoints();
            
            foreach(var value in eps)
            {
                var ip = value as IPEndPoint;
                if(ip?.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                
                //if(ip.Address.ToString()!= "100.172.168.77") continue;
                
                foreach(var localEP in localEPs[ConnectionType.UDP])
                {
                    var lEp = (IPEndPoint)localEP;
                    
                    if(lEp.AddressFamily!= AddressFamily.InterNetwork) continue;
                    
                    if(!ip.Address.IsInSameSubnet(lEp.Address))
                        continue;
                    info.IP = lEp.Address.ToString();
                    info.Port = lEp.Port;
                    if(localEPs.ContainsKey(ConnectionType.TCP))
                        
                    { var tcp = localEPs[ConnectionType.TCP]
                        .FirstOrDefault(x =>ip.Address.AddressFamily==AddressFamily.InterNetwork && ip.Address.IsInSameSubnet(((IPEndPoint) x).Address)) as IPEndPoint;
                        info.DataPort = tcp?.Port ?? 0;
                    }

                    
                    await info.Send(ip);
                    break;
                }
                
            }
        }

        private static bool _findStarted;
        public static async Task FindServer()
        {
            if (_findStarted) return;
            _findStarted = true;
            await Instance._FindServer();
            _findStarted = false;
        }
        
        private async Task _FindServer()
        {
            while(true)
                try
                {
                    var start = DateTime.Now;
                    PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
                    while ((DateTime.Now - start).TotalSeconds < 7)
                    {
                        if (Server != null)
                            break;
                        await TaskEx.Delay(TimeSpan.FromSeconds(1));
                    }
                    return;
                }
                catch (Exception e)
                {
                    await TaskEx.Delay(100);
                }
            
            
        }

        public int Session { get; set; }

        public static async Task<DesktopLoginResult> Login(string username, string password)
        {
            return await Instance._Login(username, password);
        }
        
        private async Task<DesktopLoginResult> _Login(string username, string password, bool annonymous = false)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            DesktopLoginResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<DesktopLoginResult>(DesktopLoginResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(DesktopLoginResult.Header);
                    result = r;
                });
            
            await new DesktopLoginRequest()
            {
                Username = username,
                Password = password, //very secure :D
            }.Send(Server.IP,Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<DeleteOfficeResult> DeleteOffice(long officeId)
        {
            return await Instance._DeleteOffice(officeId);
        }

        private async Task<DeleteOfficeResult> _DeleteOffice(long officeId)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            DeleteOfficeResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<DeleteOfficeResult>(DeleteOfficeResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(DeleteOfficeResult.Header);
                    result = r;
                });

            await new DeleteOffice()
            {
                Id = officeId,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<ChangePasswordResult> ChangePassword(string password, string newPassword, long userId)
        {
            return await Instance._ChangePassword(password,newPassword,userId);
        }

        private async Task<ChangePasswordResult> _ChangePassword(string password, string newPassword, long userId)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            ChangePasswordResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<ChangePasswordResult>(ChangePasswordResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(ChangePasswordResult.Header);
                    result = r;
                });

            await new ChangePassword()
            {
                Id = userId,
                Current = password,
                NewPassword = newPassword
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<ResetPasswordResult> ResetPassword(long officeId)
        {
            return await Instance._ResetPassword(officeId);
        }

        private async Task<ResetPasswordResult> _ResetPassword(long officeId)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            ResetPasswordResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<ResetPasswordResult>(ResetPasswordResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(ResetPasswordResult.Header);
                    result = r;
                });

            await new ResetPassword()
            {
                Id = officeId,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<AddOfficeAdminResult> AddOfficeAdmin(long officeId, long userId)
        {
            return await Instance._AddOfficeAdmin(officeId,userId);
        }

        private async Task<AddOfficeAdminResult> _AddOfficeAdmin(long officeId,long userId)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            AddOfficeAdminResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<AddOfficeAdminResult>(AddOfficeAdminResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(AddOfficeAdminResult.Header);
                    result = r;
                });

            await new AddOfficeAdmin()
            {
                UserId = userId,
                OfficeId = officeId
                
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<SaveUserResult> SaveUser(UserInfo user)
        {
            return await Instance._SaveUser(user);
        }

        private async Task<SaveUserResult> _SaveUser(UserInfo user)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            SaveUserResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<SaveUserResult>(SaveUserResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(SaveUserResult.Header);
                    result = r;
                });

            await new SaveUser()
            {
                User = user,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<DeleteUserResult> DeleteUser(long officeId)
        {
            return await Instance._DeleteUser(officeId);
        }

        private async Task<DeleteUserResult> _DeleteUser(long officeId)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            DeleteUserResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<DeleteUserResult>(DeleteUserResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(DeleteUserResult.Header);
                    result = r;
                });

            await new DeleteUser()
            {
                Id = officeId,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<SetPictureResult> SetPicture(long officeId, byte[] picture)
        {
            if (picture?.Length > 0)
                return await Instance._SetPicture(officeId, picture);
            return null;
        }

        private async Task<SetPictureResult> _SetPicture(long officeId, byte[] picture)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            SetPictureResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<SetPictureResult>(SetPictureResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(SetPictureResult.Header);
                    result = r;
                });

            await new SetPicture()
            {
                Id = officeId,
                Picture = picture,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<SetOfficePictureResult> SetOfficePicture(long officeId, byte[] picture)
        {
            if(picture?.Length>0)
                return await Instance._SetOfficePicture(officeId, picture);
            return null;
        }

        private async Task<SetOfficePictureResult> _SetOfficePicture(long officeId, byte[] picture)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            SetOfficePictureResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<SetOfficePictureResult>(SetOfficePictureResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(SetOfficePictureResult.Header);
                    result = r;
                });

            await new SetOfficePicture()
            {
                Id = officeId,
                Picture = picture,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<SaveOfficeResult> SaveOffice(long officeId, string shortName, string longName)
        {
            return await Instance._SaveOffice(officeId, shortName,longName);
        }
        
        private async Task<SaveOfficeResult> _SaveOffice(long officeId, string shortName, string longName)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;
            
            SaveOfficeResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<SaveOfficeResult>(SaveOfficeResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(SaveOfficeResult.Header);
                    result = r;
                });

            await new SaveOffice()
            {
                Id = officeId,
                ShortName = shortName,
                LongName = longName
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<ToggleCommentsResult> ToggleComments(long suggestionId,long userId)
        {
            return await Instance._ToggleComments(suggestionId,userId);
        }
        private async Task<ToggleCommentsResult> _ToggleComments(long suggestionId, long userId)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            ToggleCommentsResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<ToggleCommentsResult>(ToggleCommentsResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(ToggleCommentsResult.Header);
                    result = r;
                });

            await new ToggleComments()
            {
                SuggestionId = suggestionId,
                UserId = userId,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<ReplyCommentResult> SendComment(long suggestionId, long userId, string message)
        {
            return await Instance._SendComment(suggestionId, userId,message);
        }

        private async Task<ReplyCommentResult> _SendComment(long suggestionId, long userId, string message)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            ReplyCommentResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<ReplyCommentResult>(ReplyCommentResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(ReplyCommentResult.Header);
                    result = r;
                });

            await new ReplyComment()
            {
                SuggestionId = suggestionId,
                UserId = userId,
                Message = message,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<DeleteSuggestionsResult> DeleteSuggestions(long userId, List<long> ids)
        {
            return await Instance._DeleteSuggestions(userId, ids);
        }

        private async Task<DeleteSuggestionsResult> _DeleteSuggestions(long userId, List<long> ids)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            DeleteSuggestionsResult result = null;
            NetworkComms.AppendGlobalIncomingPacketHandler<DeleteSuggestionsResult>(DeleteSuggestionsResult.Header,
                (h, c, r) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(DeleteSuggestionsResult.Header);
                    result = r;
                });

            await new DeleteSuggestions()
            {
                UserId = userId,
                Ids = ids
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        //private async void FetchOfficePicture(long id)
        //{
        //    if (Server == null)
        //        await _FindServer();
        //    if (Server == null)
        //        return;
            
        //    await new GetOfficePicture() {Session = Session, OfficeId = id}.Send(Server.IP, Server.Port);
        //}

        //private async void FetchOfficePictures(IEnumerable<long> officeIds)
        //{
        //    if (Server == null)
        //        await _FindServer();
        //    if (Server == null)
        //        return;

        //    try
        //    {
        //        foreach (var id in officeIds)
        //        {
        //            await new GetOfficePicture() {Session = Session, OfficeId = id}
        //                .Send(Server.IP, Server.Port);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        //
        //    }

        //}

        //private static async Task<Offices> GetOffices()
        //{
        //    return await Instance._GetOffices();
        //}
        //private async Task<Offices> _GetOffices()
        //{
        //    if (Server == null) await _FindServer();
        //    if (Server == null) return null;

        //    Offices result = null;
        //    NetworkComms.AppendGlobalIncomingPacketHandler<Offices>(Offices.Header,
        //        (h, c, res) =>
        //        {
        //            NetworkComms.RemoveGlobalIncomingPacketHandler(Offices.Header);
        //            result = res;
        //        });

        //    await Packet.Send(Requests.GET_OFFICES,Server.IP,Server.Port);

        //    var start = DateTime.Now;
        //    while ((DateTime.Now - start).TotalSeconds < 17)
        //    {
        //        if (result != null)
        //            return result;
        //        await TaskEx.Delay(TimeSpan.FromSeconds(1));
        //    }
        //    Server = null;
        //    return null;
        //}
        
        public static async Task<RateOfficeResult> RateOffice(long officeId, int rating, string message, bool isPrivate, long returnCount = -1)
        {
            return await Instance._RateOffice(officeId, rating, message, isPrivate, returnCount);
        }
        private async Task<RateOfficeResult> _RateOffice(long officeId, int rating, string message, bool isPrivate = false, long returnCount = -1)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            RateOfficeResult result = null;

            NetworkComms.AppendGlobalIncomingPacketHandler<RateOfficeResult>(RateOfficeResult.Header,
                (h, c, res) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(RateOfficeResult.Header);
                    result = res;
                });
            
            await new RateOffice()
            {
                IsPrivate = isPrivate,
                Message = message,
                OfficeId = officeId,
                Rating = rating,
                Session = Session,
                ReturnCount = returnCount,
            }.Send(Server.IP, Server.Port);
            
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }
        
        //private async Task<UserPicture> FetchUserPicture(long id,long revision)
        //{
        //    if (Server == null) await _FindServer();
        //    if (Server == null) return null;

        //    UserPicture result = null;
        //    NetworkComms.AppendGlobalIncomingPacketHandler<UserPicture>(UserPicture.Header,
        //        (h, c, res) =>
        //        {
        //            NetworkComms.RemoveGlobalIncomingPacketHandler(UserPicture.Header);
        //            result = res;
        //        });

        //    await new GetPicture()
        //    {
        //        Session = Session,
        //        UserId = id,
        //        Revision = revision,
        //    }.Send(Server.IP, Server.Port);

        //    var start = DateTime.Now;
        //    while ((DateTime.Now - start).TotalSeconds < 17)
        //    {
        //        if (result != null)
        //            return result;
        //        await TaskEx.Delay(TimeSpan.FromSeconds(1));
        //    }
        //    Server = null;
        //    return null;
        //}
        
        //private async void FetchUserPictures(IEnumerable<Models.User> users)
        //{
        //    if (!users.Any()) return;
        //    if (Server == null)
        //        await _FindServer();
        //    if (Server == null) return;
           
        //    try
        //    {
        //        foreach (var user in users)
        //        {
        //            //if(Pictures.Any(x=>x.UserId==id)) continue;
        //            var pic = await FetchUserPicture(user.Id,user.PictureRevision);
        //            if (pic != null)
        //            {
        //                var u = Models.User.Cache.FirstOrDefault(x => x.Id == pic.UserId);
        //                if(u==null) continue;
        //                if (pic.Revision != u.PictureRevision)
        //                {
        //                    u.Update(nameof(User.Picture),pic.Picture);
        //                    u.Update(nameof(User.PictureRevision),pic.Revision);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //       //
        //    }
            
        //}
        
        public static Suggestion SelectedSuggestion { get; set; }

        public static async Task<SuggestResult> Suggest(long officeId, string subject, string body,bool isPrivate)
        {
            return await Instance._Suggest(officeId, subject, body, isPrivate);
        }
        
        private async Task<SuggestResult> _Suggest(long officeId, string subject, string body,bool isPrivate)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return null;

            SuggestResult result = null;

            NetworkComms.AppendGlobalIncomingPacketHandler<SuggestResult>(SuggestResult.Header,
                (h, c, res) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(SuggestResult.Header);
                    result = res;
                });

            await new Suggest()
            {
                OfficeId = officeId,
                Session = Session,
                Subject = subject,
                Body = body,
                IsPrivate = isPrivate
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }

        public static async Task<long?> LikeSuggestion(long suggestionId, bool dislike)
        {
            return await Instance._LikeSuggestion(suggestionId, dislike);
        }
        private async Task<long?> _LikeSuggestion(long suggestionId, bool dislike)
        {
            if (Server == null) await _FindServer();
            if (Server == null) return null;

            long? result = null;

            NetworkComms.AppendGlobalIncomingPacketHandler<long>(Requests.LIKE_SUGGESTION,
                (h, c, res) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(Requests.LIKE_SUGGESTION);
                    result = res;
                });

            await new LikeSuggestion()
            {
                SuggestionId = suggestionId,
                Dislike = dislike,
                Session = Session,
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return null;
        }
        
        public static async Task<bool> AddComment(long suggestionId, string comment)
        {
            return await Instance._AddComment(suggestionId, comment);
        }
        private async Task<bool> _AddComment(long suggestionId, string comment)
        {
            if (Server == null)
                await _FindServer();
            if (Server == null)
                return false;

            bool? result = null;

            NetworkComms.AppendGlobalIncomingPacketHandler<bool>(Requests.ADD_COMMENT,
                (h, c, res) =>
                {
                    NetworkComms.RemoveGlobalIncomingPacketHandler(Requests.ADD_COMMENT);
                    result = res;
                });

            await new AddComment()
            {
                SuggestionId = suggestionId,
                Session = Session,
                Message = comment
            }.Send(Server.IP, Server.Port);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 17)
            {
                if (result != null)
                    return result??false;
                await TaskEx.Delay(TimeSpan.FromSeconds(1));
            }
            Server = null;
            return false;

        }

      
        private Dictionary<long,OfficeRating> MyRatings = new Dictionary<long, OfficeRating>();

        private void AddRating(long id, OfficeRating rating)
        {
            if (MyRatings.ContainsKey(id))
                MyRatings[id] = rating;
            else
                MyRatings.Add(id,rating);
        }
        
        public static OfficeRating GetMyRating(long id)
        {
            return Instance._GetMyRating(id);
        }

        private OfficeRating _GetMyRating(long id)
        {
            while (true)
            {
                try
                {
                    return MyRatings[id];
                }
                catch (Exception e)
                {
                    //
                }
            }
        }
    }
}