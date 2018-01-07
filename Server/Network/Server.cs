using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Devcorner.NIdenticon;
using Devcorner.NIdenticon.BrushGenerators;
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
            NetworkComms.AppendGlobalIncomingPacketHandler<string>(Requests.GET_OFFICES, GetOfficesHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<RateOffice>(RateOffice.Header, OfficeRatingHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetRatings>(GetRatings.Header, GetRatingsHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetComments>(GetComments.Header, GetCommentsHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<AddComment>(AddComment.Header, AddCommentHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetSuggestions>(GetSuggestions.Header,GetSuggestionsHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<Suggest>(Suggest.Header,SuggestionHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetPicture>(GetPicture.Header, GetPictureHandler);

            NetworkComms.AppendGlobalIncomingPacketHandler<LikeSuggestion>(LikeSuggestion.Header, LikeSuggestionHandler);
            
            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            
        }

        private async void GetPictureHandler(PacketHeader packetheader, Connection connection, GetPicture i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session))
                return;
            var usr = User.Cache.FirstOrDefault(x => x.Id == i.UserId);
            if (usr == null) return;
            await new UserPicture()
            {
                UserId = usr.Id,
                Picture = usr.Picture,
            }.Send(dev.IP,dev.Port);
        }

        private async void AddCommentHandler(PacketHeader packetheader, Connection connection, AddComment i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session))
                return;
            var student = Sessions[i.Session];

            new Models.Comment()
            {
                Message = i.Message,
                UserId = student.Id,
                SuggestionId = i.SuggestionId
            }.Save();

            await Packet.Send(Requests.ADD_COMMENT, true, dev.IP, dev.Port);
            //SendComments(i.SuggestionId, dev);
        }

        private async void LikeSuggestionHandler(PacketHeader packetheader, Connection connection, LikeSuggestion i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session))
                return;
            var student = Sessions[i.Session];

            var like = Like.Cache.FirstOrDefault(x => x.SuggestionId == i.SuggestionId && x.UserId == student.Id) ?? new Like()
            {
                SuggestionId = i.SuggestionId,
                UserId = student.Id
            };
            
            like.Dislike = i.Dislike;
            like.Save();

            await Packet.Send(Requests.LIKE_SUGGESTION, true, dev.IP, dev.Port);
        }

        private void SuggestionHandler(PacketHeader packetheader, Connection connection, Suggest i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session))
                return;
            var student = Sessions[i.Session];
            
            new Models.Suggestion()
            {
                Body = i.Body,
                IsPrivate = i.IsPrivate,
                OfficeId = i.OfficeId,
                UserId = student.Id,
                Title = i.Subject
            }.Save();

            SendSuggestions(i.OfficeId, dev, student);
        }

        private void GetSuggestionsHandler(PacketHeader packetheader, Connection connection, GetSuggestions i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session))
                return;
            var student = Sessions[i.Session];

            SendSuggestions(i.OfficeId, dev, student);
        }

        private async void SendSuggestions(long id, AndroidDevice dev, User student)
        {
            var result = new Suggestions();
            var comments = Models.Suggestion.Cache.
                Where(x => x.OfficeId == id && (!x.IsPrivate || x.UserId!=student.Id)).ToList();
            foreach (var item in comments)
            {
                result.Items.Add(new Suggestion()
                {
                    Body = item.Body,
                    StudentName = item.User.IsAnnonymous ? "Anonymous" : item.User.Fullname,
                    Title = item.Title,
                    Likes = item.Likes,
                    Dislikes = item.Dislikes,
                    Id = item.Id,
                    UserId = item.UserId,
                 //   Liked = Like.Cache.Any(x=>x.SuggestionId == id && x.UserId == student.Id && !x.Dislike),
                   // Disliked = Like.Cache.Any(x => x.SuggestionId == id && x.UserId == student.Id && x.Dislike)
                });
            }
            await result.Send(dev.IP, dev.Port);
        }

        private void GetCommentsHandler(PacketHeader packetheader, Connection connection, GetComments i)
        {
            var dev = GetDevice(connection);
            if (dev == null) return;

            if(!Sessions.ContainsKey(i.Session))
                return;
            var student = Sessions[i.Session];

            SendComments(i.SuggestionId, dev);
        }

        private async void SendComments(long id, AndroidDevice dev)
        {
            var result = new Comments();
            var comments = Models.Comment.Cache.Where(x => x.SuggestionId == id).ToList();
            foreach (var comment in comments)
            {
                result.Items.Add(new Comment()
                {
                    Id = comment.Id,
                    Message = comment.Message,
                    ParentId = comment.ParentId,
                    Sender = comment.User.IsAnnonymous ? "Anonymous" : comment.User.Fullname,
                    SuggestionId = id,
                    UserId = comment.UserId,
                });
            }
            await result.Send(dev.IP, dev.Port);
        }

        private void GetRatingsHandler(PacketHeader packetheader, Connection connection, GetRatings incomingobject)
        {
            var dev = GetDevice(connection);
            if (dev == null) return;

            if (!Sessions.ContainsKey(incomingobject.Session))
                return;
            var student = Sessions[incomingobject.Session];
            
            SendRatings(incomingobject.OfficeId, dev, student);
        }

        private void OfficeRatingHandler(PacketHeader packetheader, Connection connection, RateOffice rating)
        {
            var dev = GetDevice(connection);
            if (dev == null) return;

            if (!Sessions.ContainsKey(rating.Session)) return;
            var student = Sessions[rating.Session];

            var studentRating = Models.Rating.Cache.FirstOrDefault(x => x.OfficeId == rating.OfficeId && x.UserId == student.Id);
            if (studentRating == null)
            {
                studentRating = new Rating()
                {
                    OfficeId = rating.OfficeId,
                    UserId = student.Id,
                };
            }
            
            studentRating.Message = rating.Message;
            studentRating.Value = rating.Rating;
            studentRating.Save();

            SendRatings(rating.OfficeId, dev, student);
        }

        private async void SendRatings(long officeId, AndroidDevice dev, User user)
        {
            var result = new OfficeRatings();
            result.OfficeId = officeId;
            var ratings = Models.Rating.Cache.Where(x => x.OfficeId == officeId).ToList();

            foreach (var item in ratings)
            {
                if(item.IsPrivate && user.Id!=item.UserId) continue;
                result.Ratings.Add(
                    new OfficeRating()
                    {
                        IsPrivate = item.IsPrivate,
                        Rating = item.Value,
                        Message = item.Message,
                        OfficeId = item.OfficeId,
                        StudentName = item.User.IsAnnonymous ? "Anonymous" : item.User?.Fullname,
                        MyRating = item.UserId == user.Id
                    }
                );
            }

            await result.Send(dev.IP, dev.Port);
        }

        private async void GetOfficesHandler(PacketHeader packetheader, Connection connection, string incomingobject)
        {
            var dev = GetDevice(connection);
            if (dev == null) return;
            
            var offices = new Offices();
            foreach (var office in Models.Office.Cache)
            {
                var ratings = Models.Rating.Cache.Where(x => x.OfficeId == office.Id).ToList();
                var avgRating = ratings.Count>0 ? (float) ratings.Average(x => x.Value) : 0f;
                offices.Items.Add(new Office()
                {
                    Id = office.Id,
                    LongName = office.LongName,
                    Rating = avgRating,
                    ShortName = office.ShortName
                });
            }

            await offices.Send(dev.IP, dev.Port);
        }

        private Dictionary<int, User> Sessions { get; } = new Dictionary<int, User>();
        private Random SessionID = new Random();
        
        private async void LoginHandler(PacketHeader packetheader, Connection connection, LoginRequest request)
        {
            var dev = GetDevice(connection);
            
            if (dev == null) return;
            
            User user = null;
            if (request.Annonymous)
            {

                if (Settings.Default.AllowAnnonymousUser)
                {
                    user = new User();
                    
                    var gen = new IdenticonGenerator()
                        .WithBlocks(7, 7)
                        .WithSize(128, 128)
                        .WithBlockGenerators(IdenticonGenerator.ExtendedBlockGeneratorsConfig);
                        //.WithBackgroundColor(Color.White)
                        //.WithBrushGenerator(new StaticColorBrushGenerator(Color.DodgerBlue));

                    using (var pic = gen.Create("awooo" + DateTime.Now.Ticks))
                    {
                        using (var stream = new MemoryStream())
                        {
                            pic.Save(stream, ImageFormat.Jpeg);
                            user = new User()
                            {
                                Username = request.Username,
                                Password = request.Password,
                                Access = User.AccessLevels.Student,
                                IsAnnonymous = true,
                                Picture = stream.ToArray(),
                            };
                            user.Save();
                        }
                    }
                    
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
                var name = user.IsAnnonymous ? $"{request.Username} [Anonymous]" : user.Fullname;
                result = new LoginResult(new Student() {Name = name, IsAnonymous = user.IsAnnonymous}, sid);
            }
            await result.Send(dev.IP, dev.Port);
        }

        private AndroidDevice GetDevice(string ip)
        {
            while (true)
            {
                try
                {
                    lock (Devices)
                    {
                        return Devices.FirstOrDefault(x => x.IP == ip);

                    }
                }
                catch (Exception e)
                {
                    //success = false;
                }
            }
            
            
        }

        private AndroidDevice GetDevice(Connection connection)
        {
            lock (Devices)
            {
                return Devices.FirstOrDefault(d =>
                    d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());
            }
        }

        private async void HandShakeHandler(PacketHeader packetHeader, Connection connection, AndroidDevice ad)
        {
            var dev = GetDevice(ad.IP);
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
