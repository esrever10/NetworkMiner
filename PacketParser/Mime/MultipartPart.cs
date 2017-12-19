using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PacketParser.Mime {
    class MultipartPart {
        private System.Collections.Specialized.NameValueCollection attributes;
        //private System.Collections.Generic.List<byte> data;
        private byte[] data;

        public System.Collections.Specialized.NameValueCollection Attributes { get { return this.attributes; } }
        public byte[] Data { get { return data; } }

        internal static void ReadHeaderAttributes(System.IO.Stream stream, long partStartIndex, out System.Collections.Specialized.NameValueCollection attributes) {
            stream.Position=partStartIndex;
            UnbufferedReader streamReader=new UnbufferedReader(stream);
            attributes=new System.Collections.Specialized.NameValueCollection();

            string line=streamReader.ReadLine(200);
            char[] headerParameterSeparators={ ';' };
            //TODO: parse attribute headers properly, i.e. the stuff BEFORE ";"
            while(line!=null && line.Length>0 && stream.Position < stream.Length) {//read the part headers
                string[] headerDataCollection=line.Split(headerParameterSeparators);

                /**
                 * ==HEADER EXAMPLE 1 (SMTP)==
                 * Content-Type: application/octet-stream;
                 * .name="secretrendezvous.docx"
                 * Content-Transfer-Encoding: base64
                 * Content-Disposition: attachment;
                 * .filename="secretrendezvous.docx"
                 * 
                 * ==HEADER EXAMPLE 2 (SMTP)==
                 * Content-Type: text/plain;
                 * .charset="iso-8859-1"
                 * Content-Transfer-Encoding: quoted-printable
                 * 
                 * ==HEADER EXAMPLE 3 (HTTP POST)==
                 * Content-Disposition: form-data; name="check_type_diff"
                 * 
                 * ==HEADER EXAMPLE 4 (HTTP POST)==
                 * Content-Disposition: form-data; name="image"; filename="C:\jx-3p.txt"
                 * Content-Type: text/plain
                 * 
                 * ==Another tricky RFC 2047 example==
                 * Subject: =?UTF-8?Q?[Bokserkomania]_V=C3=A4nligen_moderera:_"Slipy_w_ksi=C4=99?=  =?UTF-8?Q?=C5=BCyce"?=
                 */
                for (int i=0; i<headerDataCollection.Length; i++) {
                    try {
                        if (headerDataCollection[i].Contains("=\"") && headerDataCollection[i].Length > headerDataCollection[i].IndexOf('\"') + 1) {
                            string parameterName = headerDataCollection[i].Substring(0, headerDataCollection[i].IndexOf('=')).Trim();
                            string parameterValue = headerDataCollection[i].Substring(headerDataCollection[i].IndexOf('\"') + 1, headerDataCollection[i].LastIndexOf('\"') - headerDataCollection[i].IndexOf('\"') - 1).Trim();
                            parameterValue = Rfc2047Parser.DecodeRfc2047Parts(parameterValue);
                            attributes.Add(parameterName, parameterValue);
                        }
                        else if (headerDataCollection[i].Contains(": ")) {
                            string parameterName = headerDataCollection[i].Substring(0, headerDataCollection[i].IndexOf(':')).Trim();
                            string parameterValue = headerDataCollection[i].Substring(headerDataCollection[i].IndexOf(':') + 1).Trim();
                            parameterValue = Rfc2047Parser.DecodeRfc2047Parts(parameterValue);
                            attributes.Add(parameterName, parameterValue);
                        }
                    }
                    catch (Exception e) {
                        //TODO log exception somewhere...
                    }
                }
                line=streamReader.ReadLine(200);
            }
      
        }

        internal MultipartPart(System.Collections.Specialized.NameValueCollection attributes) : this(attributes, new byte[0]) {
        }
        internal MultipartPart(System.Collections.Specialized.NameValueCollection attributes, byte[] data) {
            this.attributes=attributes;
            this.data=data;
        }
        internal MultipartPart(byte[] partData) : this(new ByteArrayStream(partData, 0),0, partData.Length){
            //nothing more...
        }

        internal MultipartPart(System.IO.Stream stream, long partStartIndex, int partLength) {
            ReadHeaderAttributes(stream, partStartIndex, out this.attributes);
            //read the part data
           this.data=new byte[partLength+partStartIndex-stream.Position];
           stream.Read(data, 0, data.Length);
        }

    }
}
