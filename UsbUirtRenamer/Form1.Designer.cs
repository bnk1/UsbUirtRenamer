using System.Windows.Forms;

namespace UsbUirtRenamer
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ListView lvDevices;
        private System.Windows.Forms.ColumnHeader colIdx;
        private System.Windows.Forms.ColumnHeader colUnit;        // Unit #
        private System.Windows.Forms.ColumnHeader colFriendly;    // Display Name
        private System.Windows.Forms.ColumnHeader colDesc;        // Device Description (SPDRP_DEVICEDESC)
        private System.Windows.Forms.ColumnHeader colHwId;        // Hardware ID
        private System.Windows.Forms.ColumnHeader colDeviceId;    // VID:PID / Serial (from InstanceId)
        private System.Windows.Forms.ColumnHeader colInstanceId;  // Device Instance ID

        private System.Windows.Forms.TextBox txtFilterName;
        private System.Windows.Forms.Label lblFilterName;
        private System.Windows.Forms.Label lblVid;
        private System.Windows.Forms.Label lblPid;
        private System.Windows.Forms.TextBox txtVid;
        private System.Windows.Forms.TextBox txtPid;

        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.TextBox txtNewName;
        private System.Windows.Forms.Label lblNewName;
        private System.Windows.Forms.Button btnElevate;

        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel sslStatus;
        private System.Windows.Forms.ToolStripStatusLabel sslAdmin;

        // Optional: context menu to copy IDs (handlers included in Form1.cs)
        private System.Windows.Forms.ContextMenuStrip cmsList;
        private System.Windows.Forms.ToolStripMenuItem miCopyInstanceId;
        private System.Windows.Forms.ToolStripMenuItem miCopyVidPid;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lvDevices = new ListView();
            colIdx = new ColumnHeader();
            colUnit = new ColumnHeader();
            colFriendly = new ColumnHeader();
            colDesc = new ColumnHeader();
            colHwId = new ColumnHeader();
            colDeviceId = new ColumnHeader();
            colInstanceId = new ColumnHeader();
            cmsList = new ContextMenuStrip(components);
            miCopyInstanceId = new ToolStripMenuItem();
            miCopyVidPid = new ToolStripMenuItem();
            txtFilterName = new TextBox();
            lblFilterName = new Label();
            lblVid = new Label();
            lblPid = new Label();
            txtVid = new TextBox();
            txtPid = new TextBox();
            btnRefresh = new Button();
            btnRename = new Button();
            txtNewName = new TextBox();
            lblNewName = new Label();
            btnElevate = new Button();
            statusStrip = new StatusStrip();
            sslStatus = new ToolStripStatusLabel();
            sslAdmin = new ToolStripStatusLabel();
            BtnReadEeprom = new Button();
            cmsList.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // lvDevices
            // 
            lvDevices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvDevices.Columns.AddRange(new ColumnHeader[] { colIdx, colUnit, colFriendly, colDesc, colHwId, colDeviceId, colInstanceId });
            lvDevices.ContextMenuStrip = cmsList;
            lvDevices.FullRowSelect = true;
            lvDevices.Location = new Point(21, 140);
            lvDevices.Margin = new Padding(5, 6, 5, 6);
            lvDevices.MultiSelect = false;
            lvDevices.Name = "lvDevices";
            lvDevices.Size = new Size(1883, 472);
            lvDevices.TabIndex = 10;
            lvDevices.UseCompatibleStateImageBehavior = false;
            lvDevices.View = View.Details;
            lvDevices.SelectedIndexChanged += LvDevices_SelectedIndexChanged;
            lvDevices.DoubleClick += LvDevices_DoubleClick;
            // 
            // colIdx
            // 
            colIdx.Text = "#";
            colIdx.Width = 40;
            // 
            // colUnit
            // 
            colUnit.Text = "Unit #";
            // 
            // colFriendly
            // 
            colFriendly.Text = "Display Name";
            colFriendly.Width = 240;
            // 
            // colDesc
            // 
            colDesc.Text = "Device Description";
            colDesc.Width = 250;
            // 
            // colHwId
            // 
            colHwId.Text = "Hardware ID";
            colHwId.Width = 260;
            // 
            // colDeviceId
            // 
            colDeviceId.Text = "Device ID (VID:PID / Serial)";
            colDeviceId.Width = 260;
            // 
            // colInstanceId
            // 
            colInstanceId.Text = "Instance ID";
            colInstanceId.Width = 320;
            // 
            // cmsList
            // 
            cmsList.ImageScalingSize = new Size(28, 28);
            cmsList.Items.AddRange(new ToolStripItem[] { miCopyInstanceId, miCopyVidPid });
            cmsList.Name = "cmsList";
            cmsList.Size = new Size(245, 76);
            // 
            // miCopyInstanceId
            // 
            miCopyInstanceId.Name = "miCopyInstanceId";
            miCopyInstanceId.Size = new Size(244, 36);
            miCopyInstanceId.Text = "Copy Instance ID";
            miCopyInstanceId.Click += MiCopyInstanceId_Click;
            // 
            // miCopyVidPid
            // 
            miCopyVidPid.Name = "miCopyVidPid";
            miCopyVidPid.Size = new Size(244, 36);
            miCopyVidPid.Text = "Copy VID:PID";
            miCopyVidPid.Click += MiCopyVidPid_Click;
            // 
            // txtFilterName
            // 
            txtFilterName.Location = new Point(206, 18);
            txtFilterName.Margin = new Padding(5, 6, 5, 6);
            txtFilterName.Name = "txtFilterName";
            txtFilterName.Size = new Size(306, 35);
            txtFilterName.TabIndex = 1;
            txtFilterName.Text = "USB-UIRT";
            // 
            // lblFilterName
            // 
            lblFilterName.AutoSize = true;
            lblFilterName.Location = new Point(21, 24);
            lblFilterName.Margin = new Padding(5, 0, 5, 0);
            lblFilterName.Name = "lblFilterName";
            lblFilterName.Size = new Size(158, 30);
            lblFilterName.TabIndex = 0;
            lblFilterName.Text = "Name contains:";
            // 
            // lblVid
            // 
            lblVid.AutoSize = true;
            lblVid.Location = new Point(549, 24);
            lblVid.Margin = new Padding(5, 0, 5, 0);
            lblVid.Name = "lblVid";
            lblVid.Size = new Size(103, 30);
            lblVid.TabIndex = 2;
            lblVid.Text = "VID (hex):";
            // 
            // lblPid
            // 
            lblPid.AutoSize = true;
            lblPid.Location = new Point(823, 24);
            lblPid.Margin = new Padding(5, 0, 5, 0);
            lblPid.Name = "lblPid";
            lblPid.Size = new Size(102, 30);
            lblPid.TabIndex = 4;
            lblPid.Text = "PID (hex):";
            // 
            // txtVid
            // 
            txtVid.Location = new Point(660, 18);
            txtVid.Margin = new Padding(5, 6, 5, 6);
            txtVid.Name = "txtVid";
            txtVid.Size = new Size(134, 35);
            txtVid.TabIndex = 3;
            // 
            // txtPid
            // 
            txtPid.Location = new Point(934, 18);
            txtPid.Margin = new Padding(5, 6, 5, 6);
            txtPid.Name = "txtPid";
            txtPid.Size = new Size(134, 35);
            txtPid.TabIndex = 5;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(1114, 16);
            btnRefresh.Margin = new Padding(5, 6, 5, 6);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(154, 50);
            btnRefresh.TabIndex = 6;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += BtnRefresh_Click;
            // 
            // btnRename
            // 
            btnRename.Location = new Point(771, 76);
            btnRename.Margin = new Padding(5, 6, 5, 6);
            btnRename.Name = "btnRename";
            btnRename.Size = new Size(240, 50);
            btnRename.TabIndex = 9;
            btnRename.Text = "Rename Selected";
            btnRename.UseVisualStyleBackColor = true;
            btnRename.Click += btnRename_Click;
            // 
            // txtNewName
            // 
            txtNewName.Location = new Point(206, 78);
            txtNewName.Margin = new Padding(5, 6, 5, 6);
            txtNewName.Name = "txtNewName";
            txtNewName.Size = new Size(546, 35);
            txtNewName.TabIndex = 8;
            // 
            // lblNewName
            // 
            lblNewName.AutoSize = true;
            lblNewName.Location = new Point(21, 84);
            lblNewName.Margin = new Padding(5, 0, 5, 0);
            lblNewName.Name = "lblNewName";
            lblNewName.Size = new Size(118, 30);
            lblNewName.TabIndex = 7;
            lblNewName.Text = "New name:";
            // 
            // btnElevate
            // 
            btnElevate.Location = new Point(1029, 76);
            btnElevate.Margin = new Padding(5, 6, 5, 6);
            btnElevate.Name = "btnElevate";
            btnElevate.Size = new Size(240, 50);
            btnElevate.TabIndex = 11;
            btnElevate.Text = "Restart as Admin…";
            btnElevate.UseVisualStyleBackColor = true;
            btnElevate.Click += BtnElevate_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(28, 28);
            statusStrip.Items.AddRange(new ToolStripItem[] { sslStatus, sslAdmin });
            statusStrip.Location = new Point(0, 618);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(2, 0, 24, 0);
            statusStrip.Size = new Size(1927, 39);
            statusStrip.TabIndex = 12;
            statusStrip.Text = "statusStrip1";
            // 
            // sslStatus
            // 
            sslStatus.Name = "sslStatus";
            sslStatus.Size = new Size(1731, 30);
            sslStatus.Spring = true;
            sslStatus.Text = "Ready";
            // 
            // sslAdmin
            // 
            sslAdmin.Name = "sslAdmin";
            sslAdmin.Size = new Size(170, 30);
            sslAdmin.Text = "Admin: unknown";
            // 
            // BtnReadEeprom
            // 
            BtnReadEeprom.Location = new Point(1289, 18);
            BtnReadEeprom.Margin = new Padding(5, 6, 5, 6);
            BtnReadEeprom.Name = "BtnReadEeprom";
            BtnReadEeprom.Size = new Size(154, 50);
            BtnReadEeprom.TabIndex = 13;
            BtnReadEeprom.Text = "ReadEeprom";
            BtnReadEeprom.UseVisualStyleBackColor = true;
            BtnReadEeprom.Click += BtnReadEeprom_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1927, 657);
            Controls.Add(BtnReadEeprom);
            Controls.Add(statusStrip);
            Controls.Add(btnElevate);
            Controls.Add(btnRename);
            Controls.Add(txtNewName);
            Controls.Add(lblNewName);
            Controls.Add(btnRefresh);
            Controls.Add(txtPid);
            Controls.Add(txtVid);
            Controls.Add(lblPid);
            Controls.Add(lblVid);
            Controls.Add(txtFilterName);
            Controls.Add(lblFilterName);
            Controls.Add(lvDevices);
            Margin = new Padding(5, 6, 5, 6);
            MinimumSize = new Size(1526, 500);
            Name = "Form1";
            Text = "USB-UIRT Renamer";
            cmsList.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button BtnReadEeprom;
    }
}
