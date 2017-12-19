using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser {
    public class PortProtocolFinderFactory : ISessionProtocolFinderFactory {

        private PacketHandler packetHandler;

        public PortProtocolFinderFactory(PacketHandler packetHandler) {
            this.packetHandler = packetHandler;
        }

        public ISessionProtocolFinder CreateProtocolFinder(NetworkHost client, NetworkHost server, ushort clientPort, ushort serverPort, bool tcpTransport, int startFrameNumber, DateTime startTimestamp) {
            if(tcpTransport)
                return new TcpPortProtocolFinder(client, server, clientPort, serverPort, startFrameNumber, startTimestamp, this.packetHandler);
            else
                throw new Exception("UDP protocol finder is not implemented yet");
        }

    }
}
