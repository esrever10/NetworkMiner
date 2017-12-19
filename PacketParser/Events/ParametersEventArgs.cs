using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Events {
    public class ParametersEventArgs : EventArgs {

        public int FrameNumber;
        public NetworkHost SourceHost, DestinationHost;
        public string SourcePort, DestinationPort;
        public System.Collections.Specialized.NameValueCollection Parameters;
        public DateTime Timestamp;
        public string Details;

        public ParametersEventArgs(int frameNumber, NetworkHost sourceHost, NetworkHost destinationHost, string sourcePort, string destinationPort, IEnumerable<KeyValuePair<string,string>> parameters, DateTime timestamp, string details) {
            this.FrameNumber = frameNumber;
            this.SourceHost = sourceHost;
            this.DestinationHost = destinationHost;
            this.SourcePort = sourcePort;
            this.DestinationPort = destinationPort;
            this.Parameters = new System.Collections.Specialized.NameValueCollection();
            foreach (KeyValuePair<string, string> kvp in parameters)
                this.Parameters.Add(kvp.Key, kvp.Value);
            this.Timestamp = timestamp;
            this.Details = details;
        }

        public ParametersEventArgs(int frameNumber, NetworkHost sourceHost, NetworkHost destinationHost, string sourcePort, string destinationPort, System.Collections.Specialized.NameValueCollection parameters, DateTime timestamp, string details) {
            this.FrameNumber=frameNumber;
            this.SourceHost=sourceHost;
            this.DestinationHost=destinationHost;
            this.SourcePort=sourcePort;
            this.DestinationPort=destinationPort;
            this.Parameters=parameters;
            this.Timestamp=timestamp;
            this.Details=details;
        }

    }
}
