//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Packets {

    //http://en.wikipedia.org/wiki/Http
    //http://tools.ietf.org/html/rfc2616
    //http://tools.ietf.org/html/rfc2617 (HTTP Authentication)
    class HttpPacket : AbstractPacket, ISessionPacket{
        internal enum RequestMethods { GET, HEAD, POST, PUT, DELETE, TRACE, OPTIONS, CONNECT, none }
        internal enum ContentEncodings { Gzip, Compress, Deflate, Identity }//Identity is default

        //general variables
        private bool messageTypeIsRequest;
        private List<string> headerFields;
        private byte[] messageBody;//the content

        //request variables
        private RequestMethods requestMethod;//this one is "none" unless the message is a request message
        private string requestedHost;
        private string requestedFileName;
        private string userAgentBanner;//client or requester web browser or spider. See: http://en.wikipedia.org/wiki/User_agent

        //reply variables
        private string statusCode;//3 digit status number, if first digit is 2 then it is "OK"
        private string serverBanner;//server or reply web server. See: http://www.blackhat.com/presentations/bh-asia-02/bh-asia-02-grossman.pdf or http://www.blackhat.com/presentations/bh-usa-03/bh-us-03-shah/bh-us-03-shah.ppt
        private string contentType;
        private int contentLength;//defined in # bytes for "Content-Lenght" as in RFC2616
        private string contentEncoding;//this could be for example GZIP, see: http://www.faqs.org/rfcs/rfc1952.html
        private string cookie;
        private string transferEncoding;//if encoding is "chunked" (such as for www.ripe.net) I will have to deal with: http://tools.ietf.org/html/rfc2616#section-3.6.1 Why can't people simply rely on TCP sequencing???
        private string wwwAuthenticateRealm;//Used to be wwwAuthenticateBasicRealm
        private string authorizationCredentialsUsername;
        private string authorizationCredentailsPassword;

        private string contentDispositionFilename;

        //ISessionPacket
        private bool packetHeaderIsComplete;//the main reason for using this variable is because HTTP POST's often are splitted into two packets right at the start of Content-Type definition

        internal bool MessageTypeIsRequest { get { return this.messageTypeIsRequest; } }
        internal RequestMethods RequestMethod { get { return this.requestMethod; } }
        internal string RequestedHost { get { return requestedHost; } }
        internal string RequestedFileName { get { return this.requestedFileName; } }
        internal string UserAgentBanner { get { return this.userAgentBanner; } }
        internal string StatusCode { get { return this.statusCode; } }
        internal string ServerBanner { get { return this.serverBanner; } }
        internal string ContentType { get { return this.contentType; } }
        internal int ContentLength { get { return this.contentLength; } }
        internal string ContentEncoding { get { return this.contentEncoding; } }
        internal string Cookie { get { return this.cookie; } }
        internal string TransferEncoding { get { return this.transferEncoding; } }
        internal List<string> HeaderFields { get { return this.headerFields; } }
        internal string WwwAuthenticateRealm { get { return this.wwwAuthenticateRealm; } }//Used to be WwwAuthenticateBasicRealm
        internal string AuthorizationCredentialsUsername { get { return this.authorizationCredentialsUsername; } }
        internal string AuthorizationCredentialsPassword { get { return this.authorizationCredentailsPassword; } }

        internal string ContentDispositionFilename { get { return this.contentDispositionFilename; } }

        internal byte[] MessageBody { get { return this.messageBody; } }
        /*
        public override 
            LÄGG TILL EN PACKET FACTORY TILL VARJE PAKET_KLASS OCH LÅT DEN GÖRA EN ENKEL KOLL ATT PAKETET KAN PARSAS!
        GÖR SJÄLVA KONSTRUKTORN PRIVAT!
        */
        new public static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result) {
            result=null;

            //start testing
            int dataIndex=packetStartIndex;
            string startLine = Utils.ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
            if(startLine==null)
                return false;
            else if(startLine.Length>2048)
                return false;
            else if(!(startLine.StartsWith("GET") || startLine.StartsWith("HEAD") || startLine.StartsWith("POST") || startLine.StartsWith("PUT") || startLine.StartsWith("DELETE") || startLine.StartsWith("TRACE") || startLine.StartsWith("OPTIONS") || startLine.StartsWith("CONNECT") || startLine.StartsWith("HTTP"))) {
                return false;
            }


            try {
                result=new HttpPacket(parentFrame, packetStartIndex, packetEndIndex);
            }
            catch {
                result=null;
            }


            if(result==null)
                return false;
            else
                return true;
        }

        private HttpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex)
            : base(parentFrame, packetStartIndex, packetEndIndex, "HTTP") {
            /*
             * 
             *         generic-message = start-line
             *             *(message-header CRLF)
             *             CRLF
             *             [ message-body ]
             * 
             *         start-line      = Request-Line | Status-Line
             * 
             * */
            this.headerFields=new List<string>();
            this.requestedHost=null;
            this.requestedFileName=null;
            this.userAgentBanner=null;
            this.statusCode=null;
            this.serverBanner=null;
            this.contentType=null;
            this.contentLength=-1;//instead of null
            this.contentEncoding=null;
            this.cookie=null;
            this.transferEncoding=null;
            this.wwwAuthenticateRealm=null;
            this.authorizationCredentialsUsername=null;
            this.authorizationCredentailsPassword=null;
            this.packetHeaderIsComplete=false;
            this.contentDispositionFilename=null;

            int dataIndex=packetStartIndex;

            //a start-line
            string startLine = Utils.ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
            if(startLine==null)
                throw new Exception("HTTP packet does not contain a valid start line. Probably a false HTTP positive");
            if(startLine.Length>2048)
                throw new Exception("HTTP start line is longer than 255 bytes. Probably a false HTTP positive");

            if(dataIndex>packetEndIndex)
                throw new Exception("HTTP start line ends after packet end...");

            if(startLine.StartsWith("GET")) {
                this.messageTypeIsRequest=true;
                this.requestMethod=RequestMethods.GET;
            }
            else if(startLine.StartsWith("HEAD")) {
                this.messageTypeIsRequest=true;
                this.requestMethod=RequestMethods.HEAD;
            }
            else if(startLine.StartsWith("POST")) {
                this.messageTypeIsRequest=true;
                this.requestMethod=RequestMethods.POST;
            }
            else if(startLine.StartsWith("PUT")) {
                this.messageTypeIsRequest=true;
                this.requestMethod=RequestMethods.PUT;
            }
            else if(startLine.StartsWith("DELETE")) {
                this.messageTypeIsRequest=true;
                this.requestMethod=RequestMethods.DELETE;
            }
            else if(startLine.StartsWith("TRACE")) {
                this.messageTypeIsRequest=true;
                this.requestMethod=RequestMethods.TRACE;
            }
            else if(startLine.StartsWith("OPTIONS")) {
                this.messageTypeIsRequest=true;
                this.requestMethod=RequestMethods.OPTIONS;
            }
            else if(startLine.StartsWith("CONNECT")) {
                this.messageTypeIsRequest=true;
                this.requestMethod=RequestMethods.CONNECT;
            }
            else if(startLine.StartsWith("HTTP")) {
                this.messageTypeIsRequest=false;
                this.requestMethod=RequestMethods.none;
            }
            else
                throw new Exception("Incorrect HTTP Message Type or Request Method");
         
            //zero or more header fields (also known as "headers")
            while(true){
                string headerLine = Utils.ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
                if(headerLine==null)
                    break;//this.packetHeaderIsComplete will NOT be true!
                else if(headerLine.Length>0) {
                    this.headerFields.Add(headerLine);
                    ExtractHeaderField(headerLine);
                }
                else {//headerLine.Length==0
                    this.packetHeaderIsComplete=true;//the header is complete and that's enough
                    break;//the for loop should stop now...
                }
            }

            //possibly a message-body
            if(this.packetHeaderIsComplete && dataIndex<packetEndIndex) {//we have a body!
                this.messageBody=new byte[packetEndIndex-dataIndex+1];
                Array.Copy(parentFrame.Data, dataIndex, this.messageBody, 0, this.messageBody.Length);
                /*
                for(int i=0; i<this.messageBody.Length; i++)
                    this.messageBody[i]=parentFrame.Data[dataIndex+i];
                 * */
            }
            else {
                this.messageBody=null;
            }


            //now extract some interresting information from the packet
            if(this.messageTypeIsRequest) {//REQUEST
                if(this.requestMethod==RequestMethods.GET) {
                    string fileURI=startLine.Substring(4, startLine.Length-4);
                    if(fileURI.Contains(" HTTP")) {
                        fileURI=fileURI.Substring(0, fileURI.IndexOf(" HTTP"));
                    }
                    if(fileURI.Length>0) {//If it is the index-file the URI will be just "/"
                        this.requestedFileName=fileURI;
                    }
                    else
                        this.requestedFileName=null;
                }
                else if(this.requestMethod==RequestMethods.POST) {
                    string fileURI=startLine.Substring(5, startLine.Length-5);
                    if(fileURI.Contains(" HTTP")) {
                        fileURI=fileURI.Substring(0, fileURI.IndexOf(" HTTP"));
                    }
                    if(fileURI.Length>0) {//If it is the index-file the URI will be just "/"
                        this.requestedFileName=fileURI;
                    }
                }
            }
            else {//REPLY
                if(startLine.StartsWith("HTTP/1.")) {
                    this.statusCode=startLine.Substring(9, 3);
                }
            }
        }

        private void ExtractHeaderField(string headerField) {
            if(headerField.StartsWith("Host: ")) {//look for the host
                this.requestedHost=headerField.Substring(6);
                if (!this.ParentFrame.QuickParse)
                    base.Attributes.Add("Requested Host", headerField.Substring(6));
            }
            else if(headerField.StartsWith("User-Agent: ", StringComparison.OrdinalIgnoreCase)) {
                this.userAgentBanner=headerField.Substring(12);
                if (!this.ParentFrame.QuickParse)
                    base.Attributes.Add("User-Agent", this.userAgentBanner=headerField.Substring(12));
            }
            else if(headerField.StartsWith("Server: ", StringComparison.OrdinalIgnoreCase)) {
                this.serverBanner=headerField.Substring(8);
                if (!this.ParentFrame.QuickParse)
                    this.Attributes.Add("Server banner", this.serverBanner=headerField.Substring(8));
            }
            else if(headerField.StartsWith("Cookie: ", StringComparison.OrdinalIgnoreCase)) {
                //http://www.w3.org/Protocols/rfc2109/rfc2109
                this.cookie=headerField.Substring(8);
                if (!this.ParentFrame.QuickParse)
                    this.Attributes.Add("Cookie", this.cookie);
            }
            else if(headerField.StartsWith("Content-Type: ", StringComparison.OrdinalIgnoreCase))
                this.contentType=headerField.Substring(14);
            else if(headerField.StartsWith("Content-Length: ", StringComparison.OrdinalIgnoreCase))
                this.contentLength=Convert.ToInt32(headerField.Substring(16));
            else if(headerField.StartsWith("Content-Encoding: ", StringComparison.OrdinalIgnoreCase))
                this.contentEncoding=headerField.Substring(18);
            else if(headerField.StartsWith("Transfer-Encoding: ", StringComparison.OrdinalIgnoreCase))
                this.transferEncoding=headerField.Substring(19);
            else if(headerField.StartsWith("WWW-Authenticate: ", StringComparison.OrdinalIgnoreCase) && headerField.Contains("realm=\"")) {
                int realmStart=headerField.IndexOf("realm=\"")+7;
                int realmEnd=headerField.IndexOf('\"', realmStart);
                if(realmStart>=0 && realmEnd>0)
                    this.wwwAuthenticateRealm=headerField.Substring(realmStart, realmEnd-realmStart);
            }
            /*
            else if(headerField.StartsWith("WWW-Authenticate: Basic realm=", StringComparison.OrdinalIgnoreCase))
                this.wwwAuth0enticateBasicRealm=headerField.Substring(31, headerField.Length-32);
             * */
            else if(headerField.StartsWith("Proxy-Authenticate: Basic realm=", StringComparison.OrdinalIgnoreCase))
                this.wwwAuthenticateRealm=headerField.Substring(33, headerField.Length-34);
            else if(headerField.StartsWith("Authorization: Basic ", StringComparison.OrdinalIgnoreCase)) {
                try {
                    string base64string=headerField.Substring(21);
                    Byte[] bArray=Convert.FromBase64String(base64string);
                    StringBuilder sb=new StringBuilder(bArray.Length);
                    foreach(byte b in bArray)
                        sb.Append((char)b);
                    //string s=System.Text.Encoding.Unicode.GetString(bArray);
                    string s=sb.ToString();
                    if(s.Contains(":")) {
                        this.authorizationCredentialsUsername=s.Substring(0, s.IndexOf(':'));
                        if(s.IndexOf(':')+1<s.Length)
                            this.authorizationCredentailsPassword=s.Substring(s.IndexOf(':')+1);
                        else
                            this.authorizationCredentailsPassword="";
                    }
                }
                catch(Exception e) {
                    if (!this.ParentFrame.QuickParse)
                        this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, PacketStartIndex, this.PacketEndIndex, "Cannot parse credentials in HTTP Authorization ("+e.Message+")"));
                }
            }
            else if (headerField.StartsWith("Authorization: Digest ", StringComparison.OrdinalIgnoreCase)) {
                try {
                    string authorizationString = headerField.Substring(22);
                    foreach (string keyValueString in authorizationString.Split(new char[] { ',' })) {
                        //username="fmeyer"
                        string[] parts = keyValueString.Split(new char[] { '=' });
                        if (parts.Length == 2) {
                            /**
                             *         private string wwwAuthenticateRealm;//Used to be wwwAuthenticateBasicRealm
                             *         private string authorizationCredentialsUsername;
                             *         private string authorizationCredentailsPassword;
                             **/
                            string name = parts[0].Trim();
                            string value = parts[1].Trim(new char[] { ' ', '\"', '\'' });
                            if (name.Equals("username", StringComparison.InvariantCultureIgnoreCase)) {
                                this.authorizationCredentialsUsername = value;
                                if(this.authorizationCredentailsPassword == null)
                                    this.authorizationCredentailsPassword = "N/A";
                            }
                            else if (name.Equals("realm", StringComparison.InvariantCultureIgnoreCase))
                                this.wwwAuthenticateRealm = value;
                        }
                    }
                }
                catch (Exception e) {
                    if (!this.ParentFrame.QuickParse)
                        this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, PacketStartIndex, this.PacketEndIndex, "Cannot parse credentials in HTTP Authorization (" + e.Message + ")"));
                }
            }
            else if(headerField.StartsWith("Content-Disposition:") && headerField.Contains("filename=")){
                string filename=headerField.Substring(headerField.IndexOf("filename=")+9);
                filename=filename.Trim();
                if(filename.StartsWith("\"") && filename.IndexOf('\"', 1)>0)//get the string inside the quotations
                    filename=filename.Substring(1, filename.IndexOf('\"', 1)-1);
                if(filename.Length>0)
                    this.contentDispositionFilename=filename;
            }
        }

        private System.Collections.Specialized.NameValueCollection GetUrlEncodedNameValueCollection(string urlEncodedData, bool isFormPostData) {
            System.Collections.Specialized.NameValueCollection returnCollection=new System.Collections.Specialized.NameValueCollection();
            char[] separator1={ '&' };
            char[] separator2={ '=' };
            //string messageBody=ByteConverter.ReadString(this.messageBody);
            string data = System.Web.HttpUtility.UrlDecode(urlEncodedData);
            ICollection<string> formNameValues = data.Split(separator1);
            if (isFormPostData) {
                List<string> mergedNameValues = new List<string>();
                bool lastValueCompleted = true;
                foreach (string formNameValue in formNameValues) {
                    if (lastValueCompleted) {
                        mergedNameValues.Add(formNameValue);
                        if (formNameValue.Contains("=[{") && !formNameValue.EndsWith("}]"))
                            lastValueCompleted = false;//we need to read until the end bracket before splitting again
                    }
                    else {
                        //rebuild the split data
                        mergedNameValues[mergedNameValues.Count - 1] = mergedNameValues[mergedNameValues.Count - 1] + separator1 + formNameValue;
                        if (formNameValue.EndsWith("}]"))
                            lastValueCompleted = true;
                    }
                }
                formNameValues = mergedNameValues;
            }
            foreach (string formNameValue in formNameValues) {
                if(formNameValue.Length>0) {
                    int eqIndex=formNameValue.IndexOf('=');
                    if(eqIndex>0 && eqIndex<formNameValue.Length-1) {
                        string controlName=System.Web.HttpUtility.UrlDecode(formNameValue.Substring(0, eqIndex));
                        string formValue=System.Web.HttpUtility.UrlDecode(formNameValue.Substring(eqIndex+1));

                        returnCollection.Add(controlName, formValue);
                    }
                }
            }
            return returnCollection;
        }

        /// <summary>
        /// Returns the names and values of the parameters (variables) passed in the querystring
        /// </summary>
        /// <returns></returns>
        internal System.Collections.Specialized.NameValueCollection GetQuerystringData() {
            if(requestedFileName!=null && requestedFileName.Contains("?"))
                return GetUrlEncodedNameValueCollection(requestedFileName.Substring(requestedFileName.IndexOf('?')+1), false);
            else
                return null;
        }

        //http://www.w3.org/TR/html4/interact/forms.html#h-17.13.4
        //internal System.Collections.Specialized.NameValueCollection GetFormData() {
        internal System.Collections.Generic.List<Mime.MultipartPart> GetFormData() {
            System.Collections.Generic.List<Mime.MultipartPart> returnMultiPartData=new List<Mime.MultipartPart>();

            if(this.RequestMethod!=RequestMethods.POST || this.messageBody==null || this.messageBody.Length<=0 || this.contentType==null)
                return returnMultiPartData;
            else if(this.contentType.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("application/x-www-form-urlencoded")) {
                Mime.MultipartPart mimeMultipart = new Mime.MultipartPart(GetUrlEncodedNameValueCollection(Utils.ByteConverter.ReadString(this.messageBody), true));

                returnMultiPartData.Add(mimeMultipart);
                return returnMultiPartData;
            }
            else if(this.contentType.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("multipart/form-data")) {
                //http://www.ietf.org/rfc/rfc2388.txt
                //http://www.w3.org/TR/html4/interact/forms.html#h-17.13.4
                //wireshark: \epan\dissectors\packet-multipart.c
                //The content "multipart/form-data" follows the rules of all multipart MIME data streams as outlined in [RFC2045]. 
                //see if there is a boundary in the Content-Type definition

                string contentTypeEnding=contentType.Substring(21);
                if(contentTypeEnding.StartsWith("boundary=")) {
                    string boundary=contentTypeEnding.Substring(9);
                    foreach(Mime.MultipartPart part in Mime.PartBuilder.GetParts(this.messageBody, boundary))
                        returnMultiPartData.Add(part);
                    return returnMultiPartData;
                    

                }

            }
            return null;//we failed to get the data
        }

        internal bool ContentIsComplete() {
            if(this.contentLength==0)
                return true;
            if(this.messageBody==null)
                return false;
            return this.messageBody.Length>=this.contentLength;
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference) {
            if(includeSelfReference)
                yield return this;
            //Do nothing, no known sub packets... well or can I define HTTP data chunks as a sub packet?
            yield break;
        }

        #region ISessionPacket Members

        public bool PacketHeaderIsComplete {
            get { return this.packetHeaderIsComplete; }
        }

        public int ParsedBytesCount { get { return base.PacketLength; } }

        #endregion

    }
}
