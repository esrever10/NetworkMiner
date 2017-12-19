//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.FileTransfer {
    public class ReconstructedFile {

        private string path, details;
        private NetworkHost sourceHost, destinationHost;
        private ushort sourcePort, destinationPort;
        private bool tcpTransfer;
        private FileStreamTypes fileStreamType;
        private long fileSize;

        private string filename;

        private int initialFrameNumber;
        private DateTime timestamp;

        private string md5Sum=null;//uses lazy initialization

        public string FilePath { get { return this.path; } }
        public NetworkHost SourceHost { get { return this.sourceHost; } }
        public string SourcePortString { get { return this.GetTransportProtocolString()+" "+sourcePort; } }
        public NetworkHost DestinationHost { get { return this.destinationHost; } }
        public string DestinationPortString { get { return this.GetTransportProtocolString()+" "+destinationPort; } }
        public string Filename { get { return this.filename; } }
        public string FileSizeString {
            get {
                //return string.Format(new 
                System.Globalization.NumberFormatInfo nfi=new System.Globalization.NumberFormatInfo();
                nfi.NumberDecimalDigits=0;
                nfi.NumberGroupSizes=new int[] {3};
                nfi.NumberGroupSeparator=" ";
                //nfi.
                return this.fileSize.ToString("N", nfi)+" B";
            } }
        public string Details { get { return this.details; } }
        public FileStreamTypes FileStreamType { get { return this.fileStreamType; } }
        public int InitialFrameNumber { get { return this.initialFrameNumber; } }
        public DateTime Timestamp { get { return this.timestamp; } }

        public string MD5Sum {
            get {
                //this parameter uses lazy initialization
                if(this.md5Sum==null)
                    this.md5Sum=PcapFileHandler.Md5SingletonHelper.Instance.GetMd5Sum(this.path);
                return this.md5Sum;
            }
        }

        private string GetFileEnding() {
            if(!filename.Contains("."))
                return "";
            if(filename.EndsWith("."))
                return "";
            return this.filename.Substring(filename.LastIndexOf('.')+1).ToLower();
        }

        public bool IsImage() {
            string fileEnding=GetFileEnding();
            if(fileEnding.Length==0)
                return false;
            if(fileEnding=="jpg" || fileEnding=="jpeg" || fileEnding=="gif" || fileEnding=="png" || fileEnding=="bmp" || fileEnding=="tif" || fileEnding=="tiff")
                return true;
            else
                return false;
        }

        public bool IsIcon() {
            string fileEnding=GetFileEnding();
            if(fileEnding.Length==0)
                return false;
            if(fileEnding=="ico")
                return true;
            else
                return false;
        }

        public bool IsMultipartFormData() {
            string fileEnding=GetFileEnding();
            if(fileEnding.Length==0)
                return false;
            if(fileEnding=="mime")
                return true;
            else
                return false;
        }



        internal ReconstructedFile(string path, NetworkHost sourceHost, NetworkHost destinationHost, ushort sourcePort, ushort destinationPort, bool tcpTransfer, FileStreamTypes fileStreamType, string details, int initialFrameNumber, DateTime timestamp) {
            this.path=path;
            try {
                if(path.Contains("\\"))
                    this.filename=path.Substring(path.LastIndexOf('\\')+1);
                else if(path.Contains("/"))
                    this.filename=path.Substring(path.LastIndexOf('/')+1);
                else
                    this.filename=path;

            }
            catch(Exception) {
                this.filename="";
            }
            this.sourceHost=sourceHost;
            this.destinationHost=destinationHost;
            this.sourcePort=sourcePort;
            this.destinationPort=destinationPort;
            this.tcpTransfer=tcpTransfer;
            this.fileStreamType=fileStreamType;
            this.details=details;

            System.IO.FileInfo fi=new System.IO.FileInfo(path);
            this.fileSize=fi.Length;
            this.initialFrameNumber=initialFrameNumber;
            this.timestamp=timestamp;
        }

        private string GetTransportProtocolString() {
            if(this.tcpTransfer)
                return "TCP";
            else
                return "UDP";
        }

        public override string ToString() {
            string sourceInfo;
            string destinationInfo;
            if(tcpTransfer) {
                sourceInfo=sourceHost.ToString()+" TCP "+sourcePort;
                destinationInfo=destinationHost.ToString()+" TCP "+destinationPort;
            }
            else {
                sourceInfo=sourceHost.ToString()+" UDP "+sourcePort;
                destinationInfo=destinationHost.ToString()+" UDP "+destinationPort;
            }

            return filename+"\t"+sourceInfo+"\t"+destinationInfo;

        }

        public byte[] GetHeaderBytes(int nBytes) {

            using (System.IO.FileStream fileStream = new System.IO.FileStream(this.path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite, nBytes, System.IO.FileOptions.SequentialScan)) {

                byte[] bytes = new byte[nBytes];
                int bytesRead = fileStream.Read(bytes, 0, nBytes);
                fileStream.Close();
                if (bytesRead >= nBytes)
                    return bytes;
                else if (bytesRead < 0)
                    return null;
                else { //0 <= bytesRead < nBytes)
                    byte[] b = new byte[bytesRead];
                    Array.Copy(bytes, b, bytesRead);
                    return b;
                }
            }
        }

    }
}
