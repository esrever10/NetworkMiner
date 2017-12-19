using System;
using System.Collections.Generic;
using System.Text;
using PacketParser.Packets;

namespace PacketParser.PacketHandlers {
    class SmtpPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler {

        internal class SmtpSession {
            internal enum SmtpState { None, AuthLogin, Username, Password, Authenticated, Data, Footer }

            private static readonly byte[] DATA_TERMINATOR = {0x0d, 0x0a, 0x2e, 0x0d, 0x0a}; //CRLF.CRLF

            private SmtpState state;
            private string username;
            private string password;
            private string mailFrom;
            private List<string> rcptTo;


            //private PacketParser.Mime.UnbufferedReader mimeReader;
            private System.IO.MemoryStream dataStream; //to hold the DATA part of an email
            //private System.IO.TextWriter textWriter;
            private System.Text.ASCIIEncoding asciiEncoding;

            internal SmtpState State { get { return this.state; } set { this.state = value; } }
            internal string Username { get { return this.username; } set { this.username = value; } }
            internal string Password { get { return this.password; } set { this.password = value; } }
            internal string MailFrom { get { return this.mailFrom; } set { this.mailFrom=value; } }
            internal IEnumerable<string> RcptTo { get { return this.rcptTo; } }
            internal System.IO.MemoryStream DataStream { get { return this.dataStream; } }

            internal SmtpSession() {
                this.state = SmtpState.None;
                this.rcptTo = new List<string>();
                this.dataStream = new System.IO.MemoryStream();
                this.asciiEncoding = new System.Text.ASCIIEncoding();
            }

            internal void AddRecipient(string rcptTo) {
                this.rcptTo.Add(rcptTo);
            }

            internal void AddData(string dataString) {
                byte[] data = asciiEncoding.GetBytes(dataString);
                this.AddData(data, 0, data.Length);
            }

            internal void AddData(byte[] buffer, int offset, int count) {
                List<byte> readBytes;
                
                long terminatorIndex = Utils.KnuthMorrisPratt.ReadTo(DATA_TERMINATOR, buffer, offset, out readBytes);
                //terminator might be split in between two packets
                if (terminatorIndex == -1 && this.dataStream.Length > 0) {
                    int oldBytesToRead = Math.Min(DATA_TERMINATOR.Length-1, (int)dataStream.Length);
                    byte[] oldBufferTail = new byte[oldBytesToRead];
                    this.dataStream.Seek(this.dataStream.Length - oldBytesToRead, System.IO.SeekOrigin.Begin);
                    int oldBytesRead = this.dataStream.Read(oldBufferTail, 0, oldBytesToRead);
                    byte[] tempBuffer = new byte[oldBytesRead + buffer.Length - offset];
                    Array.Copy(oldBufferTail, 0, tempBuffer, 0, oldBytesRead);
                    Array.Copy(buffer, offset, tempBuffer, oldBytesRead, buffer.Length - offset);
                    long tempTerminatorIndex = Utils.KnuthMorrisPratt.ReadTo(DATA_TERMINATOR, tempBuffer, 0, out readBytes);
                    if (tempTerminatorIndex >= 0) {
                        count = (int)tempTerminatorIndex - oldBytesRead + 2;
                        this.state = SmtpState.Footer;
                    }
                }
                else if(terminatorIndex >= 0) {
                    //terminator was found

                    //the final <cr><lf>.<cr><lf> will not included, but let's at least add one <cr><lf> at the end
                    count = (int)terminatorIndex-offset+2; //"+2" adds the <cr><lf>
                    //offset = 0;
                    this.state = SmtpState.Footer;
                }
                if (count > 0) {
                    this.dataStream.Seek(0, System.IO.SeekOrigin.End);
                    this.dataStream.Write(buffer, offset, count);
                }
            }

        }

        private PopularityList<NetworkTcpSession, SmtpSession> smtpSessionList;

        public SmtpPacketHandler(PacketHandler mainPacketHandler)
            : base(mainPacketHandler) {
            this.smtpSessionList=new PopularityList<NetworkTcpSession, SmtpSession>(100);//max 100 simultaneous SMTP sessions
        }

        

        #region ITcpSessionPacketHandler Members

        public ApplicationLayerProtocol HandledProtocol {
            get { return ApplicationLayerProtocol.Smtp; }
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<PacketParser.Packets.AbstractPacket> packetList) {
            SmtpSession smtpSession;
            if(this.smtpSessionList.ContainsKey(tcpSession))
                smtpSession=this.smtpSessionList[tcpSession];
            else {
                smtpSession = new SmtpSession();
                this.smtpSessionList.Add(tcpSession, smtpSession);
            }

            Packets.TcpPacket tcpPacket=null;
            Packets.SmtpPacket smtpPacket=null;

            foreach(Packets.AbstractPacket p in packetList) {
                if(p.GetType()==typeof(Packets.TcpPacket))
                    tcpPacket=(Packets.TcpPacket)p;
                else if(p.GetType()==typeof(Packets.SmtpPacket))
                    smtpPacket=(Packets.SmtpPacket)p;
            }



            if(smtpPacket!=null) {
                if(smtpPacket.ClientToServer) {
                    if(smtpSession.State == SmtpSession.SmtpState.Username) {
                        string base64Username = smtpPacket.ReadLine().Trim();
                        try {
                            byte[] usernameBytes = System.Convert.FromBase64String(base64Username);
                            smtpSession.Username = System.Text.ASCIIEncoding.ASCII.GetString(usernameBytes);
                        }
                        catch(FormatException e) { }
                    }
                    else if(smtpSession.State == SmtpSession.SmtpState.Password) {
                        string base64Password = smtpPacket.ReadLine().Trim();
                        try {
                            byte[] passwordBytes = System.Convert.FromBase64String(base64Password);
                            smtpSession.Password = System.Text.ASCIIEncoding.ASCII.GetString(passwordBytes);
                        }
                        catch(FormatException e) { }
                    }
                    else if(smtpSession.State == SmtpSession.SmtpState.Data) {
                        //write data to file until we receive "\n.\n" could also be \r\n.\r\n
                        smtpSession.AddData(smtpPacket.ParentFrame.Data, smtpPacket.PacketStartIndex, smtpPacket.PacketLength);
                        //check if state has transitioned over to footer
                        if(smtpSession.State == SmtpSession.SmtpState.Footer) {

                            Mime.UnbufferedReader ur = new PacketParser.Mime.UnbufferedReader(smtpSession.DataStream);
                            string from=null;
                            string to=null;
                            string subject=null;
                            string messageId=null;
                            System.Collections.Specialized.NameValueCollection rootAttributes=null;
                            foreach(Mime.MultipartPart multipart in Mime.PartBuilder.GetParts(ur)) {
                                if(rootAttributes==null) {
                                    from=multipart.Attributes["From"];
                                    to=multipart.Attributes["To"];
                                    subject=multipart.Attributes["Subject"];
                                    messageId=multipart.Attributes["Message-ID"];
                                    rootAttributes=multipart.Attributes;
                                }

                                base.MainPacketHandler.OnParametersDetected(new PacketParser.Events.ParametersEventArgs(smtpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP "+tcpPacket.SourcePort, "TCP "+tcpPacket.DestinationPort, multipart.Attributes, tcpPacket.ParentFrame.Timestamp, "SMTP packet"));

                                string contentType = multipart.Attributes["Content-Type"];
                                string charset = multipart.Attributes["charset"];
                                Encoding encoding = null;
                                if (charset != null && charset.Length > 0) {
                                    try {
                                        encoding = System.Text.Encoding.GetEncoding(charset);
                                    }
                                    catch { };
                                }
                                bool attachment = false;
                                string contentDisposition = multipart.Attributes["Content-Disposition"];
                                if (contentDisposition != null && contentDisposition.Contains("attachment"))
                                    attachment = true;
                                if(!attachment && contentType == null || contentType.Equals("text/plain", StringComparison.InvariantCultureIgnoreCase)) {
                                    //print the data as text
                                    //string textData = null;
                                    byte[] textDataBytes = null;
                                    if (multipart.Attributes["Content-Transfer-Encoding"] == "quoted-printable") {
                                        textDataBytes = Utils.ByteConverter.ReadQuotedPrintable(multipart.Data).ToArray();
                                        //textData = Utils.ByteConverter.ReadString();
                                    }
                                    else if (multipart.Attributes["Content-Transfer-Encoding"] == "base64") {
                                        textDataBytes = System.Convert.FromBase64String(Utils.ByteConverter.ReadString(multipart.Data));
                                        //textData = Utils.ByteConverter.ReadString();
                                    }
                                    else {
                                        textDataBytes = multipart.Data;
                                        //textData = Utils.ByteConverter.ReadString();
                                    }
                                    string textData = null;
                                    if (encoding == null)
                                        textData = Utils.ByteConverter.ReadString(textDataBytes);
                                    else
                                        textData = encoding.GetString(textDataBytes);
                                    if(textData!=null) {
                                        //System.Collections.Specialized.NameValueCollection tmpCol=new System.Collections.Specialized.NameValueCollection();
                                        //tmpCol.Add("e-mail", textData);
                                        //base.MainPacketHandler.OnParametersDetected(new PacketParser.Events.ParametersEventArgs(smtpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP "+tcpPacket.SourcePort, "TCP "+tcpPacket.DestinationPort, tmpCol, tcpPacket.ParentFrame.Timestamp, "SMTP packet"));
                                        System.Collections.Specialized.NameValueCollection aggregatedAttributes = new System.Collections.Specialized.NameValueCollection();
                                        aggregatedAttributes.Add(rootAttributes);
                                        aggregatedAttributes.Add(multipart.Attributes);
                                        base.MainPacketHandler.OnMessageDetected(new PacketParser.Events.MessageEventArgs(ApplicationLayerProtocol.Smtp, sourceHost, destinationHost, smtpPacket.ParentFrame.FrameNumber, smtpPacket.ParentFrame.Timestamp, from, to, subject, textData, aggregatedAttributes));
                                    }
                                }
                                else {
                                    //store the stuff to disk
                                    string filename = multipart.Attributes["name"];
                                    if(filename==null || filename.Length==0)
                                        filename = multipart.Attributes["filename"];
                                    if(filename==null || filename.Length==0) {
                                        if(subject!=null && subject.Length>3) {
                                            filename = Utils.StringManglerUtil.ConvertToFilename(subject, 10);
                                        }
                                        else if(messageId!=null && messageId.Length>3) {
                                            filename = Utils.StringManglerUtil.ConvertToFilename(messageId, 10);
                                        }
                                        if(filename==null || filename.Length<3)
                                            filename = "email_"+(multipart.GetHashCode()%1000);

                                        string extension = Utils.StringManglerUtil.GetExtension(contentType);
                                        if(extension==null || extension.Length<1)
                                            extension="dat";
                                        filename = filename + "." + extension;
                                    }
                                    //check if filename is encoded as '?CharacterSet?Enum(Q,B)?', for example '=?UTF-8?B?IE1ldGhvZA==?=' RFC2047
                                    /*
                                    if (Mime.Rfc2047Parser.IsRfc2047String(filename)) {
                                        try {
                                            filename = Mime.Rfc2047Parser.ParseRfc2047String(filename);
                                        }
                                        catch (Exception) { }
                                    }*/
                                    List<byte> fileData=new List<byte>();
                                    if(multipart.Attributes["Content-Transfer-Encoding"]=="base64") {
                                        //decode base64 stuff
                                        int index = 0;
                                        while(index<multipart.Data.Length) {
                                            string base64 = Utils.ByteConverter.ReadLine(multipart.Data, ref index);
                                            if (base64 == null && index < multipart.Data.Length) {
                                                //read the remaining data
                                                base64 = Utils.ByteConverter.ReadString(multipart.Data, index, multipart.Data.Length - index, false, false);
                                                index = multipart.Data.Length;
                                            }
#if DEBUG
                                            if(base64==null)
                                                System.Diagnostics.Debugger.Break();
#endif
                                            //if (base64 != null && base64.Length > 0) {
                                            try {
                                                fileData.AddRange(Convert.FromBase64String(base64));
                                            }
                                            catch (FormatException e) { }
                                        }
                                    }
                                    else if(multipart.Attributes["Content-Transfer-Encoding"]=="quoted-printable") {
                                        //must be decoded according to http://www.ietf.org/rfc/rfc2045.txt
                                        fileData = Utils.ByteConverter.ReadQuotedPrintable(multipart.Data);
                                    }
                                    else {
                                        //Add the raw data
                                        fileData.AddRange(multipart.Data);
                                    }

                                    if(fileData!=null && fileData.Count > 0) {
                                        FileTransfer.FileStreamAssembler assembler=new FileTransfer.FileStreamAssembler(MainPacketHandler.FileStreamAssemblerList, sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true, FileTransfer.FileStreamTypes.SMTP, filename, "/", fileData.Count, fileData.Count, "E-mail From: "+from+" To: "+to+" Subject: "+subject, filename, tcpPacket.ParentFrame.FrameNumber, tcpPacket.ParentFrame.Timestamp);
                                        if(assembler.TryActivate()) {
                                            assembler.AddData(fileData.ToArray(), tcpPacket.SequenceNumber);
                                            //assembler.FinishAssembling();
                                        }
                                        else {
                                            assembler.Clear();
                                            assembler.FinishAssembling();
                                        }
                                    }

                                }
                            }
                        }
                    }
                    else {
                        foreach(KeyValuePair<string, string> requestCommandAndArgument in smtpPacket.RequestCommandsAndArguments) {
                            if(requestCommandAndArgument.Key.Equals(SmtpPacket.ClientCommands.HELO.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                                string clientDomain = requestCommandAndArgument.Value;
                            }
                            else if(requestCommandAndArgument.Key.Equals(SmtpPacket.ClientCommands.EHLO.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                                string clientDomain = requestCommandAndArgument.Value;
                            }
                            else if(requestCommandAndArgument.Key.Equals(SmtpPacket.ClientCommands.AUTH.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                                if(requestCommandAndArgument.Value.ToUpper().Contains("LOGIN"))
                                    smtpSession.State = SmtpSession.SmtpState.AuthLogin;
                            }
                            else if(requestCommandAndArgument.Key.Equals(SmtpPacket.ClientCommands.MAIL.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                                if(requestCommandAndArgument.Value.StartsWith("FROM", StringComparison.InvariantCultureIgnoreCase)) {
                                    int colonIndex = requestCommandAndArgument.Value.IndexOf(':');
                                    if(colonIndex>0 && requestCommandAndArgument.Value.Length > colonIndex+1)
                                        smtpSession.MailFrom = requestCommandAndArgument.Value.Substring(colonIndex+1).Trim();
                                }
                            }
                            else if(requestCommandAndArgument.Key.Equals(SmtpPacket.ClientCommands.RCPT.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                                if(requestCommandAndArgument.Value.StartsWith("TO", StringComparison.InvariantCultureIgnoreCase)) {
                                    int colonIndex = requestCommandAndArgument.Value.IndexOf(':');
                                    if(colonIndex>0 && requestCommandAndArgument.Value.Length > colonIndex+1)
                                        smtpSession.AddRecipient(requestCommandAndArgument.Value.Substring(colonIndex+1).Trim());
                                }
                            }
                            else if(requestCommandAndArgument.Key.Equals(SmtpPacket.ClientCommands.DATA.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                                smtpSession.State = SmtpSession.SmtpState.Data;
                            }
#if DEBUG
                            base.MainPacketHandler.OnParametersDetected(new Events.ParametersEventArgs(tcpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + tcpPacket.SourcePort, "TCP " + tcpPacket.DestinationPort, smtpPacket.RequestCommandsAndArguments, tcpPacket.ParentFrame.Timestamp, "SMTP Request"));
#endif
                        }
                    }
                    /*}
                    else if(smtpPacket.RequestCommand != null) {
                        if(smtpPacket.RequestCommand.Equals(SmtpPacket.ClientCommands.HELO.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                            string clientDomain = smtpPacket.RequestArgument;
                        }
                        else if(smtpPacket.RequestCommand.Equals(SmtpPacket.ClientCommands.EHLO.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                            string clientDomain = smtpPacket.RequestArgument;
                        }
                        else if(smtpPacket.RequestCommand.Equals(SmtpPacket.ClientCommands.AUTH.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                            if(smtpPacket.RequestArgument.ToUpper().Contains("LOGIN"))
                                smtpSession.State = SmtpSession.SmtpState.AuthLogin;
                        }
                        else if(smtpPacket.RequestCommand.Equals(SmtpPacket.ClientCommands.MAIL.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                            if(smtpPacket.RequestArgument.StartsWith("FROM", StringComparison.InvariantCultureIgnoreCase)) {
                                int colonIndex = smtpPacket.RequestArgument.IndexOf(':');
                                if(colonIndex>0 && smtpPacket.RequestArgument.Length > colonIndex+1)
                                    smtpSession.MailFrom = smtpPacket.RequestArgument.Substring(colonIndex+1).Trim();
                            }
                        }
                        else if(smtpPacket.RequestCommand.Equals(SmtpPacket.ClientCommands.RCPT.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                            if(smtpPacket.RequestArgument.StartsWith("TO", StringComparison.InvariantCultureIgnoreCase)) {
                                int colonIndex = smtpPacket.RequestArgument.IndexOf(':');
                                if(colonIndex>0 && smtpPacket.RequestArgument.Length > colonIndex+1)
                                    smtpSession.AddRecipient(smtpPacket.RequestArgument.Substring(colonIndex+1).Trim());
                            }
                        }
                        else if(smtpPacket.RequestCommand.Equals(SmtpPacket.ClientCommands.DATA.ToString(), StringComparison.InvariantCultureIgnoreCase)) {
                            smtpSession.State = SmtpSession.SmtpState.Data;
                        }
                    }*/

                }
                else {//server to client
                    foreach(KeyValuePair<int, string> replyCodeAndArgument in smtpPacket.Replies) {
                        if(replyCodeAndArgument.Key == 334) { //AUTH LOGIN
                            if(replyCodeAndArgument.Value.Equals("VXNlcm5hbWU6"))
                                smtpSession.State = SmtpSession.SmtpState.Username;
                            else if(replyCodeAndArgument.Value.Equals("UGFzc3dvcmQ6"))
                                smtpSession.State = SmtpSession.SmtpState.Password;
                        }
                        else if(replyCodeAndArgument.Key == 235) { //AUTHENTICATION SUCCESSFUL 
                            base.MainPacketHandler.AddCredential(new NetworkCredential(tcpSession.ClientHost, tcpSession.ServerHost, smtpPacket.PacketTypeDescription, smtpSession.Username, smtpSession.Password, smtpPacket.ParentFrame.Timestamp));
                            smtpSession.State = SmtpSession.SmtpState.Authenticated;
                        }
                        else if(replyCodeAndArgument.Key >= 500) //error
                            smtpSession.State = SmtpSession.SmtpState.None;
                        else if(replyCodeAndArgument.Key == 354) //DATA "Start mail input; end with <CRLF>.<CRLF>"
                            smtpSession.State = SmtpSession.SmtpState.Data;
                        else if(replyCodeAndArgument.Key == 250) //"Requested mail action okay, completed"
                            smtpSession.State = SmtpSession.SmtpState.None;
                    }
                }
                //There was a SMTP packet, so treat this as a sucsessfull extraction
                return tcpPacket.PayloadDataLength;
            }
            else //smtpPacket == null
                return 0;


            
            
        }

        public void Reset() {
            this.smtpSessionList.Clear();
        }

        #endregion
    }
}
