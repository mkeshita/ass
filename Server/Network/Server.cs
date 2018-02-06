using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using norsu.ass.Models;
using norsu.ass.Server;
using norsu.ass.Server.Properties;
using norsu.ass.Server.ViewModels;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.DPSBase.SevenZipLZMACompressor;
using NetworkCommsDotNet.Tools;

namespace norsu.ass.Network
{
    class Server : INotifyPropertyChanged
    {
        private Server()
        {
            var serializer = DPSManager.GetDataSerializer<ProtobufSerializer>();
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

            NetworkComms.AppendGlobalIncomingPacketHandler<Desktop>(Desktop.Header, DesktopHandshakeHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<DesktopLoginRequest>(DesktopLoginRequest.Header, DesktopLoginHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetUsers>(GetUsers.Header,GetUsersHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<long>(Packet.GET_SUGGESTIONS, HandleGetSuggestionsDesktop);
            NetworkComms.AppendGlobalIncomingPacketHandler<long>(Packet.GET_REVIEWS, HandlerGetReviewsDesktop);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetCommentsDesktop>(GetCommentsDesktop.Header, HandleGetCommentsDesktop);
            NetworkComms.AppendGlobalIncomingPacketHandler<GetVotes>(GetVotes.Header,HandleGetVotes);
            NetworkComms.AppendGlobalIncomingPacketHandler<ToggleComments>(ToggleComments.Header,HandleToggleCOmments);
            NetworkComms.AppendGlobalIncomingPacketHandler<ReplyComment>(ReplyComment.Header,ReplyCommentHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<DeleteSuggestions>(DeleteSuggestions.Header,DeleteSuggestionsHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<Database>(Database.Header,DatabaseHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<SaveOffice>(SaveOffice.Header,SaveOfficeHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<DeleteOffice>(DeleteOffice.Header,DeleteOfficeHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<SetOfficePicture>(SetOfficePicture.Header,SetOfficePictureHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<SettingsViewModel>("settings",SettingsHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<SetPicture>(SetPicture.Header,SetPictureHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<ResetPassword>(ResetPassword.Header,ResetPasswordHandler);
            NetworkComms.AppendGlobalIncomingPacketHandler<DeleteUser>(DeleteUser.Header,DeleteUserHandler);
            
            PeerDiscovery.EnableDiscoverable(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
            
        }

        private async void DeleteUserHandler(PacketHeader packetheader, Connection connection, DeleteUser req)
        {
            var dev = GetDesktop(connection);
            if (dev == null)
                return;

            var office = Models.User.Cache.FirstOrDefault(x => x.Id == req.Id);
            office?.Delete(false);

            await new DeleteUserResult()
            {
                Success = true,
            }.Send(dev.IP, dev.Port);
        }

        private async void ResetPasswordHandler(PacketHeader packetheader, Connection connection, ResetPassword req)
        {
            var dev = GetDesktop(connection);
            if (dev == null)
                return;

            var office = Models.User.Cache.FirstOrDefault(x => x.Id == req.Id);
            office?.Update(nameof(User.Password), "");

            await new ResetPasswordResult()
            {
                Success = true,
            }.Send(dev.IP, dev.Port);
        }

        private async void SetPictureHandler(PacketHeader packetheader, Connection connection, SetPicture req)
        {
            var dev = GetDesktop(connection);
            if (dev == null)
                return;

            var office = Models.User.Cache.FirstOrDefault(x => x.Id == req.Id);
            if (office != null)
            {
                office.Update(nameof(User.Picture), req.Picture);
                Console.WriteLine($"User picture updated. Source: {dev.IP} User: {office.Username}");
            }

            await new SetPictureResult()
            {
                Success = true,
            }.Send(dev.IP, dev.Port);
        }

        private async void SettingsHandler(PacketHeader packetheader, Connection connection, SettingsViewModel req)
        {
            var dev = GetDesktop(connection);
            if (dev == null)
                return;

            Settings.Default.AllowAnnonymousUser = req.AllowAnonymous;
            Settings.Default.AllowUserPrivateSuggestion = req.AllowPrivate;
            Settings.Default.AllowAndroidRegistration = req.AndroidRegistration;
            Settings.Default.OfficeAdminCanDeleteSuggestions = req.OfficeAdminCanDeleteSuggestions;
            Settings.Default.OfficeAdminCanSeeUserFullname = req.OfficeAdminCanSeePrivate;
            Settings.Default.OfficeAdminCommentAsOffice = req.OfficeAdminReplyAs;
            Settings.Default.SuggestionBodyMax = req.SuggestionBodyMaximum;
            Settings.Default.SuggestionBodyMin = req.SuggestionBodyMinimum;
            Settings.Default.SuggestionTitleMax = req.SuggestionTitleMaximum;
            Settings.Default.SuggestionTitleMin = req.SuggestionTitleMinimum;
            Settings.Default.Save();
            
            await Packet.Send("settings", req, dev.IP, dev.Port);
        }

        private async void SetOfficePictureHandler(PacketHeader packetheader, Connection connection, SetOfficePicture req)
        {
            var dev = GetDesktop(connection);
            if (dev == null)
                return;

            var office = Models.Office.Cache.FirstOrDefault(x => x.Id == req.Id);
            if (office != null)
            {
                office.Update(nameof(Office.Picture),req.Picture);
                Console.WriteLine($"Office picture updated. Source: {dev.IP} Office: {office.ShortName}");
            }

            await new SetOfficePictureResult()
            {
                Success = true,
            }.Send(dev.IP, dev.Port);
        }

        private async void DeleteOfficeHandler(PacketHeader packetheader, Connection connection, DeleteOffice req)
        {
            var dev = GetDesktop(connection);
            if (dev == null)
                return;

            var office = Models.Office.Cache.FirstOrDefault(x => x.Id == req.Id);
            if (office != null)
            {
                office.Delete(false);
                Console.WriteLine($"Office deleted. Source: {dev.IP} Office: {office.ShortName}");
            }
            
            await new DeleteOfficeResult()
            {
                Success = true,
            }.Send(dev.IP, dev.Port);
        }

        private async void SaveOfficeHandler(PacketHeader packetheader, Connection connection, SaveOffice req)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;

            var office = Models.Office.Cache.FirstOrDefault(x => x.Id == req.Id);
            if (office == null)
                office = new Models.Office();
            office.LongName = req.LongName;
            office.ShortName = req.ShortName;
            
            if(office.Id==0)
                Console.WriteLine($"New office added. Source: {dev.IP} Office: #{office.Id} {office.ShortName} | {office.LongName}");
            else
                Console.WriteLine(
                    $"Office details changed. Source: {dev.IP} Office: #{office.Id}");

            office.Save();

            await new SaveOfficeResult()
            {
                Success = true,
                Id = office.Id,
            }.Send(dev.IP, dev.Port);
        }

        private void DatabaseHandler(PacketHeader packetheader, Connection connection, Database incomingobject)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;
            SendDatabase(dev);
        }

        private async void DeleteSuggestionsHandler(PacketHeader packetheader, Connection connection, DeleteSuggestions req)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;
            
            Models.Suggestion.Delete(req.Ids);

            await new DeleteSuggestionsResult() {Success = true}.Send(dev.IP, dev.Port);

        }

        private async void ReplyCommentHandler(PacketHeader packetheader, Connection connection, ReplyComment req)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;

            var comment = new Models.Comment()
            {
                Message = req.Message,
                UserId = req.UserId,
                SuggestionId = req.SuggestionId
            };
            comment.Save();

            Console.WriteLine($"New comment added for suggestion #{req.SuggestionId}. IP: {dev.IP}");
            
            await new ReplyCommentResult()
            {
                Success = true,
                CommentId = comment.Id,
            }.Send(dev.IP, dev.Port);
        }

        private async void HandleToggleCOmments(PacketHeader packetheader, Connection connection, ToggleComments req)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;

            var suggestion = Models.Suggestion.Cache.FirstOrDefault(x => x.Id == req.SuggestionId);
            if (suggestion == null)
            {
                await new ToggleCommentsResult() {Success = false}.Send(dev.IP, dev.Port);
                return;
            }
            if(suggestion.AllowComments)
               suggestion.CommentsDisabledBy = req.UserId;
            suggestion.AllowComments = !suggestion.AllowComments;
            suggestion.Save();
            
            if(suggestion.AllowComments)
                Console.WriteLine($"Commenting for suggestion #{req.SuggestionId} is enabled. IP: {dev.IP}");
            else
                Console.WriteLine($"Commenting for suggestion #{req.SuggestionId} is disabled. IP: {dev.IP}");

            await new ToggleCommentsResult()
            {
                Success = true,
                AllowComments = suggestion.AllowComments,
            }.Send(dev.IP, dev.Port);
        }

        private async void HandleGetVotes(PacketHeader packetheader, Connection connection, GetVotes req)
        {
            var dev = GetDesktop(connection);
            if (dev == null)
                return;

            var votes = Models.Like.Cache.Where(x => x.SuggestionId == req.SuggestionId && x.Id > req.HighestId)
                .OrderBy(x => x.Id).ToList();

            var result = new Votes();
            foreach (var vote in votes)
            {
                result.List.Add(new Vote()
                {
                    SuggestionId = vote.SuggestionId,
                    DownVote = vote.Dislike,
                    UserId = vote.UserId,
                    Id = vote.Id,
                });
            }

            await result.Send(dev.IP, dev.Port);
        }

        private async void HandleGetCommentsDesktop(PacketHeader packetheader, Connection connection,
            GetCommentsDesktop req)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;
            
            var result = new Comments();
            var comments = Models.Comment.Cache.Where(x => x.SuggestionId == req.SuggestionId && x.Id>req.HighestId)
                .OrderBy(x=>x.Id).ToList();
            foreach (var comment in comments)
            {
                var com = new Comment()
                {
                    Id = comment.Id,
                    Message = comment.Message,
                    ParentId = comment.ParentId,
                    SuggestionId = req.SuggestionId,
                    Time = comment.Time,
                    UserId = comment.UserId,
                };
                
                result.Items.Add(com);
            }
            await result.Send(dev.IP, dev.Port);
        }

        private async void HandlerGetReviewsDesktop(PacketHeader packetHeader, Connection connection, long id)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;
            
            var result = new OfficeRatings();
            result.OfficeId = id;
            
            //Get all public reviews excluding the user's
            var ratings = Models.Rating.Cache.Where(x => x.OfficeId == id).ToList();
            
            foreach (var item in ratings)
            {
                result.Ratings.Add(
                    new OfficeRating()
                    {
                        Id = item.Id,
                        IsPrivate = item.IsPrivate,
                        Rating = item.Value,
                        Message = item.Message,
                        OfficeId = item.OfficeId,
                        UserId = item.UserId,
                    }
                );
                if (result.Ratings.Count == Settings.Default.PageSize)
                {
                    await result.Send(dev.IP, dev.Port);
                    result.Ratings.Clear();
                }
            }
            
        }

        private async void HandleGetSuggestionsDesktop(PacketHeader packetHeader, Connection connection, long id)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;
            
            var result = new Suggestions();
            var suggestions = Models.Suggestion.Cache.Where(x => x.OfficeId == id).ToList();
            
            foreach (var item in suggestions)
            {
                result.Items.Add(new Suggestion()
                {
                    Body = item.Body,
                    OfficeId = item.OfficeId,
                    Title = item.Title,
                    Id = item.Id,
                    UserId = item.UserId,
                    AllowComment = item.AllowComments,
                    CommentsDisabledBy = item.CommentsDisabledBy,
                    IsPrivate = item.IsPrivate,
                    Time = item.Time,
                });
            }

            await result.Send(dev.IP, dev.Port);
          
        }

        private async void GetUsersHandler(PacketHeader packetheader, Connection connection, GetUsers req)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;

            var list = Models.User.Cache
                .Where(x => x.Id > req.HighestId)
                .Select(user => new UserInfo()
                {
                    Access = (int) (user.Access ?? 0),
                    Id = user.Id,
                    Username = user.Username,
                    Firstname = user.Firstname,
                    Lastname = user.Lastname,
                    Description = user.Course,
                    StudentId = user.StudentId,
                    IsAnonymous = user.IsAnnonymous,
                    PictureRevision = user.PictureRevision
                }).ToList();
            
            await new GetUsersResult()
            {
                Users = list
            }.Send(dev.IP,dev.Port);
            
        }

        private void SendDatabase(Desktop dev)
        {
            
            
            //Perform the send in a task so that we don't lock the GUI
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var db = Path.GetRandomFileName();
                    try
                    {
                        if (File.Exists(db))
                            File.Delete(db);
                    }
                    catch (Exception e)
                    {
                        //
                    }

                    try
                    {
                        File.Copy(awooo.DataSource, db,true);
                    }
                    catch (Exception e)
                    {
                        //
                    }
                    

                    var filename = Path.GetFullPath(db);
                    var remoteIP = dev.IP;
                    var remotePort = dev.DataPort;
                    
                    
                    //Create a fileStream from the selected file
                    FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);

                    //Wrap the fileStream in a threadSafeStream so that future operations are thread safe
                    StreamTools.ThreadSafeStream safeStream = new StreamTools.ThreadSafeStream(stream);

                    //Get the filename without the associated path information
                    string shortFileName = "Database.db3";

                    //Parse the remote connectionInfo
                    //We have this in a separate try catch so that we can write a clear message to the log window
                    //if there are problems
                    ConnectionInfo remoteInfo;
                    try
                    {
                        remoteInfo = new ConnectionInfo(remoteIP, remotePort);
                    } catch(Exception)
                    {
                        throw new InvalidDataException("Failed to parse remote IP and port. Check and try again.");
                    }

                    //Get a connection to the remote side
                    Connection connection = TCPConnection.GetConnection(remoteInfo);

                    //Break the send into 20 segments. The less segments the less overhead 
                    //but we still want the progress bar to update in sensible steps
                    long sendChunkSizeBytes = (long)(stream.Length / 20.0) + 1;

                    //Limit send chunk size to 500MB
                    long maxChunkSizeBytes = 500L * 1024L * 1024L;
                    if(sendChunkSizeBytes > maxChunkSizeBytes)
                        sendChunkSizeBytes = maxChunkSizeBytes;

                    long totalBytesSent = 0;
                    do
                    {
                        //Check the number of bytes to send as the last one may be smaller
                        long bytesToSend = (totalBytesSent + sendChunkSizeBytes < stream.Length ? sendChunkSizeBytes : stream.Length - totalBytesSent);

                        //Wrap the threadSafeStream in a StreamSendWrapper so that we can get NetworkComms.Net
                        //to only send part of the stream.
                        StreamTools.StreamSendWrapper streamWrapper = new StreamTools.StreamSendWrapper(safeStream, totalBytesSent, bytesToSend);

                        var customOptions = NetworkComms.DefaultSendReceiveOptions;

                        //We want to record the packetSequenceNumber
                        long packetSequenceNumber;
                        //Send the select data
                        connection.SendObject("PartialFileData", streamWrapper,customOptions, out packetSequenceNumber);
                        //Send the associated SendInfo for this send so that the remote can correctly rebuild the data
                        connection.SendObject("PartialFileDataInfo", new SendInfo(shortFileName, stream.Length, totalBytesSent, packetSequenceNumber), customOptions);

                        totalBytesSent += bytesToSend;

                        //Update the GUI with our send progress
                        //Console.WriteLine($"Sending database to {dev.IP}: {((double) totalBytesSent / stream.Length)*100}%");
                        
                    } while(totalBytesSent < stream.Length);

                    //Clean up any unused memory
                    GC.Collect();

                   // Console.WriteLine("Completed file send to '" + connection.ConnectionInfo.ToString() + "'.");
                } catch(CommunicationException)
                {
                    //If there is a communication exception then we just write a connection
                    //closed message to the log window
                    Console.WriteLine("Failed to complete send as connection was closed.");
                } catch(Exception ex)
                {
                    //If we get any other exception which is not an InvalidDataException
                    //we log the error
                    if(ex.GetType() != typeof(InvalidDataException))
                    {
                        Console.WriteLine(ex.Message.ToString());
                        //LogTools.LogException(ex, "SendFileError");
                    }
                }
                
            });
        }

        private async void DesktopLoginHandler(PacketHeader packetheader, Connection connection, DesktopLoginRequest login)
        {
            var dev = GetDesktop(connection);
            if (dev == null) return;
#if CLI
            Program.Log($"Login request from desktop client {dev.IP}");
#endif

            var user = User.Cache.FirstOrDefault(x => x.Username.ToLower() == login.Username);
            if (user == null)
            {
                await new DesktopLoginResult()
                {
                    ErrorMessage = "Invalid username/password!",
                    Success = false,
                }.Send(dev.IP, dev.Port);
                return;
            }
            if(string.IsNullOrEmpty(user.Password))
                user.Update(nameof(User.Password),login.Password);

            if (user.Password != login.Password)
            {
                await new DesktopLoginResult()
                {
                    ErrorMessage = "Invalid username/password!",
                    Success = false,
                }.Send(dev.IP, dev.Port);
                return;
            }
            
            var usr = new UserInfo()
            {
                Access =(int) (user.Access??0),
                Id = user.Id,
                Username = user.Username,
                Password = user.Password,
                Picture = user.Picture,
                Firstname = user.Firstname,
                Lastname = user.Lastname,
                Description = user.Course
            };

            await new DesktopLoginResult()
            {
                Success = true,
                User = usr,
            }.Send(dev.IP, dev.Port);

            await Packet.Send("settings", new SettingsViewModel()
            {
                AllowAnonymous = Settings.Default.AllowAnnonymousUser,
                AllowPrivate = Settings.Default.AllowUserPrivateSuggestion,
                AndroidRegistration = Settings.Default.AllowAndroidRegistration,
                OfficeAdminCanDeleteSuggestions = Settings.Default.OfficeAdminCanDeleteSuggestions,
                OfficeAdminCanSeePrivate = Settings.Default.OfficeAdminCanSeeUserFullname,
                OfficeAdminReplyAs = Settings.Default.OfficeAdminCommentAsOffice,
                SuggestionBodyMaximum = Settings.Default.SuggestionBodyMax,
                SuggestionBodyMinimum = Settings.Default.SuggestionBodyMin,
                SuggestionTitleMaximum = Settings.Default.SuggestionTitleMax,
                SuggestionTitleMinimum = Settings.Default.SuggestionTitleMin,

            }, dev.IP, dev.Port);
        }

        private Desktop GetDesktop(Connection con)
        {
            var ip = ((IPEndPoint) con.ConnectionInfo.RemoteEndPoint).Address.ToString();
            return GetDesktop(ip);
        }

        private Desktop GetDesktop(string ip)
        {
            while(true)
            try
            {
                lock(_DesktopClients)
                return _DesktopClients.FirstOrDefault(x => x.IP == ip);
            }
            catch (Exception e)
            {
                //
            }
        }
        private static List<Desktop> _DesktopClients = new List<Desktop>();
        
        private async void DesktopHandshakeHandler(PacketHeader packetheader, Connection connection, Desktop d)
        {
            var dev = GetDesktop(d.IP);
            
            if (dev == null)
            {
#if CLI
                Program.Log($"Handshake received from desktop client at {d.IP}");
#endif
                _DesktopClients.Add(d);
                dev = d;
            }
            
            var serverInfo = new ServerInfo()
            {
                AllowAnnonymous = Settings.Default.AllowAnnonymousUser,
                AllowPrivateSuggestions = Settings.Default.AllowUserPrivateSuggestion,
                AllowRegistration = Settings.Default.AllowAndroidRegistration,
                CanDeleteSuggestion = Settings.Default.UserCanDeleteOwnSuggestion,
                CanEditSuggestion = Settings.Default.UserCanEditOwnSuggestion,
                FullnameRequired = Settings.Default.RequireUserFullname,
                ReplyDepth = Settings.Default.ReplyDepth,
                SuggestionTitleMin = Settings.Default.SuggestionTitleMin,
                SuggestionTitleMax = Settings.Default.SuggestionTitleMax,
                SuggestionBodyMin = Settings.Default.SuggestionBodyMin,
                SuggestionBodyMax = Settings.Default.SuggestionBodyMax,
            };

            var localEPs = Connection.AllExistingLocalListenEndPoints();

            var ip = new IPEndPoint(IPAddress.Parse(d.IP), d.Port);

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
            var user = Models.User.Cache.FirstOrDefault(x => x.Username.ToLower() == i.Username.ToLower() && !x.IsAnnonymous);
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
            user = new Models.User()
            {
                Username = i.Username,
                Access = Models.AccessLevels.Student,
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

        private int GenerateSession(Models.User user)
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
            var desktop = GetDesktop(connection);
            if (dev == null && desktop == null)
                return;

            var dIP = dev?.IP ?? desktop.IP;
            var dPort = dev?.Port ?? desktop.Port;

            //if (!Sessions.ContainsKey(i.Session)) return;
            var office = Models.Office.GetById(i.OfficeId);
            if (office == null) return;

            if (!office.HasPicture) return;
            
            await new OfficePicture()
            {
                OfficeId = office.Id,
                Picture = office.Picture,
            }.Send(dIP, dPort);
        }

        private async void GetPictureHandler(PacketHeader packetheader, Connection connection, GetPicture i)
        {
            var dev = GetDevice(connection);
            var desktop = GetDesktop(connection);
            if (dev == null && desktop == null)
                return;

            var dIP = dev?.IP ?? desktop.IP;
            var dPort = dev?.Port ?? desktop.Port;

           // if (!Sessions.ContainsKey(i.Session)) return;

            if (i.UserId > 0)
            {
                var usr = Models.User.GetById(i.UserId);
                if (usr == null) return;

                if (!usr.HasPicture) usr.Update(nameof(Models.User.Picture), ImageProcessor.Generate());

                await new UserPicture()
                {
                    UserId = usr.Id,
                    Picture = usr.PictureRevision!=i.Revision ? usr.Picture : null,
                    Revision = usr.PictureRevision,
                }.Send(dIP, dPort);
            } else if (i.UserId < 0)
            {
                var office = Models.Office.GetById(Math.Abs(i.UserId));
                if (office == null) return;

                if (!office.HasPicture) return;

                await new UserPicture()
                {
                    UserId = i.UserId,
                    Picture = office.Picture,
                }.Send(dIP, dPort);
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

            var like = Models.Like.Cache.FirstOrDefault(x => x.SuggestionId == i.SuggestionId && x.UserId == student.Id) ?? new
                           Models.Like()
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
                    if(Models.Like.Cache.Any(x=>x.Id==like.Id)) break;
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
            if (dev == null) return;

            if (!Sessions.ContainsKey(i.Session)) return;
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

            await new SuggestResult()
            {
                Success = true,
                TotalCount = Models.Suggestion.Cache.Count(x=>x.OfficeId==i.OfficeId),
                Result = new Suggestion()
                {
                    Id = s.Id,
                    AllowComment = s.AllowComments,
                    Body = s.Body,
                    Comments = 0,
                    OfficeId = s.OfficeId,
                    Title = s.Title,
                    UserId = s.UserId,
                    StudentName = student.IsAnnonymous ? "Anonymous" : student.Fullname,
                },
            }.Send(dev);
        }

        private void GetSuggestionsHandler(PacketHeader packetheader, Connection connection, GetSuggestions i)
        {
            var dev = GetDevice(connection);
            if (dev == null)
                return;

            if (!Sessions.ContainsKey(i.Session))
                return;
            var student = Sessions[i.Session];

            SendSuggestions(i.OfficeId, dev, student,i.Page);
        }
        
        private long GetLikes(long id)
        {
            while (true)
            {
                try
                {
                    return Models.Like.Cache.Count(x => !x.Dislike && x.SuggestionId == id) -
                           Models.Like.Cache.Count(x => x.Dislike && x.SuggestionId == id);
                }
                catch (Exception e)
                {
                    //
                }
            }
        }

        private async void SendSuggestions(long id, AndroidDevice dev, Models.User student, int page)
        {
            var result = new Suggestions();
            var suggestions = Models.Suggestion.Cache
                            .Where(x => x.OfficeId == id && (!x.IsPrivate || x.UserId == student.Id))
                            .OrderByDescending(x=>x.Votes).ToList();

            
            for (var i = page*Settings.Default.PageSize; i < suggestions.Count; i++)
            {
                var item = suggestions[i];

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

                if (result.Items.Count >= Settings.Default.PageSize)
                {
                    result.Full = true;
                    break;
                }
            }
            
            result.Pages = (int) Math.Floor(suggestions.Count / (Settings.Default.PageSize * 1.0));
            result.Page = page;
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
                    var usr = Models.User.GetById(comment.UserId);
                    
                    var admin = (usr.Access == Models.AccessLevels.SuperAdmin);
                    if(!admin) admin = Models.OfficeAdmin.Cache.Any(x =>x.OfficeId == suggestion.OfficeId && x.UserId == comment.UserId);

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

        private async void OfficeRatingHandler(PacketHeader packetheader, Connection connection, RateOffice rating)
        {
            var dev = GetDevice(connection);
            if (dev == null) return;

            if (!Sessions.ContainsKey(rating.Session)) return;
            var student = Sessions[rating.Session];

            var studentRating = Models.Rating.Cache.FirstOrDefault(x => x.OfficeId == rating.OfficeId && x.UserId == student.Id);
            if (studentRating == null)
            {
                studentRating = new Models.Rating()
                {
                    OfficeId = rating.OfficeId,
                    UserId = student.Id,
                };
            }
            
            studentRating.Message = rating.Message;
            studentRating.Value = rating.Rating;
            studentRating.Save();

            //SendRatings(rating.OfficeId, dev, student, rating.ReturnCount);
            await new RateOfficeResult()
            {
                Success = true,
            }.Send(dev);
        }

        private async void SendRatings(long officeId, AndroidDevice dev, Models.User user, int page = 0)
        {
            var result = new OfficeRatings();
            result.OfficeId = officeId;

            if (page == 0)
            {
                var myRating = Models.Rating.Cache.FirstOrDefault(x => x.OfficeId == officeId && x.UserId == user.Id);
                if (myRating == null)
                {
                    myRating = new Models.Rating()
                    {
                        OfficeId = officeId,
                        UserId = user.Id,
                    };
                }

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
                    });

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
            var desktop = GetDesktop(connection);
            if (dev == null && desktop==null) return;

            var dIp = dev?.IP ?? desktop.IP;
            var dPort = dev?.Port ?? desktop.Port;
            
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
                    RatingCount = Models.Rating.Cache.Count(x=>x.OfficeId==office.Id),
                    SuggestionsCount = Models.Suggestion.Cache.Count(x=>x.OfficeId==office.Id),
                });
            }

            await offices.Send(dIp, dPort);
        }

        private Dictionary<int, Models.User> Sessions { get; } = new Dictionary<int, Models.User>();
        private Random SessionID = new Random();
        
        private async void LoginHandler(PacketHeader packetheader, Connection connection, LoginRequest request)
        {
            var dev = GetDevice(connection);
            if (dev == null) return;

            Models.User user = null;
            if (request.Annonymous)
            {
                //Create new anonymous user if allowed.
                if (Settings.Default.AllowAnnonymousUser)
                {
                    user = new Models.User()
                    {
                        Username = request.Username,
                        Password = request.Password,
                        Access = Models.AccessLevels.Student,
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
                user = Models.User.Cache.FirstOrDefault(x => x.Username.ToLower() == request.Username.ToLower());
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
                SuggestionTitleMin = Settings.Default.SuggestionTitleMin,
                SuggestionTitleMax = Settings.Default.SuggestionTitleMax,
                SuggestionBodyMin = Settings.Default.SuggestionBodyMin,
                SuggestionBodyMax = Settings.Default.SuggestionBodyMax,
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
            Connection.StartListening(ConnectionType.UDP, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0), true);
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
