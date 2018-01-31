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
using System.Threading.Tasks;
using Devcorner.NIdenticon;
using Devcorner.NIdenticon.BrushGenerators;
using norsu.ass.Models;
using norsu.ass.Server;
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
            NetworkComms.AppendGlobalIncomingPacketHandler<GetOfficePicture>(GetOfficePicture.Header, GetOfficePictureHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<RegistrationRequest>(RegistrationRequest.Header, RegistrationHandler);

            NetworkComms.AppendGlobalIncomingPacketHandler<LikeSuggestion>(LikeSuggestion.Header, LikeSuggestionHandler);
            
            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            
        }

        private async void RegistrationHandler(PacketHeader packetHeader, Connection connection, RegistrationRequest i)
        {
            var dev = GetDevice(connection);
            if (dev == null) return;

            //Check if registration is enabled
            if (!Settings.Default.AllowAndroidRegistration)
            {
                await new RegistrationResult()
                {
                    Success = false,
                    Message = "Registration is disabled"
                }.Send(dev.IP, dev.Port);
                return;
            }
            
            //Check if username is taken.
            var user = User.Cache.FirstOrDefault(x => x.Username.ToLower() == i.Username.ToLower() && !x.IsAnnonymous);
            if (user != null)
            {
                //return failed registration
                await new RegistrationResult()
                {
                    Message = "Student ID already registered!",
                    Success = false,
                }.Send(dev.IP, dev.Port);
                return;
            }
            
            //Add new user
            user = new User()
            {
                Username = i.Username,
                Access = AccessLevels.Student,
                Course = i.Course,
                Firstname = i.Firstname,
                Lastname = i.Lastname,
                IsAnnonymous = false,
                Password = i.Password,
                Picture = ImageProcessor.Generate(),
            };
            user.Save();

            var sid = GenerateSession(user);
            
            //return successful registration
            await new RegistrationResult()
            {
                Success = true,
                UserId = user.Id,
                Session = sid,
            }.Send(dev.IP, dev.Port);
        }

        private int GenerateSession(User user)
        {
            var sid = SessionID.Next(7, int.MaxValue);
            while (Sessions.ContainsKey(sid))
                sid = SessionID.Next(7, int.MaxValue);
            Sessions.Add(sid, user);
            return sid;
        }

        private async void GetOfficePictureHandler(PacketHeader packetheader, Connection connection, GetOfficePicture i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session)) return;
            var office = Models.Office.GetById(i.OfficeId);
            if (office == null) return;

            if (!office.HasPicture) return;
            
            await new OfficePicture()
            {
                OfficeId = office.Id,
                Picture = office.Picture,
            }.Send(dev.IP, dev.Port);
        }

        private async void GetPictureHandler(PacketHeader packetheader, Connection connection, GetPicture i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session)) return;

            if (i.UserId > 0)
            {
                var usr = User.GetById(i.UserId);
                if (usr == null) return;

                if (!usr.HasPicture) usr.Update(nameof(User.Picture), ImageProcessor.Generate());

                await new UserPicture()
                {
                    UserId = usr.Id,
                    Picture = usr.Picture,
                }.Send(dev.IP, dev.Port);
            } else if (i.UserId < 0)
            {
                var office = Models.Office.GetById(Math.Abs(i.UserId));
                if (office == null) return;

                if (!office.HasPicture) return;

                await new UserPicture()
                {
                    UserId = i.UserId,
                    Picture = office.Picture,
                }.Send(dev.IP, dev.Port);
            }
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

            var likes = GetLikes(i.SuggestionId);
            like.Dislike = i.Dislike;
            like.Save();

            while (true)
            {
                try
                {
                    if(Like.Cache.Any(x=>x.Id==like.Id)) break;
                    await TaskEx.Delay(10);
                }
                catch (Exception e)
                {
                    //
                }
            }
            
            likes = GetLikes(i.SuggestionId);

            await Packet.Send(Requests.LIKE_SUGGESTION, likes, dev.IP, dev.Port);
        }

        private async void SuggestionHandler(PacketHeader packetheader, Connection connection, Suggest i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session))
                return;
            var student = Sessions[i.Session];
            
            var s = new Models.Suggestion()
                {
                    Body = i.Body,
                    IsPrivate = i.IsPrivate,
                    OfficeId = i.OfficeId,
                    UserId = student.Id,
                    Title = i.Subject
                };
            s.Save();

            while (true)
            {
                try
                {
                    if(Models.Suggestion.Cache.Any(x=>x.Id==s.Id)) break;
                    await TaskEx.Delay(10);
                }
                catch (Exception e)
                {
                    //
                }
            }
            
            SendSuggestions(i.OfficeId, dev, student, 7);
        }

        private void GetSuggestionsHandler(PacketHeader packetheader, Connection connection, GetSuggestions i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session))
                return;
            var student = Sessions[i.Session];

            SendSuggestions(i.OfficeId, dev, student,i.Count);
        }
        
        private long GetLikes(long id)
        {
            while (true)
            {
                try
                {
                    return Like.Cache.Count(x => !x.Dislike && x.SuggestionId == id) -
                            Like.Cache.Count(x => x.Dislike && x.SuggestionId == id);
                }
                catch (Exception e)
                {
                    //
                }
            }
        }

        private async void SendSuggestions(long id, AndroidDevice dev, User student, long count = -1)
        {
            var result = new Suggestions();
            var comments = Models.Suggestion.Cache.
                Where(x => x.OfficeId == id && (!x.IsPrivate || x.UserId==student.Id))
                .OrderByDescending(x=> GetLikes(x.Id)).ToList();
            foreach (var item in comments)
            {
                result.Items.Add(new Suggestion()
                {
                    Body = item.Body,
                    StudentName = item.User.IsAnnonymous ? "Anonymous" : item.User.Fullname,
                    Title = item.Title,
                    Likes = GetLikes(item.Id),
                    Id = item.Id,
                    UserId = item.UserId,
                    AllowComment = item.AllowComments,
                    CommentsDisabledBy = item.CommentsDisabledBy
                });
                if(count>0 && result.Items.Count>=count) break;
            }
            result.TotalCount = Models.Suggestion.Cache.Count(x => x.OfficeId == id);
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
            var suggestion = Models.Suggestion.Cache.FirstOrDefault(x => x.Id == id);
            if (suggestion == null) return;
            
            var result = new Comments();
            var comments = Models.Comment.Cache.Where(x => x.SuggestionId == id).ToList();
            foreach (var comment in comments)
            {
                var com = new Comment()
                {
                    Id = comment.Id,
                    Message = comment.Message,
                    ParentId = comment.ParentId,
                    Sender = comment.User.IsAnnonymous ? "Anonymous" : comment.User.Fullname,
                    SuggestionId = id,
                    Time = comment.Time,
                    UserId = comment.UserId,
                };
                
                if (Settings.Default.OfficeAdminCommentAsOffice)
                {
                    var usr = User.GetById(comment.UserId);
                    
                    var admin = (usr.Access == AccessLevels.SuperAdmin);
                    if(!admin) admin = OfficeAdmin.Cache.Any(x =>x.OfficeId == suggestion.OfficeId && x.UserId == comment.UserId);

                    if (admin)
                    {
                        com.Sender = Models.Office.GetById(suggestion.OfficeId).ShortName;
                        com.UserId = -suggestion.OfficeId;
                    }
                }
                
                result.Items.Add(com);
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
            
            SendRatings(incomingobject.OfficeId, dev, student,incomingobject.Page);
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

            //SendRatings(rating.OfficeId, dev, student, rating.ReturnCount);
        }

        private async void SendRatings(long officeId, AndroidDevice dev, User user, int page = 0)
        {
            var result = new OfficeRatings();
            result.OfficeId = officeId;

            if (page == 0)
            {
                var myRating = Models.Rating.Cache.FirstOrDefault(x => x.OfficeId == officeId && x.UserId == user.Id);
                if (myRating != null)
                    result.Ratings.Add(
                        new OfficeRating()
                        {
                            IsPrivate = myRating.IsPrivate,
                            Rating = myRating.Value,
                            Message = myRating.Message,
                            OfficeId = myRating.OfficeId,
                            StudentName = user.IsAnnonymous ? "Anonymous" : user?.Fullname,
                            MyRating = true,
                            UserId = user.Id,
                        }
                    );
            }

            //Get all public reviews excluding the user's
            var ratings = Models.Rating.Cache
                .Where(x => x.OfficeId == officeId && !x.IsPrivate && x.UserId!=user.Id)
                .OrderByDescending(x=>x.Id).ToList();

            var pages = (int) Math.Floor(ratings.Count/(Settings.Default.PageSize*1.0));
            if (ratings.Count % Settings.Default.PageSize > 0) pages++;
            
            for (var i = page*Settings.Default.PageSize; i < ratings.Count; i++)
            {
                var item = ratings[i];
                if(item.UserId==user.Id) continue;
                result.Ratings.Add(
                    new OfficeRating()
                    {
                        IsPrivate = item.IsPrivate,
                        Rating = item.Value,
                        Message = item.Message,
                        OfficeId = item.OfficeId,
                        StudentName = item.User.IsAnnonymous ? "Anonymous" : item.User?.Fullname,
                        MyRating = item.UserId == user.Id,
                        UserId = item.UserId,
                    }
                );
                if(result.Ratings.Count==Settings.Default.PageSize) break;
            }

            result.Pages = pages;
            result.TotalCount = Models.Rating.Cache.Count(x => x.OfficeId == officeId);
            if(Models.Rating.Cache.Any(x=>x.OfficeId==officeId))
                result.Rating = Models.Rating.Cache.Where(x => x.OfficeId == officeId).Average(x => x.Value * 1f);
            
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
                var avgRating = 0f;
                
                while (true)
                {
                    try
                    {
                        avgRating = ratings.Count > 0 ? (float) ratings.Average(x => x.Value) : 0f;
                        break;
                    }
                    catch (Exception e)
                    {
                        //
                    }
                }
                
                offices.Items.Add(new Office()
                {
                    Id = office.Id,
                    LongName = office.LongName,
                    Rating = avgRating,
                    ShortName = office.ShortName,
                    RatingCount = Rating.Cache.Count(x=>x.OfficeId==office.Id),
                    SuggestionsCount = Models.Suggestion.Cache.Count(x=>x.OfficeId==office.Id),
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
                //Create new anonymous user if allowed.
                if (Settings.Default.AllowAnnonymousUser)
                {
                    user = new User()
                    {
                        Username = request.Username,
                        Password = request.Password,
                        Access = AccessLevels.Student,
                        IsAnnonymous = true,
                        Picture = ImageProcessor.Generate(),
                    };
                    user.Save();
                }
                else
                {
                    await new LoginResult {Success = false}.Send(dev.IP, dev.Port);
                    return;
                }

            }
            else
            {
                user = User.Cache.FirstOrDefault(x => x.Username.ToLower() == request.Username.ToLower());
            }

            var result = new LoginResult();
            
            if(user != null)
            {
                if (user.Password == request.Password)
                {
                    var sid = GenerateSession(user);
                    var name = user.IsAnnonymous ? $"Anonymous" : user.Fullname;
                    result = new LoginResult(new Student()
                    {
                        Name = name,
                        UserName = user.Username,
                        IsAnonymous = user.IsAnnonymous,
                        Id = user.Id,
                    }, sid);
                }
                else
                {
                    result.Success = false;
                }
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

        public async void Stop()
        {
            var msg = new Shutdown();
            List<AndroidDevice> devs;
            lock (Devices) devs = Devices.ToList();
            foreach (var dev in devs) if(dev!=null) await msg.Send(dev.IP, dev.Port);
            
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
