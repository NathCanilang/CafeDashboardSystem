using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CafeSystem
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NHaF5cXmVCf1FpRGFGfV5yd0VHal5XTnNeUiweQnxTdEZiWH5ccHNVR2BVVERyXg==");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NHaF5cXmVCf1FpRGFGfV5yd0VHal5XTnNeUiweQnxTdEZiWH5ccHNVR2BVVERyXg==");
            /*
             * The license below ay kay coloma na community use it when neccessary
            */
            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NHaF5cWWBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdgWH5fcXRURGVZVEJxW0I=");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CafeDeLunaDashboard());
        }
    }
}
