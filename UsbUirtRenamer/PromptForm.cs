using System.Windows.Forms;

namespace UsbUirtRenamer
{
    public partial class PromptForm : Form
    {
        public string InputText => txtInput.Text;

        public PromptForm(string promptText, string defaultValue = "")
        {
            InitializeComponent();
            lblPrompt.Text = promptText;
            txtInput.Text = defaultValue;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Position the textbox below the auto-sized label with padding
            int textboxTop = lblPrompt.Bottom + 10;
            txtInput.Top = textboxTop;

            // Position the button panel below the textbox
            int formContentHeight = txtInput.Bottom + 12 + pnlButtons.Height;
            ClientSize = new System.Drawing.Size(ClientSize.Width, formContentHeight);

            txtInput.SelectAll();
            txtInput.Focus();
        }

        /// <summary>
        /// Shows the prompt dialog and returns the entered text, or null if cancelled.
        /// </summary>
        public static string? ShowPrompt(IWin32Window? owner, string promptText, string defaultValue = "")
        {
            using var form = new PromptForm(promptText, defaultValue);
            return form.ShowDialog(owner) == DialogResult.OK ? form.InputText : null;
        }
    }
}
