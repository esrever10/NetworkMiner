//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.PacketHandlers {
    class TlsRecordPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler {

        public ApplicationLayerProtocol HandledProtocol {
            get { return ApplicationLayerProtocol.Ssl; }
        }

        public TlsRecordPacketHandler(PacketHandler mainPacketHandler)
            : base(mainPacketHandler) {
            //empty constructor
        }

        #region ITcpSessionPacketHandler Members

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<Packets.AbstractPacket> packetList) {
            bool successfulExtraction=false;
            /*
            Packets.TlsRecordPacket tlsRecordPacket=null;
            Packets.TcpPacket tcpPacket=null;
            foreach(Packets.AbstractPacket p in packetList) {
                if(p.GetType()==typeof(Packets.TlsRecordPacket))
                    tlsRecordPacket=(Packets.TlsRecordPacket)p;
                else if(p.GetType()==typeof(Packets.TcpPacket))
                    tcpPacket=(Packets.TcpPacket)p;
            }
             * */

            Packets.TcpPacket tcpPacket=null;
            foreach(Packets.AbstractPacket p in packetList)
                if(p.GetType()==typeof(Packets.TcpPacket))
                    tcpPacket=(Packets.TcpPacket)p;
            int parsedBytes = 0;
            if(tcpPacket!=null) {
                
                //there might be several TlsRecordPackets in an SSL packet
                foreach(Packets.AbstractPacket p in packetList) {
                    if(p.GetType()==typeof(Packets.TlsRecordPacket)) {
                        Packets.TlsRecordPacket tlsRecordPacket=(Packets.TlsRecordPacket)p;
                        if(tlsRecordPacket.TlsRecordIsComplete) {
                            ExtractTlsRecordData(tlsRecordPacket, tcpPacket, sourceHost, destinationHost, base.MainPacketHandler);
                            successfulExtraction=true;
                            parsedBytes += tlsRecordPacket.Length+5;
                        }
                        else if(tlsRecordPacket.Length>4000) {//it should have been complete, so just skip it... there is no point in reassembling it any more
                            successfulExtraction=true;
                            parsedBytes = tcpPacket.PayloadDataLength;
                        }
                    }
                }
            }

            if(successfulExtraction) {
                return parsedBytes;
                //return tcpPacket.PayloadDataLength;
            }
            else
                return 0;
        }

        public void Reset() {
            //noting here since this object holds no state
        }

        #endregion

        private void ExtractTlsRecordData(Packets.TlsRecordPacket tlsRecordPacket, Packets.TcpPacket tcpPacket, NetworkHost sourceHost, NetworkHost destinationHost, PacketHandler mainPacketHandler) {

            foreach(Packets.AbstractPacket p in tlsRecordPacket.GetSubPackets(false)) {
                if(p.GetType()==typeof(Packets.TlsRecordPacket.HandshakePacket)) {
                    Packets.TlsRecordPacket.HandshakePacket handshake=(Packets.TlsRecordPacket.HandshakePacket)p;
                    if(handshake.MessageType==Packets.TlsRecordPacket.HandshakePacket.MessageTypes.Certificate)
                        for(int i=0; i<handshake.CertificateList.Count; i++) {
                            byte[] certificate=handshake.CertificateList[i];
                            string x509CertSubject;
                            System.Security.Cryptography.X509Certificates.X509Certificate x509Cert=null;
                            try {
                                x509Cert=new System.Security.Cryptography.X509Certificates.X509Certificate(certificate);
                                x509CertSubject=x509Cert.Subject;
                            }
                            catch {
                                x509CertSubject="Unknown_x509_Certificate_Subject";
                                x509Cert=null;
                            }
                            if(x509CertSubject.Length>28)
                                x509CertSubject=x509CertSubject.Substring(0, 28);
                            if(x509CertSubject.Contains("="))
                                x509CertSubject=x509CertSubject.Substring(x509CertSubject.IndexOf('=')+1);
                            if(x509CertSubject.Contains(","))
                                x509CertSubject=x509CertSubject.Substring(0, x509CertSubject.IndexOf(','));
                            while(x509CertSubject.EndsWith(".") || x509CertSubject.EndsWith(" "))
                                x509CertSubject=x509CertSubject.Substring(0, x509CertSubject.Length-1);

                            string filename=x509CertSubject+".cer";
                            string fileLocation="/";
                            string details;
                            if(x509Cert!=null)
                                details="TLS Certificate: "+x509Cert.Subject;
                            else
                                details="TLS Certificate: Unknown x509 Certificate";


                            FileTransfer.FileStreamAssembler assembler=new FileTransfer.FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true, FileTransfer.FileStreamTypes.TlsCertificate, filename, fileLocation, certificate.Length, certificate.Length, details, null, tlsRecordPacket.ParentFrame.FrameNumber, tlsRecordPacket.ParentFrame.Timestamp);
                            mainPacketHandler.FileStreamAssemblerList.Add(assembler);

                            if(assembler.TryActivate())
                                assembler.AddData(certificate, tcpPacket.SequenceNumber);//this one should trigger FinnishAssembling()
                        }
                }
            }

        }
    }
}
