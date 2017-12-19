using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Events {
    public class MessageEventArgs : EventArgs {
        private const int MAX_SUBJECT_LENGTH = 50;

        public PacketParser.ApplicationLayerProtocol Protocol;
        public NetworkHost SourceHost;
        public NetworkHost DestinationHost;
        public int StartFrameNumber;
        public DateTime StartTimestamp;

        public string From;
        public string To;
        public string Subject;
        public string Message;
        public System.Collections.Specialized.NameValueCollection Attributes;

        public MessageEventArgs(PacketParser.ApplicationLayerProtocol protocol, NetworkHost sourceHost, NetworkHost destinationHost, int startFrameNumber, DateTime startTimestamp, string from, string to, string subject, string message, System.Collections.Specialized.NameValueCollection attributes) {
            this.Protocol = protocol;
            this.SourceHost = sourceHost;
            this.DestinationHost = destinationHost;
            this.StartFrameNumber = startFrameNumber;
            this.StartTimestamp = startTimestamp;
            this.From = from;
            this.To = to;
            this.Subject = subject;
            if(this.Subject != null && this.Subject.Length > MAX_SUBJECT_LENGTH)
                this.Subject = this.Subject.Substring(0, MAX_SUBJECT_LENGTH)+"...";
            this.Message = message;
            this.Attributes = attributes;
        }
    }
}
