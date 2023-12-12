using System;
using System.Drawing;
using System.Windows.Forms;

namespace CafeSystem
{
    internal class TextboxPlaceholders
    {
        public class PlaceholderHandler
        {
            private readonly string placeholderText;
            private readonly bool isPassword; // New property to indicate if it's a password field
            private readonly Color originalTextColor;

            public readonly Color placeholderColor = Color.Gray;

            public PlaceholderHandler(string placeholderText, TextBox textBox, bool isPassword = false)
            {
                this.placeholderText = placeholderText;
                this.isPassword = isPassword;
                this.originalTextColor = textBox.ForeColor; // Initialize originalTextColor
            }

            public void Enter(object sender, EventArgs e)
            {
                TextBox textBox = sender as TextBox;
                if (textBox.Text.Equals(this.placeholderText))
                {
                    textBox.Text = string.Empty;
                    textBox.ForeColor = originalTextColor;

                    // Set password char property only if the current text is still the placeholder
                    if (isPassword)
                    {
                        textBox.PasswordChar = '*'; // You can change '*' to any character you prefer
                    }
                }
            }

            public void Leave(object sender, EventArgs e)
            {
                TextBox textBox = sender as TextBox;

                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = this.placeholderText;
                    textBox.ForeColor = placeholderColor;

                    // Reset password char property
                    if (isPassword)
                    {
                        textBox.PasswordChar = '\0'; // Set to '\0' to disable password char
                    }
                }
            }
        }

        public static void SetPlaceholder(TextBox textBox, string placeholderText, bool isPassword = false)
        {
            PlaceholderHandler handler = new PlaceholderHandler(placeholderText, textBox, isPassword);
            textBox.Enter += handler.Enter;
            textBox.Leave += handler.Leave;
            textBox.ForeColor = handler.placeholderColor;
            textBox.Text = placeholderText;
        }
    }
}
