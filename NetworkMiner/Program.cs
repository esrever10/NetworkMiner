//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NetworkMiner {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                NetworkMinerForm networkMinerForm = new NetworkMinerForm();
                Application.Run(networkMinerForm);
            }
            catch(Exception e) {
                MessageBox.Show(e.Message, "Unable to start NetworkMiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            
            

        }
    }
}