using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Events {
    public class KeywordEventArgs {

        public Frame Frame;
        public int KeywordIndex, KeywordLength;
        public NetworkHost SourceHost, DestinationHost;
        public string SourcePort, DestinationPort;

        public KeywordEventArgs(Frame frame, int keywordIndex, int keywordLength, NetworkHost sourceHost, NetworkHost destinationHost, string sourcePort, string destinationPort) {
            this.Frame=frame;
            this.KeywordIndex=keywordIndex;
            this.KeywordLength=keywordLength;
            this.SourceHost=sourceHost;
            this.DestinationHost=destinationHost;
            this.SourcePort=sourcePort;
            this.DestinationPort=destinationPort;
        }
    
    }
}
