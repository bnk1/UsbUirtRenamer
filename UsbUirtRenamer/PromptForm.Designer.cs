namespace UsbUirtRenamer
{
    partial class PromptForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblPrompt;
        private System.Windows.Forms.TextBox txtInput;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel pnlButtons;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblPrompt = new System.Windows.Forms.Label();
            txtInput = new System.Windows.Forms.TextBox();
            btnOK = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            pnlButtons = new System.Windows.Forms.Panel();
            pnlButtons.SuspendLayout();
            SuspendLayout();

            // lblPrompt
            lblPrompt.AutoSize = true;
            lblPrompt.Location = new System.Drawing.Point(14, 14);
            lblPrompt.MaximumSize = new System.Drawing.Size(476, 0);
            lblPrompt.Name = "lblPrompt";
            lblPrompt.Size = new System.Drawing.Size(0, 15);
            lblPrompt.TabIndex = 0;

            // txtInput
            txtInput.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtInput.Location = new System.Drawing.Point(14, 80);
            txtInput.Name = "txtInput";
            txtInput.Size = new System.Drawing.Size(476, 23);
            txtInput.TabIndex = 1;

            // pnlButtons
            pnlButtons.Controls.Add(btnOK);
            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlButtons.Location = new System.Drawing.Point(0, 120);
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Size = new System.Drawing.Size(504, 44);
            pnlButtons.TabIndex = 2;

            // btnOK
            btnOK.Anchor = System.Windows.Forms.AnchorStyles.Right;
            btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOK.Location = new System.Drawing.Point(330, 8);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 28);
            btnOK.TabIndex = 0;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;

            // btnCancel
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(415, 8);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 28);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;

            // PromptForm
            AcceptButton = btnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(504, 164);
            Controls.Add(lblPrompt);
            Controls.Add(txtInput);
            Controls.Add(pnlButtons);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PromptForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Rename USB-UIRT";
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
