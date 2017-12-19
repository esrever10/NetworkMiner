namespace NetworkMiner {
    partial class CaseFileForm {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CaseFileForm));
            this.metadataGridView = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.metadataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // metadataGridView
            // 
            this.metadataGridView.AllowUserToAddRows = false;
            this.metadataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.metadataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.metadataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.metadataGridView.Location = new System.Drawing.Point(0, 0);
            this.metadataGridView.Name = "metadataGridView";
            this.metadataGridView.ReadOnly = true;
            this.metadataGridView.RowHeadersVisible = false;
            this.metadataGridView.ShowCellErrors = false;
            this.metadataGridView.ShowCellToolTips = false;
            this.metadataGridView.ShowEditingIcon = false;
            this.metadataGridView.ShowRowErrors = false;
            this.metadataGridView.Size = new System.Drawing.Size(284, 264);
            this.metadataGridView.TabIndex = 0;
            // 
            // CaseFileForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 264);
            this.Controls.Add(this.metadataGridView);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CaseFileForm";
            this.Text = "Metadata";
            ((System.ComponentModel.ISupportInitialize)(this.metadataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView metadataGridView;
    }
}