// From NetworkComms ExampleFileTransfer.WPF

// 
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
// 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NetworkCommsDotNet;
using ProtoBuf;

namespace norsu.ass.Network
{
    /// <summary>
    /// A local class which can be used to populate the WPF list box
    /// </summary>
    class ReceivedFile : INotifyPropertyChanged
    {
        /// <summary>
        /// The name of the file
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// The connectionInfo corresponding with the source
        /// </summary>
        public ConnectionInfo SourceInfo { get; private set; }

        /// <summary>
        /// The total size in bytes of the file
        /// </summary>
        public long SizeBytes { get; private set; }

        /// <summary>
        /// The total number of bytes received so far
        /// </summary>
        public long ReceivedBytes { get; private set; }

        /// <summary>
        /// Getter which returns the completion of this file, between 0 and 1
        /// </summary>
        public double CompletedPercent
        {
            get { return (double)ReceivedBytes / SizeBytes; }

            //This set is required for the application to work
            set { throw new Exception("An attempt to modify read-only value."); }
        }

        /// <summary>
        /// A formatted string of the SourceInfo
        /// </summary>
        public string SourceInfoStr
        {
            get { return "[" + SourceInfo.RemoteEndPoint.ToString() + "]"; }
        }

        /// <summary>
        /// Returns true if the completed percent equals 1
        /// </summary>
        public bool IsCompleted
        {
            get { return ReceivedBytes == SizeBytes; }
        }

        /// <summary>
        /// Private object used to ensure thread safety
        /// </summary>
        object SyncRoot = new object();

        /// <summary>
        /// A memory stream used to build the file
        /// </summary>
        Stream data;

        /// <summary>
        ///Event subscribed to by GUI for updates
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Create a new ReceivedFile
        /// </summary>
        /// <param name="filename">Filename associated with this file</param>
        /// <param name="sourceInfo">ConnectionInfo corresponding with the file source</param>
        /// <param name="sizeBytes">The total size in bytes of this file</param>
        public ReceivedFile(string filename, ConnectionInfo sourceInfo, long sizeBytes)
        {
            if (!Directory.Exists("Temp"))
                Directory.CreateDirectory("Temp");
            this.Filename = filename;//Path.Combine("Temp", Path.GetRandomFileName());
            this.SourceInfo = sourceInfo;
            this.SizeBytes = sizeBytes;

            //We create a file on disk so that we can receive large files
            data = new FileStream(Filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8 * 1024, FileOptions.DeleteOnClose);
        }

        
        /// <summary>
        /// Add data to file
        /// </summary>
        /// <param name="dataStart">Where to start writing this data to the internal memoryStream</param>
        /// <param name="bufferStart">Where to start copying data from buffer</param>
        /// <param name="bufferLength">The number of bytes to copy from buffer</param>
        /// <param name="buffer">Buffer containing data to add</param>
        public void AddData(long dataStart, int bufferStart, int bufferLength, byte[] buffer)
        {
            
            
            lock(SyncRoot)
            {
                data.Seek(dataStart, SeekOrigin.Begin);
                data.Write(buffer, (int)bufferStart, (int)bufferLength);

                ReceivedBytes += (int)(bufferLength - bufferStart);

                //Ensure the data is correctly flushed if we have received everything
                if (ReceivedBytes == SizeBytes)
                {
                    data.Flush();
                    Messenger.Default.Broadcast(Messages.DatabaseDownloaded,this);
                }
            }

            NotifyPropertyChanged("CompletedPercent");
            NotifyPropertyChanged("IsCompleted");
        }

        /// <summary>
        /// Saves the completed file to the provided saveLocation
        /// </summary>
        /// <param name="saveLocation">Location to save file</param>
        public void SaveFileToDisk(string saveLocation)
        {
            if(ReceivedBytes != SizeBytes)
                throw new Exception("Attempted to save out file before data is complete.");

            if(!File.Exists(Filename))
                throw new Exception("The transferred file should have been created within the local application directory. Where has it gone?");
            try
            {
                File.Delete(saveLocation);
            }
            catch (Exception e)
            {
                //
            }

            try
            {
                File.Copy(Filename, saveLocation, true);
            }
            catch (Exception e)
            {
                //
            }
            
        }

        /// <summary>
        /// Closes and releases any resources maintained by this file
        /// </summary>
        public void Close()
        {
            try
            {
                data.Dispose();
            } catch(Exception) { }

            try
            {
                data.Close();
            } catch(Exception) { }
        }

        /// <summary>
        /// Triggers a GUI update on a property change
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged(string propertyName = "")
        {
            if(PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    /// <summary>
    /// Information class used to associate incoming data with the correct ReceivedFile
    /// </summary>
    [ProtoContract]
    class SendInfo
    {
        /// <summary>
        /// Corresponding filename
        /// </summary>
        [ProtoMember(1)]
        public string Filename { get; private set; }

        /// <summary>
        /// The starting point for the associated data
        /// </summary>
        [ProtoMember(2)]
        public long BytesStart { get; private set; }

        /// <summary>
        /// The total number of bytes expected for the whole ReceivedFile
        /// </summary>
        [ProtoMember(3)]
        public long TotalBytes { get; private set; }

        /// <summary>
        /// The packet sequence number corresponding to the associated data
        /// </summary>
        [ProtoMember(4)]
        public long PacketSequenceNumber { get; private set; }

        /// <summary>
        /// Private constructor required for deserialisation
        /// </summary>
        private SendInfo()
        {
        }

        /// <summary>
        /// Create a new instance of SendInfo
        /// </summary>
        /// <param name="filename">Filename corresponding to data</param>
        /// <param name="totalBytes">Total bytes of the whole ReceivedFile</param>
        /// <param name="bytesStart">The starting point for the associated data</param>
        /// <param name="packetSequenceNumber">Packet sequence number corresponding to the associated data</param>
        public SendInfo(string filename, long totalBytes, long bytesStart, long packetSequenceNumber)
        {
            this.Filename = filename;
            this.TotalBytes = totalBytes;
            this.BytesStart = bytesStart;
            this.PacketSequenceNumber = packetSequenceNumber;
        }
    }
}
