using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Events {
    public class DnsRecordEventArgs : EventArgs {

        public Packets.DnsPacket.IDnsResponseInfo Record;
        public NetworkHost DnsServer, DnsClient;
        public Packets.IPv4Packet IpPakcet;
        public Packets.UdpPacket UdpPacket;
        //internal void ShowDnsRecord(Packets.DnsPacket.ResourceRecord record, NetworkHost dnsServer, NetworkHost dnsClient, Packets.IPv4Packet ipPakcet, Packets.UdpPacket udpPacket) {
        public DnsRecordEventArgs(Packets.DnsPacket.IDnsResponseInfo record, NetworkHost dnsServer, NetworkHost dnsClient, Packets.IPv4Packet ipPakcet, Packets.UdpPacket udpPacket) {
            this.Record = record;
            this.DnsServer = dnsServer;
            this.DnsClient = dnsClient;
            this.IpPakcet = ipPakcet;
            this.UdpPacket = udpPacket;
        }
    }
}
