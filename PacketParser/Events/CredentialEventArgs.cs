using System;
using System.Collections.Generic;
using System.Text;

namespace PacketParser.Events {
    public class CredentialEventArgs : EventArgs {
        public NetworkCredential Credential;
        public CredentialEventArgs(NetworkCredential credential) {
            this.Credential=credential;
        }
    }
}
