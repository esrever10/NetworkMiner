using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Events {
    public class SessionEventArgs : EventArgs {
        //public NetworkTcpSession NetworkTcpSession;

        public PacketParser.ApplicationLayerProtocol Protocol;
        public NetworkHost Client;
        public NetworkHost Server;
        public ushort ClientPort;
        public ushort ServerPort;
        public bool Tcp;
        public int StartFrameNumber;
        public DateTime StartTimestamp;
        

        public SessionEventArgs(PacketParser.ApplicationLayerProtocol protocol, NetworkHost client, NetworkHost server, ushort clientPort, ushort serverPort, bool tcp, int startFrameNumber, DateTime startTimestamp) {
            this.Protocol = protocol;
            this.Client = client;
            this.Server = server;
            this.ClientPort = clientPort;
            this.ServerPort = serverPort;
            this.Tcp = tcp;
            this.StartFrameNumber = startFrameNumber;
            this.StartTimestamp = startTimestamp;
        }
        /*
        public SessionEventArgs(NetworkTcpSession networkTcpSession) {
            this.NetworkTcpSession = networkTcpSession;
        }*/

    }
}
