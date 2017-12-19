using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser {
    public interface ISessionProtocolFinderFactory {
        ISessionProtocolFinder CreateProtocolFinder(PacketParser.NetworkHost client, PacketParser.NetworkHost server, ushort clientPort, ushort serverPort, bool tcpTransport, int startFrameNumber, DateTime startTimestamp);
    }
}
