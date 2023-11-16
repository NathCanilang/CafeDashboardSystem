using System;
using System.Windows.Forms;

namespace Microsoft.VisualBasic
{
    internal class Interaction
    {
        internal static string InputBox(string prompt, string title, string defaultResponse)
        {
            using (Form inputForm = new Form())
            using (TextBox textBox = new TextBox())
            using (Button okButton = new Button())
            using (Label label = new Label())
            {
                inputForm.Text = title;
                inputForm.Size = new System.Drawing.Size(300, 150);
                inputForm.FormBorderStyle = FormBorderStyle.FixedSingle;
                inputForm.StartPosition = FormStartPosition.CenterScreen;

                label.Text = prompt;
                label.Size = new System.Drawing.Size(200, 20);
                label.Location = new System.Drawing.Point(50, 10);

                textBox.Size = new System.Drawing.Size(200, 20);
                textBox.Location = new System.Drawing.Point(50, 30);
                textBox.Text = defaultResponse;

                okButton.DialogResult = DialogResult.OK;
                okButton.Name = "okButton";
                okButton.Size = new System.Drawing.Size(75, 23);
                okButton.Location = new System.Drawing.Point(50, 70);
                okButton.Text = "OK";

                inputForm.Controls.AddRange(new Control[] { label, textBox, okButton });

                inputForm.AcceptButton = okButton;

                DialogResult result = inputForm.ShowDialog();

                return result == DialogResult.OK ? textBox.Text : string.Empty;
            }
        }
    }
}