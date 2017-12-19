using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Events {
    public class CleartextWordsEventArgs : EventArgs {
        //public IEnumerable<string> Words { get { return this.words; } }
        public IList<string> Words;
        public int WordCharCount;
        public int TotalByteCount;
        public int FrameNumber;
        public DateTime Timestamp;

        //private List<string> words;

        public CleartextWordsEventArgs(IList<string> words, int wordCharCount, int totalByteCount, int frameNumber, DateTime timestamp) {
            this.Words=words;
            this.WordCharCount=wordCharCount;
            this.TotalByteCount=totalByteCount;
            this.FrameNumber = frameNumber;
            this.Timestamp = timestamp;
        }
    }
}
