//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;

namespace PacketParser {
    public class NetworkHost : IComparable{
        public enum OperatingSystemID { Windows, Linux, UNIX, FreeBSD, NetBSD, Solaris, MacOS, Cisco, Other, Unknown }

        #region Comparators for extended sorting
        public class MacAddressComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                string xMac="";
                if(x.MacAddress!=null)
                    xMac=x.MacAddress.ToString();
                string yMac="";
                if(y.MacAddress!=null)
                    yMac=y.MacAddress.ToString();

                return String.Compare(xMac, yMac);
            }
        }
        public class HostNameComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                return String.Compare(x.HostName, y.HostName);
            }
        }
        public class SentPacketsComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                return y.SentPackets.Count-x.SentPackets.Count;
            }
        }
        public class ReceivedPacketsComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                return y.ReceivedPackets.Count-x.ReceivedPackets.Count;
            }
        }
        public class SentBytesComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                return y.SentPackets.TotalBytes-x.SentPackets.TotalBytes;
            }
        }
        public class ReceivedBytesComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                return y.ReceivedPackets.TotalBytes-x.ReceivedPackets.TotalBytes;
            }
        }
        public class OpenTcpPortsCountComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                return y.OpenTcpPorts.Length-x.OpenTcpPorts.Length;
            }
        }
        public class OperatingSystemComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                return String.Compare(x.OS.ToString(), y.OS.ToString());
            }
        }
        public class TimeToLiveDistanceComparer : System.Collections.Generic.IComparer<NetworkHost> {

            public int Compare(NetworkHost x, NetworkHost y) {
                //make sure large senders are sorted first
                return x.TtlDistance-y.TtlDistance;
            }
        }
        #endregion

        #region Basic Private Data
        private IPAddress ipAddress;
        private PhysicalAddress macAddress;//the MAC address the host is behind
        private PopularityList<string, PhysicalAddress> recentMacAdresses;
        private System.Collections.Generic.List<string> hostNameList;//DNS, NetBIOS, HTTP-GET-host-value or similar
        

        private System.Collections.Generic.List<ushort> openTcpPortList;//I shouldn't really need this since the networkServiceList has almost the same information
        

        private System.Collections.Generic.SortedList<byte, int> ttlCount;//TTL values from host packets
        private System.Collections.Generic.SortedList<byte, int> ttlDistanceCount;//the guessed distance to the host

        private System.Collections.Generic.SortedList<string, System.Collections.Generic.SortedList<string, double>> operatingSystemCounterList;
        private NetworkPacketList sentPackets, receivedPackets;

        private List<NetworkTcpSession> incomingSessionList, outgoingSessionList;
        private SortedList<ushort, NetworkServiceMetadata> networkServiceMetadataList;//the key is identified by the TCP port number of the service (on the server)
        #endregion

        #region Extra (detailed) Private Data
        private List<IPAddress> queriedIpList;
        private List<string> domainNameList;
        private List<string> queriedNetBiosNameList;
        private List<string> queriedDnsNameList;
        private List<string> httpUserAgentBannerList;
        private List<string> httpServerBannerList;
        private List<string> ftpServerBannerList;
        private List<string> dhcpVendorCodeList;
        private SortedList<string, string> universalPlugAndPlayFieldList;

        private List<string> acceptedSmbDialectsList;
        private string preferredSmbDialect;

        private SortedList<string, string> extraDetailsList;//a simple list to store extra details about the host

        #endregion

        public PhysicalAddress MacAddress { 
            get { return this.macAddress; }
            set {
                this.macAddress=value;
                if(value != null)
                    this.recentMacAdresses.Add(value.ToString(), value);
            }
        }
        public IPAddress IPAddress { get { return this.ipAddress; } }
        //public string IPAddressString { get { return this.ipAddress.ToString(); } }
        public string HostName {
            get {
                StringBuilder sb=new StringBuilder("");
                foreach(string hostname in hostNameList)
                    sb.Append(hostname+", ");
                //remove the last ", "
                if(sb.Length>=2)
                    sb.Remove(sb.Length-2, 2);
                return sb.ToString();
            }
        }
        public List<string> HostNameList { get { return this.hostNameList; } }
        public ushort[] OpenTcpPorts { get { return openTcpPortList.ToArray(); } }

        public NetworkPacketList SentPackets { get { return sentPackets; } }
        public NetworkPacketList ReceivedPackets { get { return receivedPackets; } }
        public List<NetworkTcpSession> IncomingSessionList { get { return incomingSessionList; } }
        public List<NetworkTcpSession> OutgoingSessionList { get { return outgoingSessionList; } }
        public SortedList<ushort, NetworkServiceMetadata> NetworkServiceMetadataList { get { return networkServiceMetadataList; } }
        public SortedList<string, string> UniversalPlugAndPlayFieldList { get { return this.universalPlugAndPlayFieldList; } set { this.universalPlugAndPlayFieldList=value; } }

        public System.Collections.Specialized.NameValueCollection HostDetailCollection { 
            get{
                System.Collections.Specialized.NameValueCollection details=new System.Collections.Specialized.NameValueCollection();
                if(this.queriedIpList.Count>0) {
                    StringBuilder queriedIPs=new StringBuilder();
                    foreach(IPAddress ip in this.queriedIpList) {
                        details.Add("Queried IP Addresses", ip.ToString());
                    }
                }
                if(this.queriedNetBiosNameList.Count>0) {
                    StringBuilder queriedNames=new StringBuilder();
                    foreach(string name in this.queriedNetBiosNameList) {
                        details.Add("Queried NetBIOS names", name);
                    }
                }
                if(this.queriedDnsNameList.Count>0) {
                    StringBuilder queriedNames=new StringBuilder();
                    foreach(string name in this.queriedDnsNameList) {
                        details.Add("Queried DNS names", name);
                    }
                }
                for(int i=0; i<this.domainNameList.Count; i++)
                    details.Add("Domain Name "+(i+1), domainNameList[i]);
                for(int i=0; i<this.httpUserAgentBannerList.Count; i++)
                    details.Add("Web Browser User-Agent "+(i+1), httpUserAgentBannerList[i]);
                for(int i=0; i<this.httpServerBannerList.Count; i++)
                    details.Add("Web Server Banner "+(i+1), httpServerBannerList[i]);
                for(int i=0; i<this.ftpServerBannerList.Count; i++)
                    details.Add("FTP Server Banner "+(i+1), ftpServerBannerList[i]);
                for(int i=0; i<this.dhcpVendorCodeList.Count; i++)
                    details.Add("DHCP Vendor Code "+(i+1), dhcpVendorCodeList[i]);
                if(this.universalPlugAndPlayFieldList!=null)
                    foreach(string field in this.universalPlugAndPlayFieldList.Values) {
                        if(field.Contains(":")) {
                            details.Add("UPnP field : "+field.Substring(0,field.LastIndexOf(':')),field.Substring(field.LastIndexOf(':')+1));
                        }
                        else
                            details.Add("UPnP field", field);
                    }
                if(this.acceptedSmbDialectsList!=null)
                    foreach(string dialectName in acceptedSmbDialectsList)
                        details.Add("Accepted SMB dialects", dialectName);
                if(this.preferredSmbDialect!=null)
                    details.Add("Preferred SMB dialect", this.preferredSmbDialect);
                foreach(KeyValuePair<string, string> keyValue in extraDetailsList) {
                    details.Add(keyValue.Key, keyValue.Value);
                }
                return details;
                
            }
        }

        public bool IpIsMulticast {
            get{
                byte[] ip=this.ipAddress.GetAddressBytes();
                if(ip.Length==4){//let's start with IPv4
                    //http://en.wikipedia.org/wiki/Multicast_address
                    if(ip[0]>=224 && ip[0]<=239)
                        return true;
                }
                return false;
            }
        }
        public bool IpIsBroadcast {//this one is probabilistic, since we don't know the subnet mask
            get {
                if(this.sentPackets.Count==0) {
                    byte[] ip=this.ipAddress.GetAddressBytes();
                    //let's assume we need a subnet mask of 6 bits or more
                    byte mask=0x3f;
                    if((ip[ip.Length-1]&mask)==mask)
                        return true;
                }
                return false;
            }
        }
        public bool IpIsReserved {//IANA Reserved IP
            get {
                return Utils.IpAddressUtil.IsIanaReserved(this.ipAddress);
            }
        }
        public OperatingSystemID OS {//there is something called System.PlatformID, but it isn't good enough
            get{

                System.Collections.Generic.Dictionary<OperatingSystemID, double> myOsCount=new Dictionary<OperatingSystemID,double>();
                foreach(System.Collections.Generic.SortedList<string, double> operatingSystemCount in operatingSystemCounterList.Values) {
                    foreach(string os in operatingSystemCount.Keys) {
                        if(os.ToLower().Contains("windows")) {
                            if(!myOsCount.ContainsKey(OperatingSystemID.Windows))
                                myOsCount.Add(OperatingSystemID.Windows, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.Windows]+=operatingSystemCount[os];
                        }
                        else if(os.ToLower().Contains("linux")) {
                            if(!myOsCount.ContainsKey(OperatingSystemID.Linux))
                                myOsCount.Add(OperatingSystemID.Linux, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.Linux]+=operatingSystemCount[os];
                        }
                        else if(os.ToLower().Contains("unix")) {
                            if(!myOsCount.ContainsKey(OperatingSystemID.UNIX))
                                myOsCount.Add(OperatingSystemID.UNIX, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.UNIX]+=operatingSystemCount[os];
                        }
                        else if(os.ToLower().Contains("freebsd") || os.ToLower().Contains("free bsd")) {
                            if(!myOsCount.ContainsKey(OperatingSystemID.FreeBSD))
                                myOsCount.Add(OperatingSystemID.FreeBSD, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.FreeBSD]+=operatingSystemCount[os];
                        }
                        else if(os.ToLower().Contains("netbsd") || os.ToLower().Contains("net bsd")) {
                            if(!myOsCount.ContainsKey(OperatingSystemID.NetBSD))
                                myOsCount.Add(OperatingSystemID.NetBSD, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.NetBSD]+=operatingSystemCount[os];
                        }
                        else if(os.ToLower().Contains("solaris")) {
                            if(!myOsCount.ContainsKey(OperatingSystemID.Solaris))
                                myOsCount.Add(OperatingSystemID.Solaris, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.Solaris]+=operatingSystemCount[os];
                        }
                        else if (os.ToLower().Contains("macos") || os.ToLower().Contains("mac os") || os.ToLower().Contains("apple") || os.Contains("iOS")) {//Avoid confusion with Cisco IOS
                            if(!myOsCount.ContainsKey(OperatingSystemID.MacOS))
                                myOsCount.Add(OperatingSystemID.MacOS, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.MacOS]+=operatingSystemCount[os];
                        }
                        else if(os.ToLower().Contains("cisco") || os.Contains("IOS")) {//avoid confusion with Apple iOS by doing case sensitive comparison
                            if(!myOsCount.ContainsKey(OperatingSystemID.Cisco))
                                myOsCount.Add(OperatingSystemID.Cisco, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.Cisco]+=operatingSystemCount[os];
                        }
                        else {
                            if(!myOsCount.ContainsKey(OperatingSystemID.Other))
                                myOsCount.Add(OperatingSystemID.Other, operatingSystemCount[os]);
                            else
                                myOsCount[OperatingSystemID.Other]+=operatingSystemCount[os];
                        }
                    }
                }
                //we now have counted all the OS's and want to get the one with best count
                OperatingSystemID probableOS=OperatingSystemID.Unknown;
                double bestOsCount=0.0;
                foreach(OperatingSystemID os in myOsCount.Keys)
                    if(myOsCount[os]>bestOsCount) {
                        probableOS=os;
                        bestOsCount=myOsCount[os];
                    }
                return probableOS;
            }
        }

        public IList<string> OsCounterNames {
            get {
                return this.operatingSystemCounterList.Keys;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="osCounterName">for example "p0f" or "Ettercap"</param>
        /// <returns></returns>
        public string GetOsDetails(string osCounterName) {
            SortedList<string, double> operatingSystemCount=operatingSystemCounterList[osCounterName];

            if(operatingSystemCount.Count==0)
                return "";
            else {
                StringBuilder osString=new StringBuilder("");
                double totalOsCount=0.0;

                foreach(string os in operatingSystemCount.Keys) {
                    totalOsCount+=operatingSystemCount[os];
                }
                if(totalOsCount==0)//just an extra check to avoid zero division
                    return "";
                else {
                    string[] osNames=new string[operatingSystemCount.Count];
                    double[] percentages=new double[operatingSystemCount.Count];

                    operatingSystemCount.Keys.CopyTo(osNames, 0);
                    operatingSystemCount.Values.CopyTo(percentages, 0);

                    Array.Sort<double, string>(percentages, osNames);
                    for(int i=osNames.Length-1; i>=0; i--) {
                        osString.Append(osNames[i]+" ("+((double)(percentages[i]/totalOsCount)).ToString("p")+") ");
                    }

                    /*
                    foreach(string os in operatingSystemCount.Keys)
                        osString.Append(os+" ("+((double)(operatingSystemCount[os]/totalOsCount)).ToString("p")+") ");
                     * */
                    return osString.ToString();
                }
            }
        }
        public byte Ttl {
            get {
                if(ttlCount.Count==0)
                    return byte.MinValue;
                else {
                    int bestTtlCount=0;
                    byte bestTtl=0x00;
                    foreach(byte ttl in ttlCount.Keys) {
                        int count=ttlCount[ttl];
                        if(count>bestTtlCount) {
                            bestTtl=ttl;
                            bestTtlCount=count;
                        }
                    }
                    return bestTtl;
                }
            }
        }
        public byte TtlDistance {
            get {
                if(ttlDistanceCount.Count==0)
                    return byte.MaxValue;
                else {
                    int bestTtlDistanceCount=0;
                    byte bestTtlDistance=0x00;
                    foreach(byte ttlDistance in ttlDistanceCount.Keys) {
                        int count=ttlDistanceCount[ttlDistance];
                        if(count>=bestTtlDistanceCount) {//if several are of equal value, I wan the largest TTL Distance
                            bestTtlDistance=ttlDistance;
                            bestTtlDistanceCount=count;
                        }
                    }
                    return bestTtlDistance;
                }
            }
        }
        public List<string> AcceptedSmbDialectsList { get { return this.acceptedSmbDialectsList; } set { this.acceptedSmbDialectsList=value; } }
        public string PreferredSmbDialect { get { return this.preferredSmbDialect; } set { this.preferredSmbDialect=value; } }
        public SortedList<string, string> ExtraDetailsList { get { return this.extraDetailsList; } }

        internal NetworkHost(IPAddress ipAddress) {
            this.ipAddress=ipAddress;
            this.macAddress=null;
            this.recentMacAdresses = new PopularityList<string, PhysicalAddress>(255);
            this.ttlCount=new SortedList<byte, int>();
            this.ttlDistanceCount=new SortedList<byte, int>();
            this.operatingSystemCounterList=new SortedList<string, SortedList<string, double>>();
            this.hostNameList=new List<string>();
            this.domainNameList=new List<string>();
            this.openTcpPortList=new List<ushort>();
            this.networkServiceMetadataList=new SortedList<ushort, NetworkServiceMetadata>();

            this.sentPackets=new NetworkPacketList();
            this.receivedPackets=new NetworkPacketList();
            this.incomingSessionList=new List<NetworkTcpSession>();
            this.outgoingSessionList=new List<NetworkTcpSession>();
            this.queriedIpList=new List<IPAddress>();
            this.queriedNetBiosNameList=new List<string>();
            this.queriedDnsNameList=new List<string>();
            this.httpUserAgentBannerList=new List<string>();
            this.httpServerBannerList=new List<string>();
            this.ftpServerBannerList=new List<string>();
            this.dhcpVendorCodeList=new List<string>();
            this.extraDetailsList=new SortedList<string, string>();

            this.universalPlugAndPlayFieldList=null;//I could just as well set this to null 'cause it is not often used. I'll initialize it when it is needed.
            this.acceptedSmbDialectsList=null;
            this.preferredSmbDialect=null;
        }

        public override int GetHashCode() {
            return ipAddress.GetHashCode();
            //return base.GetHashCode();
        }
        public override string ToString() {
            string str=ipAddress.ToString();
            foreach(string hostname in this.hostNameList)
                str+=" ["+hostname+"]";
            if(this.operatingSystemCounterList.Count>0)
                str+=" ("+this.OS+")";
            return str;
        }

        public SortedList<NetworkHost, NetworkPacketList> GetSentPacketListsPerDestinationHost() {
            SortedList<NetworkHost, NetworkPacketList> masterList=new SortedList<NetworkHost, NetworkPacketList>();
            foreach(NetworkPacket p in sentPackets) {
                if(!masterList.ContainsKey(p.DestinationHost)) {
                    masterList.Add(p.DestinationHost, new NetworkPacketList());
                }
                masterList[p.DestinationHost].Add(p);
            }
            return masterList;
        }
        public SortedList<NetworkHost, NetworkPacketList> GetReceivedPacketListsPerSourceHost() {
            SortedList<NetworkHost, NetworkPacketList> masterList=new SortedList<NetworkHost, NetworkPacketList>();
            foreach(NetworkPacket p in ReceivedPackets) {
                if(!masterList.ContainsKey(p.SourceHost)) {
                    masterList.Add(p.SourceHost, new NetworkPacketList());
                }
                masterList[p.SourceHost].Add(p);
            }
            return masterList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operatingSystem"></param>
        /// <param name="probability">A number between 0.0 and 1.0</param>
        internal void AddProbableOs(string fingerprinterName, string operatingSystem, double probability) {
            if(!this.operatingSystemCounterList.ContainsKey(fingerprinterName))
                this.operatingSystemCounterList.Add(fingerprinterName, new SortedList<string, double>());
            SortedList<string, double> operatingSystemCount=this.operatingSystemCounterList[fingerprinterName];

            if(operatingSystemCount.ContainsKey(operatingSystem))
                operatingSystemCount[operatingSystem]+=probability;
            else
                operatingSystemCount.Add(operatingSystem, probability);
        }

        internal void AddTtl(byte ttl) {
            if(this.ttlCount.ContainsKey(ttl))
                this.ttlCount[ttl]++;
            else
                this.ttlCount.Add(ttl, 1);
        }
        internal void AddProbableTtlDistance(byte ttlDistance){
            if(this.ttlDistanceCount.ContainsKey(ttlDistance))
                this.ttlDistanceCount[ttlDistance]++;
            else
                this.ttlDistanceCount.Add(ttlDistance, 1);
        }

        /// <summary>
        /// Adds a host name of some form
        /// </summary>
        /// <param name="hostname">DNS address, NetBIOS name or simiar</param>
        internal void AddHostName(string hostname) {
            if(!this.hostNameList.Contains(hostname))
                this.hostNameList.Add(hostname);
        }
        internal void AddDomainName(string domainName) {
            if(!this.domainNameList.Contains(domainName))
                this.domainNameList.Add(domainName);
        }
        internal void AddQueriedIP(IPAddress ip) {
            if(!this.queriedIpList.Contains(ip))
                this.queriedIpList.Add(ip);
        }
        internal void AddQueriedNetBiosName(string netBiosName) {
            if(!this.queriedNetBiosNameList.Contains(netBiosName))
                this.queriedNetBiosNameList.Add(netBiosName);
        }
        internal void AddQueriedDnsName(string dnsName) {
            if(!this.queriedDnsNameList.Contains(dnsName))
                this.queriedDnsNameList.Add(dnsName);
        }
        internal void AddHttpUserAgentBanner(string banner) {
            if(!this.httpUserAgentBannerList.Contains(banner))
                this.httpUserAgentBannerList.Add(banner);
        }
        internal void AddHttpServerBanner(string banner, ushort serverTcpPort) {
            if(!this.httpServerBannerList.Contains("TCP "+serverTcpPort+" : "+banner))
                this.httpServerBannerList.Add("TCP "+serverTcpPort+" : "+banner);
        }
        internal void AddFtpServerBanner(string banner, ushort serverTcpPort) {
            if(!this.ftpServerBannerList.Contains("TCP "+serverTcpPort+" : "+banner))
                this.ftpServerBannerList.Add("TCP "+serverTcpPort+" : "+banner);
        }
        internal void AddDhcpVendorCode(string vendorCode) {
            if(!this.dhcpVendorCodeList.Contains(vendorCode))
                this.dhcpVendorCodeList.Add(vendorCode);
        }
        internal void AddNumberedExtraDetail(string name, string value) {
            for(int i=1; i<100; i++)
                if(this.ExtraDetailsList.ContainsKey(name+" "+i)) {
                    if(this.ExtraDetailsList[name+" "+i].Equals(value, StringComparison.InvariantCultureIgnoreCase))
                        break;//the value is already stored
                }
                else {
                    this.ExtraDetailsList.Add(name+" "+i, value);
                    break;
                }
        }
        internal void AddOpenTcpPort(ushort port) {
            this.openTcpPortList.Add(port);
        }
        internal bool TcpPortIsOpen(ushort port) {
            return this.openTcpPortList.Contains(port);
        }

        internal bool IsRecentMacAddress(PhysicalAddress macAddress) {
            return this.recentMacAdresses.ContainsKey(macAddress.ToString());
        }
        


        #region IComparable Members
        
        public int CompareTo(NetworkHost host) {

            if(this.IPAddress.Equals(host.IPAddress))
                return 0;
            else {
                byte[] localBytes=this.IPAddress.GetAddressBytes();
                byte[] remoteBytes=host.IPAddress.GetAddressBytes();
                if (localBytes.Length != remoteBytes.Length)
                    return localBytes.Length - remoteBytes.Length;
                for(int i=0; i<localBytes.Length && i<remoteBytes.Length; i++) {
                    if(localBytes[i]!=remoteBytes[i])
                        return localBytes[i]-remoteBytes[i];
                }
                return 0;
            }
        }

        public int CompareTo(object obj) {
            NetworkHost host=(NetworkHost)obj;
            return CompareTo(host);
        }

        #endregion

    }
}
