using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.PacketHandlers {
    class SshPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler {

        public ApplicationLayerProtocol HandledProtocol {
            get { return ApplicationLayerProtocol.Ssh; }
        }

        public SshPacketHandler(PacketHandler mainPacketHandler)
            : base(mainPacketHandler) {
            //empty?
        }

        #region ITcpSessionPacketHandler Members

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<PacketParser.Packets.AbstractPacket> packetList) {
            foreach(Packets.AbstractPacket p in packetList) {
                if(p.GetType()==typeof(Packets.SshPacket)) {
                    Packets.SshPacket sshPacket=(Packets.SshPacket)p;
                    if(!sourceHost.ExtraDetailsList.ContainsKey("SSH Version"))
                        sourceHost.ExtraDetailsList.Add("SSH Version", sshPacket.SshVersion);
                    if(!sourceHost.ExtraDetailsList.ContainsKey("SSH Application"))
                        sourceHost.ExtraDetailsList.Add("SSH Application", sshPacket.SshApplication);
                    return p.PacketLength;
                }
            }
            return 0;
        }

        public void Reset() {
            //do nothgin; no state...
        }

        #endregion
    }
}
