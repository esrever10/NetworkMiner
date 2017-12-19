//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.PacketHandlers {
    class HttpPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler {

        public ApplicationLayerProtocol HandledProtocol {
            get { return ApplicationLayerProtocol.Http; }
        }

        public HttpPacketHandler(PacketHandler mainPacketHandler)
            : base(mainPacketHandler) {
            //empty constructor
        }

        #region ITcpSessionPacketHandler Members

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<Packets.AbstractPacket> packetList) {
            bool successfulExtraction=false;

            Packets.HttpPacket httpPacket=null;
            Packets.TcpPacket tcpPacket=null;
            foreach(Packets.AbstractPacket p in packetList) {
                if(p.GetType()==typeof(Packets.HttpPacket))
                    httpPacket=(Packets.HttpPacket)p;
                else if(p.GetType()==typeof(Packets.TcpPacket))
                    tcpPacket=(Packets.TcpPacket)p;
            }

            if(httpPacket!=null && tcpPacket!=null){
                if(httpPacket.PacketHeaderIsComplete) {
                    //check if it is a POST and content length is small
                    if(httpPacket.RequestMethod!=Packets.HttpPacket.RequestMethods.POST || httpPacket.ContentLength>4096/* used to be 1024*/ || httpPacket.ContentIsComplete()) {
                        successfulExtraction = ExtractHttpData(httpPacket, tcpPacket, sourceHost, destinationHost, base.MainPacketHandler);
                        //successfulExtraction=true;
                    }
                }

            }
            if(successfulExtraction)
                return tcpPacket.PayloadDataLength;
            else
                return 0;
        }

        public void Reset() {
            //do nothing...
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpPacket"></param>
        /// <param name="tcpPacket"></param>
        /// <param name="sourceHost"></param>
        /// <param name="destinationHost"></param>
        /// <param name="mainPacketHandler"></param>
        /// <returns>True if the data was successfully parsed. False if the data need to be parsed again with more data</returns>
        private bool ExtractHttpData(Packets.HttpPacket httpPacket, Packets.TcpPacket tcpPacket, NetworkHost sourceHost, NetworkHost destinationHost, PacketHandler mainPacketHandler) {
            if(httpPacket.MessageTypeIsRequest) {
                //HTTP request

                System.Collections.Specialized.NameValueCollection cookieParams = null;
                if(httpPacket.UserAgentBanner!=null && httpPacket.UserAgentBanner.Length>0)
                    sourceHost.AddHttpUserAgentBanner(httpPacket.UserAgentBanner);
                if(httpPacket.RequestedHost!=null && httpPacket.RequestedHost.Length>0)
                    destinationHost.AddHostName(httpPacket.RequestedHost);
                if(httpPacket.Cookie!=null) {
                    cookieParams=new System.Collections.Specialized.NameValueCollection();
                    char[] separators={ ';', ',' };
                    foreach(string s in httpPacket.Cookie.Split(separators)){
                        string cookieFragment=s.Trim();
                        int splitOffset=cookieFragment.IndexOf('=');
                        if(splitOffset>0)
                            cookieParams.Add(cookieFragment.Substring(0, splitOffset), cookieFragment.Substring(splitOffset+1));
                        else
                            cookieParams.Add(cookieFragment, "");
                    }
                    NetworkCredential inCookieCredential = NetworkCredential.GetNetworkCredential(cookieParams, sourceHost, destinationHost, "HTTP Cookie parameter", httpPacket.ParentFrame.Timestamp);
                    if (inCookieCredential != null)
                        mainPacketHandler.AddCredential(inCookieCredential);

                    mainPacketHandler.OnParametersDetected(new Events.ParametersEventArgs(tcpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP "+tcpPacket.SourcePort, "TCP "+tcpPacket.DestinationPort, cookieParams, httpPacket.ParentFrame.Timestamp, "HTTP Cookie"));
                    NetworkCredential credential=new NetworkCredential(sourceHost, destinationHost, "HTTP Cookie", httpPacket.Cookie, "N/A", httpPacket.ParentFrame.Timestamp);
                    mainPacketHandler.AddCredential(credential);
                }
                if(httpPacket.AuthorizationCredentialsUsername!=null) {
                    NetworkCredential nc=new NetworkCredential(sourceHost, destinationHost, httpPacket.PacketTypeDescription, httpPacket.AuthorizationCredentialsUsername, httpPacket.AuthorizationCredentialsPassword, httpPacket.ParentFrame.Timestamp);
                    mainPacketHandler.AddCredential(nc);
                    //this.AddCredential(nc);
                }
                if (httpPacket.HeaderFields != null && httpPacket.HeaderFields.Count > 0) {
                    SortedList<string, string> ignoredHeaderNames = new SortedList<string, string>();
                    ignoredHeaderNames.Add("accept", null);
                    ignoredHeaderNames.Add("connection", null);
                    ignoredHeaderNames.Add("accept-language", null);
                    ignoredHeaderNames.Add("accept-encoding", null);

                    //Build a sortedList of all headers
                    System.Collections.Specialized.NameValueCollection httpHeaders = new System.Collections.Specialized.NameValueCollection();
                    //SortedList<string, string> httpHeaders = new SortedList<string, string>();
                    foreach (string header in httpPacket.HeaderFields) {
                        int delimiterIndex = header.IndexOf(':');
                        if (delimiterIndex > 0 && delimiterIndex < header.Length) {
                            string headerName = header.Substring(0, delimiterIndex).Trim();
                            //if (!httpHeaders.ContainsKey(headerName))
                            if(!ignoredHeaderNames.ContainsKey(headerName.ToLower()))
                                httpHeaders.Add(headerName, header.Substring(delimiterIndex + 1).Trim());
                        }
                    }
                    //mainPacketHandler.OnParametersDetected
                    base.MainPacketHandler.OnParametersDetected(new Events.ParametersEventArgs(httpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + tcpPacket.SourcePort, "TCP " + tcpPacket.DestinationPort, httpHeaders, httpPacket.ParentFrame.Timestamp, "HTTP Header"));

                    foreach (string headerName in httpHeaders.Keys) {
                        
                        /**
                         * http://mobiforge.com/developing/blog/useful-x-headers
                         * http://nakedsecurity.sophos.com/2012/01/25/smartphone-website-telephone-number/
                         * http://www.nowsms.com/discus/messages/485/14998.html
                         * http://coding-talk.com/f46/check-isdn-10962/
                         **/
                        if (headerName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase)) {
                            sourceHost.AddNumberedExtraDetail("HTTP header: " + headerName, httpHeaders[headerName]);
                        }
                        else if (headerName.StartsWith("HTTP_X", StringComparison.InvariantCultureIgnoreCase)) {
                            sourceHost.AddNumberedExtraDetail("HTTP header: " + headerName, httpHeaders[headerName]);
                        }
                        else if (headerName.StartsWith("X_", StringComparison.InvariantCultureIgnoreCase)) {
                            sourceHost.AddNumberedExtraDetail("HTTP header: " + headerName, httpHeaders[headerName]);
                        }
                        else if (headerName.StartsWith("HTTP_MSISDN", StringComparison.InvariantCultureIgnoreCase)) {
                            sourceHost.AddNumberedExtraDetail("HTTP header: " + headerName, httpHeaders[headerName]);
                        }
                    }

                    
                    
                }


                //file transfer
                if((httpPacket.RequestMethod==Packets.HttpPacket.RequestMethods.GET || httpPacket.RequestMethod==Packets.HttpPacket.RequestMethods.POST) && httpPacket.RequestedFileName!=null) {

                    System.Collections.Specialized.NameValueCollection queryStringData=httpPacket.GetQuerystringData();
                    if(queryStringData!=null && queryStringData.Count>0) {
                        //parentForm.ShowParameters(tcpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP "+tcpPacket.SourcePort, "TCP "+tcpPacket.DestinationPort, queryStringData, tcpPacket.ParentFrame.Timestamp, "HTTP QueryString");
                        mainPacketHandler.OnParametersDetected(new Events.ParametersEventArgs(tcpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP "+tcpPacket.SourcePort, "TCP "+tcpPacket.DestinationPort, queryStringData, tcpPacket.ParentFrame.Timestamp, "HTTP QueryString"));
                        //mainPacketHandler.AddCredential(new NetworkCredential(sourceHost, destinationHost, "HTTP GET QueryString", 
                        NetworkCredential credential=NetworkCredential.GetNetworkCredential(queryStringData, sourceHost, destinationHost, "HTTP GET QueryString", tcpPacket.ParentFrame.Timestamp);
                        if(credential!=null)
                            mainPacketHandler.AddCredential(credential);
                        if(queryStringData.HasKeys()) {
                            Dictionary<string, string> queryStringDictionary = new Dictionary<string, string>();
                            foreach (string key in queryStringData.AllKeys)
                                queryStringDictionary.Add(key, queryStringData[key]);

                            if (queryStringDictionary.ContainsKey("utmsr"))
                                sourceHost.AddNumberedExtraDetail("Screen resolution (Google Analytics)", queryStringDictionary["utmsr"]);
                            if (queryStringDictionary.ContainsKey("utmsc"))
                                sourceHost.AddNumberedExtraDetail("Color depth (Google Analytics)", queryStringDictionary["utmsc"]);
                            if (queryStringDictionary.ContainsKey("utmul"))
                                sourceHost.AddNumberedExtraDetail("Browser language (Google Analytics)", queryStringDictionary["utmul"]);
                            if (queryStringDictionary.ContainsKey("utmfl"))
                                sourceHost.AddNumberedExtraDetail("Flash version (Google Analytics)", queryStringDictionary["utmfl"]);
                            if (httpPacket.RequestMethod == Packets.HttpPacket.RequestMethods.POST && queryStringDictionary.ContainsKey("a") && queryStringDictionary["a"].Equals("SendMessage")) {
                                if (!httpPacket.ContentIsComplete())//we must have all the content when parsing AOL data
                                    return false;
                            }
                        }
                    }

                    //file transfer stuff
                    string fileUri=httpPacket.RequestedFileName;
                    string queryString=null;
                    if(fileUri.Contains("?")) {
                        if(fileUri.IndexOf('?')+1<fileUri.Length)
                            queryString=fileUri.Substring(fileUri.IndexOf('?')+1);
                        fileUri=fileUri.Substring(0, fileUri.IndexOf('?'));
                    }
                    if(fileUri.StartsWith("http://"))
                        fileUri=fileUri.Substring(7);
                    if(fileUri.StartsWith("www.") && fileUri.Contains("/"))
                        fileUri=fileUri.Substring(fileUri.IndexOf("/"));

                    //char[] separators={ '/' };
                    char[] separators=new char[System.IO.Path.GetInvalidPathChars().Length+1];
                    Array.Copy(System.IO.Path.GetInvalidPathChars(), separators, System.IO.Path.GetInvalidPathChars().Length);
                    separators[separators.Length-1]='/';

                    string[] uriParts=fileUri.Split(separators);
                    string filename;
                    string fileLocation="";

                    if(fileUri.EndsWith("/")) {
                        filename="index.html";
                        for(int i=0; i<uriParts.Length; i++)
                            if(uriParts[i].Length>0 && !uriParts[i].Contains(".."))
                                fileLocation+="/"+uriParts[i];
                    }
                    else {
                        filename=uriParts[uriParts.Length-1];
                        for(int i=0; i<uriParts.Length-1; i++)
                            if(uriParts[i].Length>0 && !uriParts[i].Contains(".."))
                                fileLocation+="/"+uriParts[i];
                    }

                    //make sure all queryString-depending dynamic webpages are shown individually
                    if(queryString!=null && queryString.Length>0)
                        filename+="."+queryString.GetHashCode().ToString("X4");

                    //I will have to switch source and destination host here since this is only the request, not the actual file transfer!
                    try {

                        //see if there is an old assembler that needs to be removed
                        if(mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, true)) {
                            FileTransfer.FileStreamAssembler oldAssembler=mainPacketHandler.FileStreamAssemblerList.GetAssembler(destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, true);
                            mainPacketHandler.FileStreamAssemblerList.Remove(oldAssembler, true);
                        }

                        string fileDetails = httpPacket.RequestedFileName;
                        if (httpPacket.RequestedHost != null && httpPacket.RequestedHost.Length > 0 && httpPacket.RequestedFileName != null && httpPacket.RequestedFileName.StartsWith("/"))
                            fileDetails = httpPacket.RequestedHost + httpPacket.RequestedFileName;
                        FileTransfer.FileStreamAssembler assembler=new FileTransfer.FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, tcpPacket!=null, FileTransfer.FileStreamTypes.HttpGetNormal, filename, fileLocation, fileDetails, httpPacket.ParentFrame.FrameNumber, httpPacket.ParentFrame.Timestamp);
                        //FileTransfer.FileStreamAssembler assembler=new FileTransfer.FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, tcpPacket!=null, FileTransfer.FileStreamTypes.HttpGetNormal, filename, fileLocation, httpPacket.RequestedFileName, httpPacket.ParentFrame.FrameNumber, httpPacket.ParentFrame.Timestamp);
                        mainPacketHandler.FileStreamAssemblerList.Add(assembler);

                    }
                    catch(Exception e) {
                        mainPacketHandler.OnAnomalyDetected("Error creating assembler for HTTP file transfer: "+e.Message);

                    }
                    

                    //Large HTTP POSTs should also be dumped to files
                    //if(httpPacket.RequestMethod==Packets.HttpPacket.RequestMethods.POST && !httpPacket.ContentIsComplete() && httpPacket.ContentLength>4096 && httpPacket.ContentType.StartsWith("multipart/form-data")) {
                    
                    if(httpPacket.RequestMethod==Packets.HttpPacket.RequestMethods.POST){

                        //All Multipart MIME HTTP POSTs should be dumped to file
                        //the fileAssembler extracts the form parameters after assembly
                        if(httpPacket.ContentType!=null && httpPacket.ContentType.StartsWith("multipart/form-data")) {
                            FileTransfer.FileStreamAssembler assembler=null;
                            try {
                                //see if there is an old assembler that needs to be removed
                                if(mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true)) {
                                    FileTransfer.FileStreamAssembler oldAssembler=mainPacketHandler.FileStreamAssemblerList.GetAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true);
                                    mainPacketHandler.FileStreamAssemblerList.Remove(oldAssembler, true);
                                }

                                string mimeBoundary;
                                if(httpPacket.ContentType.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("multipart/form-data; boundary=") && httpPacket.ContentType.Length>30) {
                                    mimeBoundary=httpPacket.ContentType.Substring(30);
                                }
                                else
                                    mimeBoundary="";

                                assembler=new FileTransfer.FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, tcpPacket!=null, FileTransfer.FileStreamTypes.HttpPostMimeMultipartFormData, filename+".form-data.mime", fileLocation, mimeBoundary, httpPacket.ParentFrame.FrameNumber, httpPacket.ParentFrame.Timestamp);
                                assembler.FileContentLength=httpPacket.ContentLength;
                                assembler.FileSegmentRemainingBytes=httpPacket.ContentLength;
                                mainPacketHandler.FileStreamAssemblerList.Add(assembler);
                                if(assembler.TryActivate()) {
                                    //assembler is now active
                                    if(httpPacket.MessageBody!=null && httpPacket.MessageBody.Length>0)
                                        assembler.AddData(httpPacket.MessageBody, tcpPacket.SequenceNumber);
                                }

                            }
                            catch(Exception e) {
                                if(assembler!=null)
                                    assembler.Clear();
                                mainPacketHandler.OnAnomalyDetected("Error creating assembler for HTTP file transfer: "+e.Message);

                            }

                        }
                        else {//form data (not multipart)
                            System.Collections.Generic.List<Mime.MultipartPart> formMultipartData=httpPacket.GetFormData();
                            if (formMultipartData != null) {
                                foreach (Mime.MultipartPart mimeMultipart in formMultipartData) {
                                    if (mimeMultipart.Attributes["requests"] != null && httpPacket.GetQuerystringData() != null && httpPacket.GetQuerystringData()["a"] == "SendMessage") {
                                        //To handle AOL webmail
                                        string encodedMessage = mimeMultipart.Attributes["requests"];
                                        if (encodedMessage.StartsWith("[{") && encodedMessage.EndsWith("}]")) {
                                            encodedMessage = encodedMessage.Substring(2, encodedMessage.Length - 4);
                                        }
                                        int startIndex = -1;
                                        int endIndex = -1;
                                        while (endIndex < encodedMessage.Length - 2) {
                                            //startIndex = endIndex + 1;
                                            if (endIndex > 0)
                                                startIndex = encodedMessage.IndexOf(',', endIndex) + 1;
                                            else
                                                startIndex = 0;
                                            bool escapedString = encodedMessage[startIndex] == '\"';
                                            if (escapedString) {
                                                startIndex = encodedMessage.IndexOf('\"', startIndex) + 1;
                                                endIndex = encodedMessage.IndexOf('\"', startIndex);
                                                while (encodedMessage[endIndex - 1] == '\\') {
                                                    endIndex = encodedMessage.IndexOf('\"', endIndex + 1);
                                                }
                                            }
                                            else
                                                endIndex = encodedMessage.IndexOf(':', startIndex);
                                            
                                            string attributeName = encodedMessage.Substring(startIndex, endIndex - startIndex);

#if DEBUG
                                            //System.Diagnostics.Debug.Assert(encodedMessage[endIndex + 1] == ':');
                                            if (encodedMessage[endIndex] != ':' && encodedMessage[endIndex + 1] != ':')
                                                System.Diagnostics.Debugger.Break();
#endif
                                            startIndex = encodedMessage.IndexOf(':', endIndex)+1;
                                            escapedString = encodedMessage[startIndex] == '\"';
                                            if (escapedString) {
                                                startIndex = encodedMessage.IndexOf('\"', startIndex) + 1;
                                                endIndex = encodedMessage.IndexOf('\"', startIndex);
                                                while (encodedMessage[endIndex - 1] == '\\') {
                                                    endIndex = encodedMessage.IndexOf('\"', endIndex + 1);
                                                }
                                            }
                                            else if(encodedMessage.IndexOf(',', startIndex) > 0)
                                                endIndex = encodedMessage.IndexOf(',', startIndex);
                                            else
                                                endIndex = encodedMessage.Length;
                                            
                                            string attributeValue = encodedMessage.Substring(startIndex, endIndex - startIndex);
                                            //replace some special characters
                                            encodedMessage = encodedMessage.Replace("\\n", System.Environment.NewLine).Replace("\\r", "\r").Replace("\\t", "\t");
                                            mimeMultipart.Attributes.Add(attributeName, attributeValue);
                                        }
                                        //END OF AOL WEBMAIL CODE
                                        
                                    }
                                }
                                this.MainPacketHandler.ExtractMultipartFormData(formMultipartData, sourceHost, destinationHost, tcpPacket.ParentFrame.Timestamp, httpPacket.ParentFrame.FrameNumber, "TCP " + tcpPacket.SourcePort, "TCP " + tcpPacket.DestinationPort, ApplicationLayerProtocol.Http, cookieParams);
                            }
                        }
                    }
                }

                
            }
            else {//reply
                if(httpPacket.ServerBanner!=null && httpPacket.ServerBanner.Length>0)
                    sourceHost.AddHttpServerBanner(httpPacket.ServerBanner, tcpPacket.SourcePort);
                if(httpPacket.WwwAuthenticateRealm!=null && httpPacket.WwwAuthenticateRealm.Length>0) {
                    sourceHost.AddHostName(httpPacket.WwwAuthenticateRealm);
                    sourceHost.ExtraDetailsList["WWW-Authenticate realm"]=httpPacket.WwwAuthenticateRealm;
                }
                if(mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true)) {
                    FileTransfer.FileStreamAssembler assembler=mainPacketHandler.FileStreamAssemblerList.GetAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true);

                    //http://www.mail-archive.com/wireshark-dev@wireshark.org/msg08695.html
                    //There could also be no content-length when http-keepalives are not used.
                    //In that case, the client just collects all data till the TCP-FIN.
                    //-1 is set instead of null if Content-Length is not defined
                    if(httpPacket.ContentLength>=0 || httpPacket.ContentLength==-1) {
                        assembler.FileContentLength=httpPacket.ContentLength;
                        assembler.FileSegmentRemainingBytes=httpPacket.ContentLength;//we get the whole file in one segment (one serie of TCP packets)
                    }

                    if(httpPacket.ContentLength==0) {
                        mainPacketHandler.FileStreamAssemblerList.Remove(assembler, true);
                    }
                    else {
                        if(httpPacket.ContentDispositionFilename!=null) {
                            assembler.Filename=httpPacket.ContentDispositionFilename;
                        }
                        //append content type extention to file name
                        if(httpPacket.ContentType!=null && httpPacket.ContentType.Contains("/") && httpPacket.ContentType.IndexOf('/')<httpPacket.ContentType.Length-1) {
                            string extension=Utils.StringManglerUtil.GetExtension(httpPacket.ContentType);
                            /*
                            string extension=httpPacket.ContentType.Substring(httpPacket.ContentType.IndexOf('/')+1);
                            if(extension.Contains(";"))
                                extension=extension.Substring(0, extension.IndexOf(";"));
                             * */
                            if(extension.Length>0 &&
                                !assembler.Filename.EndsWith("."+extension, StringComparison.InvariantCultureIgnoreCase) &&
                                !(assembler.Filename.EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) && extension.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase)) &&
                                !(assembler.Filename.EndsWith("htm", StringComparison.InvariantCultureIgnoreCase) && extension.Equals("html", StringComparison.InvariantCultureIgnoreCase))) {
                                //append the content type as extension
                                assembler.Filename=assembler.Filename+"."+extension;
                            }
                        }

                        if(httpPacket.TransferEncoding=="chunked")
                            assembler.FileStreamType=FileTransfer.FileStreamTypes.HttpGetChunked;
                        if(httpPacket.ContentEncoding!=null && httpPacket.ContentEncoding.Length>0) {
                            if(httpPacket.ContentEncoding.Equals("gzip"))//I'll only care aboute gzip for now
                                assembler.ContentEncoding=Packets.HttpPacket.ContentEncodings.Gzip;
                            else if(httpPacket.ContentEncoding.Equals("deflate"))//http://tools.ietf.org/html/rfc1950
                                assembler.ContentEncoding=Packets.HttpPacket.ContentEncodings.Deflate;
                        }


                        if(assembler.TryActivate()) {
                            //the assembler is now ready to receive data

                            if(httpPacket.MessageBody!=null && httpPacket.MessageBody.Length>0)
                                if(assembler.FileStreamType==FileTransfer.FileStreamTypes.HttpGetChunked || httpPacket.MessageBody.Length<=assembler.FileSegmentRemainingBytes || assembler.FileSegmentRemainingBytes==-1)
                                    assembler.AddData(httpPacket.MessageBody, tcpPacket.SequenceNumber);
                        }
                    }
                    //}
                    //else {
                    //    mainPacketHandler.FileStreamAssemblerList.Remove(assembler, true);
                    //}
                }

            }
            return true;
        }


        #endregion
    }
}
