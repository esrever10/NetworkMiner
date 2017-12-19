//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Packets {

    //Common Internet File System (CIFS) Packet
    //CIFS is an enhanced version of Microsoft's open, cross-platform Server Message Block (SMB) protocol
    //http://www.protocols.com/pbook/ibm.htm#SMB
    //http://msdn2.microsoft.com/en-us/library/aa302213.aspx
    //http://www.microsoft.com/mind/1196/cifs.asp
    //http://ubiqx.org/cifs/SMB.html  (good source!)
    //http://www.snia.org/tech_activities/CIFS/CIFS-TR-1p00_FINAL.pdf

    class CifsPacket : AbstractPacket {
        private const uint smbProtocolIdentifier=0xff534d42;//=0xff+SMB

        internal enum CommandTypes : byte {//from http://www.snia.org/tech_activities/CIFS/CIFS-TR-1p00_FINAL.pdf 5.1. SMB Command Codes
            SMB_COM_CREATE_DIRECTORY=0x00,
            SMB_COM_DELETE_DIRECTORY=0x01,
            SMB_COM_OPEN=0x02,
            SMB_COM_CREATE=0x03,
            SMB_COM_CLOSE=0x04,
            SMB_COM_FLUSH=0x05,
            SMB_COM_DELETE=0x06,
            SMB_COM_RENAME=0x07,
            SMB_COM_QUERY_INFORMATION=0x08,
            SMB_COM_SET_INFORMATION=0x09,
            SMB_COM_READ=0x0A,
            SMB_COM_WRITE=0x0B,
            SMB_COM_LOCK_BYTE_RANGE=0x0C,
            SMB_COM_UNLOCK_BYTE_RANGE=0x0D,
            SMB_COM_CREATE_TEMPORARY=0x0E,
            SMB_COM_CREATE_NEW=0x0F,
            SMB_COM_CHECK_DIRECTORY=0x10,
            SMB_COM_PROCESS_EXIT=0x11,
            SMB_COM_SEEK=0x12,
            SMB_COM_LOCK_AND_READ=0x13,
            SMB_COM_WRITE_AND_UNLOCK=0x14,
            SMB_COM_READ_RAW=0x1A,
            SMB_COM_READ_MPXv0x1B,
            SMB_COM_READ_MPX_SECONDARY=0x1C,
            SMB_COM_WRITE_RAW=0x1D,
            SMB_COM_WRITE_MPX=0x1E,
            SMB_COM_WRITE_MPX_SECONDARY=0x1F,
            SMB_COM_WRITE_COMPLETE=0x20,
            SMB_COM_QUERY_SERVER=0x21,
            SMB_COM_SET_INFORMATION2=0x22,
            SMB_COM_QUERY_INFORMATION2=0x23,
            SMB_COM_LOCKING_ANDX=0x24,
            SMB_COM_TRANSACTION=0x25,
            SMB_COM_TRANSACTION_SECONDARY=0x26,
            SMB_COM_IOCTL=0x27,
            SMB_COM_IOCTL_SECONDARY=0x28,
            SMB_COM_COPY=0x29,
            SMB_COM_MOVE=0x2A,
            SMB_COM_ECHO=0x2B,
            SMB_COM_WRITE_AND_CLOSE=0x2C,
            SMB_COM_OPEN_ANDX=0x2D,
            SMB_COM_READ_ANDX=0x2E,
            SMB_COM_WRITE_ANDX=0x2F,
            SMB_COM_NEW_FILE_SIZE=0x30,
            SMB_COM_CLOSE_AND_TREE_DISC=0x31,
            SMB_COM_TRANSACTION2=0x32,
            SMB_COM_TRANSACTION2_SECONDARY=0x33,
            SMB_COM_FIND_CLOSE2=0x34,
            SMB_COM_FIND_NOTIFY_CLOSE=0x35,
            /* Used by Xenix/Unix 0x60 – 0x6E */
            SMB_COM_TREE_CONNECT=0x70,
            SMB_COM_TREE_DISCONNECT=0x71,
            SMB_COM_NEGOTIATE=0x72,
            SMB_COM_SESSION_SETUP_ANDX=0x73,
            SMB_COM_LOGOFF_ANDX=0x74,
            SMB_COM_TREE_CONNECT_ANDX=0x75,
            SMB_COM_QUERY_INFORMATION_DISK=0x80,
            SMB_COM_SEARCH=0x81,
            SMB_COM_FIND=0x82,
            SMB_COM_FIND_UNIQUE=0x83,
            SMB_COM_FIND_CLOSE=0x84,
            SMB_COM_NT_TRANSACT=0xA0,
            SMB_COM_NT_TRANSACT_SECONDARY=0xA1,
            SMB_COM_NT_CREATE_ANDX=0xA2,
            SMB_COM_NT_CANCEL=0xA4,
            SMB_COM_NT_RENAME=0xA5,
            SMB_COM_OPEN_PRINT_FILE=0xC0,
            SMB_COM_WRITE_PRINT_FILE=0xC1,
            SMB_COM_CLOSE_PRINT_FILE=0xC2,
            SMB_COM_GET_PRINT_QUEUE=0xC3,
            SMB_COM_READ_BULK=0xD8,
            SMB_COM_WRITE_BULK=0xD9,
            SMB_COM_WRITE_BULK_DATA=0xDA
        }

        #region SMB Header
        private uint protocolIdentifier;//the value must be "0xFF+'SMB'"
        private byte command;
        #region status
        private byte errorClass;
        private byte reserved;
        private ushort error;
        #endregion
        private byte flags;
        

        //here there are 14 bytes of data which is used differently among different dialects.
        //I do want the flags2 however so I'll try parsing them
        private ushort flags2;

        private ushort treeId;
        private ushort processId;
        private ushort userId;
        private ushort multiplexId;
        //trans request
        private byte wordCount;//Count of parameter words defining the data portion of the packet.
        //from here it might be undefined...

        private int parametersStartIndex;

        private ushort byteCount;//buffer length
        private int bufferStartIndex;
        #endregion

        internal byte WordCount { get { return this.wordCount; } }
        internal int ParametersStartIndex { get { return this.parametersStartIndex; } }
        internal ushort ByteCount { get { return this.byteCount; } }
        internal int BufferStartIndex { get { return this.bufferStartIndex; } }
        internal bool FlagsResponse { get { return (this.flags&0x80)==0x80; } }
        internal bool Flags2UnicodeStrings { get { return (this.flags2&0x8000)==0x8000; } }
        internal ushort TreeId { get { return this.treeId; } }
        internal ushort MultiplexId { get { return this.multiplexId; } }
        internal ushort ProcessId { get { return this.processId; } }


        internal CifsPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex)
            : base(parentFrame, packetStartIndex, packetEndIndex, "CIFS Server Message Block (SMB)") {
                this.protocolIdentifier = Utils.ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex);
            if(this.protocolIdentifier!=smbProtocolIdentifier) {
                //there's no need to throw an exception since it is probably just a fragmented packet...
                throw new Exception("SMB protocol identifier is: "+protocolIdentifier.ToString("X2"));
            }
            this.command=parentFrame.Data[packetStartIndex+4];
            if (!parentFrame.QuickParse) {
                try {
                    this.Attributes.Add("Command code", ((CommandTypes)command).ToString() + " (0x" + command.ToString("X2") + ")");
                }
                catch {
                    this.Attributes.Add("Command code", "(0x" + command.ToString("X2") + ")");
                }
            }
            //errors (they should hepefully be all 0's (they are also known as SMB/NT status)
            this.errorClass=parentFrame.Data[packetStartIndex+5];
            this.reserved=parentFrame.Data[packetStartIndex+6];
            this.error = Utils.ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 7, true);
            //flags
            this.flags=parentFrame.Data[packetStartIndex+9];
            this.flags2 = Utils.ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 10, true);
            //now skip agead 14 bytes from flags (not flags2!)
            this.treeId = Utils.ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 24, true);// Tree identifier
            if (!parentFrame.QuickParse)
                this.Attributes.Add("Tree ID", treeId.ToString());
            this.processId = Utils.ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 26, true);// Caller’s process ID, opaque for client use
            if (!parentFrame.QuickParse)
                this.Attributes.Add("Process ID", processId.ToString());
            this.userId = Utils.ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 28, true);// User id
            if (!parentFrame.QuickParse)
                this.Attributes.Add("User ID", userId.ToString());
            this.multiplexId = Utils.ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 30, true);// multiplex id
            if (!parentFrame.QuickParse)
                this.Attributes.Add("Multiplex ID", multiplexId.ToString());

            this.wordCount=parentFrame.Data[packetStartIndex+32];

            this.parametersStartIndex=packetStartIndex+33;//I guess it is always the same number...


            this.byteCount = Utils.ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 33 + wordCount * 2, true);
            if (!parentFrame.QuickParse)
                this.Attributes.Add("Buffer Total Length", byteCount.ToString());


            /*
             * In all cases where a string is passed in Unicode format, the Unicode string
             * must be word-aligned with respect to the beginning of the SMB. Should the string not naturally
             * fall on a two-byte boundary, a null byte of padding will be inserted, and the Unicode string will
             * begin at the next address.
             * */

            this.bufferStartIndex=packetStartIndex+33+wordCount*2+2;//no padding

            /*
             * For type-prefixed Unicode strings, the padding byte is found after the type byte. The type byte is
             * 4 (indicating SMB_FORMAT_ASCII) independent of whether the string is ASCII or Unicode. For
             * strings whose start addresses are found using offsets within the fixed part of the SMB (as
             * opposed to simply being found at the byte following the preceding field,) it is guaranteed that the
             * offset will be properly aligned.
             * */
        }

        //changge this one so that it uses ByteConverter.ReadNullTerminatedString
        internal string DecodeBufferString() {

            int dataIndex=this.bufferStartIndex;

            if(Flags2UnicodeStrings && ((bufferStartIndex-PacketStartIndex)%2==1)) {
                //must start on a word boundrary (2 bytes)
                dataIndex++;
                return Utils.ByteConverter.ReadString(ParentFrame.Data, ref dataIndex, this.byteCount - 1, this.Flags2UnicodeStrings, true);
            }
            else
                return Utils.ByteConverter.ReadString(ParentFrame.Data, ref dataIndex, this.byteCount, this.Flags2UnicodeStrings, true);
        }


        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference) {
            if(includeSelfReference)
                yield return this;

            AbstractPacket packet=null;
            try {
                CommandTypes commandType=(CommandTypes)command;
                if(commandType==CommandTypes.SMB_COM_NT_CREATE_ANDX) {
                    if(!this.FlagsResponse)
                        packet=new NTCreateAndXRequest(this);
                    else
                        packet=new NTCreateAndXResponse(this);
                }
                else if(commandType==CommandTypes.SMB_COM_READ_ANDX) {
                    if(this.FlagsResponse)
                        packet=new ReadAndXResponse(this);
                    else
                        packet=new ReadAndXRequest(this);
                }
                else if(commandType==CommandTypes.SMB_COM_CLOSE) {
                    if(!this.FlagsResponse)
                        packet=new CloseRequest(this);
                }
                else if(commandType==CommandTypes.SMB_COM_NEGOTIATE) {
                    if(!this.FlagsResponse)
                        packet=new NegotiateProtocolRequest(this);
                    else
                        packet=new NegotiateProtocolResponse(this);
                }
                else if(commandType==CommandTypes.SMB_COM_SESSION_SETUP_ANDX) {
                    if(!this.FlagsResponse)
                        packet=new SetupAndXRequest(this);
                    else
                        packet=new SetupAndXResponse(this);
                }
                    
            }
            catch(Exception e) {
                yield break;//no sub packets
            }

            if(packet!=null) {
                yield return packet;
                foreach(AbstractPacket subPacket in packet.GetSubPackets(false))
                    yield return subPacket;
            }
            else
                yield break;
        }


        #region internal command classes
        internal abstract class AbstractSmbCommand : AbstractPacket{

            private CifsPacket parentCifsPacket;
            private int? securityBlobIndex;
            private ushort securityBlobLength;


            internal CifsPacket ParentCifsPacket { get { return this.parentCifsPacket; } }
            internal int? SecurityBlobIndex {
                get { return this.securityBlobIndex; }
                set { this.securityBlobIndex=value; }
            }
            internal ushort SecurityBlobLength {
                get { return this.securityBlobLength; }
                set { this.securityBlobLength=value; }
            }



            internal AbstractSmbCommand(CifsPacket parentCifsPacket, string packetTypeDescription)
                : base(parentCifsPacket.ParentFrame, parentCifsPacket.ParametersStartIndex, parentCifsPacket.ParentFrame.Data.Length-1, packetTypeDescription) {

                this.parentCifsPacket=parentCifsPacket;
                this.securityBlobIndex=null;
                this.securityBlobLength=0;
            }

            public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
            {
                if(includeSelfReference)
                    yield return this;
                if(this.securityBlobIndex!=null) {

                    AbstractPacket securityBlobPacket=new SecurityBlob(ParentFrame, securityBlobIndex.Value, securityBlobIndex.Value+securityBlobLength-1);
                    yield return securityBlobPacket;
                    foreach(AbstractPacket subPacket in securityBlobPacket.GetSubPackets(false))
                        yield return subPacket;

                }
                else
                    yield break;//no sub packets
            }

        }

        internal class NTCreateAndXRequest : AbstractSmbCommand {
            private string filename;
            //private string fileId;

            internal string Filename{get{return this.filename;}}
            //internal string FileId { get { return this.fileId; } }

            internal NTCreateAndXRequest(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "CIFS NT Create AndX Request") {


                if(parentCifsPacket.WordCount==24) {
                    //int nameLength=ParentFrame.Data[PacketStartIndex+37];
                    int nameLength=ParentFrame.Data[parentCifsPacket.ParametersStartIndex+5];
                    int fileNameIndex=parentCifsPacket.BufferStartIndex;
                    
                    this.filename=parentCifsPacket.DecodeBufferString();
                    //NetBiosPacket.DecodeNetBiosName(ParentFrame, ref fileNameIndex);
                    if (!this.ParentFrame.QuickParse)
                        this.Attributes.Add("Filename", this.filename);
                }
                else
                    throw new Exception("Word Cound is not 24 ("+parentCifsPacket.WordCount.ToString()+")");
            }
        }
        internal class NTCreateAndXResponse : AbstractSmbCommand {
            private ushort fileId;//FID
            private ulong endOfFile;//File length in bytes

            /// <summary>
            /// File length in bytes
            /// </summary>
            internal ulong EndOfFile { get { return this.endOfFile; } }
            internal ushort FileId { get { return this.fileId; } }

            internal NTCreateAndXResponse(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "NT Create AndX Response") {
                this.fileId = Utils.ByteConverter.ToUInt16(ParentFrame.Data, parentCifsPacket.parametersStartIndex + 5, true);
                if (!this.ParentFrame.QuickParse)
                    base.Attributes.Add("File ID", "0x"+fileId.ToString("X2"));
                this.endOfFile = Utils.ByteConverter.ToUInt64(ParentFrame.Data, parentCifsPacket.parametersStartIndex + 55, true);
            }
        }
        internal class ReadAndXRequest : AbstractSmbCommand {

            private ushort fileId;

            internal ushort FileId { get { return this.fileId; } }

            internal ReadAndXRequest(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "CIFS Read AndX Request") {

                this.fileId = Utils.ByteConverter.ToUInt16(ParentFrame.Data, parentCifsPacket.parametersStartIndex + 4, true);
                if (!this.ParentFrame.QuickParse)
                    base.Attributes.Add("File ID", "0x"+fileId.ToString("X2"));
            }
        }
        internal class ReadAndXResponse : AbstractSmbCommand {
            private ushort dataLenght;
            private ushort dataOffset;

            internal ushort DataLength { get { return this.dataLenght; } }
            internal ushort DataOffset { get { return this.dataOffset; } }

            internal ReadAndXResponse(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "CIFS Read AndX Response") {
               //SE TILL ATT ram_load.sh kan extraheras (smb.fid == 0x21a0)

                    this.dataLenght = Utils.ByteConverter.ToUInt16(ParentFrame.Data, parentCifsPacket.parametersStartIndex + 10, true);
                    this.dataOffset = Utils.ByteConverter.ToUInt16(ParentFrame.Data, parentCifsPacket.parametersStartIndex + 12, true);
            }

            internal byte[] GetFileData() {
                //if(this.dataLenght != ParentFrame.Data.Length-(ParentCifsPacket.PacketStartIndex+this.DataOffset)) do nothing?;
                System.Diagnostics.Debug.Assert(this.dataLenght == ParentFrame.Data.Length - (ParentCifsPacket.PacketStartIndex + this.DataOffset), "CIFS frame is not complete!");

                int packetDataLength=Math.Min(this.dataLenght, ParentFrame.Data.Length-(ParentCifsPacket.PacketStartIndex+this.DataOffset));
                byte[] returnArray=new byte[packetDataLength];
                Array.Copy(ParentFrame.Data, ParentCifsPacket.PacketStartIndex+this.DataOffset, returnArray, 0, packetDataLength);
                return returnArray;
            }
        }
        internal class CloseRequest : AbstractSmbCommand {
            private ushort fileId;

            internal ushort FileId { get { return this.fileId; } }

            internal CloseRequest(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "Close Request") {
                this.fileId = Utils.ByteConverter.ToUInt16(ParentFrame.Data, parentCifsPacket.parametersStartIndex, true);
                if (!this.ParentFrame.QuickParse)
                    base.Attributes.Add("File ID", "0x"+fileId.ToString("X2"));
            }
        }

        //See 4.1.2. SESSION_SETUP_ANDX: Session Setup in CIFS-TR-1p00_FINAL.pdf
        internal class SetupAndXRequest : AbstractSmbCommand {//It would be cool to use this one in order to extract some extra information!
            private string nativeOs;
            private string nativeLanManager;//LAN Man
            //Username and password is transmitted Pre NT LM 0.12
            private string accountName;
            private string primaryDomain;
            private string accountPassword;



            internal string AccountName { get { return this.accountName; } }
            internal string AccountPassword { get { return this.accountPassword; } }
            internal string NativeOs { get { return this.nativeOs; } }
            internal string NativeLanManager { get { return this.nativeLanManager; } }
            internal string PrimaryDomain { get { return this.primaryDomain; } }

            internal SetupAndXRequest(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "CIFS Setup AndX Request") {

                this.nativeOs=null;
                this.nativeLanManager=null;
                this.accountName=null;
                this.primaryDomain=null;
                this.accountPassword=null;

                

                //OK, a big problem here is that I don't at this level know which protocol has been negotiated for the SMB session...
                //A good way to solve that problem is to look at the WordCount (number of parameters)
                if(parentCifsPacket.WordCount==10){//If wordCount is 10 then the dialect is prior to "NT LM 0.12"
                    ushort passwordLength = Utils.ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 14, true);
                    int packetIndex=parentCifsPacket.parametersStartIndex+22;
                    this.accountPassword = Utils.ByteConverter.ReadString(parentCifsPacket.ParentFrame.Data, ref packetIndex, passwordLength, false, true);
                    this.accountName = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    //I currently don't care about the primary domain...
                    this.primaryDomain = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeOs = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeLanManager = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                }
                else if(parentCifsPacket.WordCount==12) {//If wordCount is 12 then the dialect is "NT LM 0.12" or later
                    base.SecurityBlobLength = Utils.ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 14, true);
                    int packetIndex=parentCifsPacket.parametersStartIndex+26+base.SecurityBlobLength;
                    if(parentCifsPacket.Flags2UnicodeStrings && ((packetIndex-parentCifsPacket.PacketStartIndex)%2==1))
                        packetIndex++;//must start on a word boundrary (2 bytes)
                    this.nativeOs = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeLanManager = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                }
                else if(parentCifsPacket.WordCount==13) {//smb.wct == 13
                    ushort ansiPasswordLength = Utils.ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 14, true);
                    ushort unicodePasswordLength = Utils.ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 16, true);
                    if(ansiPasswordLength>0) {
                        //this.accountPassword=ByteConverter.ReadString(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex+28, ansiPasswordLength);
                        this.accountPassword = Utils.ByteConverter.ReadHexString(parentCifsPacket.ParentFrame.Data, ansiPasswordLength, parentCifsPacket.parametersStartIndex + 28);
                    }
                    if(unicodePasswordLength>0) {
                        string decodedPassword = accountPassword = Utils.ByteConverter.ReadString(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 28 + ansiPasswordLength, unicodePasswordLength, true, false);
                        string hexPassword = accountPassword = Utils.ByteConverter.ReadHexString(parentCifsPacket.ParentFrame.Data, unicodePasswordLength, parentCifsPacket.parametersStartIndex + 28 + ansiPasswordLength);
                        //this.accountPassword=decodedPassword+" (HEX: "+hexPassword+")";
                        this.accountPassword=hexPassword;
                    }
                    int packetIndex=parentCifsPacket.parametersStartIndex+28+ansiPasswordLength+unicodePasswordLength;
                    //I think we need an even word boundary (stupid SMB spec!)
                    if(parentCifsPacket.Flags2UnicodeStrings && ((packetIndex-parentCifsPacket.PacketStartIndex)%2==1))
                        packetIndex++;
                    if(unicodePasswordLength>0) {
                        this.accountName = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                        this.primaryDomain = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                        this.nativeOs = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                        this.nativeLanManager = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    }
                    else {
                        this.accountName = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, false, true);

                        this.primaryDomain = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, false, true);
                        this.nativeOs = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, false, true);
                        this.nativeLanManager = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, false, true);
                    }
                }

                if(base.SecurityBlobLength>0)
                    base.SecurityBlobIndex=parentCifsPacket.parametersStartIndex+2+parentCifsPacket.WordCount*2;

                if (!this.ParentFrame.QuickParse) {
                    if (accountName != null && accountName.Length > 0)
                        this.Attributes.Add("Account Name", accountName);
                    if (primaryDomain != null && primaryDomain.Length > 0)
                        this.Attributes.Add("Primary Domain", primaryDomain);
                    if (nativeOs != null && nativeOs.Length > 0)
                        this.Attributes.Add("Native OS", nativeOs);
                    if (nativeLanManager != null && nativeLanManager.Length > 0)
                        this.Attributes.Add("Native LAN Manager", nativeLanManager);
                }

                //note: if an older dialect is used then the securityBlobLength will contain the value for PasswordLength (Account password size)
            }




        }
        //See 4.1.2. SESSION_SETUP_ANDX: Session Setup in CIFS-TR-1p00_FINAL.pdf
        internal class SetupAndXResponse : AbstractSmbCommand {//It would be cool to use this one in order to extract some extra information!
            private string nativeOs;
            private string nativeLanManager;//LAN Man
            private string primaryDomain;


            internal string NativeOs { get { return this.nativeOs; } }
            internal string NativeLanManager { get { return this.nativeLanManager; } }

            internal SetupAndXResponse(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "CIFS Setup AndX Response") {

                this.nativeOs=null;
                this.nativeLanManager=null;
                this.primaryDomain=null;

                

                //OK, a big problem here is that I don't at this level know which protocol has been negotiated for the SMB session...
                //A good way to solve that problem is to look at the WordCount (number of parameters)
                if(parentCifsPacket.WordCount==3) {//If wordCount is 3 then the dialect is prior to "NT LM 0.12"
                    int packetIndex=parentCifsPacket.parametersStartIndex+9;
                    this.nativeOs = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeLanManager = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.primaryDomain = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                }
                else if(parentCifsPacket.WordCount==4) {//If wordCount is 4 then the dialect is "NT LM 0.12" or later
                    base.SecurityBlobLength = Utils.ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 6, true);

                    int packetIndex=parentCifsPacket.parametersStartIndex+10+base.SecurityBlobLength;
                    if(parentCifsPacket.Flags2UnicodeStrings && ((packetIndex-parentCifsPacket.PacketStartIndex)%2==1))
                        packetIndex++;//must start on a word boundrary (2 bytes)
                    this.nativeOs = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeLanManager = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.primaryDomain = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                }

                if(base.SecurityBlobLength>0)
                    base.SecurityBlobIndex=parentCifsPacket.parametersStartIndex+2+parentCifsPacket.WordCount*2;
            }


        }


        //4.1.1. NEGOTIATE: Negotiate Protocol
        internal class NegotiateProtocolRequest : AbstractSmbCommand {
            private List<string> dialectList;

            internal List<string> DialectList { get { return this.dialectList; } }

            internal NegotiateProtocolRequest(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "CIFS Negotiate Protocol Request") {
                this.dialectList=new List<string>();
                ushort byteCount = Utils.ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex, true);


                int packetIndex=parentCifsPacket.ParametersStartIndex+2;//It now points to the first BufferFormat in Dialects[]
                int dialectsStartIndex=packetIndex;
                packetIndex++;//I've now skipped pased the first 0x02 (buffer format)
                while(packetIndex-dialectsStartIndex<byteCount && packetIndex<parentCifsPacket.ParentFrame.Data.Length) {
                    string dialectName = Utils.ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref packetIndex);
                    this.dialectList.Add(dialectName);
                    packetIndex++;//skip the next 0x02 buffer format
                }
            }
        }
        internal class NegotiateProtocolResponse : AbstractSmbCommand {
            private ushort dialectIndex;

            internal ushort DialectIndex { get { return this.dialectIndex; } }

            internal NegotiateProtocolResponse(CifsPacket parentCifsPacket)
                : base(parentCifsPacket, "CIFS Negotiate Protocol Response") {
                    this.dialectIndex = Utils.ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex, true);
            }

        }
        #endregion

        #region Security Blob, SPNEGO and NTLMSSP
        internal class SecurityBlob : AbstractPacket {

            //SPNEGO: http://tools.ietf.org/html/rfc4559
            //SPNEGO: http://msdn.microsoft.com/en-us/library/ms995330.aspx
            //GSS-API: http://tools.ietf.org/html/rfc4178
            //NTLMSSP: http://davenport.sourceforge.net/ntlm.html

            private int? spnegoIndex;
            private int? ntlmsspIndex;

            internal SecurityBlob(Frame parentFrame, int packetStartIndex, int packetEndIndex)
                : base(parentFrame, packetStartIndex, packetEndIndex, "Security Blob") {

                this.spnegoIndex=null;
                this.ntlmsspIndex=null;

                if(parentFrame.Data[PacketStartIndex]==0x60) {
                    // 60, xx, 06, 06, [SPNEGO OID], [SPNEGO]
                    this.spnegoIndex=packetStartIndex+10;
                }
                else if(parentFrame.Data[PacketStartIndex]==0x4e) {
                    this.ntlmsspIndex=packetStartIndex;
                }
                else if(parentFrame.Data[PacketStartIndex]==0xA0) {
                    this.spnegoIndex=packetStartIndex;
                }
                else if(parentFrame.Data[PacketStartIndex]==0xA1) {
                    this.spnegoIndex=packetStartIndex;
                }

            }

            public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference) {
                if(includeSelfReference)
                    yield return this;
                AbstractPacket packet=null;
                try {
                    if(this.spnegoIndex!=null)
                        packet=new SimpleAndProtectedGssapiNegotiation(ParentFrame, spnegoIndex.Value, PacketEndIndex);
                    else if(this.ntlmsspIndex!=null)
                        packet=new Packets.NtlmSspPacket(ParentFrame, ntlmsspIndex.Value, PacketEndIndex);
                }
                catch(Exception){
                    //do nothing...
                }
                if(packet!=null) {
                    yield return packet;
                    foreach(AbstractPacket subPacket in packet.GetSubPackets(false))
                        yield return subPacket;
                }
                else
                    yield break;
                    
            }
        }

        /// <summary>
        /// SPNEGO = Simple and Protected GSSAPI Negotiation Mechanism.
        /// </summary>
        internal class SimpleAndProtectedGssapiNegotiation : AbstractPacket{
            //http://msdn.microsoft.com/en-us/library/ms995330.aspx

            internal enum BasicTokenTypes : byte { NegTokenInit=0xa0, NegTokenTarg=0xa1 }

            private byte basicTokenType;
            private int? ntlmsspIndex;
            private int ntlmsspLength;

            internal SimpleAndProtectedGssapiNegotiation(Frame parentFrame, int packetStartIndex, int packetEndIndex)
                : base(parentFrame, packetStartIndex, packetEndIndex, "SPNEGO") {
                this.basicTokenType=parentFrame.Data[packetStartIndex];
                this.ntlmsspIndex=null;
                this.ntlmsspLength=0;

                int packetIndex=packetStartIndex;
                int packetLength=GetSequenceElementLength(parentFrame.Data, ref packetIndex);

                if(parentFrame.Data[packetIndex]!=0x30)
                    throw new Exception("Not a valid SPNEGO packet format");
                //packetIndex++;
                int constructedSequenceLength=GetSequenceElementLength(parentFrame.Data, ref packetIndex);
                if(constructedSequenceLength>=packetLength)
                    throw new Exception("SPNEGO Packet length is not larger than Constructed Sequence length");
                while(packetIndex<packetEndIndex && packetIndex<packetStartIndex+packetLength) {
                    //read sequence elements...
                    byte sequenceElementIdentifier=parentFrame.Data[packetIndex];
                    int sequenceElementLength=GetSequenceElementLength(parentFrame.Data, ref packetIndex);
                    if((sequenceElementIdentifier&0xf0)==0xa0) {//SPNEGO sequence element
                        int sequenceElementNumber=sequenceElementIdentifier&0x0f;
                        if (!this.ParentFrame.QuickParse)
                            base.Attributes.Add("SPNEGO Element "+sequenceElementNumber+" length", sequenceElementLength.ToString());
                    }
                    else if(sequenceElementIdentifier==0x04) {//NTLMSSP identifier
                        if(parentFrame.Data[packetIndex]==(byte)0x4e) {//make sure it is NTLMSSP and not for example Kerberos
                            //Get the NTLMSSP packet
                            this.ntlmsspIndex=packetIndex;
                            this.ntlmsspLength=sequenceElementLength;
                        }
                        packetIndex+=sequenceElementLength;
                        break;//there is no point in looping any more once we have the NTLMSSP
                    }
                }

            }

            /// <summary>
            /// Gets the sequence element length (in number of bytes) and advances the index value to the first byte after the length data
            /// </summary>
            /// <param name="data">The raw data</param>
            /// <param name="index">The index should point to the 0xA? position in data. The index will be moved to the first position after the lenght parameter after the function is executed.</param>
            /// <returns>Seq. Element length</returns>
            private int GetSequenceElementLength(byte[] data, ref int index) {
                //make sure the first nibble is 0xa
                //if(data[index]&0xf0!=0xa0)
                //    throw new Exception("Not a valid sequence element, first nibble != 0xa");
                index++;
                int sequenceElementLength=0;
                //see if first bit (indicating long data) is set
                if(data[index]>=0x80) {
                    int bytesInLengthValue=data[index]&0x0f;
                    index++;
                    sequenceElementLength = (int)Utils.ByteConverter.ToUInt32(data, index, bytesInLengthValue, true);
                    index+=bytesInLengthValue;
                }
                else {//just a short single byte lenght value
                    sequenceElementLength=(int)data[index];
                    index++;
                }
                return sequenceElementLength;
            }

            public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference) {
                if(includeSelfReference)
                    yield return this;
                if(this.ntlmsspIndex!=null && this.ntlmsspLength>0){
                    Packets.NtlmSspPacket ntlmSspPacket=null;
                    try {
                         ntlmSspPacket=new Packets.NtlmSspPacket(ParentFrame, ntlmsspIndex.Value, ntlmsspIndex.Value+ntlmsspLength-1);
                    }
                    catch(Exception ex) {
                        if (!this.ParentFrame.QuickParse)
                            ParentFrame.Errors.Add(new Frame.Error(ParentFrame, ntlmsspIndex.Value, ntlmsspIndex.Value+ntlmsspLength-1, ex.Message));
                        yield break;
                    }
                    yield return ntlmSspPacket;
                }
                else
                    yield break;
            }
        }

        #endregion
    }
}
