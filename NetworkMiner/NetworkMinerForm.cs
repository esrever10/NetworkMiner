//  Copyright: Erik Hjelmvik <hjelmvik@users.sourceforge.net>
//
//  NetworkMiner is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License
//


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//using MyClasses;

namespace NetworkMiner {
    public partial class NetworkMinerForm : Form {

        public static bool IsRunningOnMono() {
            return Type.GetType("Mono.Runtime") != null;
        }

        delegate void AppendTextCallback(string text);//used for EventLog and Callback
        delegate void GenericStringCallback(string text);
        delegate void GenericIntCallback(int value);
        delegate void AppendFrameToTreeViewCallback(PacketParser.Frame frame);
        delegate void AppendNetworkHostToTreeViewCallback(PacketParser.NetworkHost networkHost);
        delegate void SetControlTextCallback(Control control, string text);
        delegate void AppendFileToFileList(PacketParser.FileTransfer.ReconstructedFile file);
        //delegate void AppendCredentialToCredentialList(NetworkCredential credential, int credentialCount);
        delegate void AppendCredentialToCredentialList(PacketParser.NetworkCredential credential);
        delegate void AppendParametersCallback(int frameNumber, PacketParser.NetworkHost sourceHost, PacketParser.NetworkHost destinationHost, string sourcePort, string destinationPort, System.Collections.Specialized.NameValueCollection parameters, DateTime timestamp, string details);
        delegate void AddDnsRecordToDnsListCallback(PacketParser.Packets.DnsPacket.IDnsResponseInfo record, PacketParser.NetworkHost dnsServer, PacketParser.NetworkHost dnsClient, PacketParser.Packets.IPv4Packet ipPacket, PacketParser.Packets.UdpPacket udpPacket);
        delegate void AddSessionCallback(PacketParser.ApplicationLayerProtocol protocol, PacketParser.NetworkHost client, PacketParser.NetworkHost server, ushort clientPort, ushort serverPort, bool tcp, int startFrameNumber, DateTime startTimestamp);
        delegate void AddMessageCallback(PacketParser.ApplicationLayerProtocol protocol, PacketParser.NetworkHost client, PacketParser.NetworkHost server, int startFrameNumber, DateTime startTimestamp, string from, string to, string subject, string message, System.Collections.Specialized.NameValueCollection attributes);
        public delegate void AddCaseFileCallback(string fileFullPath, string filename);

        //keywords
        delegate byte[][] GetKeywordsCallback();
        delegate ListViewItem AddItemToListView(ListViewItem item);
        delegate int GetIntValueCallback();
        delegate void EmptyDelegateCallback();



        private int nFilesReceived;
        private NetworkWrapper.ISniffer sniffer;
        private ImageList imageList;
        private PacketHandlerWrapper packetHandlerWrapper;
        private PacketParser.CleartextDictionary.WordDictionary dictionary;
        private int pcapFileReaderQueueSize=1000;
        private string aboutText = null;
        private string productLink = "http://www.netresec.com/?page=NetworkMiner";
        private ToolInterfaces.ISettingsForm settingsForm = null;

        private NetworkWrapper.PacketReceivedHandler packetReceivedHandler = null;

        //interfaces
        private ToolInterfaces.IReportGenerator reportGenerator=null;
        private ToolInterfaces.IColorHandler<System.Net.IPAddress> ipColorHandler=null;
        private ToolInterfaces.IIPLocator ipLocator=null;
        private ToolInterfaces.IDataExporterFactory dataExporterFactory = null;
        private ToolInterfaces.IPcapOverIpReceiverFactory pcapOverIpReceiverFactory = null;
        private ToolInterfaces.IHostDetailsGenerator hostDetailsGenerator = null;
        private ToolInterfaces.IDomainNameFilter domainNameFilter = null;

        private Form upgradeCodeForm = null;
        private Form licenseSignatureForm = null;

        

        //properties
        public IEnumerable<CaseFile> CaseFiles {
            get {
                foreach(ListViewItem item in this.casePanelFileListView.Items)
                    if(item.Tag!=null)
                        yield return (CaseFile)item.Tag;
            }
        }
        public PacketHandlerWrapper PacketHandlerWrapper { get { return this.packetHandlerWrapper; } }

        public ToolInterfaces.ISettingsForm SettingsForm {
            set {
                this.settingsForm = value;
                if (this.settingsForm != null) {
                    this.settingsToolStripMenuItem.Click += new EventHandler(settingsToolStripMenuItem_Click);
                    this.settingsToolStripMenuItem.Enabled = true;
                    this.settingsToolStripMenuItem.Visible = true;
                }
                else {
                    this.settingsToolStripMenuItem.Enabled = false;
                    this.settingsToolStripMenuItem.Visible = false;
                }
            }
        }

        public NetworkMinerForm(string applicationTitle, int pcapFileReaderQueueSize, ToolInterfaces.IColorHandler<System.Net.IPAddress> ipColorHandler, ToolInterfaces.IIPLocator ipLocator, string aboutText, ToolInterfaces.IDataExporterFactory dataExporterFactory, /*ToolInterfaces.IPcapOverIpReceiverFactory pcapOverIpReceiverFactory, */ToolInterfaces.IHostDetailsGenerator hostDetailsGenerator, NetworkMiner.ToolInterfaces.IDomainNameFilter domainNameFilter, string productLink, bool showWinPcapAdapterMissingError, Form upgradeCodeForm, Form licenseSignatureForm)
            : this(showWinPcapAdapterMissingError) {
            this.Text=applicationTitle;
            this.pcapFileReaderQueueSize=pcapFileReaderQueueSize;
            this.ipColorHandler=ipColorHandler;
            this.ipLocator=ipLocator;
            this.domainNameFilter = domainNameFilter;
            this.aboutText = aboutText;
            this.dataExporterFactory = dataExporterFactory;
            this.hostDetailsGenerator = hostDetailsGenerator;

            if (hostDetailsGenerator != null) {
                this.toolStripSeparator2.Visible = true;
                this.downloadRIPEDBToolStripMenuItem.Enabled = true;
                this.downloadRIPEDBToolStripMenuItem.Visible = true;
                this.downloadRIPEDBToolStripMenuItem.Click += new EventHandler(downloadRIPEDBToolStripMenuItem_Click);
            }
            else {
                this.toolStripSeparator2.Visible = false;
                this.downloadRIPEDBToolStripMenuItem.Enabled = false;
                this.downloadRIPEDBToolStripMenuItem.Visible = false;
            }
            

            if (this.dataExporterFactory != null) {
                this.exportToolStripMenuItem.Enabled = true;
                this.exportToolStripMenuItem.Visible = true;
            }

            /*
            this.pcapOverIpReceiverFactory = pcapOverIpReceiverFactory;
            if (this.pcapOverIpReceiverFactory != null) {
                this.receivePcapOverIPToolStripMenuItem.Enabled = true;
                this.receivePcapOverIPToolStripMenuItem.Visible = true;
            }*/

            this.productLink = productLink;

            this.upgradeCodeForm = upgradeCodeForm;
            if (this.upgradeCodeForm != null) {
                this.getUpgradeCodeToolStripMenuItem.Enabled = true;
                this.getUpgradeCodeToolStripMenuItem.Visible = true;
            }
            this.licenseSignatureForm = licenseSignatureForm;
            if (this.licenseSignatureForm != null) {
                this.signWithLicenseToolStripMenuItem.Enabled = true;
                this.signWithLicenseToolStripMenuItem.Visible = true;
            }
        }

        


        

        public NetworkMinerForm() : this(false) {

        }

        public NetworkMinerForm(bool showWinPcapAdapterMissingError) {
            
            try {
                //require FileIOPermission to be PermissionState.Unrestricted
                System.Security.Permissions.FileIOPermission fileIOPerm=new System.Security.Permissions.FileIOPermission(System.Security.Permissions.PermissionState.Unrestricted);
                fileIOPerm.Demand();
            }
            catch(System.Security.SecurityException ex){
                MessageBox.Show("Make sure you are not running NetworkMiner from a network share!\n\nIf you need to run NetworkMiner from a non-trusted location (like a network share), then use Caspol.exe or Mscorcfg.msc to grant NetworkMiner full trust.", "File Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            

            InitializeComponent();

            if (IsRunningOnMono()) { //Avoid showing sniffing features when running under Mono
                int yDiff = this.splitContainer1.Location.Y - this.networkAdaptersComboBox.Location.Y;
                //this.splitContainer1.Location = new Point(this.splitContainer1.Location.X, this.networkAdaptersComboBox.Location.Y);//move up
                this.splitContainer1.Location = new Point(this.splitContainer1.Location.X, this.splitContainer1.Location.Y - yDiff);//move up
                this.splitContainer1.Height = this.splitContainer1.Height + yDiff;
                this.networkAdaptersComboBox.Visible = false;
                this.button2.Visible = false;
                this.startButton.Visible = false;
                this.stopButton.Visible = false;
                this.startCapturingToolStripMenuItem.Visible = false;
                this.stopCapturingToolStripMenuItem.Visible = false;
                this.toolStripSeparator3.Visible = false;
                
                this.snifferBufferToolStripProgressBar.Visible = false;
                this.toolStripStatusLabel1.Text = "Running NetworkMiner with Mono";
            }

            ContextMenuStrip fileCommandStrip=new ContextMenuStrip();
            fileCommandStrip.Items.Add(new ToolStripMenuItem("Open file", null, new EventHandler(OpenFile_Click)));
            fileCommandStrip.Items.Add(new ToolStripMenuItem("Open folder", null, new EventHandler(OpenFolder_Click)));
            filesListView.ContextMenuStrip=fileCommandStrip;

            ContextMenuStrip credentialCommandStrip=new ContextMenuStrip();
            credentialCommandStrip.Items.Add(new ToolStripMenuItem("Copy Username", null, new EventHandler(CopyCredentialUsernameToClipboard_Click)));
            credentialCommandStrip.Items.Add(new ToolStripMenuItem("Copy Password", null, new EventHandler(CopyCredentialPasswordToClipboard_Click)));
            credentialsListView.ContextMenuStrip=credentialCommandStrip;

            ContextMenuStrip imageCommandStrip=new ContextMenuStrip();
            imageCommandStrip.Items.Add(new ToolStripMenuItem("Open image", null, new EventHandler(OpenImage_Click)));
            imagesListView.ContextMenuStrip=imageCommandStrip;

            /*
            //make sure that folders exists
            ÅÄÄÖ
            System.IO.DirectoryInfo di=new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)+"\\AssembledFiles");
            if(!di.Exists)
                di.Create();
            di=new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)+"\\AssembledFiles\\cache");
            if(!di.Exists)
                di.Create();
            di=new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)+"\\Captures");
            if(!di.Exists)
                di.Create();
            this.packetHandlerWrapper=new PacketHandlerWrapper(this);
            */
            this.CreateNewPacketHandlerWrapper(new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)));

            this.imageList=new ImageList();
            imageList.ImageSize=new Size(64, 64);
            this.imagesListView.LargeImageList=imageList;

            

            //this.networkAdaptersComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.hostSortOrderComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.hostSortOrderComboBox.SelectedIndex=0;
            this.cleartextSearchModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cleartextSearchModeComboBox.SelectedIndex=1;//Search TCP and UDP payload

            
            //this.packetHandler=new PacketHandler(this, osDetectors);
            

            this.nFilesReceived=0;
            
            List<NetworkWrapper.IAdapter> networkAdapters=new List<NetworkWrapper.IAdapter>();
            networkAdapters.Add(new NetworkWrapper.NullAdapter());

            List<string> dllDirectories = new List<string>();
            List<string> winPcapDllFiles = new List<string>();
            dllDirectories.Add(Environment.CurrentDirectory);
            dllDirectories.Add(System.Windows.Forms.Application.ExecutablePath);
            dllDirectories.Add(System.Windows.Forms.Application.StartupPath);
            winPcapDllFiles.Add("wpcap.dll");
            winPcapDllFiles.Add("packet.dll");
            string hijackedPath;
            if(NetworkWrapper.Utils.Security.DllHijackingAttempted(dllDirectories, winPcapDllFiles, out hijackedPath)) {
                MessageBox.Show("A DLL Hijacking attempt was thwarted!\n" + hijackedPath, "NetworkMiner DLL Hijacking Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else {
                //Get the WinPcap adapters
                try {
                    networkAdapters.AddRange(NetworkWrapper.WinPCapAdapter.GetAdapters());
                }
                catch (Exception ex) {
                    if (showWinPcapAdapterMissingError)
                        MessageBox.Show("Unable to find any WinPcap adapter, live sniffing with Raw Sockets is still possible though.\nPlease install WinPcap (www.winpcap.org) or Wireshark (www.wireshark.org) if you wish to sniff with a WindPcap adapter.\n\n" + ex.Message, "NetworkMiner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            //get all SocketAdapters
            networkAdapters.AddRange(NetworkWrapper.SocketAdapter.GetAdapters());

            /*
            //get all MicroOLAP adapters
            networkAdapters.AddRange(NetworkWrapper.MicroOlapAdapter.GetAdapters());
            */
            this.networkAdaptersComboBox.DataSource=networkAdapters;

            this.framesTreeView.Nodes.Clear();


            this.networkHostTreeView.ImageList=new ImageList();
            //AddImage(networkHostTreeView, "white", "white.gif");
            AddImage(networkHostTreeView, "white", "white.jpg");//first is default
            AddImage(networkHostTreeView, "iana", "iana.jpg");
            AddImage(networkHostTreeView, "computer", "computer.jpg");
            AddImage(networkHostTreeView, "multicast", "multicast.jpg");
            AddImage(networkHostTreeView, "broadcast", "broadcast.jpg");

            AddImage(networkHostTreeView, "windows", "windows.jpg");
            AddImage(networkHostTreeView, "macos", "macos.jpg");
            //AddImage(networkHostTreeView, "unix", "unix.gif");
            AddImage(networkHostTreeView, "unix", "unix.jpg");
            AddImage(networkHostTreeView, "linux", "linux.jpg");
            AddImage(networkHostTreeView, "freebsd", "freebsd.jpg");
            AddImage(networkHostTreeView, "netbsd", "netbsd.jpg");
            AddImage(networkHostTreeView, "solaris", "solaris.jpg");

            AddImage(networkHostTreeView, "sent", "arrow_sent.jpg");
            AddImage(networkHostTreeView, "received", "arrow_received.jpg");
            AddImage(networkHostTreeView, "incoming", "arrow_incoming.jpg");
            AddImage(networkHostTreeView, "outgoing", "arrow_outgoing.jpg");

            AddImage(networkHostTreeView, "nic", "network_card.jpg");
            AddImage(networkHostTreeView, "details", "details.gif");

            this.networkHostTreeView.BeforeExpand+=new TreeViewCancelEventHandler(NetworkHostTreeView_BeforeExpand);
            
            if(this.networkAdaptersComboBox.SelectedValue.GetType().Equals(typeof(NetworkWrapper.WinPCapAdapter)))
                this.sniffer=new NetworkWrapper.WinPCapSniffer((NetworkWrapper.WinPCapAdapter)this.networkAdaptersComboBox.SelectedValue);
            else if(this.networkAdaptersComboBox.SelectedValue.GetType().Equals(typeof(NetworkWrapper.SocketAdapter)))
                this.sniffer=new NetworkWrapper.SocketSniffer((NetworkWrapper.SocketAdapter)this.networkAdaptersComboBox.SelectedValue);
            else if(this.networkAdaptersComboBox.SelectedValue.GetType().Equals(typeof(NetworkWrapper.NullAdapter)))
                this.sniffer=null;
            else
                throw new Exception(""+this.networkAdaptersComboBox.SelectedValue.GetType().ToString());


            this.dictionary=new PacketParser.CleartextDictionary.WordDictionary();
            this.loadCleartextDictionary("all-words.txt");

            this.openPcapFileDialog.Filter="Pcap files (*.pcap, *.cap, *.dump, *.dmp, *.log)|*.pcap;*.cap;*.dump;*.dmp;*.log|NetworkMiner files (*.nmine)|*.nmine|All files (*.*)|*.*";
            this.openPcapFileDialog.FileName="";

            //column sorting. From: ms-help://MS.VSCC.v80/MS.MSDN.v80/MS.NETDEVFX.v20.en/CPref17/html/P_System_Windows_Forms_ListView_ListViewItemSorter.htm
            //this.filesListView.ColumnClick+=new ColumnClickEventHandler(ListViewColumnClick);
            //this.credentialsListView.ColumnClick+=new ColumnClickEventHandler(ListViewColumnClick);
            //this.parametersListView.ColumnClick+=new ColumnClickEventHandler(ListViewColumnClick);
            //this.detectedKeywordsListView.ColumnClick+=new ColumnClickEventHandler(ListViewColumnClick);
            //this.dnsListView.ColumnClick+=new ColumnClickEventHandler(ListViewColumnClick);
            //this.parametersListView.ColumnClick+=new ColumnClickEventHandler(ListViewColumnClick);

            /*
            NetworkWrapper.WinPCapSniffer.PacketReceived += new NetworkWrapper.PacketReceivedHandler(packetHandlerWrapper.SnifferPacketReceived);
            NetworkWrapper.SocketSniffer.PacketReceived += new NetworkWrapper.PacketReceivedHandler(packetHandlerWrapper.SnifferPacketReceived);

            packetHandlerWrapper.StartBackgroundThreads();
            */

            //this.HandleCreated+=new EventHandler(NetworkMinerForm_HandleCreated);
            this.HandleCreated += new EventHandler(LoadNextPcapFileFromCommandLineArgs);

            this.messageTextBox.Text="[no message selected]";

            this.pcapOverIpReceiverFactory = new PcapOverIP.PcapOverIpReceiverFactory();
            
            if (this.pcapOverIpReceiverFactory != null) {
                this.receivePcapOverIPToolStripMenuItem.Enabled = true;
                this.receivePcapOverIPToolStripMenuItem.Visible = true;
            }

        }

        


        public void CreateNewPacketHandlerWrapper(System.IO.DirectoryInfo outputDirectory) {
            //make sure that folders exists
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(outputDirectory.FullName + System.IO.Path.DirectorySeparatorChar + PacketParser.FileTransfer.FileStreamAssembler.ASSMEBLED_FILES_DIRECTORY);
            if(!di.Exists)
                di.Create();
            di = new System.IO.DirectoryInfo(outputDirectory.FullName + System.IO.Path.DirectorySeparatorChar + PacketParser.FileTransfer.FileStreamAssembler.ASSMEBLED_FILES_DIRECTORY + System.IO.Path.DirectorySeparatorChar + "cache");
            if(!di.Exists)
                di.Create();
            di = new System.IO.DirectoryInfo(outputDirectory.FullName + System.IO.Path.DirectorySeparatorChar + "Captures");
            if(!di.Exists)
                di.Create();

            if (this.packetHandlerWrapper !=null)
                this.packetHandlerWrapper.AbortBackgroundThreads();

            //Unregister all existing events
            if (this.packetReceivedHandler != null) {
                NetworkWrapper.WinPCapSniffer.PacketReceived -= this.packetReceivedHandler;
                NetworkWrapper.SocketSniffer.PacketReceived -= this.packetReceivedHandler;
            }

            
            this.packetHandlerWrapper = new PacketHandlerWrapper(this, outputDirectory);
            //create new handler
            this.packetReceivedHandler = new NetworkWrapper.PacketReceivedHandler(packetHandlerWrapper.SnifferPacketReceived);

            NetworkWrapper.WinPCapSniffer.PacketReceived += this.packetReceivedHandler;
            NetworkWrapper.SocketSniffer.PacketReceived += this.packetReceivedHandler;

            this.packetHandlerWrapper.StartBackgroundThreads();
        }

        /// <summary>
        /// Loads pcap files provided in command line argument
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        ///
        /*
        void NetworkMinerForm_HandleCreated(object sender, EventArgs e) {
            //check the command line arguments in case the user wants to open a file
            if(Environment.GetCommandLineArgs().Length>1) {
                string[] args=Environment.GetCommandLineArgs();
                for(int i=1; i<args.Length; i++) {
                    //this.LoadPcapFileBlocking(args[i]);
                    BackgroundWorker fileLoader = this.LoadPcapFile(args[i]);
                    fileLoader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(fileLoader_RunWorkerCompleted);
                    break;
                }
            }
        }*/

        void LoadNextPcapFileFromCommandLineArgs(object sender, EventArgs e) {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++) {
                if (!this.casePanelFileListView.Items.ContainsKey(args[i])) {
                    BackgroundWorker fileLoader = this.LoadPcapFile(args[i]);
                    fileLoader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LoadNextPcapFileFromCommandLineArgs);
                    break;
                }
            }
        }

        private void AddImage(TreeView treeView, string key, string imageFileName) {
            
            //treeView.ImageList.Images.Add(key, Image.FromFile(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)+"\\images\\"+imageFileName));
            treeView.ImageList.Images.Add(key, Image.FromFile(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + System.IO.Path.DirectorySeparatorChar + "Images" + System.IO.Path.DirectorySeparatorChar + imageFileName));
        }



        private void AddFrameToTreeView(PacketParser.Frame frame) {
            TreeNode frameNode=new TreeNode(frame.ToString());
            foreach(PacketParser.Packets.AbstractPacket p in frame.PacketList/*.Values*/) {
                TreeNode packetNode=new TreeNode(p.PacketTypeDescription+" ["+p.PacketStartIndex+"-"+p.PacketEndIndex+"]");
                foreach(string attributeKey in p.Attributes.AllKeys)
                    packetNode.Nodes.Add(attributeKey+" = "+p.Attributes[attributeKey]);
                frameNode.Nodes.Add(packetNode);

                if (this.ipColorHandler != null && p is PacketParser.Packets.IIPPacket) {
                    PacketParser.Packets.IIPPacket ipPacket = (PacketParser.Packets.IIPPacket)p;
                    this.ipColorHandler.Colorize(ipPacket.SourceIPAddress, frameNode);
                    this.ipColorHandler.Colorize(ipPacket.DestinationIPAddress, frameNode);
                    this.ipColorHandler.Colorize(ipPacket.SourceIPAddress, packetNode);
                    this.ipColorHandler.Colorize(ipPacket.DestinationIPAddress, packetNode);
                }
                /*
                if(this.ipColorHandler!=null && p.GetType()==typeof(PacketParser.Packets.IPv4Packet)) {
                    PacketParser.Packets.IPv4Packet ipPacket=(PacketParser.Packets.IPv4Packet)p;
                    this.ipColorHandler.Colorize(ipPacket.SourceIPAddress, frameNode);
                    this.ipColorHandler.Colorize(ipPacket.DestinationIPAddress, frameNode);
                    this.ipColorHandler.Colorize(ipPacket.SourceIPAddress, packetNode);
                    this.ipColorHandler.Colorize(ipPacket.DestinationIPAddress, packetNode);

                }
                if(this.ipColorHandler!=null && p.GetType()==typeof(PacketParser.Packets.IPv6Packet)) {
                    PacketParser.Packets.IPv6Packet ipPacket=(PacketParser.Packets.IPv6Packet)p;
                    this.ipColorHandler.Colorize(ipPacket.SourceIP, frameNode);
                    this.ipColorHandler.Colorize(ipPacket.DestinationIP, frameNode);
                    this.ipColorHandler.Colorize(ipPacket.SourceIP, packetNode);
                    this.ipColorHandler.Colorize(ipPacket.DestinationIP, packetNode);

                }*/
            }
            this.framesTreeView.Nodes.Add(frameNode);
        }

        private void AddNetworkHostToTreeView(PacketParser.NetworkHost networkHost) {
            NetworkHostTreeNode treeNode=new NetworkHostTreeNode(networkHost, this.ipLocator, this.hostDetailsGenerator);


            
            
            if(this.ipColorHandler!=null)
                this.ipColorHandler.Colorize(networkHost.IPAddress, treeNode);

            this.networkHostTreeView.Nodes.Add(treeNode);
            this.SetControlText(this.tabPageDetectedHosts, "Hosts ("+this.packetHandlerWrapper.PacketHandler.NetworkHostList.Count+")");
        }

        private void OpenParentFolderInExplorer(string filePath) {
            if (filePath.Contains(System.IO.Path.DirectorySeparatorChar.ToString()))
                filePath = filePath.Substring(0, filePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar));
            System.Diagnostics.Process.Start("explorer.exe", filePath);
        }

        private void AddFileToFileList(PacketParser.FileTransfer.ReconstructedFile file) {
            string extension = "";
            if(file.Filename.Contains(".") && file.Filename.LastIndexOf('.')+1 < file.Filename.Length)
                extension=file.Filename.Substring(file.Filename.LastIndexOf('.')+1);

            ListViewItem item=new ListViewItem(
                new string[] {
                    file.InitialFrameNumber.ToString(),
                    file.FilePath,
                    file.SourceHost.ToString(),
                    file.SourcePortString,
                    file.DestinationHost.ToString(),
                    file.DestinationPortString,
                    file.FileStreamType.ToString(),
                    file.Filename,
                    extension,
                    file.FileSizeString,
                    file.Timestamp.ToString(),
                    file.Details
                });
            item.ToolTipText=item.Text;
            item.Tag=file.FilePath;

            
            /*
            if(ipColors.ContainsKey(file.DestinationHost.IPAddress))
                item.BackColor=ipColors[file.DestinationHost.IPAddress];
            else if(ipColors.ContainsKey(file.SourceHost.IPAddress))
                item.BackColor=ipColors[file.SourceHost.IPAddress];
             * */
            if(this.ipColorHandler!=null) {
                this.ipColorHandler.Colorize(file.SourceHost.IPAddress, item);
                this.ipColorHandler.Colorize(file.DestinationHost.IPAddress, item);
            }
            this.filesListView.Items.Add(item);

            this.nFilesReceived++;
            this.SetControlText(this.tabPageFiles, "Files ("+this.nFilesReceived+")");

            try {
                if(file.IsImage())
                    AddImageToImageList(file, new Bitmap(file.FilePath));
                else if(file.IsIcon())
                    AddImageToImageList(file, new Icon(file.FilePath).ToBitmap());
            }
            catch(Exception e) {
                this.ShowError("Error: Exception when loading image \"" + file.Filename + "\". " + e.Message, DateTime.Now);
            }

        }
        private void AddParameters(int frameNumber, PacketParser.NetworkHost sourceHost, PacketParser.NetworkHost destinationHost, string sourcePort, string destinationPort, System.Collections.Specialized.NameValueCollection parameters, DateTime timestamp, string details) {
            foreach(string parameterName in parameters.AllKeys) {
                ListViewItem item=new ListViewItem(
                    new string[] {
                    parameterName,
                    parameters[parameterName],
                    frameNumber.ToString(),
                    sourceHost.ToString(),
                    sourcePort,
                    destinationHost.ToString(),
                    destinationPort,
                    timestamp.ToString(),
                    details
                }
                    );
                item.ToolTipText=parameterName+" = "+parameters[parameterName];

                //add a tag
                System.Collections.Generic.KeyValuePair<string, string> nameValueTag=new KeyValuePair<string, string>(parameterName, parameters[parameterName]);
                item.Tag=nameValueTag;

                if(this.ipColorHandler!=null) {
                    this.ipColorHandler.Colorize(sourceHost.IPAddress, item);
                    this.ipColorHandler.Colorize(destinationHost.IPAddress, item);
                }
                this.parametersListView.Items.Add(item);
                this.SetControlText(this.tabPageParameters, "Parameters ("+parametersListView.Items.Count+")");
            }
        }
        private void AddSessionToSessionList(PacketParser.ApplicationLayerProtocol protocol, PacketParser.NetworkHost client, PacketParser.NetworkHost server, ushort clientPort, ushort serverPort, bool tcp, int startFrameNumber, DateTime startTimestamp) {
            string protocolString="";
            if(protocol != PacketParser.ApplicationLayerProtocol.Unknown)
                protocolString = protocol.ToString();

            ListViewItem item = new ListViewItem(
                new string[] {
                    startFrameNumber.ToString(),
                    client.ToString(),
                    clientPort.ToString(),//add "TCP" ?
                    server.ToString(),
                    serverPort.ToString(),
                    protocolString,
                    startTimestamp.ToString()
                });
            //item.Tag = session;//this might occupy a lot of memory....
            //no tooltip...

            if(this.ipColorHandler!=null) {
                this.ipColorHandler.Colorize(client.IPAddress, item);
                this.ipColorHandler.Colorize(server.IPAddress, item);
            }
            this.sessionsListView.Items.Add(item);
            this.SetControlText(this.tabPageSessions, "Sessions ("+this.sessionsListView.Items.Count+")");
        }
        private void AddMessage(PacketParser.ApplicationLayerProtocol protocol, PacketParser.NetworkHost sourceHost, PacketParser.NetworkHost destinationHost, int startFrameNumber, DateTime startTimestamp, string from, string to, string subject, string message, System.Collections.Specialized.NameValueCollection attributes) {
            ListViewItem item = new ListViewItem(
                new string[] {
                    startFrameNumber.ToString(),
                    sourceHost.ToString(),
                    destinationHost.ToString(),
                    from,
                    to,
                    subject,
                    protocol.ToString(),
                    startTimestamp.ToString()
                });

            //set the message and attributes as tag for retrieval from the GUI when the message is selected
            item.Tag = new KeyValuePair<System.Collections.Specialized.NameValueCollection, string>(attributes, message);

            if(this.ipColorHandler!=null) {
                this.ipColorHandler.Colorize(sourceHost.IPAddress, item);
                this.ipColorHandler.Colorize(destinationHost.IPAddress, item);
            }
            this.messagesListView.Items.Add(item);
            this.SetControlText(this.tabPageMessages, "Messages ("+this.messagesListView.Items.Count+")");

        }

        private void AddCredentialToCredentialList(PacketParser.NetworkCredential credential) {
            if (
                (this.showCookiesCheckBox.Checked || !credential.ProtocolString.ToLower().Contains("cookie")) &&
                (this.showNtlmSspCheckBox.Checked || !credential.ProtocolString.ToUpper().Contains("NTLMSSP"))
                ) {
                string validCredential = "Unknown";
                if (credential.IsProvenValid)
                    validCredential = "Yes";

                string displayedPassword = "";
                if (credential.Password == null)
                    displayedPassword = "";
                else if(this.maskPasswordsCheckBox.Checked)
                    displayedPassword = new String('*', credential.Password.Length);
                else
                    displayedPassword = credential.Password;

                ListViewItem item = new ListViewItem(
                    new string[] {
                    credential.Client.ToString(),
                    credential.Server.ToString(),
                    credential.ProtocolString,
                    credential.Username,
                    //ByteConverter.ToMd5HashString(credential.Password),//credential.Password
                    displayedPassword,
                    validCredential,
                    credential.LoginTimestamp.ToString()
                });
                item.Tag = credential;
                item.ToolTipText = item.Text;





                if (this.ipColorHandler != null) {
                    if(credential.Client != null)
                        this.ipColorHandler.Colorize(credential.Client.IPAddress, item);
                    if (credential.Server != null)
                        this.ipColorHandler.Colorize(credential.Server.IPAddress, item);
                }
                this.credentialsListView.Items.Add(item);
                this.SetControlText(this.tabPageCredentials, "Credentials (" + credentialsListView.Items.Count + ")");
            }
        }

        private void AddImageToImageList(PacketParser.FileTransfer.ReconstructedFile file, Bitmap bitmapImage) {
            this.imageList.Images.Add(new Bitmap(bitmapImage));//I do the new in order to release the file handle for the original file
            ListViewItem item=this.imagesListView.Items.Add(file.Filename+"\n"+bitmapImage.Width+"x"+bitmapImage.Height+", "+file.FileSizeString, imageList.Images.Count-1);
            item.Tag=file;
            item.ToolTipText="Source: "+file.SourceHost+"\nDestination: "+file.DestinationHost+"\nReconstructed file path: "+file.FilePath;
            




            if(this.ipColorHandler!=null) {
                this.ipColorHandler.Colorize(file.SourceHost.IPAddress, item);
                this.ipColorHandler.Colorize(file.DestinationHost.IPAddress, item);
            }
            this.SetControlText(this.tabPageImages, "Images ("+this.imageList.Images.Count+")");
        }

        private void AddDnsRecordToDnsList(PacketParser.Packets.DnsPacket.IDnsResponseInfo record, PacketParser.NetworkHost dnsServer, PacketParser.NetworkHost dnsClient, PacketParser.Packets.IPv4Packet ipPacket, PacketParser.Packets.UdpPacket udpPacket) {
            string queriedName=null;
            string answeredName=null;

            if(record.DNS!=null)
                queriedName=record.DNS;
            else
                return;

            if (record.IP != null)
                answeredName = record.IP.ToString();
            else if (record.PrimaryName != null)
                answeredName = record.PrimaryName;
            else if (record is PacketParser.Packets.DnsPacket.ResponseWithErrorCode)
                answeredName = (record as PacketParser.Packets.DnsPacket.ResponseWithErrorCode).GetResultCodeString();
            else
                return;

            string recordTypeString="0x"+record.Type.ToString("X4");
            if(record.Type==(ushort)PacketParser.Packets.DnsPacket.RRTypes.CNAME)
                recordTypeString=recordTypeString+" (CNAME)";
            else if(record.Type==(ushort)PacketParser.Packets.DnsPacket.RRTypes.DomainNamePointer)
                recordTypeString=recordTypeString+" (Domain Name Pointer)";
            else if(record.Type==(ushort)PacketParser.Packets.DnsPacket.RRTypes.HostAddress)
                recordTypeString=recordTypeString+" (Host Address)";
            else if(record.Type==(ushort)PacketParser.Packets.DnsPacket.RRTypes.NB)
                recordTypeString=recordTypeString+" (NB)";
            else if(record.Type==(ushort)PacketParser.Packets.DnsPacket.RRTypes.NBSTAT)
                recordTypeString=recordTypeString+" (NBSTAT)";

            string serverUdpPort="unknown";
            string clientUdpPort="unknown";
            if(udpPacket!=null) {
                serverUdpPort=udpPacket.SourcePort.ToString();
                clientUdpPort=udpPacket.DestinationPort.ToString();
            }

            string alexaTop1m;
            if (this.domainNameFilter == null)
                alexaTop1m = "N/A (Pro version only)";
            else if (this.domainNameFilter.ContainsDomain(queriedName, out alexaTop1m) || this.domainNameFilter.ContainsDomain(answeredName, out alexaTop1m))
                alexaTop1m = "Yes (" + alexaTop1m + ")";
            else
                alexaTop1m = "No";

            ListViewItem item=new ListViewItem(
                new string[] {
                    ipPacket.ParentFrame.FrameNumber.ToString(),
                    ipPacket.ParentFrame.Timestamp.ToString(),
                    dnsClient.ToString(),
                    clientUdpPort,
                    dnsServer.ToString(),
                    serverUdpPort,
                    ipPacket.TimeToLive.ToString(),
                    record.TimeToLive.ToString(),
                    "0x"+record.ParentPacket.TransactionId.ToString("X4"),
                    recordTypeString,
                    queriedName,
                    answeredName,
                    alexaTop1m,
                });
            item.ToolTipText=item.Text;

            if(this.ipColorHandler!=null) {
                if(record!=null && record.IP != null)
                    this.ipColorHandler.Colorize(record.IP, item);
                if(ipPacket!=null) {
                    this.ipColorHandler.Colorize(ipPacket.SourceIPAddress, item);
                    this.ipColorHandler.Colorize(ipPacket.DestinationIPAddress, item);
                }
                if(dnsClient!=null)
                    this.ipColorHandler.Colorize(dnsClient.IPAddress, item);
                if(dnsServer!=null)
                    this.ipColorHandler.Colorize(dnsServer.IPAddress, item);
            }
            this.dnsListView.Items.Add(item);
            this.SetControlText(this.tabPageDns, "DNS ("+this.dnsListView.Items.Count+")");
            
        }


        //test
        void NetworkHostTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e) {
            //try {
            if(e.Node is IBeforeExpand) {
                IBeforeExpand expandable=(IBeforeExpand)e.Node;
                expandable.BeforeExpand();
            }
        }

        //see: ms-help://MS.VSCC.v80/MS.MSDN.v80/MS.VisualStudio.v80.en/dv_fxmclictl/html/138f38b6-1099-4fd5-910c-390b41cbad35.htm
        //or: http://www.osix.net/modules/article/?id=832
        internal void ShowReceivedFrame(PacketParser.Frame frame) {

            //show only the first two value numbers of frames count, the others will be marked "x"
            int frameDigitCount=frame.FrameNumber.ToString().Length;
            int moduluNumber=(int)Math.Pow(10, frameDigitCount-2);
            if(frameDigitCount<3 || frame.FrameNumber%moduluNumber==0) {
                string displayNumber=frame.FrameNumber.ToString();
                if(frameDigitCount>2) {
                    displayNumber=displayNumber.Substring(0, 2);
                    for(int i=0; i<frameDigitCount-2; i++)
                        displayNumber+="x";

                }
                this.SetControlText(this.tabPageReceivedFrames, "Frames ("+displayNumber+")");
            }
        

            //this.SetControlText(this.nBytesLabel, frame.Data.Length.ToString("n0")+" B");
            foreach(PacketParser.Frame.Error error in frame.Errors) {
                //it is better to show the frame's timestamp, since this is from where the error probably stems
                //ShowError(error.ToString()+" (frame nr: "+frame.FrameNumber+")");
                //DateTime.Now.ToString()
                ShowError(error.ToString() + " (frame nr: " + frame.FrameNumber + ")", frame.Timestamp);
            }

            int maxFramesToShow=1000;//this should maybe be a changeable class variable...
            if(this.framesTreeView.Nodes.Count < maxFramesToShow) {
                AppendFrameToTreeViewCallback treeViewCallback = new AppendFrameToTreeViewCallback(AddFrameToTreeView);
                if(this.IsHandleCreated)
                    this.Invoke(treeViewCallback, frame);
            }
        }

        internal void ShowCleartextWords(IEnumerable<string> words, int wordCharCount, int totalByteCount) {
            //if(this.showDetectedCleartextCheckBox.Checked && wordCharCount*10 > totalByteCount) {
            if(wordCharCount*16 > totalByteCount) {//at least 1/16 of the data shall be clear text, otherwise it is probable to be false positives
                StringBuilder sb=new StringBuilder();
                foreach(string word in words) {
                    sb.Append(word);
                    sb.Append(" ");
                }

                //this.cleartextTextBox.ForeColor=System.Drawing.Color.FromArgb(wordCharCount.GetHashCode());
                this.cleartextTextBox.ForeColor=System.Drawing.Color.Red;
                //AppendTextCallback cleartextCallback = new AppendTextCallback(this.cleartextTextBox.ForeColor);
                //this.Invoke(cleartextCallback, sb.ToString());
                

                AppendTextCallback cleartextCallback = new AppendTextCallback(this.cleartextTextBox.AppendText);
                this.Invoke(cleartextCallback, sb.ToString());
            }
        }

        internal void ShowReconstructedFile(PacketParser.FileTransfer.ReconstructedFile file) {
            AppendFileToFileList newFileCallback=new AppendFileToFileList(AddFileToFileList);
            this.Invoke(newFileCallback, file);
        }
        internal void ShowParameters(int frameNumber, PacketParser.NetworkHost sourceHost, PacketParser.NetworkHost destinationHost, string sourcePort, string destinationPort, System.Collections.Specialized.NameValueCollection parameters, DateTime timestamp, string details) {
            AppendParametersCallback newParametersCallback=new AppendParametersCallback(AddParameters);
            this.Invoke(newParametersCallback, frameNumber, sourceHost, destinationHost, sourcePort, destinationPort, parameters, timestamp, details);
        }
        internal void ShowCredential(PacketParser.NetworkCredential credential) {
            AppendCredentialToCredentialList newCredentialCallback=new AppendCredentialToCredentialList(AddCredentialToCredentialList);
            //do I have to make an object[] here???
            this.Invoke(newCredentialCallback, credential);
        }
        internal void ShowDetectedHost(PacketParser.NetworkHost host) {
            AppendNetworkHostToTreeViewCallback newHostCallback=new AppendNetworkHostToTreeViewCallback(AddNetworkHostToTreeView);
            this.Invoke(newHostCallback, host);
        }
        internal void ShowDnsRecord(PacketParser.Packets.DnsPacket.IDnsResponseInfo record, PacketParser.NetworkHost dnsServer, PacketParser.NetworkHost dnsClient, PacketParser.Packets.IPv4Packet ipPakcet, PacketParser.Packets.UdpPacket udpPacket) {
            AddDnsRecordToDnsListCallback newDnsRecordCallback=new AddDnsRecordToDnsListCallback(AddDnsRecordToDnsList);
            this.Invoke(newDnsRecordCallback, record, dnsServer, dnsClient, ipPakcet, udpPacket);
        }
        internal void ShowError(string errorText, DateTime errorTimestamp) {
            AppendTextCallback logCallback = new AppendTextCallback(this.eventLog.AppendText);
            //this.Invoke(logCallback, "\r\n["+DateTime.Now.ToString()+"] Error : "+errorText);
            this.Invoke(logCallback, "\r\n[" + errorTimestamp + "] Error : " + errorText);
        }
        internal void ShowSession(PacketParser.ApplicationLayerProtocol protocol, PacketParser.NetworkHost client, PacketParser.NetworkHost server, ushort clientPort, ushort serverPort, bool tcp, int startFrameNumber, DateTime startTimestamp) {
            AddSessionCallback sessionCallback = new AddSessionCallback(this.AddSessionToSessionList);
            this.Invoke(sessionCallback, protocol, client, server, clientPort, serverPort, tcp, startFrameNumber, startTimestamp);
        }
        internal void ShowMessage(PacketParser.ApplicationLayerProtocol protocol, PacketParser.NetworkHost sourceHost, PacketParser.NetworkHost destinationHost, int startFrameNumber, DateTime startTimestamp, string from, string to, string subject, string message, System.Collections.Specialized.NameValueCollection attributes) {
            AddMessageCallback messageCallback = new AddMessageCallback(this.AddMessage);
            this.Invoke(messageCallback, protocol, sourceHost, destinationHost, startFrameNumber, startTimestamp, from, to, subject, message, attributes);
        }
        internal void SetBufferUsagePercent(int percent) {
            if(this.IsHandleCreated)
                this.Invoke((GenericIntCallback)delegate(int i) { this.snifferBufferToolStripProgressBar.Value=i; }, percent);
        }

        /// <summary>
        /// Returns the selected index in the Dropdown ComboBox for Cleartext Search
        /// </summary>
        /// <returns>0 [Full content search], 1 [Raw packet search], 2 [No search]</returns>
        internal int GetCleartextSearchModeIndex() {

            //http://www.csharp411.com/property-delegates-with-anonymous-methods/
            return (int) this.Invoke( (GetIntValueCallback) delegate() { return this.cleartextSearchModeComboBox.SelectedIndex; } );

            //GetIntValueCallback selectedIndexCallback=new GetIntValueCallback(this.cleartextSearchModeComboBox.SelectedIndex);
            //return (int) this.Invoke(selectedIndexCallback);

            //return this.cleartextSearchModeComboBox.SelectedIndex;
            
        }

        internal int GetHostSortOrderIndex() {

            //http://www.csharp411.com/property-delegates-with-anonymous-methods/
            return (int)this.Invoke((GetIntValueCallback)delegate() { return this.hostSortOrderComboBox.SelectedIndex; });

        }

        /*
        //keywords
        internal byte[][] GetKeywords() {
            return (byte[][])Invoke(new GetKeywordsCallback(this.GetKeywordsPrivate));
        }*/

        //http://www.codeproject.com/csharp/begininvoke.asp?df=100&forumid=178776&exp=0&select=1519433
        private void SetControlText(Control c, string text) {

            if(this.InvokeRequired) {
                object[] args=new object[2];
                args[0]=c;
                args[1]=text;
                this.BeginInvoke(new SetControlTextCallback(SetControlText), args);
            }
            else if(c.Text!=text) {
                c.Text=text;
            }


        }

        private void CopyCredentialUsernameToClipboard_Click(object sender, EventArgs e) {
            if(credentialsListView.SelectedItems.Count>0){
                PacketParser.NetworkCredential c=(PacketParser.NetworkCredential)credentialsListView.SelectedItems[0].Tag;
                Clipboard.SetDataObject(c.Username, true);
            }
        }
        private void CopyCredentialPasswordToClipboard_Click(object sender, EventArgs e) {
            if(credentialsListView.SelectedItems.Count>0) {
                PacketParser.NetworkCredential c=(PacketParser.NetworkCredential)credentialsListView.SelectedItems[0].Tag;
                Clipboard.SetDataObject(c.Password, true);
            }
        }
        private void OpenImage_Click(object sender, EventArgs e) {
            if(imagesListView.SelectedItems.Count>0) {
                PacketParser.FileTransfer.ReconstructedFile file=(PacketParser.FileTransfer.ReconstructedFile)imagesListView.SelectedItems[0].Tag;
                try {
                    System.Diagnostics.Process.Start(file.FilePath);
                }
                catch(Exception ex) {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        //see: ms-help://MS.VSCC.v80/MS.MSDN.v80/MS.NETDEVFX.v20.en/CPref17/html/C_System_Windows_Forms_ToolStripMenuItem_ctor_2_8a3c7c15.htm
        private void OpenFile_Click(object sender, EventArgs e) {
            //http://msdn2.microsoft.com/en-us/library/system.diagnostics.process.start(VS.71).aspx
            if(filesListView.SelectedItems.Count>0) {
                string filePath=filesListView.SelectedItems[0].Tag.ToString();//.Text;
                try {
                    System.Diagnostics.Process.Start(filePath);
                }
                catch(Exception ex) {
                    MessageBox.Show(ex.Message);
                }
                //throw new Exception("Not implemented yet");
            }
        }
        //see: ms-help://MS.VSCC.v80/MS.MSDN.v80/MS.NETDEVFX.v20.en/CPref17/html/C_System_Windows_Forms_ToolStripMenuItem_ctor_2_8a3c7c15.htm
        private void OpenFolder_Click(object sender, EventArgs e) {
            if(filesListView.SelectedItems.Count>0) {
                string filePath=filesListView.SelectedItems[0].Tag.ToString();//.Text;
                string folderPath=filePath;
                if (filePath.Contains(System.IO.Path.DirectorySeparatorChar.ToString()))
                    folderPath = filePath.Substring(0, filePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
                System.Diagnostics.Process.Start(folderPath);
                //throw new Exception("Not implemented yet");
            }
        }

        private void startButton_Click(object sender, EventArgs e) {
            networkAdaptersComboBox.Enabled=false;
            startButton.Enabled=false;
            startCapturingToolStripMenuItem.Enabled=false;
            //System.IO.Path.GetFullPath(System.Windows.Forms.Application.ExecutablePath)
            //Path.GetDirectoryName(applicationExecutablePath)+"\\";
            if(packetHandlerWrapper.PcapWriter!=null && packetHandlerWrapper.PcapWriter.IsOpen)
                packetHandlerWrapper.PcapWriter.Close();
            string filename = PcapFileHandler.Tools.GenerateCaptureFileName(DateTime.Now);
            //string filename="NM_"+DateTime.Now.ToString("s", System.Globalization.DateTimeFormatInfo.InvariantInfo).Replace(':','-')+".pcap";
            string fileFullPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(System.Windows.Forms.Application.ExecutablePath)) + System.IO.Path.DirectorySeparatorChar + "Captures" + System.IO.Path.DirectorySeparatorChar + filename;
            //make sure to get the right datalink type
            PcapFileHandler.PcapFrame.DataLinkTypeEnum dataLinkType;
            if(sniffer.BasePacketType== NetworkWrapper.PacketReceivedEventArgs.PacketTypes.Ethernet2Packet)
                dataLinkType = PcapFileHandler.PcapFrame.DataLinkTypeEnum.WTAP_ENCAP_ETHERNET;
            else if(sniffer.BasePacketType== NetworkWrapper.PacketReceivedEventArgs.PacketTypes.IEEE_802_11Packet)
                dataLinkType = PcapFileHandler.PcapFrame.DataLinkTypeEnum.WTAP_ENCAP_IEEE_802_11;
            else if(sniffer.BasePacketType== NetworkWrapper.PacketReceivedEventArgs.PacketTypes.IEEE_802_11RadiotapPacket)
                dataLinkType = PcapFileHandler.PcapFrame.DataLinkTypeEnum.WTAP_ENCAP_IEEE_802_11_WLAN_RADIOTAP;
            else if(sniffer.BasePacketType== NetworkWrapper.PacketReceivedEventArgs.PacketTypes.IPv4Packet)
                dataLinkType = PcapFileHandler.PcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP;
            else
                dataLinkType = PcapFileHandler.PcapFrame.DataLinkTypeEnum.WTAP_ENCAP_ETHERNET;

            this.packetHandlerWrapper.PcapWriter=new PcapFileHandler.PcapFileWriter(fileFullPath, dataLinkType);

            //now let's start sniffing!
            sniffer.StartSniffing();

            this.AddPcapFileToCasePanel(fileFullPath, filename);
            
            /*
            ListViewItem pcapFileListViewItem = this.casePanelFileListView.Items.Add(fileFullPath, filename, null);
            pcapFileListViewItem.ToolTipText=fileFullPath;
            pcapFileListViewItem.Tag=new CaseFile(fileFullPath);
             * */
            
        }

        public void AddPcapFileToCasePanel(string fileFullPath, string filename) {
            this.AddPcapFileToCasePanel(new CaseFile(fileFullPath), false);
        }

        public void AddPcapFileToCasePanel(CaseFile caseFile, bool computeMd5) {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { AddPcapFileToCasePanel(caseFile, computeMd5); });
            else {
                ListViewItem pcapFileListViewItem = this.casePanelFileListView.Items.Add(caseFile.FilePathAndName, caseFile.Filename, null);
                if (computeMd5) {
                    caseFile.Md5 = PcapFileHandler.Md5SingletonHelper.Instance.GetMd5Sum(caseFile.FilePathAndName);
                    pcapFileListViewItem.SubItems.Add(caseFile.Md5);
                }
                pcapFileListViewItem.ToolTipText = caseFile.FilePathAndName;
                pcapFileListViewItem.Tag = caseFile;//stores metadata and more
            }
        }

        private void stopButton_Click(object sender, EventArgs e) {
            if(sniffer!=null) {
                sniffer.StopSniffing();
                detectedHostsTreeRebuildButton_Click(sender, e);//in order to rebuild the detected hosts list
            }
            networkAdaptersComboBox.Enabled=true;
            if(!this.networkAdaptersComboBox.SelectedValue.GetType().Equals(typeof(NetworkWrapper.NullAdapter))) {
                startButton.Enabled=true;
                startCapturingToolStripMenuItem.Enabled=true;
            }
            if(packetHandlerWrapper.PcapWriter!=null && packetHandlerWrapper.PcapWriter.IsOpen)
                packetHandlerWrapper.PcapWriter.Close();
            if(packetHandlerWrapper.PcapWriter!=null && packetHandlerWrapper.PcapWriter.Filename!=null && casePanelFileListView.Items.ContainsKey(packetHandlerWrapper.PcapWriter.Filename))
                casePanelFileListView.Items[packetHandlerWrapper.PcapWriter.Filename].SubItems.Add(PcapFileHandler.Md5SingletonHelper.Instance.GetMd5Sum(packetHandlerWrapper.PcapWriter.Filename));
        }

        private void button3_Click(object sender, EventArgs e) {
            eventLog.Clear();
        }

        private void networkAdaptersComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            if(this.networkAdaptersComboBox.SelectedValue.GetType().Equals(typeof(NetworkWrapper.WinPCapAdapter))) {
                this.sniffer=new NetworkWrapper.WinPCapSniffer((NetworkWrapper.WinPCapAdapter)this.networkAdaptersComboBox.SelectedValue);
                this.startButton.Enabled=true;
                this.startCapturingToolStripMenuItem.Enabled=true;
            }
            else if(this.networkAdaptersComboBox.SelectedValue.GetType().Equals(typeof(NetworkWrapper.SocketAdapter))) {
                try {
                    this.sniffer=new NetworkWrapper.SocketSniffer((NetworkWrapper.SocketAdapter)this.networkAdaptersComboBox.SelectedValue);
                    this.startButton.Enabled=true;
                    this.startCapturingToolStripMenuItem.Enabled=true;
                }
                catch(System.Net.Sockets.SocketException ex) {
                    MessageBox.Show("Ensure that you have administrator rights (required in order to use Socket connections)\n\n"+ex.Message, "NetworkMiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.networkAdaptersComboBox.SelectedIndex=0;
                    this.startButton.Enabled=false;
                    this.startCapturingToolStripMenuItem.Enabled=false;
                }
                catch(System.Exception ex) {
                    MessageBox.Show(ex.Message, "NetworkMiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.networkAdaptersComboBox.SelectedIndex=0;
                    this.startButton.Enabled=false;
                    this.startCapturingToolStripMenuItem.Enabled=false;
                }

            }
            /*else if(this.networkAdaptersComboBox.SelectedValue.GetType().Equals(typeof(NetworkWrapper.MicroOlapAdapter))) {
                this.sniffer=new NetworkWrapper.MicroOlapSniffer((NetworkWrapper.MicroOlapAdapter)this.networkAdaptersComboBox.SelectedValue);
                this.startButton.Enabled=true;
                this.startCapturingToolStripMenuItem.Enabled=true;
            }*/
            else if(this.networkAdaptersComboBox.SelectedValue.GetType().Equals(typeof(NetworkWrapper.NullAdapter))) {
                this.startButton.Enabled=false;
                this.startCapturingToolStripMenuItem.Enabled=false;
            }
            else {
                throw new Exception(""+this.networkAdaptersComboBox.SelectedValue.GetType().ToString());
            }
            
        }

        private void button1_Click(object sender, EventArgs e) {
            this.framesTreeView.Nodes.Clear();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            if(sniffer!=null)
                sniffer.StopSniffing();
            this.Close();
        }

        private void loadCleartextDictionaryToolStripMenuItem_Click(object sender, EventArgs e) {
            string dictionaryFile="all-words.txt";
            loadCleartextDictionary(dictionaryFile);
        }

        private void loadCleartextDictionary(string dictionaryFile) {
            if(this.InvokeRequired) {
                GenericStringCallback callback=new GenericStringCallback(loadCleartextDictionary);
                this.Invoke(callback,dictionaryFile);
            }
            else {
                PacketParser.CleartextDictionary.WordDictionary d=new PacketParser.CleartextDictionary.WordDictionary();
                System.IO.FileInfo dictionaryFileInfo;
                if (System.IO.File.Exists(dictionaryFile))
                    dictionaryFileInfo = new System.IO.FileInfo(dictionaryFile);
                else
                    dictionaryFileInfo = new System.IO.FileInfo(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + System.IO.Path.DirectorySeparatorChar + "CleartextTools" + System.IO.Path.DirectorySeparatorChar + dictionaryFile);
                d.LoadDictionaryFile(dictionaryFileInfo.FullName);
                packetHandlerWrapper.PacketHandler.Dictionary=d;
                //this.showDetectedCleartextCheckBox.Enabled=true;
                this.dictionaryNameLabel.Text = dictionaryFileInfo.Name;
            }
        }

        /*
        private void NetworkMinerForm_Load(object sender, EventArgs e) {

            //this.reportViewer1.RefreshReport();
        }

        private void crystalReportViewer1_Load(object sender, EventArgs e) {

        }*/

        private void button1_Click_1(object sender, EventArgs e) {
            this.cleartextTextBox.Clear();
        }

        private void detectedHostsTreeRebuildButton_Click(object sender, EventArgs e) {
            this.networkHostTreeView.Nodes.Clear();
            if(this.packetHandlerWrapper==null)
                return;//avoid using null

            //there might be errors if a host is added to the list during the period when this for-loop runs
            try {

                PacketParser.NetworkHost[] sortedHostArray=null;

                //System.Threading.Monitor.Enter(this.packetHandler.DetectedHosts
                //The quick "golden hammer": http://csharpfeeds.com/post.aspx?id=5490
                lock(((System.Collections.ICollection)this.packetHandlerWrapper.PacketHandler.NetworkHostList.Hosts).SyncRoot) {

                    sortedHostArray=new PacketParser.NetworkHost[this.packetHandlerWrapper.PacketHandler.NetworkHostList.Hosts.Count];
                    this.packetHandlerWrapper.PacketHandler.NetworkHostList.Hosts.CopyTo(sortedHostArray, 0);
                }

                //now move all hosts to a sorted list which sorts on the right attribute
                if(this.GetHostSortOrderIndex() == 0){//IP
                    //default sorting would be fine
                    Array.Sort(sortedHostArray);
                }
                else if(this.GetHostSortOrderIndex() == 1) {//MAC
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.MacAddressComparer());
                }
                else if(this.GetHostSortOrderIndex() ==2) {//Hostname
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.HostNameComparer());
                }
                else if(this.GetHostSortOrderIndex() ==3) {//Sent packets
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.SentPacketsComparer());
                }
                 else if(this.GetHostSortOrderIndex() ==4) {//Received packets
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.ReceivedPacketsComparer());
                }
                else if(this.GetHostSortOrderIndex() ==5) {//Sent bytes
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.SentBytesComparer());
                }
                else if(this.GetHostSortOrderIndex() ==6) {//Received bytes
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.ReceivedBytesComparer());
                }
                else if(this.GetHostSortOrderIndex() ==7) {//Open ports count
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.OpenTcpPortsCountComparer());
                }
                else if(this.GetHostSortOrderIndex()==8) {//OS
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.OperatingSystemComparer());
                }
                else if(this.GetHostSortOrderIndex()==9) {//router hops
                    Array.Sort(sortedHostArray, new PacketParser.NetworkHost.TimeToLiveDistanceComparer());
                }


                if(sortedHostArray!=null) {
                    foreach(PacketParser.NetworkHost host in sortedHostArray) {
                        AddNetworkHostToTreeView(host);
                        //inget mer?
                    }
                }
            }
            catch(Exception ex) {
                //this.ShowError("Error updating hostlist: "+ex.Message);
                this.networkHostTreeView.Nodes.Clear();//just to show that something went wrong...
                //System.Diagnostics.Debugger.Break();//this should not occur!
            }

        }


        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            //this.openFileDialog1.ShowDialog();
            
            if(this.openPcapFileDialog.ShowDialog()==DialogResult.OK) {
                this.LoadPcapFile(this.openPcapFileDialog.FileName);

                //let's accept .cap .pcap and .dump (kismet dumpfile) and .dmp
                //if(this.FileFormatIsSupported(this.openFileDialog1.FileName)) {
                    
                //}
                //detectedHostsTreeRebuildButton_Click(sender, e);
            }
        }

        public void LoadPcapFileBlocking(string filePathAndName) {
            BackgroundWorker bw=this.LoadPcapFile(filePathAndName);
            while(bw!=null && bw.IsBusy) {
                if(bw.CancellationPending) {
                    break;
                }
                else {
                    Application.DoEvents();
                }
            }
        }

        /// <summary>
        /// Loads a pcap file into NetworkMiner
        /// </summary>
        /// <param name="filename">The full path and filename of the pcap file to open</param>
        public BackgroundWorker LoadPcapFile(string filePathAndName) {
            /*
            if (filePathAndName.EndsWith(".pcap", StringComparison.InvariantCultureIgnoreCase) ||
                filePathAndName.EndsWith(".cap", StringComparison.InvariantCultureIgnoreCase) ||
                filePathAndName.EndsWith(".log", StringComparison.InvariantCultureIgnoreCase) ||
                filePathAndName.EndsWith(".dump", StringComparison.InvariantCultureIgnoreCase) ||
                filePathAndName.EndsWith(".dmp", StringComparison.InvariantCultureIgnoreCase) ||
                filePathAndName.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase) ||
                filePathAndName.EndsWith(".raw", StringComparison.InvariantCultureIgnoreCase) ||
                filePathAndName.Contains(".pcap") ||
                filePathAndName.Contains(".dmp")) {*/
            if(this.casePanelFileListView.Items.ContainsKey(filePathAndName))
                MessageBox.Show("File is already opened in the current case");
            else {

                PcapFileHandler.PcapFileReader pcapReader=null;
                try {
                    CaseFile caseFile = new CaseFile(filePathAndName);

                    pcapReader = new PcapFileHandler.PcapFileReader(filePathAndName, this.pcapFileReaderQueueSize, new PcapFileHandler.PcapFileReader.CaseFileLoadedCallback(this.caseFileLoaded));
                    

                    LoadingProcess lp = new LoadingProcess(pcapReader, caseFile);
                    lp.StartPosition = FormStartPosition.CenterParent;
                    lp.Show();
                    lp.Update();
                    BackgroundWorker fileWorker = new BackgroundWorker();
                    fileWorker.DoWork += new DoWorkEventHandler(fileWorker_DoWork);
                    fileWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(detectedHostsTreeRebuildButton_Click);
                    fileWorker.WorkerSupportsCancellation = true;
                    lp.Worker = fileWorker;
                    fileWorker.RunWorkerAsync(lp);

                    /*
                    ListViewItem fileItem=this.casePanelFileListView.Items.Add(caseFile.FilePathAndName, caseFile.Filename, null);
                        
                    caseFile.Md5=PcapFileHandler.Md5SingletonHelper.Instance.GetMd5Sum(caseFile.FilePathAndName);
                    fileItem.SubItems.Add(caseFile.Md5);

                    fileItem.Tag=caseFile;

                    fileItem.ToolTipText=caseFile.FilePathAndName;
                        * */
                    this.AddPcapFileToCasePanel(caseFile, true);//computes MD5
                    /*//write caseFile info to MetaData
                    System.Collections.Specialized.NameValueCollection metadata = new System.Collections.Specialized.NameValueCollection();
                    metadata.Add("Filename", caseFile.Filename);
                    metadata.Add("MD5", caseFile.Md5);
                    metadata.Add("Frames count", caseFile.FramesCount.ToString());
                    metadata.Add("First timestamp", caseFile.FirstFrameTimestamp.ToString());
                    metadata.Add("Last timestamp", caseFile.LastFrameTimestamp.ToString());
                    metadata.Add(pcapReader.PcapParserMetadata);*/
                    return fileWorker;
                }
                catch (System.IO.InvalidDataException ex) {
                    if (ex.Message.Contains("Magic number is A0D0D0A"))
                        MessageBox.Show("This is a PcapNg file. NetworkMiner Professional is required to parse PcapNg files.\n\nNetworkMiner Professional can be purchased from Netresec's website:\nhttp://www.netresec.com/", "PcapNg Files Not Supported");
                    else
                        MessageBox.Show("Error opening pcap file: " + ex.Message);
                    if (pcapReader != null)
                        pcapReader.Dispose();
                }
                catch (System.UnauthorizedAccessException) {
                    MessageBox.Show("Unauthorized to open file " + filePathAndName, "Unauthorized Access");
                    if (pcapReader != null)
                        pcapReader.Dispose();
                }
#if !DEBUG
                catch (Exception ex) {
                    MessageBox.Show("Error opening pcap file: " + ex.Message);
                    if (pcapReader != null)
                        pcapReader.Dispose();
                }
#endif
            }
            /*
            }
            else
                MessageBox.Show("Not a valid packet capture file format. Supported formats are: .pcap, .cap, .log, .dump, .dmp and .dat\n"+filePathAndName);
        */
            return null;
        }

        void fileWorker_DoWork(object sender, DoWorkEventArgs e) {
            //extract the argument (LoadingProcess)
            LoadingProcess lp=(LoadingProcess)e.Argument;
            int percentRead=0;

            PcapFileHandler.PcapFileReader pcapReader=lp.PcapReader;
            

            int millisecondsToSleep=1;

            //foreach(PcapFileHandler.PcapFrame pcapPacket in pcapReader.PacketEnumerator(Application.DoEvents, null)) {//REMOVED 2014-06-24
            foreach(PcapFileHandler.PcapFrame pcapPacket in pcapReader.PacketEnumerator()) {


                millisecondsToSleep=1;
                while(packetHandlerWrapper.PacketHandler.FramesInQueue>100) {
                    System.Threading.Thread.Sleep(millisecondsToSleep);
                    if(millisecondsToSleep<200)
                        millisecondsToSleep*=2;
                    //Application.DoEvents();//REMOVED 2014-06-24
                }

                PacketParser.Frame frame = packetHandlerWrapper.PacketHandler.GetFrame(pcapPacket.Timestamp, pcapPacket.Data, pcapPacket.DataLinkType);
                packetHandlerWrapper.PacketHandler.AddFrameToFrameParsingQueue(frame);

                if(pcapReader.PercentRead!=percentRead) {
                    percentRead=pcapReader.PercentRead;

                    /*this.Invoke((EmptyDelegateCallback)delegate() { lp.Percent=percentRead; });
                    this.Invoke(new EmptyDelegateCallback(lp.Update));*/
                    try {
                        if(lp.Visible) {
                            lp.Invoke((EmptyDelegateCallback)delegate() { lp.Percent=percentRead; });
                            lp.Invoke(new EmptyDelegateCallback(lp.Update));
                        }
                    }
                    catch(Exception) {
                        break;
                    }
                }
            }
            lp.CaseFile.AddMetadata(pcapReader.PcapParserMetadata);

            millisecondsToSleep=1;
            while(packetHandlerWrapper.PacketHandler.FramesInQueue>0) {//just to make sure we dont finish too early
                System.Threading.Thread.Sleep(millisecondsToSleep);
                if(millisecondsToSleep<5000)
                    millisecondsToSleep*=2;
                //Application.DoEvents();//REMOVED 2014-06-24
            }
            try {
                lp.Invoke(new EmptyDelegateCallback(lp.Close));
            }
            catch(Exception){}//the form might already be closed
        }

        
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e) {
            /*

             * */
        }

        private void resetCapturedDataToolStripMenuItem_Click(object sender, EventArgs e) {
            this.ResetCapturedData(true, true);
        }

        private void ResetCapturedData(bool removeCapturedFiles, bool clearIpColorHandler){
            this.networkHostTreeView.Nodes.Clear();
            this.framesTreeView.Nodes.Clear();
            this.imagesListView.Items.Clear();
            this.imageList.Images.Clear();
            this.messagesListView.Items.Clear();
            this.messageAttributeListView.Items.Clear();
            this.messageTextBox.Text="[no message selected]";
            this.filesListView.Items.Clear();
            this.credentialsListView.Items.Clear();
            this.sessionsListView.Items.Clear();
            this.dnsListView.Items.Clear();
            this.parametersListView.Items.Clear();
            this.detectedKeywordsListView.Items.Clear();
            this.cleartextTextBox.Clear();
            this.eventLog.Clear();
            this.casePanelFileListView.Items.Clear();

            this.SetControlText(this.tabPageDetectedHosts, "Hosts");
            this.SetControlText(this.tabPageReceivedFrames, "Frames");
            this.SetControlText(this.tabPageFiles, "Files");
            this.SetControlText(this.tabPageImages, "Images");
            this.SetControlText(this.tabPageMessages, "Messages");
            this.SetControlText(this.tabPageCredentials, "Credentials");
            this.SetControlText(this.tabPageSessions, "Sessions");
            this.SetControlText(this.tabPageDns, "DNS");
            this.SetControlText(this.tabPageParameters, "Parameters");

            this.packetHandlerWrapper.ResetCapturedData();
            this.nFilesReceived=0;

            if(removeCapturedFiles) {
                string capturesDirectory=System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(System.Windows.Forms.Application.ExecutablePath))+ System.IO.Path.DirectorySeparatorChar+"Captures";
                if(System.IO.Directory.Exists(capturesDirectory)){
                    foreach(string pcapFile in System.IO.Directory.GetFiles(capturesDirectory))
                        try {
                            System.IO.File.Delete(pcapFile);
                        }
                        catch {
                            this.ShowError("Error deleting file \"" + pcapFile + "\"", DateTime.Now);
                        }
                }
            }
            if(clearIpColorHandler && this.ipColorHandler!=null)
                this.ipColorHandler.Clear();


        }

        private void aboutNetworkMinerToolStripMenuItem_Click(object sender, EventArgs e) {
            new NetworkMinerAboutBox(this.productLink, this.aboutText).ShowDialog();
        }


        internal void ShowDetectedKeyword(PacketParser.Frame frame, int keywordIndex, int keywordLength, PacketParser.NetworkHost sourceHost, PacketParser.NetworkHost destinationHost, string sourcePort, string destinationPort) {
            string[] itemData=new string[8];
            itemData[0]=frame.FrameNumber.ToString();
            itemData[1]=frame.Timestamp.ToString();
            //byte[] keyword=new byte[keywordLength];
            //Array.Copy(frame.Data, keywordIndex, keyword, 0, keywordLength);
            string keywordString="";
            string keywordHexString="";
            for(int i=0;i<keywordLength;i++){
                keywordString+=(char)frame.Data[keywordIndex+i];
                keywordHexString+=frame.Data[keywordIndex+i].ToString("X2");
            }
            itemData[2]=System.Text.RegularExpressions.Regex.Replace(keywordString, @"[^ -~]", ".")+" [0x"+keywordHexString+"]";//regex from Eric Gunnerson's blog (which is really good)
            //context
            string contextString="";
            for(int i=Math.Max(0, keywordIndex-32); i<Math.Min(frame.Data.Length, keywordIndex+keywordLength+32); i++) {
                contextString+=(char)frame.Data[i];
            }
            itemData[3]=System.Text.RegularExpressions.Regex.Replace(contextString, @"[^ -~]", ".");//regex from Eric Gunnerson's blog (which is really good)
            if(sourceHost!=null)
                itemData[4]=sourceHost.ToString();
            itemData[5]=sourcePort;
            if(destinationHost!=null)
                itemData[6]=destinationHost.ToString();
            itemData[7]=destinationPort;
            ListViewItem item=new ListViewItem(itemData);

            /*
            if(this.ipColors.ContainsKey(sourceHost.IPAddress))
                item.BackColor=this.ipColors[sourceHost.IPAddress];
            else if(this.ipColors.ContainsKey(destinationHost.IPAddress))
                item.BackColor=this.ipColors[destinationHost.IPAddress];
             * */
            if(this.ipColorHandler!=null) {
                this.ipColorHandler.Colorize(sourceHost.IPAddress, item);
                this.ipColorHandler.Colorize(destinationHost.IPAddress, item);
            }

            Invoke(new AddItemToListView(this.detectedKeywordsListView.Items.Add), item);
            //this.detectedKeywordsListView.Items.Add(item);
        }


        private void addKeyword(object sender, EventArgs e) {
            string errorMessage;
            if (this.TryAddKeywordToListBox(this.keywordTextBox.Text, out errorMessage)) {
                this.keywordTextBox.Text = "";
                packetHandlerWrapper.UpdateKeywords((System.Collections.IEnumerable)this.keywordListBox.Items);
            }
            else
                MessageBox.Show(errorMessage);
        }
        /*

                if (this.keywordTextBox.Text.Length < 3)
                    MessageBox.Show("Keywords must be at least 3 bytes long to avoid false positives");
                else {

                    if (this.keywordTextBox.Text.StartsWith("0x")) {//hex string
                        try {
                            PacketParser.Utils.ByteConverter.ToByteArrayFromHexString(this.keywordTextBox.Text);//to force valid hex
                            this.keywordListBox.Items.Add(this.keywordTextBox.Text);
                            this.keywordTextBox.Text = "";

                            //Lägg till keywordet till PacketHandler.PacketHandler!!!
                        }
                        catch (Exception ex) {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    else {//normal string
                        this.keywordListBox.Items.Add(this.keywordTextBox.Text);
                        this.keywordTextBox.Text = "";
                    }
                    packetHandlerWrapper.UpdateKeywords((System.Collections.IEnumerable)this.keywordListBox.Items);

                }
        }*/

        private bool TryAddKeywordToListBox(string keyword, out string errorMessage) {
            if (keyword.Length < 3) {
                errorMessage = "Keywords must be at least 3 bytes long to avoid false positives";
                return false;
            }
            else {

                if (this.keywordTextBox.Text.StartsWith("0x")) {//hex string
                    try {
                        PacketParser.Utils.ByteConverter.ToByteArrayFromHexString(keyword);//to force valid hex
                        this.keywordListBox.Items.Add(keyword);
                        

                        //Lägg till keywordet till PacketHandler.PacketHandler!!!
                    }
                    catch (Exception ex) {
                        errorMessage = ex.Message;
                        return false;
                    }
                }
                else {//normal string
                    this.keywordListBox.Items.Add(keyword);
                    
                }
                errorMessage = null;
                return true;

            }
        }

        private void removeKeywordButton_Click(object sender, EventArgs e) {
            List<object> objectsToRemove=new List<object>();
            foreach(object o in this.keywordListBox.SelectedItems)
                objectsToRemove.Add(o);
            foreach(object o in objectsToRemove)
                this.keywordListBox.Items.Remove(o);
            packetHandlerWrapper.UpdateKeywords((System.Collections.IEnumerable)this.keywordListBox.Items);
        }

        #region ListViewSorting
        //from: ms-help://MS.VSCC.v80/MS.MSDN.v80/MS.NETDEVFX.v20.en/CPref17/html/P_System_Windows_Forms_ListView_ListViewItemSorter.htm

        private void ListViewColumnClick(object o, ColumnClickEventArgs e) {
            // Set the ListViewItemSorter property to a new ListViewItemComparer 
            // object. Setting this property immediately sorts the 
            // ListView using the ListViewItemComparer object.

            //made generic with help from: http://www.codeproject.com/KB/list/BindSortAutosizing.aspx
            ListView sortListView=(ListView)o;

            sortListView.ListViewItemSorter=new ListViewItemComparer(e.Column);// .ListViewItemSorter = new ListViewItemComparer(e.Column);
            
        }


        // Implements the manual sorting of items by columns.
        class ListViewItemComparer : System.Collections.IComparer {
            private int col;
            public ListViewItemComparer() {
                col = 0;
            }
            public ListViewItemComparer(int column) {
                col = column;
            }
            public int Compare(ListViewItem x, ListViewItem y) {
                //first check if we might have numbered values
                string xText=x.SubItems[col].Text;
                string yText=y.SubItems[col].Text;

                //int xInt, yInt;
                if(xText.Length>0 && yText.Length>0) {
                    if(Char.IsNumber(xText[0]) && Char.IsNumber(yText[0])) {
                        //we have two numbers!
                        //see which one is largest
                        double xDouble = PacketParser.Utils.ByteConverter.StringToClosestDouble(xText);
                        double yDouble = PacketParser.Utils.ByteConverter.StringToClosestDouble(yText);
                        if(xDouble<yDouble)
                            return (int)(xDouble-yDouble-1);
                        else if(xDouble>yDouble)
                            return (int)(xDouble-yDouble+1);
                    }
                    //if not just compare them normally
                }
                return String.Compare(x.SubItems[col].Text, y.SubItems[col].Text);
            }
            public int Compare(object x, object y) {
                return Compare((ListViewItem)x, (ListViewItem)y);
                //return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
            }

        }
        #endregion

        private void hostSortOrderComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            //HÄR SKA detailsHeader LIGGA Enabled MASSA OLIKA SORTERINGSORDNINGAR:
            //IP, HOTSNAME, SENT PACKETS, RECEIVED PACKETS, MAC ADDRESS
            this.detectedHostsTreeRebuildButton_Click(sender, e);
        }

        private void NetworkMinerForm_FormClosed(object sender, FormClosedEventArgs e) {
            //packetHandlerThread.Abort();
            packetHandlerWrapper.AbortBackgroundThreads();
        }

        private void NetworkMinerForm_FormClosing(object sender, FormClosingEventArgs e) {
            stopButton_Click(sender, e);
        }

        //http://blogs.techrepublic.com.com/howdoi/?p=148
        private void NetworkMinerForm_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                if (!AllPathsAreInAssembledFilesDirectory((string[])e.Data.GetData(DataFormats.FileDrop)))
                    e.Effect = DragDropEffects.Copy;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private bool AllPathsAreInAssembledFilesDirectory(string[] paths) {
            foreach (string p in paths) {
                if (!p.Contains(PacketParser.FileTransfer.FileStreamAssembler.ASSMEBLED_FILES_DIRECTORY))
                    return false;
                if (!this.IsSubDirectoryOf(new System.IO.DirectoryInfo(p), new System.IO.DirectoryInfo(this.packetHandlerWrapper.PacketHandler.OutputDirectory + PacketParser.FileTransfer.FileStreamAssembler.ASSMEBLED_FILES_DIRECTORY)))
                    return false;
            }
            return true;
        }
        private bool IsSubDirectoryOf(System.IO.DirectoryInfo path, System.IO.DirectoryInfo directory) {
            if (path.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar) == directory.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar))
                return true;
            else if(path.FullName.Length < directory.FullName.Length)
                return false;
            else if(path.Parent == null)
                return false;
            else return IsSubDirectoryOf(path.Parent, directory);

        }

        //http://blogs.techrepublic.com.com/howdoi/?p=148
        private void NetworkMinerForm_DragDrop(object sender, DragEventArgs e) {
            if(e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[] filenames=(string[])e.Data.GetData(DataFormats.FileDrop);
                foreach(string fileNameAndLocation in filenames) {
                    this.LoadPcapFileBlocking(fileNameAndLocation);
                }
            }
        }

        private void cleartextSearchModeComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            this.packetHandlerWrapper.CleartextSearchModeSelectedIndex=this.cleartextSearchModeComboBox.SelectedIndex;
        }

        private void keywordListBox_ControlAdded(object sender, ControlEventArgs e) {
            packetHandlerWrapper.UpdateKeywords((System.Collections.IEnumerable)this.keywordListBox.Items);
        }

        private void keywordListBox_ControlRemoved(object sender, ControlEventArgs e) {
            packetHandlerWrapper.UpdateKeywords((IEnumerable<string>)this.keywordListBox.Items);
        }

        private void reloadCaseFilesButton_Click(object sender, EventArgs e) {
            List<string> files=new List<string>(this.casePanelFileListView.Items.Count);
            foreach(ListViewItem fileItem in this.casePanelFileListView.Items)
                files.Add(fileItem.Name);

            this.ResetCapturedData(false, false);

            foreach(string fileNameAndLocation in files) {
                this.LoadPcapFileBlocking(fileNameAndLocation);
                /*
                BackgroundWorker bw=LoadPcapFile(fileNameAndLocation);
                while(bw.IsBusy)
                    Application.DoEvents();
                //System.Threading.Thread.Sleep
                 * */
            }
        }

        private void removeCaseFileMenuItem_Click(object sender, EventArgs e) {
            ListView.SelectedListViewItemCollection itemsToRemove = this.casePanelFileListView.SelectedItems;
            foreach(ListViewItem item in itemsToRemove)
                this.casePanelFileListView.Items.Remove(item);
        }

        private void clearAnomaliesButton_Click(object sender, EventArgs e) {
            this.eventLog.Clear();
        }

        private void openParentFolderToolStripMenuItem_Click(object sender, EventArgs e) {

            if(casePanelFileListView.SelectedItems.Count>0) {
                CaseFile caseFile=(CaseFile)casePanelFileListView.SelectedItems[0].Tag;
                //string filePath=caseFile.FilePathAndName;//.Text;
                string folderPath=caseFile.FilePathAndName;
                if (caseFile.FilePathAndName.Contains(System.IO.Path.DirectorySeparatorChar.ToString()))
                    folderPath = caseFile.FilePathAndName.Substring(0, caseFile.FilePathAndName.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
                System.Diagnostics.Process.Start(folderPath);
            }
        }

        private void printReportToolStripMenuItem_Click(object sender, EventArgs e) {
            if(this.reportGenerator!=null)
                this.reportGenerator.ShowReport(this);
            else
                MessageBox.Show("Not implemented");
        }

        private void selectHostColorMenuItem_Click(object sender, EventArgs e) {
            if(this.colorDialog1.ShowDialog()==DialogResult.OK) {
                if(this.ipColorHandler==null) {
                    MessageBox.Show("Not implemented");
                    return;
                }

                TreeNode n=this.networkHostTreeView.SelectedNode;
                while(n!=null && n.GetType()!=typeof(NetworkHostTreeNode)) {
                    n=n.Parent;
                }
                if(this.ipColorHandler!=null && n!=null) {
                    NetworkHostTreeNode node=(NetworkHostTreeNode)n;
                    //this.ipColors[node.NetworkHost.IPAddress]=this.colorDialog1.Color;
                    //node.BackColor=this.colorDialog1.Color;
                    this.ipColorHandler.AddColor(node.NetworkHost.IPAddress, this.colorDialog1.Color);
                    if(this.ipColorHandler.ReloadRequired) {
                        this.ipColorHandler.Colorize(node.NetworkHost.IPAddress, node);
                        if(this.ipColorHandler.Keys.Count<2)
                            MessageBox.Show("You need to press \"Reload Case Files\" to update the colors in other tabs");
                    }
                    
                }
            }
        }
        private void removeHostColorToolStripMenuItem_Click(object sender, EventArgs e) {
            TreeNode n=this.networkHostTreeView.SelectedNode;
            while(n!=null && n.GetType()!=typeof(NetworkHostTreeNode)) {
                n=n.Parent;
            }
            if(n!=null && this.ipColorHandler!=null) {
                NetworkHostTreeNode node=(NetworkHostTreeNode)n;

                this.ipColorHandler.RemoveColor(node.NetworkHost.IPAddress);

                if(this.ipColorHandler.ReloadRequired) {
                    node.BackColor=new Color();//a "Reload Case Files" is required
                    MessageBox.Show("You need to press \"Reload Case Files\" to update the colors in other tabs");
                }
            }
        }

        private void networkHostTreeView_MouseDown(object sender, MouseEventArgs e) {
            //http://www.knightoftheroad.com/post/2008/02/Treeview-tight-click-select.aspx
            if(e.Button!= MouseButtons.None)
                networkHostTreeView.SelectedNode=networkHostTreeView.GetNodeAt(e.X, e.Y);
        }

        private void hostColorMenuStrip_Opening(object sender, CancelEventArgs e) {
            TreeNode n=this.networkHostTreeView.SelectedNode;
            while(n!=null && n.GetType()!=typeof(NetworkHostTreeNode)) {
                n=n.Parent;
            }
            if(n!=null && this.ipColorHandler!=null) {
                NetworkHostTreeNode node=(NetworkHostTreeNode)n;
                if(this.ipColorHandler.Keys.Contains(node.NetworkHost.IPAddress))
                    this.removeHostColorToolStripMenuItem.Enabled=true;
                else
                    this.removeHostColorToolStripMenuItem.Enabled=false;
            }
        }

        private void copyParameterNameToolStripMenuItem_Click(object sender, EventArgs e) {
            if(this.parametersListView.SelectedItems.Count>0) {
                System.Collections.Generic.KeyValuePair<string, string> nameValueTag=(System.Collections.Generic.KeyValuePair<string, string>)this.parametersListView.SelectedItems[0].Tag;
                Clipboard.SetDataObject(nameValueTag.Key, true);
            }
            
        }

        private void copyParameterValueToolStripMenuItem_Click(object sender, EventArgs e) {
            if(this.parametersListView.SelectedItems.Count>0) {
                System.Collections.Generic.KeyValuePair<string, string> nameValueTag=(System.Collections.Generic.KeyValuePair<string, string>)this.parametersListView.SelectedItems[0].Tag;
                Clipboard.SetDataObject(nameValueTag.Value, true);
            }
        }

        private void copyTextToolStripMenuItem_Click(object sender, EventArgs e) {
            TreeNode n=this.networkHostTreeView.SelectedNode;
            if(n!=null) {
                Clipboard.SetDataObject(n.Text, true);
            }
        }

        private void caseFileLoaded(string filePathAndName, int packetsCount, DateTime firstFrameTimestamp, DateTime lastFrameTimestamp) {
            //ensure that the correct thread is performing the operation
            if(this.casePanelFileListView.InvokeRequired){
                object[] args = new object[4];
                args[0] = filePathAndName;
                args[1] = packetsCount;
                args[2] = firstFrameTimestamp;
                args[3] = lastFrameTimestamp;
                PcapFileHandler.PcapFileReader.CaseFileLoadedCallback cDelegate = new PcapFileHandler.PcapFileReader.CaseFileLoadedCallback(caseFileLoaded);
                this.BeginInvoke(cDelegate, args);
            }
            else{
                //set the CaseFile data
                foreach(ListViewItem item in this.casePanelFileListView.Items) {
                    if(item.Tag!=null) {
                        CaseFile caseFile=(CaseFile)item.Tag;
                        if(caseFile.FilePathAndName==filePathAndName) {
                            caseFile.FirstFrameTimestamp=firstFrameTimestamp;
                            caseFile.LastFrameTimestamp=lastFrameTimestamp;
                            caseFile.FramesCount=packetsCount;
                            item.ToolTipText = caseFile.FilePathAndName+"\nStart : "+caseFile.FirstFrameTimestamp.ToString()+"\nEnd : "+caseFile.LastFrameTimestamp.ToString()+"\nFrames : "+caseFile.FramesCount;
                        }
                    }
                }
            }
        }

        private void messagesListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e) {
            KeyValuePair<System.Collections.Specialized.NameValueCollection, string> attributesAndMessage = (KeyValuePair<System.Collections.Specialized.NameValueCollection, string>)e.Item.Tag;
            /*
            messageAttributePropertyGrid.DataBindings.Clear();

            DataTable dt = new DataTable();
            DataColumn nameColumn = new DataColumn("Attribute");
            nameColumn.DataType = System.Type.GetType("System.String");
            DataColumn valueColumn = new DataColumn("Value");
            valueColumn.DataType = System.Type.GetType("System.String");
            dt.Columns.Add(nameColumn);
            dt.Columns.Add(valueColumn);
            */
            this.messageAttributeListView.Items.Clear();

            //this.messageAttributesTreeView.Nodes.Clear();
            foreach(string attributeName in attributesAndMessage.Key.AllKeys) {
                /*
                TreeNode valueNode = new TreeNode(attributesAndMessage.Key[attributeName]);
                TreeNode[] valueNodes = new TreeNode[1];
                valueNodes[0] = valueNode;
                TreeNode node = new TreeNode(attributeName, valueNodes);
                this.messageAttributesTreeView.Nodes.Add(node);
                */

                /*
                DataRow row = table.NewRow();
                row[nameColumn] = attributeName;
                row[valueColumn] = attributesAndMessage.Key[attributeName];
                dt.Rows.Add(row);
                */
                ListViewItem item = new ListViewItem(attributeName);
                item.SubItems.Add(attributesAndMessage.Key[attributeName]);
                this.messageAttributeListView.Items.Add(item);

                
                
            }


            this.messageTextBox.Text = attributesAndMessage.Value;
        }

        private void ShowSaveCsvDialog(ListView listView) {
            try {
                if (this.dataExporterFactory != null) {
                    if (this.saveCsvFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {

                        using (ToolInterfaces.IDataExporter exporter = this.dataExporterFactory.CreateDataExporter(this.saveCsvFileDialog.FileName)) {
                            exporter.Export(listView, true);
                        }
                    }
                }
                else
                    MessageBox.Show("Not implemented");
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void credentialsToolStripMenuItem_Click(object sender, EventArgs e) {
            this.ShowSaveCsvDialog(this.credentialsListView);
        }

        private void dnsRecordsToolStripMenuItem_Click(object sender, EventArgs e) {
            this.ShowSaveCsvDialog(this.dnsListView);
        }

        private void fileInfosToolStripMenuItem_Click(object sender, EventArgs e) {
            this.ShowSaveCsvDialog(this.filesListView);
        }

        private void hostsToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                if (this.dataExporterFactory != null) {
                    if (this.saveCsvFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {

                        using (ToolInterfaces.IDataExporter exporter = this.dataExporterFactory.CreateDataExporter(this.saveCsvFileDialog.FileName)) {
                            foreach (TreeNode n in this.networkHostTreeView.Nodes) {
                                NetworkHostTreeNode host = (NetworkHostTreeNode)n;

                                List<string> items = new List<string>();
                                items.Add(host.Text);

                                host.BeforeExpand();//to make sure all tags are set

                                foreach (TreeNode child in host.Nodes)
                                    if (child.Tag != null && child.Tag.GetType() == typeof(string))
                                        items.Add((string)child.Tag);

                                exporter.Export(items);

                            }
                        }
                    }
                }
                else
                    MessageBox.Show("Not implemented");
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void messagesToolStripMenuItem_Click(object sender, EventArgs e) {
            this.ShowSaveCsvDialog(this.messagesListView);
        }

        private void parametersToolStripMenuItem_Click(object sender, EventArgs e) {
            this.ShowSaveCsvDialog(this.parametersListView);
        }

        private void sessionsToolStripMenuItem_Click(object sender, EventArgs e) {
            this.ShowSaveCsvDialog(this.sessionsListView);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            this.settingsForm.ShowDialog();
        }

        private void receivePcapOverIPToolStripMenuItem_Click(object sender, EventArgs e) {
            this.pcapOverIpReceiverFactory.GetPcapOverIp(this.packetHandlerWrapper.PacketHandler, new AddCaseFileCallback(this.AddPcapFileToCasePanel), new PcapFileHandler.PcapFileReader.CaseFileLoadedCallback(this.caseFileLoaded), new RunWorkerCompletedEventHandler(detectedHostsTreeRebuildButton_Click), this);
            //PcapFileHandler.PcapFileReader.ReadCompletedCallback
        }

        private void getUpgradeCodeToolStripMenuItem_Click(object sender, EventArgs e) {
            this.upgradeCodeForm.ShowDialog(this);
        }

        private void signWithLicenseToolStripMenuItem_Click(object sender, EventArgs e) {
            this.licenseSignatureForm.ShowDialog(this);
        }

        private void changeCleartextDictionaryButton_Click_1(object sender, EventArgs e) {
            if (this.openTextFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                this.loadCleartextDictionary(this.openTextFileDialog.FileName);
            }
        }

        private void downloadRIPEDBToolStripMenuItem_Click(object sender, EventArgs e) {
            this.hostDetailsGenerator.DownloadDatabase();
        }

        private void credentialsSettingsCheckBox_Click(object sender, EventArgs e) {
            //we need to reload all credentials
            this.credentialsListView.Items.Clear();
            foreach (PacketParser.NetworkCredential credential in this.PacketHandlerWrapper.PacketHandler.GetCredentials()) {
                this.AddCredentialToCredentialList(credential);
            }
        }

        private void clearGUIToolStripMenuItem_Click(object sender, EventArgs e) {
            this.ResetCapturedData(false, true);
        }

        private void keywordTextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return) {
                this.addKeyword(sender, e);
            }
        }

        private void addKeywordsFromFileButton_Click(object sender, EventArgs e) {
            if (this.openTextFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                //this.loadCleartextDictionary(this.openTextFileDialog.FileName);
                //this.packetHandlerWrapper.UpdateKeywords(this.openTextFileDialog.FileName);
                if (this.openTextFileDialog.FileName != null && System.IO.File.Exists(this.openTextFileDialog.FileName)) {
                    List<string> lines = new List<string>();
                    using (System.IO.TextReader reader = System.IO.File.OpenText(this.openTextFileDialog.FileName)) {

                        while (true) {
                            string line = reader.ReadLine();
                            if (line == null)
                                break;//EOF
                            else
                                lines.Add(line);
                        }
                        reader.Close();
                    }
                    foreach (string line in lines) {
                        string errorMessage;
                        /*if (!this.TryAddKeywordToListBox(line, out errorMessage))
                            MessageBox.Show(errorMessage);
                         * */
                        this.TryAddKeywordToListBox(line, out errorMessage);
                    }
                    packetHandlerWrapper.UpdateKeywords((System.Collections.IEnumerable)this.keywordListBox.Items);
                    //this.packetHandlerWrapper.UpdateKeywords(lines);
                    
                }
            }
        }

        private void showMetadataToolStripMenuItem_Click(object sender, EventArgs e) {
            if (casePanelFileListView.SelectedItems.Count > 0) {
                CaseFile caseFile = (CaseFile)casePanelFileListView.SelectedItems[0].Tag;
                //string filePath=caseFile.FilePathAndName;//.Text;
                CaseFileForm caseFileForm = new CaseFileForm(caseFile);
                caseFileForm.Show();
            }
        }

        /// <summary>
        /// Do drag-and-drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void filesListView_MouseDown(object sender, MouseEventArgs e) {
            
            //we don't wanna do drag-and-drop on double clicks
            if (e.Clicks == 1 && e.Button == System.Windows.Forms.MouseButtons.Left) {
                ListViewItem fileItem = filesListView.GetItemAt(e.X, e.Y);
                if (fileItem != null && fileItem.Tag != null) {
                    string filePath = fileItem.Tag.ToString();
                    if (System.IO.File.Exists(filePath)) {
                        DataObject data = new DataObject(DataFormats.FileDrop, new string[] { filePath });
                        this.DoDragDrop(data, DragDropEffects.All);
                    }
                }
                //networkHostTreeView.SelectedNode = networkHostTreeView.GetNodeAt(e.X, e.Y);
            }
        }

        private void tabControl1_MouseDown(object sender, MouseEventArgs e) {
            //we don't wanna do drag-and-drop on double clicks
            if (e.Clicks == 1 && e.Button == System.Windows.Forms.MouseButtons.Left) {
                ListViewItem fileItem = this.filesListView.GetItemAt(e.X, e.Y);
                if (fileItem != null && fileItem.Tag != null) {
                    string filePath = fileItem.Tag.ToString();
                    if (System.IO.File.Exists(filePath)) {
                        DataObject data = new DataObject(DataFormats.FileDrop, new string[] { filePath });
                        this.DoDragDrop(data, DragDropEffects.All);
                    }
                }
                //networkHostTreeView.SelectedNode = networkHostTreeView.GetNodeAt(e.X, e.Y);
            }
        }

        private void imagesListView_MouseDown(object sender, MouseEventArgs e) {
            //we don't wanna do drag-and-drop on double clicks
            if (e.Clicks == 1 && e.Button == System.Windows.Forms.MouseButtons.Left) {
                ListViewItem imageItem = this.imagesListView.GetItemAt(e.X, e.Y);
                if (imageItem != null && imageItem.Tag != null) {
                    PacketParser.FileTransfer.ReconstructedFile file = (PacketParser.FileTransfer.ReconstructedFile)imageItem.Tag;
                    string filePath = file.FilePath;
                    if (System.IO.File.Exists(filePath)) {
                        DataObject data = new DataObject(DataFormats.FileDrop, new string[] { filePath });
                        this.DoDragDrop(data, DragDropEffects.All);
                    }
                }
                //networkHostTreeView.SelectedNode = networkHostTreeView.GetNodeAt(e.X, e.Y);
            }
        }

    }
}