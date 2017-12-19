//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.PacketHandlers {
    class SmbCommandPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler {

        //private System.Collections.Generic.SortedList<string, FileTransfer.FileStreamAssembler> smbAssemblers;
        private PopularityList<string, SmbSession> smbSessionPopularityList;

        public ApplicationLayerProtocol HandledProtocol {
            get { return ApplicationLayerProtocol.NetBiosSessionService; }
        }

        public SmbCommandPacketHandler(PacketHandler mainPacketHandler)
            : base(mainPacketHandler) {

            //this.smbAssemblers=new SortedList<string, NetworkMiner.FileTransfer.FileStreamAssembler>();
            this.smbSessionPopularityList=new PopularityList<string, SmbSession>(100);
        }

        #region ITcpSessionPacketHandler Members

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<Packets.AbstractPacket> packetList) {
            bool successfulExtraction=false;

            Packets.CifsPacket.AbstractSmbCommand smbCommandPacket=null;
            Packets.TcpPacket tcpPacket=null;
            foreach(Packets.AbstractPacket p in packetList) {
                if(p.GetType().IsSubclassOf(typeof(Packets.CifsPacket.AbstractSmbCommand)))
                    smbCommandPacket=(Packets.CifsPacket.AbstractSmbCommand)p;
                else if(p.GetType()==typeof(Packets.TcpPacket))
                    tcpPacket=(Packets.TcpPacket)p;
            }

            if(tcpPacket!=null && smbCommandPacket!=null) {
                successfulExtraction=true;
                ExtractSmbData(sourceHost, destinationHost, tcpPacket, smbCommandPacket, base.MainPacketHandler);
            }
            if (successfulExtraction) {
                //return tcpPacket.PayloadDataLength;
                return 0;//changed 2011-04-25 since the NetBiosSessionServicePacketHandler will return the # parsed bytes anyway.
            }
            else
                return 0;
        }

        public void Reset() {
            this.smbSessionPopularityList.Clear();
        }

        #endregion

        private void ExtractSmbData(NetworkHost sourceHost, NetworkHost destinationHost, Packets.TcpPacket tcpPacket, Packets.CifsPacket.AbstractSmbCommand smbCommandPacket, PacketHandler mainPacketHandler) {

            string smbSessionId;
            if(smbCommandPacket.ParentCifsPacket.FlagsResponse)
                smbSessionId=SmbSession.GetSmbSessionId(sourceHost.IPAddress, tcpPacket.SourcePort, destinationHost.IPAddress, tcpPacket.DestinationPort);
            else
                smbSessionId=SmbSession.GetSmbSessionId(destinationHost.IPAddress, tcpPacket.DestinationPort, sourceHost.IPAddress, tcpPacket.SourcePort);


            if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.NegotiateProtocolRequest)) {
                Packets.CifsPacket.NegotiateProtocolRequest request=(Packets.CifsPacket.NegotiateProtocolRequest)smbCommandPacket;
                sourceHost.AcceptedSmbDialectsList=request.DialectList;
            }
            else if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.NegotiateProtocolResponse)) {
                Packets.CifsPacket.NegotiateProtocolResponse reply=(Packets.CifsPacket.NegotiateProtocolResponse)smbCommandPacket;
                if(destinationHost.AcceptedSmbDialectsList!=null && destinationHost.AcceptedSmbDialectsList.Count>reply.DialectIndex)
                    sourceHost.PreferredSmbDialect=destinationHost.AcceptedSmbDialectsList[reply.DialectIndex];
                //sourceHost.ExtraDetailsList.Add("Preferred SMB dialect", destinationHost.AcceptedSmbDialectsList[reply.DialectIndex]);
            }
            else if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.SetupAndXRequest)) {
                Packets.CifsPacket.SetupAndXRequest request=(Packets.CifsPacket.SetupAndXRequest)smbCommandPacket;
                if(request.NativeLanManager!=null && request.NativeLanManager.Length>0) {
                    if(sourceHost.ExtraDetailsList.ContainsKey("SMB Native LAN Manager"))
                        sourceHost.ExtraDetailsList["SMB Native LAN Manager"]=request.NativeLanManager;
                    else
                        sourceHost.ExtraDetailsList.Add("SMB Native LAN Manager", request.NativeLanManager);
                }

                if(request.NativeOs!=null && request.NativeOs.Length>0) {
                    if(sourceHost.ExtraDetailsList.ContainsKey("SMB Native OS"))
                        sourceHost.ExtraDetailsList["SMB Native OS"]=request.NativeOs;
                    else
                        sourceHost.ExtraDetailsList.Add("SMB Native OS", request.NativeOs);
                }

                if(request.PrimaryDomain!=null && request.PrimaryDomain.Length>0) {
                    sourceHost.AddDomainName(request.PrimaryDomain);
                }
                if(request.AccountName!=null && request.AccountName.Length>0) {
                    NetworkCredential nCredential=new NetworkCredential(sourceHost, destinationHost, smbCommandPacket.PacketTypeDescription, request.AccountName, request.ParentFrame.Timestamp);
                    if(request.AccountPassword!=null && request.AccountPassword.Length>0)
                        nCredential.Password=request.AccountPassword;
                    mainPacketHandler.AddCredential(nCredential);
                }
            }
            else if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.SetupAndXResponse)) {
                Packets.CifsPacket.SetupAndXResponse response=(Packets.CifsPacket.SetupAndXResponse)smbCommandPacket;
                if(response.NativeLanManager!=null && response.NativeLanManager.Length>0) {
                    if(sourceHost.ExtraDetailsList.ContainsKey("SMB Native LAN Manager"))
                        sourceHost.ExtraDetailsList["SMB Native LAN Manager"]=response.NativeLanManager;
                    else
                        sourceHost.ExtraDetailsList.Add("SMB Native LAN Manager", response.NativeLanManager);
                }

                if(response.NativeOs!=null && response.NativeOs.Length>0) {
                    if(sourceHost.ExtraDetailsList.ContainsKey("SMB Native OS"))
                        sourceHost.ExtraDetailsList["SMB Native OS"]=response.NativeOs;
                    else
                        sourceHost.ExtraDetailsList.Add("SMB Native OS", response.NativeOs);
                }
            }
            else if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.NTCreateAndXRequest)) {
                Packets.CifsPacket.NTCreateAndXRequest request=(Packets.CifsPacket.NTCreateAndXRequest)smbCommandPacket;
                string filename, filePath;

                if(request.Filename.EndsWith("\0"))
                    filename=request.Filename.Remove(request.Filename.Length-1);
                else
                    filename=request.Filename;
                if (filename.Contains(System.IO.Path.DirectorySeparatorChar.ToString())) {
                    filePath = filename.Substring(0, filename.LastIndexOf(System.IO.Path.DirectorySeparatorChar.ToString()));
                    filename = filename.Substring(filename.LastIndexOf(System.IO.Path.DirectorySeparatorChar.ToString()) + 1);
                }
                else
                    filePath = System.IO.Path.DirectorySeparatorChar.ToString();

                try {
                    //string smbSessionId=SmbSession.GetSmbSessionId(destinationHost.IPAddress, tcpPacket.DestinationPort, sourceHost.IPAddress, tcpPacket.SourcePort);
                    SmbSession smbSession;
                    if(this.smbSessionPopularityList.ContainsKey(smbSessionId)){
                        smbSession=this.smbSessionPopularityList[smbSessionId];
                    }
                    else{
                        smbSession=new SmbSession(destinationHost.IPAddress, tcpPacket.DestinationPort, sourceHost.IPAddress, tcpPacket.SourcePort);
                        this.smbSessionPopularityList.Add(smbSessionId, smbSession);
                    }
                    
                    FileTransfer.FileStreamAssembler assembler=new FileTransfer.FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, tcpPacket!=null, FileTransfer.FileStreamTypes.SMB, filename, filePath, request.Filename, smbCommandPacket.ParentFrame.FrameNumber, smbCommandPacket.ParentFrame.Timestamp);
                    smbSession.AddFileStreamAssembler(assembler, request.ParentCifsPacket.TreeId, request.ParentCifsPacket.MultiplexId, request.ParentCifsPacket.ProcessId);

                }
                catch(Exception e) {
                    MainPacketHandler.OnAnomalyDetected("Error creating assembler for SMB file transfer: "+e.Message);

                }
            }
            //else if(!smbCommandPacket.ParentCifsPacket.FlagsResponse && mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, true)) {
            else if(!smbCommandPacket.ParentCifsPacket.FlagsResponse && this.smbSessionPopularityList.ContainsKey(smbSessionId)) {
                //Request
                if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.CloseRequest) && smbSessionPopularityList.ContainsKey(smbSessionId)){

                    SmbSession smbSession=this.smbSessionPopularityList[smbSessionId];
                    Packets.CifsPacket.CloseRequest closeRequest=(Packets.CifsPacket.CloseRequest)smbCommandPacket;
                    ushort fileId=closeRequest.FileId;
                    FileTransfer.FileStreamAssembler assemblerToClose;
                    if(smbSession.ContainsFileId(closeRequest.ParentCifsPacket.TreeId, closeRequest.ParentCifsPacket.MultiplexId, closeRequest.ParentCifsPacket.ProcessId, fileId)){
                        assemblerToClose=smbSession.GetFileStreamAssembler(closeRequest.ParentCifsPacket.TreeId, closeRequest.ParentCifsPacket.MultiplexId, closeRequest.ParentCifsPacket.ProcessId, fileId);
                        smbSession.RemoveFileStreamAssembler(closeRequest.ParentCifsPacket.TreeId, closeRequest.ParentCifsPacket.MultiplexId, closeRequest.ParentCifsPacket.ProcessId, fileId, false);
                        
                        //TODO: remove the following line (added for debugging purpose 2011-04-25)
                        //assemblerToClose.FinishAssembling();

                        if(mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(assemblerToClose))
                            mainPacketHandler.FileStreamAssemblerList.Remove(assemblerToClose, true);
                        else
                            assemblerToClose.Clear();
                    }
                    
                    
                        

                }
                else if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.ReadAndXRequest) && smbSessionPopularityList.ContainsKey(smbSessionId)) {
                    SmbSession smbSession=this.smbSessionPopularityList[smbSessionId];
                    //Packets.CifsPacket.ReadAndXRequest request=this.smbSessionPopularityList[smbSessionId];
                    Packets.CifsPacket.ReadAndXRequest readRequest=(Packets.CifsPacket.ReadAndXRequest)smbCommandPacket;
                    ushort fileId=readRequest.FileId;
                    smbSession.Touch(readRequest.ParentCifsPacket.TreeId, readRequest.ParentCifsPacket.MultiplexId, readRequest.ParentCifsPacket.ProcessId, fileId);
                }

                
            }
            else if(smbCommandPacket.ParentCifsPacket.FlagsResponse && this.smbSessionPopularityList.ContainsKey(smbSessionId)) {
                //Response
                SmbSession smbSession=this.smbSessionPopularityList[smbSessionId];

                //FileTransfer.FileStreamAssembler assembler=mainPacketHandler.FileStreamAssemblerList.GetAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true);

                if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.NTCreateAndXResponse)) {
                    Packets.CifsPacket.NTCreateAndXResponse response=(Packets.CifsPacket.NTCreateAndXResponse)smbCommandPacket;
                    ushort fileId=response.FileId;
                    int fileLength=(int)response.EndOfFile;//yes, I know I will not be able to store big files now... but an int as length is really enough!

                    //tag the requested file with the fileId
                    FileTransfer.FileStreamAssembler assembler=smbSession.GetLastReferencedFileStreamAssembler(response.ParentCifsPacket.TreeId, response.ParentCifsPacket.MultiplexId, response.ParentCifsPacket.ProcessId);
                    smbSession.RemoveLastReferencedAssembler(response.ParentCifsPacket.TreeId, response.ParentCifsPacket.MultiplexId, response.ParentCifsPacket.ProcessId);

                    
                    
                    if(assembler!=null) {
                        //Add file ID as extended ID in order to differentiate between parallell file transfers on disk cache
                        assembler.ExtendedFileId = "Id" + fileId.ToString("X4"); //2011-04-18

                        smbSession.AddFileStreamAssembler(assembler, response.ParentCifsPacket.TreeId, response.ParentCifsPacket.MultiplexId, response.ParentCifsPacket.ProcessId, response.FileId);

                        assembler.FileContentLength=fileLength;
                    }

                        
                }
                else if(smbCommandPacket.GetType()==typeof(Packets.CifsPacket.ReadAndXResponse)) {
                    Packets.CifsPacket.ReadAndXResponse response=(Packets.CifsPacket.ReadAndXResponse)smbCommandPacket;
                    //move the assembler to the real FileStreamAssemblerList!
                    FileTransfer.FileStreamAssembler assembler=smbSession.GetLastReferencedFileStreamAssembler(response.ParentCifsPacket.TreeId, response.ParentCifsPacket.MultiplexId, response.ParentCifsPacket.ProcessId);
                    if (assembler == null) {
                        base.MainPacketHandler.OnAnomalyDetected("Unable to find assembler for " + smbCommandPacket.ToString());
                    }
                    else if(assembler!=null) {
                        /* Removed 2011-04-25
                        if(!mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(assembler))
                            mainPacketHandler.FileStreamAssemblerList.Add(assembler);
                         * */

                        assembler.FileSegmentRemainingBytes+=response.DataLength;//setting this one so that it can receive more bytes
                        if(!assembler.IsActive) {
                            System.Diagnostics.Debug.Assert(assembler.ExtendedFileId != null && assembler.ExtendedFileId != "", "No FileID set for SMB file transfer!");

                            if (!assembler.TryActivate()) {
                                if (!response.ParentCifsPacket.ParentFrame.QuickParse)
                                    response.ParentCifsPacket.ParentFrame.Errors.Add(new Frame.Error(response.ParentCifsPacket.ParentFrame, response.PacketStartIndex, response.PacketEndIndex, "Unable to activate file stream assembler for " + assembler.FileLocation + "/" + assembler.Filename));
                            }
                            else if (assembler.IsActive)
                                assembler.AddData(response.GetFileData(), tcpPacket.SequenceNumber);

                        }
                        /* Removed 2011-04-25
                        if(!assembler.IsActive) {//see if the file is fully assembled or if something else went wrong...
                            smbSession.RemoveLastReferencedAssembler(response.ParentCifsPacket.TreeId, response.ParentCifsPacket.MultiplexId, response.ParentCifsPacket.ProcessId);
                        }
                         * */
                    }
                }

            }
        }


        internal class SmbSession {
            private System.Net.IPAddress serverIP, clientIP;
            private ushort serverTcpPort, clientTcpPort;
            
            //treeId|multiplexId
            private System.Collections.Generic.SortedList<uint, ushort> lastReferencedFileIdPerTreeMux;

            //processId|multiplexId (like Wireshark do in smb_saved_info_equal_unmatched of packet-smb.c)
            private System.Collections.Generic.SortedList<uint, ushort> lastReferencedFileIdPerPidMux;
            //private ushort lastReferencedFileId;

            //private System.Collections.Generic.SortedList<ushort, FileTransfer.FileStreamAssembler> fileIdAssemblerList;
            private PopularityList<ushort, FileTransfer.FileStreamAssembler> fileIdAssemblerList;

            //internal PopularityList<ushort, FileTransfer.FileStreamAssembler> FileIdAssemblerList { get { return this.fileIdAssemblerList; } }

            internal static string GetSmbSessionId(System.Net.IPAddress serverIP, ushort serverTcpPort, System.Net.IPAddress clientIP, ushort clientTcpPort) {
                return serverIP.ToString()+":"+serverTcpPort.ToString("X4")+"-"+clientIP.ToString()+":"+clientTcpPort.ToString("X4");
            }

            internal SmbSession(System.Net.IPAddress serverIP, ushort serverTcpPort, System.Net.IPAddress clientIP, ushort clientTcpPort) {
                this.serverIP=serverIP;
                this.serverTcpPort=serverTcpPort;
                this.clientIP=clientIP;
                this.clientTcpPort=clientTcpPort;

                this.lastReferencedFileIdPerTreeMux=new SortedList<uint, ushort>();
                this.lastReferencedFileIdPerPidMux=new SortedList<uint, ushort>();


                this.fileIdAssemblerList=new PopularityList<ushort, FileTransfer.FileStreamAssembler>(100);

            }

            internal string GetId() {
                return GetSmbSessionId(this.serverIP, serverTcpPort, clientIP, clientTcpPort);
            }

            internal bool ContainsFileId(ushort treeId, ushort muxId, ushort processId, ushort fileId) {
                this.Touch(treeId, muxId, processId, fileId);
                return this.fileIdAssemblerList.ContainsKey(fileId);
            }

            internal void AddFileStreamAssembler(FileTransfer.FileStreamAssembler assembler, ushort treeId, ushort muxId, ushort processId) {
                this.AddFileStreamAssembler(assembler, treeId, muxId, processId, (ushort)0);
            }
            internal void AddFileStreamAssembler(FileTransfer.FileStreamAssembler assembler, ushort treeId, ushort muxId, ushort processId, ushort fileId) {
                this.lastReferencedFileIdPerTreeMux[Utils.ByteConverter.ToUInt32(treeId, muxId)] = fileId;
                this.lastReferencedFileIdPerPidMux[Utils.ByteConverter.ToUInt32(processId, muxId)] = fileId;
                if(this.fileIdAssemblerList.ContainsKey(fileId))
                    this.fileIdAssemblerList.Remove(fileId);
                this.fileIdAssemblerList.Add(fileId, assembler);
            }

            internal void RemoveLastReferencedAssembler(ushort treeId, ushort muxId, ushort processId) {
                ushort lastReferencedFileId;
                if (lastReferencedFileIdPerPidMux.ContainsKey(Utils.ByteConverter.ToUInt32(processId, muxId)))
                    lastReferencedFileId = lastReferencedFileIdPerPidMux[Utils.ByteConverter.ToUInt32(processId, muxId)];
                else if (lastReferencedFileIdPerTreeMux.ContainsKey(Utils.ByteConverter.ToUInt32(treeId, muxId)))
                    lastReferencedFileId = lastReferencedFileIdPerTreeMux[Utils.ByteConverter.ToUInt32(treeId, muxId)];
                else
                    lastReferencedFileId=(ushort)0;

                if(this.fileIdAssemblerList.ContainsKey(lastReferencedFileId))
                    this.RemoveFileStreamAssembler(treeId, muxId, processId, lastReferencedFileId);
            }
            internal void RemoveFileStreamAssembler(ushort treeId, ushort muxId, ushort processId, ushort fileId) {
                RemoveFileStreamAssembler(treeId, muxId, processId, fileId, false);
            }
            internal void RemoveFileStreamAssembler(ushort treeId, ushort muxId, ushort processId, ushort fileId, bool closeAssembler) {
                //this.Touch(treeId, muxId, fileId);
                if(this.fileIdAssemblerList.ContainsKey(fileId)) {
                    FileTransfer.FileStreamAssembler assembler=GetFileStreamAssembler(treeId, muxId, processId, fileId);
                    this.fileIdAssemblerList.Remove(fileId);
                    if(closeAssembler)
                        assembler.Clear();
                }
            }
            internal FileTransfer.FileStreamAssembler GetLastReferencedFileStreamAssembler(ushort treeId, ushort muxId, ushort processId) {
                if (lastReferencedFileIdPerPidMux.ContainsKey(Utils.ByteConverter.ToUInt32(processId, muxId)))
                    return GetFileStreamAssembler(treeId, muxId, processId, lastReferencedFileIdPerPidMux[Utils.ByteConverter.ToUInt32(processId, muxId)]);
                else if (lastReferencedFileIdPerTreeMux.ContainsKey(Utils.ByteConverter.ToUInt32(treeId, muxId)))
                    return GetFileStreamAssembler(treeId, muxId, processId, lastReferencedFileIdPerTreeMux[Utils.ByteConverter.ToUInt32(treeId, muxId)]);
                else
                    return null;
                //return GetFileStreamAssembler(lastReferencedFileId);
            }
            internal FileTransfer.FileStreamAssembler GetFileStreamAssembler(ushort treeId, ushort muxId, ushort processId, ushort fileId){
                //this.lastReferencedFileId=fileId;
                this.Touch(treeId, muxId, processId, fileId);

                if(this.fileIdAssemblerList.ContainsKey(fileId))
                    return this.fileIdAssemblerList[fileId];
                else
                    return null;
            }
            /// <summary>
            /// Updates the fileId so that it will be referenced as "LastReferencedFile"
            /// </summary>
            /// <param name="fileId"></param>
            internal void Touch(ushort treeId, ushort muxId, ushort processId, ushort fileId) {
                //System.Diagnostics.Debug.Assert(this.fileIdAssemblerList.ContainsKey(fileId), "treeID="+treeId.ToString("X4")+" muxID="+muxId.ToString("X4")+" fileID="+fileId.ToString("X4"));
                if(this.fileIdAssemblerList.ContainsKey(fileId)) {
                    this.lastReferencedFileIdPerTreeMux[Utils.ByteConverter.ToUInt32(treeId, muxId)] = fileId;
                    this.lastReferencedFileIdPerPidMux[Utils.ByteConverter.ToUInt32(processId, muxId)] = fileId;
                }
            }
        }
    }
}
