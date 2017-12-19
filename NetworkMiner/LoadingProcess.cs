using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NetworkMiner {
    public partial class LoadingProcess : Form {

        private int percent;
        private PcapFileHandler.PcapFileReader pcapReader;
        private bool isAborted;

        private BackgroundWorker worker;
        private CaseFile caseFile;

        internal delegate void SetLoadingProcessValue(int percent);
        //internal delegate System.Windows.Forms.DialogResult ShowLoadingProcess();
        internal delegate void ShowLoadingProcess();

        public bool IsAborted { get { return this.isAborted; } }
        public PcapFileHandler.PcapFileReader PcapReader { get { return this.pcapReader; } }
        public BackgroundWorker Worker { get { return this.worker; } set { this.worker=value; } }
        public CaseFile CaseFile { get { return this.caseFile; } }

        internal int Percent {
            get { return this.percent; }
            set {
                if(value>=0 && value<=100) {
                    this.percent=value;
                    this.percentLabel.Text=""+percent+"%";
                    //this.percentLabel.Update();
                    this.progressBar1.Value=percent;
                    //this.progressBar1.Update();
                    //this.textLabel.Update();
                }
            }
        }

        internal LoadingProcess(PcapFileHandler.PcapFileReader pcapReader, CaseFile caseFile)
            : this() {

            this.caseFile = caseFile;
            this.pcapReader=pcapReader;
            //this.SuspendLayout();
            this.textLabel.Text=caseFile.Filename;
            //this.textLabel.Update();
            this.progressBar1.Value=0;
            this.percent=0;
            this.percentLabel.Text=""+percent+" %";
            //this.ResumeLayout(false);
            //this.PerformLayout();
            this.isAborted=false;
            //InitializeComponent();
        }


        public LoadingProcess() {
            InitializeComponent();
        }

        private void LoadingProcess_FormClosing(object sender, FormClosingEventArgs e) {
            this.isAborted=true;
            pcapReader.AbortFileRead();
            this.worker.CancelAsync();
        }
    }
}