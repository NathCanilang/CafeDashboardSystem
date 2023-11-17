using System;
using System.Drawing;
using System.Windows.Forms;

namespace CafeSystem
{
    internal class LabelChangeColor
    {
        private Color originalBackColor;
        private Color hoverBackColor = Color.FromArgb(250, 210, 11); // Set the color you want for the background when hovering
        private Color originalFontColor;
        private Color hoverFontColor = Color.FromArgb(55, 41, 38); // Set the color you want for the font when hovering

        public LabelChangeColor()
        {
            // Set the original colors using RGB values
            originalBackColor = Color.FromArgb(55, 41, 38);
            originalFontColor = Color.FromArgb(250, 210, 11);
        }

        public void MouseHover(object sender, EventArgs e)
        {
            if (sender is Label label)
            {
                // Save the original colors
                originalBackColor = label.BackColor;
                originalFontColor = label.ForeColor;

                // Set the hover colors
                label.BackColor = hoverBackColor;
                label.ForeColor = hoverFontColor;
            }
        }

        public void MouseLeave(object sender, EventArgs e)
        {
            if (sender is Label label)
            {
                // Restore the original colors
                label.BackColor = originalBackColor;
                label.ForeColor = originalFontColor;
            }
        }
    }
}
