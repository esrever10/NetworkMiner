//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace NetworkMiner {

    internal delegate void NewNetworkHostHandler(PacketParser.NetworkHost host);

    public class PacketHandlerWrapper {

        private NetworkMinerForm parentForm;
        private PcapFileHandler.PcapFileWriter pcapWriter;


        private PacketParser.PacketHandler packetHandler;



        public int CleartextSearchModeSelectedIndex { set { this.packetHandler.CleartextSearchModeSelectedIndex=value; } }
        public PacketParser.PacketHandler PacketHandler { get { return this.packetHandler; } }
        public PcapFileHandler.PcapFileWriter PcapWriter { get { return this.pcapWriter; } set { this.pcapWriter=value; } }


        internal PacketHandlerWrapper(NetworkMinerForm parentForm)
            : this(parentForm, new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(System.Windows.Forms.Application.ExecutablePath)))) {
        }

        internal PacketHandlerWrapper(NetworkMinerForm parentForm, System.IO.DirectoryInfo outputDirectory) {
            this.parentForm = parentForm;
            this.pcapWriter=null;
            string exePath = System.IO.Path.GetFullPath(System.Windows.Forms.Application.ExecutablePath);
            this.packetHandler = new PacketParser.PacketHandler(exePath, outputDirectory.FullName);
            

            this.PacketHandler.AnomalyDetected += new PacketParser.AnomalyEventHandler(AnomalyDetected);
            this.PacketHandler.BufferUsageChanged+=new PacketParser.BufferUsageEventHandler(BufferUsageChanged);
            this.packetHandler.CleartextWordsDetected+=new PacketParser.CleartextWordsEventHandler(CleartextWordsDetected);
            this.packetHandler.CredentialDetected+=new PacketParser.CredentialEventHandler(CredentialDetected);
            this.packetHandler.DnsRecordDetected+=new PacketParser.DnsRecordEventHandler(packetHandler_DnsRecordDetected);
            this.packetHandler.FileReconstructed+=new PacketParser.FileEventHandler(packetHandler_FileReconstructed);
            this.packetHandler.FrameDetected+=new PacketParser.FrameEventHandler(packetHandler_FrameDetected);
            this.packetHandler.KeywordDetected+=new PacketParser.KeywordEventHandler(packetHandler_KeywordDetected);
            this.packetHandler.NetworkHostDetected+=new PacketParser.NetworkHostEventHandler(packetHandler_NetworkHostDetected);
            this.packetHandler.ParametersDetected+=new PacketParser.ParameterEventHandler(packetHandler_ParametersDetected);
            this.packetHandler.SessionDetected+=new PacketParser.SessionEventHandler(packetHandler_SessionDetected);
            this.packetHandler.MessageDetected+=new PacketParser.MessageEventHandler(packetHandler_MessageDetected);

        }

        void packetHandler_MessageDetected(object sender, PacketParser.Events.MessageEventArgs me) {
            parentForm.ShowMessage(me.Protocol, me.SourceHost, me.DestinationHost, me.StartFrameNumber, me.StartTimestamp, me.From, me.To, me.Subject, me.Message, me.Attributes);
        }

        void packetHandler_SessionDetected(object sender, PacketParser.Events.SessionEventArgs se) {
            parentForm.ShowSession(se.Protocol, se.Client, se.Server, se.ClientPort, se.ServerPort, se.Tcp, se.StartFrameNumber, se.StartTimestamp);
        }

        void packetHandler_ParametersDetected(object sender, PacketParser.Events.ParametersEventArgs pe) {
            parentForm.ShowParameters(pe.FrameNumber, pe.SourceHost, pe.DestinationHost, pe.SourcePort, pe.DestinationPort, pe.Parameters, pe.Timestamp, pe.Details);
        }

        void packetHandler_NetworkHostDetected(object sender, PacketParser.Events.NetworkHostEventArgs he) {
            parentForm.ShowDetectedHost(he.Host);
        }

        void packetHandler_KeywordDetected(object sender, PacketParser.Events.KeywordEventArgs ke) {
            parentForm.ShowDetectedKeyword(ke.Frame, ke.KeywordIndex, ke.KeywordLength, ke.SourceHost, ke.DestinationHost, ke.SourcePort, ke.DestinationPort);
        }

        void packetHandler_FrameDetected(object sender, PacketParser.Events.FrameEventArgs fe) {
            parentForm.ShowReceivedFrame(fe.Frame);
        }

        void packetHandler_FileReconstructed(object sender, PacketParser.Events.FileEventArgs fe) {
            parentForm.ShowReconstructedFile(fe.File);
        }

        void packetHandler_DnsRecordDetected(object sender, PacketParser.Events.DnsRecordEventArgs de) {
            parentForm.ShowDnsRecord(de.Record, de.DnsServer, de.DnsClient, de.IpPakcet, de.UdpPacket);
        }

        private void AnomalyDetected(object sender, PacketParser.Events.AnomalyEventArgs anomaly) {
            parentForm.ShowError(anomaly.Message, anomaly.Timestamp);
        }
        private void BufferUsageChanged(object sender, PacketParser.Events.BufferUsageEventArgs bufferUsage) {
            parentForm.SetBufferUsagePercent(bufferUsage.BufferUsagePercent);
        }
        private void CleartextWordsDetected(object sender, PacketParser.Events.CleartextWordsEventArgs cleartextWords) {
            parentForm.ShowCleartextWords(cleartextWords.Words, cleartextWords.WordCharCount, cleartextWords.TotalByteCount);
        }
        private void CredentialDetected(object sender, PacketParser.Events.CredentialEventArgs credential) {
            parentForm.ShowCredential(credential.Credential);
        }

        internal void ResetCapturedData() {
            if(this.pcapWriter!=null && this.pcapWriter.IsOpen)
                this.pcapWriter.Close();
            this.pcapWriter=null;
            packetHandler.ResetCapturedData();

        }

        /*
        internal void SetKeywords(byte[][] keywordList) {
            this.packetHandler.KeywordList=keywordList;
        }*/

        public void StartBackgroundThreads() {
            this.packetHandler.StartBackgroundThreads();
            

        }

        public void AbortBackgroundThreads() {
            this.packetHandler.AbortBackgroundThreads();

        }

        /*
        public void UpdateKeywords(string keywordsFile) {
            if (keywordsFile != null && System.IO.File.Exists(keywordsFile)) {
                List<string> lines = new List<string>();
                using (System.IO.TextReader reader = System.IO.File.OpenText(keywordsFile)) {

                    while (true) {
                        string line = reader.ReadLine();
                        if (line == null)
                            break;//EOF
                        else
                            lines.Add(line);
                    }
                    reader.Close();
                }
                byte[][] keywordByteArray = PacketParser.Utils.StringManglerUtil.ConvertStringsToByteArrayArray(lines);
                packetHandler.KeywordList = keywordByteArray;
            }
        }*/

        //public void UpdateKeywords(IEnumerable<string> keywords) {
        public void UpdateKeywords(System.Collections.IEnumerable keywords) {
            byte[][] keywordByteArray = PacketParser.Utils.StringManglerUtil.ConvertStringsToByteArrayArray(keywords);
            packetHandler.KeywordList = keywordByteArray;
        }

        /*
        public static byte[][] ConvertStringsToByteArrayArray(System.Collections.IEnumerable strings) {
            List<byte[]> byteArrays = new List<byte[]>();
            foreach (string s in strings) {
                //string s=(string)o;
                if (s.StartsWith("0x"))
                    byteArrays.Add(PacketParser.Utils.ByteConverter.ToByteArrayFromHexString(s));
                else {
                    char[] charArray = s.ToCharArray();

                    byte[] ansiByteArray = System.Text.Encoding.Default.GetBytes(charArray);
                    byte[] bigEndianUnicodeByteArray = System.Text.Encoding.BigEndianUnicode.GetBytes(charArray);
                    if (bigEndianUnicodeByteArray[0] == 0x00) {
                        //skip the first byte to improve search performance and comply with little endian unicode strings as well (roughly anyway)
                        byte[] tmpArray = bigEndianUnicodeByteArray;
                        bigEndianUnicodeByteArray = new byte[tmpArray.Length - 1];
                        Array.Copy(tmpArray, 1, bigEndianUnicodeByteArray, 0, bigEndianUnicodeByteArray.Length);
                    }
                    byte[] utf8ByteArray = System.Text.Encoding.UTF8.GetBytes(charArray);

                    byteArrays.Add(bigEndianUnicodeByteArray);
                    byteArrays.Add(ansiByteArray);
                    if (ansiByteArray.Length != utf8ByteArray.Length)
                        byteArrays.Add(utf8ByteArray);
                }
            }
            return byteArrays.ToArray();
        }*/

        /// <summary>
        /// Callback method to receive packets from a sniffer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        internal void SnifferPacketReceived(object sender, NetworkWrapper.PacketReceivedEventArgs packet) {
            if(packetHandler.TryEnqueueReceivedPacket(sender, packet)) {
                //add frame to pcap file
                if(this.pcapWriter!=null)
                    this.pcapWriter.WriteFrame(new PcapFileHandler.PcapFrame(packet.Timestamp, packet.Data, pcapWriter.DataLinkType));
            }
                
        }


    }
}
