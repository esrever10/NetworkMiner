using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Mime {
    static class PartBuilder {

        public static IEnumerable<MultipartPart> GetParts(byte[] mimeMultipartData, string boundary) {
            System.IO.Stream stream=new ByteArrayStream(mimeMultipartData, 0);
            Mime.UnbufferedReader reader=new UnbufferedReader(stream);
            return GetParts(reader, boundary);
        }
        public static IEnumerable<MultipartPart> GetParts(Mime.UnbufferedReader streamReader) {
            long startPosition = streamReader.BaseStream.Position;
            //find out the boundary and call the GetParts with boundary
            System.Collections.Specialized.NameValueCollection attributes;
            MultipartPart.ReadHeaderAttributes(streamReader.BaseStream, streamReader.BaseStream.Position, out attributes);
            string boundary = attributes["boundary"];
            if (boundary != null) {
                streamReader.BaseStream.Position = startPosition;
                foreach (MultipartPart part in GetParts(streamReader, boundary))
                    yield return part;
            }
            else {
                //return a single part
                yield return new MultipartPart(streamReader.BaseStream, streamReader.BaseStream.Position, (int)(streamReader.BaseStream.Length - streamReader.BaseStream.Position));
                //yield break;
            }

        }
        public static IEnumerable<MultipartPart> GetParts(Mime.UnbufferedReader streamReader, string boundary) {

            string interPartBoundary="--"+boundary;
            string finalBoundary="--"+boundary+"--";
            while(!streamReader.EndOfStream){
                long partStartPosition=streamReader.BaseStream.Position;
                int partLength=0;
                string line=streamReader.ReadLine(200);
                while(line!=interPartBoundary && line!=finalBoundary){
                    partLength=(int)(streamReader.BaseStream.Position-2-partStartPosition);//-2 is in order to remove the CRLF at the end
                    line=streamReader.ReadLine(200);
                    if(line==null) {
                        yield break;//end of stream
                        //break;
                    }
                }
                long nextPartStartPosition=streamReader.BaseStream.Position;

                if(partLength>0){
                    byte[] partData=new byte[partLength];
                    streamReader.BaseStream.Position=partStartPosition;
                    streamReader.BaseStream.Read(partData, 0, partData.Length);
                    MultipartPart part = new MultipartPart(partData);

                    if(part.Attributes["Content-Type"]!=null && part.Attributes["Content-Type"].Contains("multipart") && part.Attributes["boundary"]!=null && part.Attributes["boundary"]!=boundary) {
                        foreach(MultipartPart internalPart in GetParts(part.Data, part.Attributes["boundary"]))
                            yield return internalPart;
                    }
                    else
                        yield return part;
                }
                
                streamReader.BaseStream.Position=nextPartStartPosition;
                if(line==finalBoundary)
                    break;
            }
        }
    }
}
