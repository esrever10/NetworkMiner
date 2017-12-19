using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkMiner {
    public class CaseFile {
        private string filename;
        private string filePathAndName;
        private string md5;
        private DateTime firstFrameTimestamp;
        private DateTime lastFrameTimestamp;
        private int framesCount;
        private List<KeyValuePair<string, string>> metadata;

        public string Filename { get { return this.filename; } }
        public string FilePathAndName { get { return this.filePathAndName; } }

        public string Md5 { get { return this.md5; } set { this.md5=value; } }
        public DateTime FirstFrameTimestamp { get { return this.firstFrameTimestamp; } set { this.firstFrameTimestamp=value; } }
        public DateTime LastFrameTimestamp { get { return this.lastFrameTimestamp; } set { this.lastFrameTimestamp=value; } }
        public int FramesCount { get { return this.framesCount; } set { this.framesCount=value; } }
        public List<KeyValuePair<string, string>> Metadata { get { return this.metadata; } }


        internal CaseFile(string filePathAndName) {
            this.metadata = new List<KeyValuePair<string, string>>();
            this.filePathAndName=filePathAndName;
            if (filePathAndName.Contains(System.IO.Path.DirectorySeparatorChar.ToString()) && filePathAndName.LastIndexOf(System.IO.Path.DirectorySeparatorChar.ToString()) + 1 < filePathAndName.Length)
                this.filename = filePathAndName.Substring(filePathAndName.LastIndexOf(System.IO.Path.DirectorySeparatorChar.ToString()) + 1);
            else
                this.filename=filePathAndName;
        }

        public void AddMetadata(List<KeyValuePair<string, string>> metadata) {
            lock (this.metadata) {
                this.metadata.AddRange(metadata);
            }
        }


    }
}
