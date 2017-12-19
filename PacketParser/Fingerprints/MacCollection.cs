//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PacketParser.Fingerprints {
    public class MacCollection {
        private static MacCollection singletonInstance=null;
        private static readonly char[] WHITESPACE = { ' ', '\t' };

        public static MacCollection GetMacCollection(string applicationExecutablePath) {
            if (singletonInstance == null) {
                //http://standards.ieee.org/develop/regauth/oui/oui.txt

                //singletonInstance = new MacCollection(Path.GetDirectoryName(applicationExecutablePath) + "\\Fingerprints\\oui.txt", MacFingerprintFileFormat.Nmap);
                //singletonInstance=new MacCollection(Path.GetDirectoryName(applicationExecutablePath)+"\\"+"oui.txt", MacFingerprintFileFormat.Nmap);
                singletonInstance = new MacCollection(Path.GetDirectoryName(applicationExecutablePath) + System.IO.Path.DirectorySeparatorChar+"Fingerprints"+System.IO.Path.DirectorySeparatorChar+"oui.txt", MacFingerprintFileFormat.IEEE_OUI);

            }
            return singletonInstance;
        }

        private System.Collections.Generic.SortedDictionary<string, string> macPrefixDictionary; //Format 00:11:22
        private System.Collections.Generic.SortedDictionary<string, string> macFullDictionary;//Format 00:11:22:33:44:55
        public enum MacFingerprintFileFormat { Ettercap, Nmap, IEEE_OUI }

        /// <summary>
        /// Reads a fingerprint file wit NIC MAC addresses. The file shall be formatted according to Ettercap
        /// </summary>
        /// <param name="macFingerprintFilename">for example "etter.finger.mac"</param>
        private MacCollection(string macFingerprintFilename, MacFingerprintFileFormat format) {
            //System.IO.File.OpenRead(macFingerprintFilename)
            //Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            //Environment.CurrentDirectory;

            System.IO.FileStream fileStream=new FileStream(macFingerprintFilename, FileMode.Open, FileAccess.Read);
            StreamReader reader=new StreamReader(fileStream);
            
            macPrefixDictionary=new SortedDictionary<string, string>();
            macFullDictionary=new SortedDictionary<string, string>();
            macFullDictionary.Add("FF:FF:FF:FF:FF:FF", "Broadcast");

            while(!reader.EndOfStream){
                string line=reader.ReadLine();
                //see if it is an empty or commented line
                if(line.Length>0 && line[0]!='#'){
                    string macKey=null;
                    string vendor=null;
                    if(format==MacFingerprintFileFormat.Ettercap && line.Length>10) {
                        macKey=line.Substring(0,8);//for example 00:00:01
                        vendor=line.Substring(10);
                    }
                    else if(format==MacFingerprintFileFormat.Nmap && line.Length>7) {
                        macKey=line.Substring(0, 2)+":"+line.Substring(2, 2)+":"+line.Substring(4, 2);
                        vendor=line.Substring(7);
                    }
                    else if (format == MacFingerprintFileFormat.IEEE_OUI && line.Length > 15 && line.Contains("(hex)") && line.TrimStart(WHITESPACE)[2] == '-') {
                        line = line.TrimStart(WHITESPACE);
                        macKey = line.Substring(0, 8).Replace('-', ':');
                        vendor = line.Substring(line.LastIndexOf('\t') + 1);
                    }
                    if(macKey!=null && vendor!=null && !macPrefixDictionary.ContainsKey(macKey))
                        macPrefixDictionary.Add(macKey, vendor);
                    //hashTable.Add(mac, vendor);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="macAddress">shall be in hex format. For example "00:F3:A1:01:23:45"</param>
        /// <returns></returns>
        public string GetMacVendor(string macAddress) {
            string macKey=macAddress.Substring(0, 2)+":"+macAddress.Substring(3, 2)+":"+macAddress.Substring(6, 2);
            if(macPrefixDictionary.ContainsKey(macKey))
                return macPrefixDictionary[macKey];
            else if(macFullDictionary.ContainsKey(macAddress))
                return macFullDictionary[macAddress];
            else
                return "Unknown";

        }
        public string GetMacVendor(System.Net.NetworkInformation.PhysicalAddress macAddress) {
            return GetMacVendor(macAddress.GetAddressBytes());
        }
        public string GetMacVendor(byte[] macAddress) {
            StringBuilder macWithColons = new StringBuilder();
            foreach(byte b in macAddress) {
                macWithColons.Append(b.ToString("X2"));
                macWithColons.Append(":");
            }
            if(macWithColons.Length>0)
                macWithColons.Remove(macWithColons.Length-1, 1);
            return GetMacVendor(macWithColons.ToString());
        }
        
    }
}
