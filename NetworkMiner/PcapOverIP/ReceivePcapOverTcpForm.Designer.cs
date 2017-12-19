namespace NetworkMiner.PcapOverIP {
    partial class ReceivePcapOverTcpForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReceivePcapOverTcpForm));
            this.portNumberSelector = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.useSslCheckBox = new System.Windows.Forms.CheckBox();
            this.startReceivingButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.receivedFramesLabel = new System.Windows.Forms.Label();
            this.socketStateLabel = new System.Windows.Forms.Label();
            this.receivedFramesValueLabel = new System.Windows.Forms.Label();
            this.socketStateValueLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.timoutSelector = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.portNumberSelector)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.timoutSelector)).BeginInit();
            this.SuspendLayout();
            // 
            // portNumberSelector
            // 
            this.portNumberSelector.Location = new System.Drawing.Point(127, 7);
            this.portNumberSelector.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.portNumberSelector.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.portNumberSelector.Name = "portNumberSelector";
            this.portNumberSelector.Size = new System.Drawing.Size(57, 20);
            this.portNumberSelector.TabIndex = 0;
            this.portNumberSelector.Value = new decimal(new int[] {
            57012,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "TCP port";
            // 
            // useSslCheckBox
            // 
            this.useSslCheckBox.AutoSize = true;
            this.useSslCheckBox.Location = new System.Drawing.Point(190, 8);
            this.useSslCheckBox.Name = "useSslCheckBox";
            this.useSslCheckBox.Size = new System.Drawing.Size(68, 17);
            this.useSslCheckBox.TabIndex = 3;
            this.useSslCheckBox.Text = "Use SSL";
            this.useSslCheckBox.UseVisualStyleBackColor = true;
            // 
            // startReceivingButton
            // 
            this.startReceivingButton.Location = new System.Drawing.Point(12, 58);
            this.startReceivingButton.Name = "startReceivingButton";
            this.startReceivingButton.Size = new System.Drawing.Size(172, 23);
            this.startReceivingButton.TabIndex = 5;
            this.startReceivingButton.Text = "Start Receiving";
            this.startReceivingButton.UseVisualStyleBackColor = true;
            this.startReceivingButton.Click += new System.EventHandler(this.startReceivingButton_Click);
            // 
            // stopButton
            // 
            this.stopButton.Enabled = false;
            this.stopButton.Location = new System.Drawing.Point(190, 58);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(108, 23);
            this.stopButton.TabIndex = 6;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // receivedFramesLabel
            // 
            this.receivedFramesLabel.AutoSize = true;
            this.receivedFramesLabel.Location = new System.Drawing.Point(12, 105);
            this.receivedFramesLabel.Name = "receivedFramesLabel";
            this.receivedFramesLabel.Size = new System.Drawing.Size(93, 13);
            this.receivedFramesLabel.TabIndex = 6;
            this.receivedFramesLabel.Text = "Received Frames:";
            // 
            // socketStateLabel
            // 
            this.socketStateLabel.AutoSize = true;
            this.socketStateLabel.Location = new System.Drawing.Point(12, 84);
            this.socketStateLabel.Name = "socketStateLabel";
            this.socketStateLabel.Size = new System.Drawing.Size(72, 13);
            this.socketStateLabel.TabIndex = 7;
            this.socketStateLabel.Text = "Socket State:";
            // 
            // receivedFramesValueLabel
            // 
            this.receivedFramesValueLabel.AutoSize = true;
            this.receivedFramesValueLabel.Location = new System.Drawing.Point(111, 105);
            this.receivedFramesValueLabel.Name = "receivedFramesValueLabel";
            this.receivedFramesValueLabel.Size = new System.Drawing.Size(13, 13);
            this.receivedFramesValueLabel.TabIndex = 8;
            this.receivedFramesValueLabel.Text = "0";
            // 
            // socketStateValueLabel
            // 
            this.socketStateValueLabel.AutoSize = true;
            this.socketStateValueLabel.Location = new System.Drawing.Point(111, 84);
            this.socketStateValueLabel.Name = "socketStateValueLabel";
            this.socketStateValueLabel.Size = new System.Drawing.Size(39, 13);
            this.socketStateValueLabel.TabIndex = 9;
            this.socketStateValueLabel.Text = "Closed";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(110, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Idle timeout (seconds)";
            // 
            // timoutSelector
            // 
            this.timoutSelector.Location = new System.Drawing.Point(127, 34);
            this.timoutSelector.Maximum = new decimal(new int[] {
            3600,
            0,
            0,
            0});
            this.timoutSelector.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.timoutSelector.Name = "timoutSelector";
            this.timoutSelector.Size = new System.Drawing.Size(57, 20);
            this.timoutSelector.TabIndex = 4;
            this.timoutSelector.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // ReceivePcapOverTcpForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(310, 132);
            this.Controls.Add(this.timoutSelector);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.socketStateValueLabel);
            this.Controls.Add(this.receivedFramesValueLabel);
            this.Controls.Add(this.socketStateLabel);
            this.Controls.Add(this.receivedFramesLabel);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startReceivingButton);
            this.Controls.Add(this.useSslCheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.portNumberSelector);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReceivePcapOverTcpForm";
            this.Opacity = 0.9D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Pcap-over-IP";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ReceivePcapOverTcpForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.portNumberSelector)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.timoutSelector)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown portNumberSelector;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox useSslCheckBox;
        private System.Windows.Forms.Button startReceivingButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Label receivedFramesLabel;
        private System.Windows.Forms.Label socketStateLabel;
        private System.Windows.Forms.Label receivedFramesValueLabel;
        private System.Windows.Forms.Label socketStateValueLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown timoutSelector;
    }
}