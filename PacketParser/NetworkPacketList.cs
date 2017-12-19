//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace PacketParser {
    public class NetworkPacketList : System.Collections.Generic.List<NetworkPacket>{
        private int totalBytes;
        private int payloadBytes;
        private int cleartextBytes;
        
        //private System.Collections.Generic.Dictionary<ushort, int> sentTcpPacketsFromPort, sentUdpPacketsFromPort;
        //private System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ushort,ushort>> tcpPortPairs;

        //private System.Collections.Generic.SortedDictionary<int, IPAddress> destinationIpAddressList;
        //private System.Collections.Generic.SortedDictionary<int, IPAddress> sourceIpAddressList;

        public int TotalBytes { get { return this.totalBytes; } }
        public int PayloadBytes { get { return this.payloadBytes; } }
        public int CleartextBytes { get { return this.cleartextBytes; } }
        public double CleartextProcentage {
            get {
                if(cleartextBytes>0)
                    return (1.0*cleartextBytes)/payloadBytes;
                else
                    return 0.0;
            }
        }


        //public IList<IPAddress> DestinationIpList { get { return this.destinationIpAddressList.Values; } }
        //public IList<IPAddress> SourceIpList { get { return this.sourceIpAddressList.Values; } }


        public NetworkPacketList() : base(){
            //this.destinationIpAddressList=new SortedList<int, IPAddress>();
            //this.sourceIpAddressList=new SortedList<int, IPAddress>();
            //this.tcpPortPairs=new List<KeyValuePair<ushort, ushort>>();
        }

        public override string ToString() {
            return this.Count+" packets ("+this.TotalBytes.ToString("n0")+" Bytes), "+this.CleartextProcentage.ToString("p")+" cleartext ("+this.CleartextBytes.ToString("n0")+" of "+this.PayloadBytes.ToString("n0")+" Bytes)";
        }

        new public void AddRange(IEnumerable<NetworkPacket> collection) {
            foreach(NetworkPacket p in collection)
                Add(p);
        }
        new public void Add(NetworkPacket packet){
            base.Add(packet);
            this.totalBytes+=packet.PacketBytes;
            this.payloadBytes+=packet.PayloadBytes;
            this.cleartextBytes+=packet.CleartextBytes;

        }

        public NetworkPacketList GetSubset(System.Net.IPAddress sourceIp, System.Net.IPAddress destinationIp) {
            NetworkPacketList list=new NetworkPacketList();
            foreach(NetworkPacket p in this) {
                if(p.SourceHost.IPAddress.Equals(sourceIp) && p.DestinationHost.IPAddress.Equals(destinationIp))
                    list.Add(p);
            }
            return list;
        }
        public NetworkPacketList GetSubset(System.Net.IPAddress sourceIp, ushort? sourceTcpPort, System.Net.IPAddress destinationIp, ushort? destinationTcpPort) {
            NetworkPacketList list=new NetworkPacketList();
            foreach(NetworkPacket p in this) {
                if(p.SourceHost.IPAddress.Equals(sourceIp) && p.DestinationHost.IPAddress.Equals(destinationIp))
                    if(p.SourceTcpPort==sourceTcpPort && p.DestinationTcpPort==destinationTcpPort)
                        list.Add(p);
            }
            return list;
        }


        //private System.Collections.Generic.Dictionary<ushort, int> sentTcpPacketsFromPort, sentUdpPacketsFromPort;
        //ICollection<KeyValuePair<KeyValuePair<ushort, ushort>, NetworkPacketList>> GetSubsetPerPortPair() {
        public ICollection<KeyValuePair<ushort[], NetworkPacketList>> GetSubsetPerTcpPortPair() {

            //Dictionary<ushort, int> dictionary=new Dictionary<ushort, int>();
            Dictionary<uint, NetworkPacketList> dictionary=new Dictionary<uint,NetworkPacketList>();


            foreach(NetworkPacket p in this) {
                if(p.SourceTcpPort!=null && p.DestinationTcpPort!=null){
                    uint portKey = Utils.ByteConverter.ToUInt32((ushort)p.SourceTcpPort, (ushort)p.DestinationTcpPort);
                    if(dictionary.ContainsKey(portKey))
                        dictionary[portKey].Add(p);
                    else{
                        dictionary.Add(portKey, new NetworkPacketList());
                        dictionary[portKey].Add(p);
                    }
                }
            }
            //we must now convert the list to something more appropriate to return.
            List<KeyValuePair<ushort[], NetworkPacketList>> returnList=new List<KeyValuePair<ushort[],NetworkPacketList>>();
            foreach(uint portKey in dictionary.Keys){
                ushort[] ports=new ushort[2];
                ports[0]=(ushort)(portKey>>16);//source port
                ports[1]=(ushort)(portKey&0xffff);//destination port (mask last 16 bits)
                returnList.Add(new KeyValuePair<ushort[],NetworkPacketList>(ports, dictionary[portKey]));
            }
            return (ICollection<KeyValuePair<ushort[], NetworkPacketList>>)returnList;
        }

        public ICollection<KeyValuePair<ushort[], NetworkPacketList>> GetSubsetPerUdpPortPair() {
            Dictionary<uint, NetworkPacketList> dictionary=new Dictionary<uint, NetworkPacketList>();
            foreach(NetworkPacket p in this) {
                if(p.SourceUdpPort!=null && p.DestinationUdpPort!=null) {
                    uint portKey = Utils.ByteConverter.ToUInt32((ushort)p.SourceUdpPort, (ushort)p.DestinationUdpPort);
                    if(dictionary.ContainsKey(portKey))
                        dictionary[portKey].Add(p);
                    else {
                        dictionary.Add(portKey, new NetworkPacketList());
                        dictionary[portKey].Add(p);
                    }
                }
            }
            //we must now convert the list to something more appropriate to return.
            List<KeyValuePair<ushort[], NetworkPacketList>> returnList=new List<KeyValuePair<ushort[], NetworkPacketList>>();
            foreach(uint portKey in dictionary.Keys) {
                ushort[] ports=new ushort[2];
                ports[0]=(ushort)(portKey>>16);//source port
                ports[1]=(ushort)(portKey&0xffff);//destination port (mask last 16 bits)
                returnList.Add(new KeyValuePair<ushort[], NetworkPacketList>(ports, dictionary[portKey]));
            }
            return (ICollection<KeyValuePair<ushort[], NetworkPacketList>>)returnList;
        }


    }
}
