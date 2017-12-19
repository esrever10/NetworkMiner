using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace PcapFileHandler {
    public class Md5SingletonHelper {
        private static Md5SingletonHelper instance=null;

        public static Md5SingletonHelper Instance {
            get {
                if(instance==null)
                    instance=new Md5SingletonHelper();
                return instance;
            }
        }

        private MD5 md5;

        private Md5SingletonHelper() {
            this.md5=MD5.Create();
        }

        public string GetMd5Sum(string file){
            
            byte[] md5hash;
            using(System.IO.FileStream fileStream=new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, 262144, System.IO.FileOptions.SequentialScan)) {
                md5hash=md5.ComputeHash(fileStream);
            }
            StringBuilder sb=new StringBuilder();
            foreach(byte b in md5hash) {
                sb.Append(b.ToString("X2").ToLower());
            }
            
            return sb.ToString();
        }


    }
}
