using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Events {
    public class FileEventArgs : EventArgs {

        public FileTransfer.ReconstructedFile File;

        public FileEventArgs(FileTransfer.ReconstructedFile file) {
            this.File=file;
        }
    }
}
