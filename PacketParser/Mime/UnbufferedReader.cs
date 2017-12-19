using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PacketParser.Mime {

    class UnbufferedReader {

        private Stream stream;

        public Stream BaseStream { get { return stream; } }

        public bool EndOfStream { get { return stream.Position>=stream.Length; } }

        public UnbufferedReader(Stream stream) {
            this.stream=stream;
            this.stream.Position=0;
        }

        public string ReadLine(int returnStringTruncateLength) {
            //http://www.ietf.org/rfc/rfc2046.txt
            //The canonical form of any MIME "text" subtype MUST always represent a
            //line break as a CRLF sequence.
            byte[] lineBreak={0x0d,0x0a};
            List<byte> lineBytes;
            //long breakPosition=ReadTo(lineBreak, out lineBytes);
            long breakPosition=Utils.KnuthMorrisPratt.ReadTo(lineBreak, this.stream, out lineBytes);

            if(lineBytes.Count<2 || breakPosition<0)
                return null;
            else {
                StringBuilder sb=new StringBuilder(lineBytes.Count-2);
                for(int i=0; i<lineBytes.Count-2 && sb.Length<returnStringTruncateLength; i++)
                    if(!Char.IsControl((char)lineBytes[i]))
                        sb.Append((char)lineBytes[i]);
                return sb.ToString();
            }
        }

        


    }
}
