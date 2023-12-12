using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CafeSystem
{
    internal class LoginPanelManager
    {
        private readonly Panel LoginPanelContainer;
        private readonly Panel AdminPanelContainer;
        private readonly Panel SalesPanelContainer;
        private readonly Panel ManagerStaffPanelContainer;

        public LoginPanelManager(Panel loginPanelContainer, Panel adminPanelContainer, Panel salesPanelContainer, Panel managerStaffPanelContainer)
        {
            LoginPanelContainer = loginPanelContainer;
            AdminPanelContainer = adminPanelContainer;
            SalesPanelContainer = salesPanelContainer;
            ManagerStaffPanelContainer = managerStaffPanelContainer;
        }
        public void ShowPanel(Panel panelToShow)
        {
            if (!panelToShow.Visible)
            {
                // Main Panel
                LoginPanelContainer.Hide();
                AdminPanelContainer.Hide();
                SalesPanelContainer.Hide();
                ManagerStaffPanelContainer.Hide();

                panelToShow.Show();
            }

            if (panelToShow == ManagerStaffPanelContainer)
            {
                CafeDeLunaDashboard.cafeDeLunaInstance.GetData();
                CafeDeLunaDashboard.cafeDeLunaInstance.GetData2();
            }
        }
    }

    internal class AdminPanelManager
    {
        private readonly AdminMethods adminMethods = new AdminMethods();

        private readonly Panel AdminHomePanel;
        private readonly Panel AccountManagementPanel;
        private readonly Panel AddMenuPanel;

        public AdminPanelManager(Panel adminHomePanel, Panel accountManagementPanel, Panel addMenuPanel)
        {
            AdminHomePanel = adminHomePanel;
            AccountManagementPanel = accountManagementPanel;
            AddMenuPanel = addMenuPanel;
        }
        public void ShowPanel(Panel panelToShow)
        {
            AdminHomePanel.Hide();
            AccountManagementPanel.Hide();
            AddMenuPanel.Hide();

            panelToShow.Show();

            if (panelToShow == AccountManagementPanel)
            {
                adminMethods.GenerateAndSetRandomNumber();
                adminMethods.RefreshTbl();
            }
            else if(panelToShow == AddMenuPanel)
            {
                adminMethods.LoadMenuItems();
                adminMethods.RefreshTblForMenu();
                adminMethods.PopulateMealComboBox();
            }
        }
    }

    internal class SalesPanelManager
    {
        private readonly Panel DailyReportPanel;
        private readonly Panel WeeklyReportPanel;
        private readonly Panel MonthlyReportPanel;

        public SalesPanelManager(Panel dailyReportPanel, Panel weeklyReportPanel, Panel monthlyReportPanel)
        {
            DailyReportPanel = dailyReportPanel;
            WeeklyReportPanel = weeklyReportPanel;
            MonthlyReportPanel = monthlyReportPanel;
        }
        public void ShowPanel(Panel panelToShow)
        {
            DailyReportPanel.Hide();
            WeeklyReportPanel.Hide();
            MonthlyReportPanel.Hide();

            panelToShow.Show();

            if(panelToShow == DailyReportPanel)
            {
                CafeDeLunaDashboard.cafeDeLunaInstance.CategoryReportLbl.Text = "Daily Sales Report";
                CafeDeLunaDashboard.cafeDeLunaInstance.SelectDateLbl.Text = "Select Date to Report:";
            }
            else if(panelToShow == WeeklyReportPanel)
            {
                CafeDeLunaDashboard.cafeDeLunaInstance.CategoryReportLbl.Text = "Weekly Sales Report";
                CafeDeLunaDashboard.cafeDeLunaInstance.SelectDateLbl.Text = "Select Date Report the Week:";
            }
            else if(panelToShow == MonthlyReportPanel)
            {
                CafeDeLunaDashboard.cafeDeLunaInstance.CategoryReportLbl.Text = "Monthly Sales Report";
                CafeDeLunaDashboard.cafeDeLunaInstance.SelectDateLbl.Text = "Select a Date From a Month to Report:";
            }
        }
    }
}
