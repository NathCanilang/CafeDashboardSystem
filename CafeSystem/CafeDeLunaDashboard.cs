﻿using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using Image = System.Drawing.Image;
using TextAlignment = iText.Layout.Properties.TextAlignment;

namespace CafeSystem
{
    public partial class CafeDeLunaDashboard : Form
    {
        MySqlCommand cm;
        MySqlDataReader dr;
        private PictureBox pic;
        private PictureBox menupic;
        private Label price;
        private Label mealname;
        private readonly MySqlConnection conn;
        public static CafeDeLunaDashboard cafeDeLunaInstance;
        private readonly AdminMethods adminMethods = new AdminMethods();
        private KeypressNumbersRestrictions keypressNumbersRestrictions = new KeypressNumbersRestrictions();
        private KeypressLettersRestrictions keypressLettersRestrictions = new KeypressLettersRestrictions();
        private DailySalesReportMethod dailySalesReportMethod = new DailySalesReportMethod();
        private WeeklySalesReportMethod weeklySalesReportMethod = new WeeklySalesReportMethod();
        private MonthlySalesReportMethod monthlySalesReportMethod = new MonthlySalesReportMethod();
        private readonly LoginPanelManager loginPanelManager;
        private readonly AdminPanelManager adminPanelManager;
        private readonly SalesPanelManager salesPanelManager;
        private byte[] imageData;
        private decimal totalPrice = 0.00m;
        private bool isSearchTextPlaceholder = true;
        private int GenerateID = orderIDGenerator();
        private int employeeID;
        private string positionDB;
        private string usernameDB;

        private readonly string[] position = { "Manager", "Cashier" };
        public int EmployeeIDBeingEdited = -1;

        bool isNewImageSelected = false;
        bool isNewFoodImageSelected = false;
        bool IsEditMode = false;
        public CafeDeLunaDashboard()
        {
            InitializeComponent();
            cafeDeLunaInstance = this;
            string mysqlcon = "server=localhost;user=root;database=dashboarddb;password=";
            conn = new MySqlConnection(mysqlcon);
            loginPanelManager = new LoginPanelManager(LoginPanelContainer, AdminPanelContainer, SalesPanelContainer, ManagerStaffPanelContainer);
            adminPanelManager = new AdminPanelManager(AdminHomePanel, AccountManagementPanel, AddMenuPanel);
            salesPanelManager = new SalesPanelManager(DailyReportPanel, WeeklyReportPanel, MonthlyReportPanel);

            //Startup Panels
            loginPanelManager.ShowPanel(LoginPanelContainer);
            adminPanelManager.ShowPanel(AdminHomePanel);

            //Restrictions
            LastNTxtB_AP.KeyPress += keypressNumbersRestrictions.KeyPress;
            FirstNTxtB_AP.KeyPress += keypressNumbersRestrictions.KeyPress;
            MiddleNTxtB_AP.KeyPress += keypressNumbersRestrictions.KeyPress;
            MenuNTxtB.KeyPress += keypressNumbersRestrictions.KeyPress;
            VariationNmTxtB.KeyPress += keypressNumbersRestrictions.KeyPress;
            VariationDescTxtB.KeyPress += keypressNumbersRestrictions.KeyPress;
            VariationCostTxtB.KeyPress += keypressLettersRestrictions.KeyPress;

            //Admin Panel
            FoodTbl.DataError += new DataGridViewDataErrorEventHandler(adminMethods.FoodTable_DataError);
            FoodTbl.RowPostPaint += new DataGridViewRowPostPaintEventHandler(adminMethods.FoodTable_RowPostPaint);
            PositionComB_AP.Items.AddRange(position);
            PositionComB_AP.DropDownStyle = ComboBoxStyle.DropDownList;
            MenuSelectComB.DropDownStyle = ComboBoxStyle.DropDownList;
            adminMethods.PopulateMealComboBox();
            UserBirthdate.ValueChanged += CalculateAge;

            //Staff Panel
            dataGridView1.RowsAdded += dataGridView1_RowsAdded;
            dataGridView1.RowsRemoved += dataGridView1_RowsRemoved;
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            cashtxtBx.KeyPress += cashtxtBx_KeyPress;
        }

        private void LogoutLbl_Click(object sender, EventArgs e)
        {
            loginPanelManager.ShowPanel(LoginPanelContainer);
        }

        private void LogoutLogo_Click(object sender, EventArgs e)
        {
            loginPanelManager.ShowPanel(LoginPanelContainer);
        }

        private void AccManagementLbl_Click(object sender, EventArgs e)
        {
            adminPanelManager.ShowPanel(AccountManagementPanel);
        }

        private void AddMenuLbl_Click(object sender, EventArgs e)
        {
            adminPanelManager.ShowPanel(AddMenuPanel);
        }

        private void SalesRepLbl_Click(object sender, EventArgs e)
        {
            loginPanelManager.ShowPanel(SalesPanelContainer);
            salesPanelManager.ShowPanel(DailyReportPanel);
        }
        private void DailyLbl_Click(object sender, EventArgs e)
        {
            salesPanelManager.ShowPanel(DailyReportPanel);
        }

        private void WeeklyLbl_Click(object sender, EventArgs e)
        {
            salesPanelManager.ShowPanel(WeeklyReportPanel);
        }

        private void MonthlyLbl_Click(object sender, EventArgs e)
        {
            salesPanelManager.ShowPanel(MonthlyReportPanel);
        }

        private void BackLbl_Click(object sender, EventArgs e)
        {
            loginPanelManager.ShowPanel(AdminPanelContainer);
            adminPanelManager.ShowPanel(AdminHomePanel);
        }
        private void CalculateAge(object sender, EventArgs e)
        {
            DateTime selectedDate = UserBirthdate.Value;
            int age = adminMethods.AgeCalculation(selectedDate);
            AgeTxtB_AP.Text = age.ToString();
        }

        private void LoginBtn_Click(object sender, EventArgs e)
        {
            string usernameInput = LoginUsernameTxtB.Text;
            string passwordInput = LoginPasswordTxtB.Text;
            string hsshPasswordInput = Encryptor.HashPassword(LoginPasswordTxtB.Text);

            if (usernameInput == "Admin" && passwordInput == "admin123")
            {
                MessageBox.Show("Admin login successful", "Welcome, Admin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                loginPanelManager.ShowPanel(AdminPanelContainer);
            }
            else
            {
                using (conn)
                {
                    conn.Open();

                    string query = "SELECT Position, Username, EmployeeID FROM employee_acc WHERE Username = @username AND Password = @password";
                    using (MySqlCommand command = new MySqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@username", usernameInput);
                        command.Parameters.AddWithValue("@password", hsshPasswordInput);

                        object position = command.ExecuteScalar();

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (position != null)
                                {
                                    string userRole = position.ToString();
                                    employeeID = reader.GetInt32("EmployeeID");
                                    positionDB = reader["Position"].ToString();
                                    usernameDB = reader["Username"].ToString();

                                    if (userRole == "Manager")
                                    {
                                        MessageBox.Show("Login Successful", "Welcome, Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        loginPanelManager.ShowPanel(ManagerStaffPanelContainer);
                                    }
                                    else if (userRole == "Cashier")
                                    {
                                        MessageBox.Show("Login Successful", "Welcome, Staff", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        loginPanelManager.ShowPanel(ManagerStaffPanelContainer);
                                    }
                                    GetData();
                                }
                                else
                                {
                                    MessageBox.Show("Invalid username or password.", "Try again", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Invalid Access.", "Try again", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }

            }
            LoginUsernameTxtB.Text = "";
            LoginPasswordTxtB.Text = "";
        }

        private void SelectImgBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                openFileDialog.Title = "Select an Image File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Load the selected image
                        Image selectedImage = Image.FromFile(openFileDialog.FileName);
                        
                        // Resize the selected image
                        int newWidth = 142; // Set the new width
                        int newHeight = 115; // Set the new height
                        Image resizedImage = adminMethods.ResizeImages(selectedImage, newWidth, newHeight);

                        UserPicB.Image = resizedImage;
                        isNewImageSelected = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading the image: " + ex.Message);
                    }
                }
            }
        }

        private void CreateAccBtn_Click(object sender, EventArgs e)
        {
            string adminUsername = "Admin";
            DateTime selectedDate = UserBirthdate.Value;
            string employeeFullName = $"{LastNTxtB_AP.Text}, {FirstNTxtB_AP.Text} {MiddleNTxtB_AP.Text}";

            if (UserPicB.Image == null)
            {
                MessageBox.Show("Please select an image.", "Try again", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((string.IsNullOrWhiteSpace(LastNTxtB_AP.Text) || LastNTxtB_AP.Text == "Enter last name") ||
                (string.IsNullOrWhiteSpace(FirstNTxtB_AP.Text) || FirstNTxtB_AP.Text == "Enter first name") ||
                (string.IsNullOrWhiteSpace(MiddleNTxtB_AP.Text) || MiddleNTxtB_AP.Text == "Enter middle name") ||
                (string.IsNullOrWhiteSpace(AgeTxtB_AP.Text) || AgeTxtB_AP.Text == "Enter age") ||
                (string.IsNullOrWhiteSpace(UsernameTxtB_AP.Text) || UsernameTxtB_AP.Text == "Enter username") ||
                (string.IsNullOrWhiteSpace(PasswordTxtB_AP.Text) || PasswordTxtB_AP.Text == "Enter password") ||
                PositionComB_AP.SelectedItem == null ||
                string.IsNullOrEmpty(EmployeeIDTxtB_AP.Text) || EmployeeIDTxtB_AP.Text == "Enter ID" ||
                string.IsNullOrEmpty(EmailTxtB_AP.Text) || EmailTxtB_AP.Text == "Enter e-mail")
            {
                MessageBox.Show("Please fill out all the required data", "Missing Informations", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (UsernameTxtB_AP.Text == adminUsername)
            {
                MessageBox.Show("The entered username is not allowed", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DialogResult choices = MessageBox.Show("Are you sure the information you have entered is correct?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (choices == DialogResult.Yes)
            {
                try
                {
                    conn.Open();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        UserPicB.Image.Save(ms, ImageFormat.Jpeg); // You can choose the format you want
                        imageData = ms.ToArray();
                    }
                    string insertQuery = "INSERT INTO employee_acc(Name, Birthday, Age, Email, Username, Password, Position, EmployeeID, EmployeeIMG) " +
                        "VALUES (@Name, @Birthday, @Age, @Email, @Username, @Password, @Position, @EmployeeID, @EmployeeIMG)";

                    MySqlCommand cmdDataBase = new MySqlCommand(insertQuery, conn); cmdDataBase.Parameters.AddWithValue("@Name", employeeFullName);
                    cmdDataBase.Parameters.AddWithValue("@Birthday", selectedDate);
                    cmdDataBase.Parameters.AddWithValue("@Age", AgeTxtB_AP.Text);
                    cmdDataBase.Parameters.AddWithValue("@Email", EmailTxtB_AP.Text);
                    cmdDataBase.Parameters.AddWithValue("@Username", UsernameTxtB_AP.Text);
                    cmdDataBase.Parameters.AddWithValue("@Password", Encryptor.HashPassword(PasswordTxtB_AP.Text));
                    cmdDataBase.Parameters.AddWithValue("@Position", PositionComB_AP.SelectedItem.ToString());
                    cmdDataBase.Parameters.AddWithValue("@EmployeeID", EmployeeIDTxtB_AP.Text);
                    cmdDataBase.Parameters.AddWithValue("@EmployeeIMG", imageData);
                    cmdDataBase.ExecuteNonQuery();

                    adminMethods.RefreshTbl();
                    MessageBox.Show("Account Created!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (MySqlException a)
                {
                    if (a.Number == 1062)
                    {
                        MessageBox.Show("Username already exists.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        UsernameTxtB_AP.Clear();
                    }
                    else
                    {
                        MessageBox.Show(a.Message, "Registration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception b)
                {
                    MessageBox.Show(b.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private void EditAccBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to edit accounts?", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                if (AccDataTbl.SelectedRows.Count == 1)
                {
                    IsEditMode = true;
                    UpdateAccBtn.Show();
                    CancelAccBtn.Show();
                    CreateAccBtn.Hide();
                    EditAccBtn.Hide();

                    DataGridViewRow selectedRow = AccDataTbl.SelectedRows[0];
                    string nameColumn = selectedRow.Cells["Name"].Value.ToString();
                    string birthdayColumn = selectedRow.Cells["Birthday"].Value.ToString().Trim();
                    string ageColumn = selectedRow.Cells["Age"].Value.ToString();
                    string emailColumn = selectedRow.Cells["Email"].Value.ToString();
                    string usernameColumn = selectedRow.Cells["Username"].Value.ToString();
                    string positionColumn = selectedRow.Cells["Position"].Value.ToString();
                    int employeeIDColumn = Convert.ToInt32(AccDataTbl.SelectedRows[0].Cells["EmployeeID"].Value);
                    string[] nameParts = nameColumn.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    EmployeeIDBeingEdited = Convert.ToInt32(AccDataTbl.SelectedRows[0].Cells["EmployeeID"].Value);


                    if (nameParts.Length > 0)
                    {
                        string lastName = nameParts[0].Trim();      // Trim the last name
                        LastNTxtB_AP.Text = lastName;
                    }

                    if (nameParts.Length > 1)
                    {
                        string[] firstMiddleNameParts = nameParts[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (firstMiddleNameParts.Length > 0)
                        {
                            string firstName = firstMiddleNameParts[0].Trim();     // Trim the first name
                            FirstNTxtB_AP.Text = firstName;
                        }

                        if (firstMiddleNameParts.Length > 1)
                        {
                            string middleName = firstMiddleNameParts[1].Trim();    // Trim the middle name
                            MiddleNTxtB_AP.Text = middleName;
                        }
                    }
                    if (DateTime.TryParse(birthdayColumn, out DateTime birthday))
                    {
                        UserBirthdate.Value = birthday;
                    }
                    else
                    {
                        MessageBox.Show("Invalid date format in the 'Birthday' column.", "Try again", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    AgeTxtB_AP.Text = ageColumn;
                    EmailTxtB_AP.Text = emailColumn;
                    UsernameTxtB_AP.Text = usernameColumn;
                    PositionComB_AP.Text = positionColumn;
                    EmployeeIDTxtB_AP.Text = employeeIDColumn.ToString();
                    adminMethods.LoadUserImage(employeeIDColumn);
                }
                else
                {
                    MessageBox.Show("Please select a single row for editing.", "Try again", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void CancelAccBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to cancel the operation?", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                UpdateAccBtn.Hide();
                CancelAccBtn.Hide();
                CreateAccBtn.Show();
                EditAccBtn.Show();

                TextboxPlaceholders.SetPlaceholder(LastNTxtB_AP, "Last name");
                TextboxPlaceholders.SetPlaceholder(FirstNTxtB_AP, "First name");
                TextboxPlaceholders.SetPlaceholder(MiddleNTxtB_AP, "Middle name");

                UserBirthdate.Value = DateTime.Today;
                AgeTxtB_AP.Text = "";
                UsernameTxtB_AP.Text = "";
                PasswordTxtB_AP.Text = "";
                EmailTxtB_AP.Text = "";
                PositionComB_AP.SelectedIndex = -1;
                UserPicB.Image = null;

                adminPanelManager.ShowPanel(AccountManagementPanel);
            }
        }

        private void UpdateAccBtn_Click(object sender, EventArgs e)
        {
            string adminUsername = "Admin";
            DateTime selectedDate = UserBirthdate.Value;
            string employeeFullName = $"{LastNTxtB_AP.Text}, {FirstNTxtB_AP.Text} {MiddleNTxtB_AP.Text}";

            if (UserPicB.Image == null)
            {
                MessageBox.Show("Please select an image.", "Try again", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((string.IsNullOrWhiteSpace(LastNTxtB_AP.Text) || LastNTxtB_AP.Text == "Enter last name") ||
                (string.IsNullOrWhiteSpace(FirstNTxtB_AP.Text) || FirstNTxtB_AP.Text == "Enter first name") ||
                (string.IsNullOrWhiteSpace(MiddleNTxtB_AP.Text) || MiddleNTxtB_AP.Text == "Enter middle name") ||
                (string.IsNullOrWhiteSpace(AgeTxtB_AP.Text) ||
                (string.IsNullOrWhiteSpace(UsernameTxtB_AP.Text) ||
                PositionComB_AP.SelectedItem == null ||
                string.IsNullOrEmpty(EmployeeIDTxtB_AP.Text) ||
                string.IsNullOrEmpty(EmailTxtB_AP.Text))))
            {
                MessageBox.Show("Please fill out all the required data", "Missing Informations", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (UsernameTxtB_AP.Text == adminUsername)
            {
                MessageBox.Show("The entered username is not allowed", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (UsernameTxtB_AP.Text == adminUsername)
            {
                MessageBox.Show("The entered username is not allowed", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DialogResult choices = MessageBox.Show("Are you sure the information you have entered is correct?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (choices == DialogResult.Yes)
            {
                try
                {
                    conn.Open();
                    string updateQuery;
                    if (string.IsNullOrEmpty(PasswordTxtB_AP.Text))
                    {
                        // If password field is empty, don't update the password
                        updateQuery = "UPDATE employee_acc " +
                            "SET Name = @Name, Birthday = @Birthday, Age = @Age, Email = @Email, " +
                            "Username = @Username, Position = @Position";

                        if (isNewImageSelected)
                        {
                            updateQuery += ", EmployeeIMG = @EmployeeIMG";
                        }

                        updateQuery += " WHERE EmployeeID = @EmployeeID";
                    }
                    else
                    {
                        // If password field is not empty, update the password
                        updateQuery = "UPDATE employee_acc " +
                            "SET Name = @Name, Birthday = @Birthday, Age = @Age, Email = @Email, " +
                            "Username = @Username, Password = @Password, Position = @Position";

                        if (isNewImageSelected)
                        {
                            updateQuery += ", EmployeeIMG = @EmployeeIMG";
                        }

                        updateQuery += " WHERE EmployeeID = @EmployeeID";
                    }

                    MySqlCommand cmdDataBase = new MySqlCommand(updateQuery, conn);
                    cmdDataBase.Parameters.AddWithValue("@Name", employeeFullName);
                    cmdDataBase.Parameters.AddWithValue("@Birthday", selectedDate);
                    cmdDataBase.Parameters.AddWithValue("@Age", AgeTxtB_AP.Text);
                    cmdDataBase.Parameters.AddWithValue("@Email", EmailTxtB_AP.Text);
                    cmdDataBase.Parameters.AddWithValue("@Username", UsernameTxtB_AP.Text);

                    if (!string.IsNullOrEmpty(PasswordTxtB_AP.Text))
                    {
                        cmdDataBase.Parameters.AddWithValue("@Password", Encryptor.HashPassword(PasswordTxtB_AP.Text));
                    }

                    cmdDataBase.Parameters.AddWithValue("@Position", PositionComB_AP.SelectedItem.ToString());
                    cmdDataBase.Parameters.AddWithValue("@EmployeeID", EmployeeIDTxtB_AP.Text);

                    if (isNewImageSelected)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            UserPicB.Image.Save(ms, ImageFormat.Jpeg); // You can choose the format you want
                            byte[] imageData = ms.ToArray();
                            cmdDataBase.Parameters.AddWithValue("@EmployeeIMG", imageData);
                        }
                    }

                    cmdDataBase.ExecuteNonQuery();

                    adminMethods.RefreshTbl();
                    MessageBox.Show("Account Updated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (MySqlException a)
                {
                    if (a.Number == 1062)
                    {
                        MessageBox.Show("Username already exists.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        UsernameTxtB_AP.Clear();
                    }
                    else
                    {
                        MessageBox.Show(a.Message, "Registration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception b)
                {
                    MessageBox.Show(b.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
            UpdateAccBtn.Hide();
            CancelAccBtn.Hide();
            CreateAccBtn.Show();
            EditAccBtn.Show();

            TextboxPlaceholders.SetPlaceholder(LastNTxtB_AP, "Last name");
            TextboxPlaceholders.SetPlaceholder(FirstNTxtB_AP, "First name");
            TextboxPlaceholders.SetPlaceholder(MiddleNTxtB_AP, "Middle name");
            UserBirthdate.Value = DateTime.Today;
            AgeTxtB_AP.Text = "";
            UsernameTxtB_AP.Text = "";
            PasswordTxtB_AP.Text = "";
            EmailTxtB_AP.Text = "";
            PositionComB_AP.SelectedIndex = -1;
            UserPicB.Image = null;

            adminPanelManager.ShowPanel(AccountManagementPanel);
        }

        private void MenuAddImgBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                openFileDialog.Title = "Select an Image File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Load the selected image
                        Image selectedImage = Image.FromFile(openFileDialog.FileName);

                        // Resize the selected image
                        int newWidth = 745; // Set the new width
                        int newHeight = 110; // Set the new height
                        Image resizedImage = adminMethods.ResizeImages(selectedImage, newWidth, newHeight);

                        MenuPicB.Image = resizedImage;
                        isNewImageSelected = true; // Set the flag to true
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading the image: " + ex.Message);
                    }
                }
            }
        }

        private void AddMenuBtn_Click(object sender, EventArgs e)
        {
            string mealName = MenuNTxtB.Text;

            if (MenuPicB.Image == null)
            {
                MessageBox.Show("No image selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(MenuNTxtB.Text) || MenuNTxtB.Text == "Menu Name")
            {
                MessageBox.Show("Please fill out all the required data", "Missing Informations", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DialogResult choices = MessageBox.Show("Are you sure the information you have entered is correct?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (choices == DialogResult.Yes)
            {
                try
                {
                    conn.Open();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        MenuPicB.Image.Save(ms, ImageFormat.Jpeg);
                        imageData = ms.ToArray();
                    }
                    string insertQuery = "INSERT INTO meal (MealName, MealImage) VALUES (@mealName, @mealImage)";
                    MySqlCommand command = new MySqlCommand(insertQuery, conn);
                    command.Parameters.AddWithValue("@mealName", mealName);
                    command.Parameters.AddWithValue("@mealImage", imageData);
                    command.ExecuteNonQuery();

                    adminMethods.PopulateMealComboBox();
                    MessageBox.Show("New meal added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    //TxtPlaceholder.SetPlaceholder(MenuNTxtB, "Menu Name");
                    MenuPicB.Image = null;

                }
                catch (MySqlException a)
                {
                    if (a.Number == 1062)
                    {
                        MessageBox.Show("Menu already exists", "Food Creattion", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private void VarietyAddImgBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                openFileDialog.Title = "Select an Image File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Load the selected image
                        Image selectedImage = Image.FromFile(openFileDialog.FileName);

                        // Resize the selected image
                        int newWidth = 163; // Set the new width
                        int newHeight = 128; // Set the new height
                        Image resizedImage = adminMethods.ResizeImages(selectedImage, newWidth, newHeight);

                        VariationPicB.Image = resizedImage;
                        isNewFoodImageSelected = true; // Set the flag to true
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading the image: " + ex.Message);
                    }
                }
            }
        }

        private void AddVarietyBtn_Click(object sender, EventArgs e)
        {
            string variationName = VariationNmTxtB.Text;
            string variationDescription = VariationDescTxtB.Text;
            decimal variationCost = decimal.Parse(VariationCostTxtB.Text);
            string variationCostText = VariationCostTxtB.Text;
            string selectedMenuCategory = MenuSelectComB.SelectedItem.ToString();
            int mealID = adminMethods.GetMealIDFromDatabase(selectedMenuCategory);

            if (string.IsNullOrWhiteSpace(variationCostText) || !decimal.TryParse(variationCostText, out variationCost))
            {
                MessageBox.Show("Invalid variation cost.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((string.IsNullOrWhiteSpace(variationName) || variationName == "Food Name") ||
                string.IsNullOrEmpty(variationDescription) || variationDescription == "Description")
            {
                MessageBox.Show("Please fill out all the required data", "Missing Informations", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DialogResult choices = MessageBox.Show("Are you sure the information you have entered is correct?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (choices == DialogResult.Yes)
            {
                try
                {
                    conn.Open();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        VariationPicB.Image.Save(ms, ImageFormat.Jpeg);
                        imageData = ms.ToArray();
                    }
                    string insertQuery = "INSERT INTO mealvariation (MealImage, MealID, VariationName, VariationDescription, VariationCost ) " +
                        "VALUES (@variationImage, @mealID, @variationName, @variationDescription, @variationCost)";

                    MySqlCommand command = new MySqlCommand(insertQuery, conn);
                    command.Parameters.AddWithValue("@variationImage", imageData);
                    command.Parameters.AddWithValue("@mealID", mealID);
                    command.Parameters.AddWithValue("@variationName", variationName);
                    command.Parameters.AddWithValue("@variationDescription", variationDescription);
                    command.Parameters.AddWithValue("@variationCost", variationCost);

                    command.ExecuteNonQuery();

                    MessageBox.Show("New variation added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    TextboxPlaceholders.SetPlaceholder(VariationNmTxtB, "Food Name");
                    TextboxPlaceholders.SetPlaceholder(VariationDescTxtB, "Description");
                    TextboxPlaceholders.SetPlaceholder(VariationCostTxtB, "Price");
                    VariationPicB.Image = null;
                }
                catch (MySqlException a)
                {
                    if (a.Number == 1062)
                    {
                        MessageBox.Show("Menu already exists", "Food Creattion", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    conn.Close();
                }
                adminMethods.LoadMenuItems();
            }
        }

        private void EditMealBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to edit this meal?", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                if (FoodTbl.SelectedRows.Count == 1)
                {
                    DataGridViewRow selectedRow = FoodTbl.SelectedRows[0];

                    string variationName = selectedRow.Cells["VariationName"].Value.ToString();
                    string variationDesc = selectedRow.Cells["VariationDescription"].Value.ToString().Trim();
                    string variationCost = selectedRow.Cells["VariationCost"].Value.ToString();
                    string mealID = selectedRow.Cells["MealID"].Value.ToString();
                    string variationID = selectedRow.Cells["VariationID"].Value.ToString();
                    int variationIDColumn = Convert.ToInt32(FoodTbl.SelectedRows[0].Cells["VariationID"].Value);

                    VariationNmTxtB.Text = variationName;
                    VariationDescTxtB.Text = variationDesc;
                    VariationCostTxtB.Text = variationCost;
                    VariationIDTxtBox.Text = variationID;
                    adminMethods.LoadMenuItemImageFood(variationIDColumn);

                    try
                    {
                        conn.Open();
                        string sqlQuery = "SELECT MealName FROM meal WHERE mealID = @mealID";
                        MySqlCommand cmdDataBase = new MySqlCommand(sqlQuery, conn);
                        cmdDataBase.Parameters.AddWithValue("@mealID", mealID); // Replace 'yourMealID' with the actual mealID
                        MySqlDataReader reader = cmdDataBase.ExecuteReader();

                        // Loop through the results and add them to the ComboBox

                        if (reader.Read())
                        {
                            string mealName = reader.GetString(0);
                            MenuSelectComB.SelectedItem = mealName; // Set the selected item in the ComboBox
                        }
                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                    finally
                    {
                        conn.Close();
                    }

                }
                else
                {
                    MessageBox.Show("Please select a single row for editing.", "Try again", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            UpdateMealBtn.Show();
            CancelMealBtn.Show();
            DeleteFoodlBtn.Hide();
            EditMealBtn.Hide();
            }
        }

        private void DeleteFoodlBtn_Click(object sender, EventArgs e)
        {
            if (FoodTbl.SelectedRows.Count == 1)
            {
                DataGridViewRow selectedRow = FoodTbl.SelectedRows[0];
                int variationIDColumn = Convert.ToInt32(FoodTbl.SelectedRows[0].Cells["VariationID"].Value);

                DialogResult result = MessageBox.Show("Are you sure you want to delete this row?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        conn.Open();
                        string deleteQuery = "DELETE FROM mealvariation WHERE VariationID = @VariationID";
                        MySqlCommand cmdDataBase = new MySqlCommand(deleteQuery, conn);
                        cmdDataBase.Parameters.AddWithValue("@VariationID", variationIDColumn);
                        cmdDataBase.ExecuteNonQuery();

                        adminMethods.LoadMenuItems();
                        MessageBox.Show("Row Deleted!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a single row for deletion.", "Try again", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateMealBtn_Click(object sender, EventArgs e)
        {
            string variationName = VariationNmTxtB.Text;
            string variationDescription = VariationDescTxtB.Text;
            decimal variationCost = decimal.Parse(VariationCostTxtB.Text);
            string variationCostText = VariationCostTxtB.Text;
            string selectedMenuCategory = MenuSelectComB.SelectedItem.ToString();
            int variationID = Convert.ToInt32(FoodTbl.SelectedRows[0].Cells["VariationID"].Value);
            int mealID = adminMethods.GetMealIDFromDatabase(selectedMenuCategory);

            if (string.IsNullOrWhiteSpace(variationCostText) || !decimal.TryParse(variationCostText, out variationCost))
            {
                MessageBox.Show("Invalid variation cost.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((string.IsNullOrWhiteSpace(variationName) || variationName == "Food Name") ||
                string.IsNullOrEmpty(variationDescription) || variationDescription == "Description")
            {
                MessageBox.Show("Please fill out all the required data", "Missing Informations", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DialogResult choices = MessageBox.Show("Are you sure the information you have entered is correct?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (choices == DialogResult.Yes)
            {
                try
                {
                    conn.Open();
                    string updateQuery = "UPDATE mealvariation " +
                    "SET VariationName = @variationName, VariationDescription = @variationDescription, VariationCost = @variationCost, MealID = @mealID";

                    byte[] imageData = null;

                    if (isNewFoodImageSelected)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            VariationPicB.Image.Save(ms, ImageFormat.Jpeg); // You can choose the format you want
                            imageData = ms.ToArray();
                            updateQuery += ", MealImage = @mealImage";
                        }
                    }

                    updateQuery += " WHERE VariationID = @variationID";
                    MySqlCommand cmdDataBase = new MySqlCommand(updateQuery, conn);
                    cmdDataBase.Parameters.AddWithValue("@variationName", variationName);
                    cmdDataBase.Parameters.AddWithValue("@variationDescription", variationDescription);
                    cmdDataBase.Parameters.AddWithValue("@variationCost", variationCost);
                    cmdDataBase.Parameters.AddWithValue("@mealID", mealID);
                    cmdDataBase.Parameters.AddWithValue("@variationID", variationID);

                    if (isNewFoodImageSelected)
                    {
                        cmdDataBase.Parameters.AddWithValue("@MealImage", imageData);
                    }

                    cmdDataBase.ExecuteNonQuery();

                    adminMethods.LoadMenuItems();
                    MessageBox.Show("Meal Updated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                catch (MySqlException a)
                {
                    if (a.Number == 1062)
                    {
                        MessageBox.Show("Variation name already exist.", "Add variation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        UsernameTxtB_AP.Clear();
                    }
                    else
                    {
                        MessageBox.Show(a.Message, "Add variation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception b)
                {
                    MessageBox.Show(b.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
            UpdateMealBtn.Hide();
            CancelMealBtn.Hide();
            DeleteFoodlBtn.Show();
            EditMealBtn.Show();

            TextboxPlaceholders.SetPlaceholder(VariationNmTxtB, "Food Name");
            TextboxPlaceholders.SetPlaceholder(VariationDescTxtB, "Description");
            TextboxPlaceholders.SetPlaceholder(VariationCostTxtB, "Price");
            VariationPicB.Image = null;
            MenuSelectComB.SelectedIndex = -1;
            VariationIDTxtBox.Clear();

            adminPanelManager.ShowPanel(AddMenuPanel);
        }

        private void CancelMealBtn_Click(object sender, EventArgs e)
        {
            UpdateMealBtn.Hide();
            CancelMealBtn.Hide();
            DeleteFoodlBtn.Show();
            EditMealBtn.Show();

            TextboxPlaceholders.SetPlaceholder(VariationNmTxtB, "Food Name");
            TextboxPlaceholders.SetPlaceholder(VariationDescTxtB, "Description");
            TextboxPlaceholders.SetPlaceholder(VariationCostTxtB, "Price");
            VariationPicB.Image = null;
            MenuSelectComB.SelectedIndex = -1;
            VariationIDTxtBox.Clear();

            adminPanelManager.ShowPanel(AddMenuPanel);
        }

        private void GenerateDailyReport_Click(object sender, EventArgs e)
        {
            DateTime selectedDate = StartDatePicker.Value;
            dailySalesReportMethod.CalculateAndDisplaySalesReportDaily(DailyDGV, ComputedSalesDailyTbl, selectedDate);

            DataTable mostSoldItem = dailySalesReportMethod.GetMostSoldItemForDay(selectedDate);
            MostSalesDailyTbl.DataSource = mostSoldItem;
        }
        private void GenerateWeeklyReportBtn_Click(object sender, EventArgs e)
        {
            DateTime selectedDate = StartDatePicker.Value.Date;  // Only consider the date part

            // Calculate the start date (Sunday) of the week for the selected date
            int diff = selectedDate.DayOfWeek - DayOfWeek.Sunday;
            if (diff < 0) diff += 7;
            DateTime startDate = selectedDate.AddDays(-diff);

            // End date is 7 days after the start date
            DateTime endDate = startDate.AddDays(7);

            weeklySalesReportMethod.CalculateAndDisplaySalesReportWeekly(WeeklyDGV, ComputedSalesWeeklyTbl, startDate, endDate);

            DataTable mostSoldItem = weeklySalesReportMethod.GetMostSoldItemForWeek(startDate, endDate);
            MostSalesWeeklyTbl.DataSource = mostSoldItem;
        }
        private void GenerateMonthlyReportBtn_Click(object sender, EventArgs e)
        {
            DateTime selectedDate = StartDatePicker.Value;
            DateTime startDate = new DateTime(selectedDate.Year, selectedDate.Month, 1);  // Start date is the first day of the selected month
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);  // End date is the last day of the selected month

            monthlySalesReportMethod.CalculateAndDisplaySalesReportMonthly(MonthlyDGV, ComputedSalesMonthlyTbl, startDate, endDate);

            DataTable mostSoldItem = monthlySalesReportMethod.GetMostSoldItemForMonth(startDate, endDate);
            MostSalesMonthlyTbl.DataSource = mostSoldItem;
        }


        //Staff Panel
        public static int orderIDGenerator()
        {
            Random random = new Random();
            return random.Next(1000, 1000000);
        }

        public void GetData()
        {
            conn.Close();
            flowLayoutPanel1.Controls.Clear();
            conn.Open();
            cm = new MySqlCommand("SELECT VariationName, VariationCost, MealImage, VariationID FROM mealvariation", conn);
            dr = cm.ExecuteReader();

            while (dr.Read())
            {
                byte[] imageBytes = (byte[])dr["MealImage"];

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Image mealImage = Image.FromStream(ms);
                    pic = new PictureBox
                    {
                        Width = 150,
                        Height = 150,
                        BackgroundImage = mealImage,
                        BackgroundImageLayout = ImageLayout.Stretch,
                        Tag = dr["VariationID"].ToString(),
                    };

                    price = new Label
                    {
                        Text = "Php. " + dr["VariationCost"].ToString(),
                        Width = 25,
                        Height = 15,
                        TextAlign = ContentAlignment.TopLeft,
                        Dock = DockStyle.Top,
                        BackColor = Color.White,
                    };

                    mealname = new Label
                    {
                        Text = dr["VariationName"].ToString(),
                        Width = 25,
                        Height = 15,
                        TextAlign = ContentAlignment.BottomCenter,
                        Dock = DockStyle.Bottom,
                        BackColor = Color.White,
                    };

                    pic.Controls.Add(mealname);
                    pic.Controls.Add(price);
                    flowLayoutPanel1.Controls.Add(pic);
                    pic.Click += OnFLP1Click;
                }
            }
            dr.Close();
            conn.Close();
        }

        public void GetData2()
        {
            flowLayoutPanel2.Controls.Clear();
            conn.Open();
            cm = new MySqlCommand("SELECT MealImage, MealID, MealName FROM meal WHERE MealID>=24", conn);
            dr = cm.ExecuteReader();

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,  // One column for one picture per row
            };

            while (dr.Read())
            {
                int mealID = (int)dr["MealID"];
                byte[] imageBytes = (byte[])dr["MealImage"];

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Image mealImage = Image.FromStream(ms);
                    menupic = new PictureBox
                    {
                        Width = 140,
                        Height = 125,
                        BackgroundImage = mealImage,
                        BackgroundImageLayout = ImageLayout.Stretch,
                        Tag = mealID.ToString(),
                    };

                    mealname = new Label
                    {
                        Text = dr["MealName"].ToString(),
                        Width = 25,
                        Height = 15,
                        TextAlign = ContentAlignment.BottomCenter,
                        Dock = DockStyle.Bottom,
                        BackColor = Color.White,
                    };
                    table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    table.Controls.Add(menupic);
                    table.Controls.Add(mealname);
                    flowLayoutPanel2.Controls.Add(table);
                    menupic.Click += OnFLP2Click;
                }
            }
            dr.Close();
            conn.Close();
        }

        private void OnFLP1Click(object sender, EventArgs e)
        {
            PictureBox clickedPic = (PictureBox)sender;
            string tag = clickedPic.Tag.ToString();
            conn.Open();
            cm = new MySqlCommand("Select * from mealvariation where VariationID like'" + tag + "'", conn);
            dr = cm.ExecuteReader();
            dr.Read();

            if (dr.HasRows)
            {
                string variationName = dr["VariationName"].ToString();
                string variationCost = dr["VariationCost"].ToString();
                string quantity = dr["qty"].ToString();

                // Check if a variation with the same VariationName already exists in the DataGridView
                bool exists = false;
                int rowIndex = -1; // To store the index of the existing row
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells[0].Value != null && row.Cells[0].Value.ToString() == variationName)
                    {
                        exists = true;
                        rowIndex = row.Index;
                        break;
                    }
                }

                if (!exists)
                {
                    dataGridView1.Rows.Add(variationName, "-", quantity, "+", variationCost, "X");
                    UpdateTotalPrice();
                }
                else
                {
                    // Increment the quantity column for the existing meal
                    int currentQty = int.Parse(dataGridView1.Rows[rowIndex].Cells[2].Value.ToString());
                    currentQty++;
                    dataGridView1.Rows[rowIndex].Cells[2].Value = currentQty;
                    AddTotalPrice(rowIndex);
                }
            }
            dr.Close();
            conn.Close();
        }



        private void OnFLP2Click(object sender, EventArgs e)
        {
            if (sender is PictureBox clickedPic)
            {
                string mealID = clickedPic.Tag.ToString();
                DisplayVariationNamesByMealID(mealID);
            }
        }

        private void DisplayVariationNamesByMealID(string mealID)
        {
            flowLayoutPanel1.Controls.Clear();
            conn.Open();
            cm = new MySqlCommand("SELECT VariationName, VariationCost, MealImage, VariationID FROM mealvariation WHERE MealID = @mealID", conn);
            cm.Parameters.AddWithValue("@mealID", mealID);
            dr = cm.ExecuteReader();

            while (dr.Read())
            {
                string mealName = dr["VariationName"].ToString();

                if (!dr.IsDBNull(dr.GetOrdinal("MealImage")))
                {
                    byte[] imageBytes = (byte[])dr["MealImage"];

                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        Image mealImage = Image.FromStream(ms);
                        pic = new PictureBox
                        {
                            Width = 150,
                            Height = 150,
                            BackgroundImage = mealImage,
                            BackgroundImageLayout = ImageLayout.Stretch,
                            Tag = dr["VariationID"].ToString(),
                        };

                        price = new Label
                        {
                            Text = "Php. " + dr["VariationCost"].ToString(),
                            Width = 25,
                            Height = 15,
                            TextAlign = ContentAlignment.TopLeft,
                            Dock = DockStyle.Top,
                            BackColor = Color.White,
                        };

                        mealname = new Label
                        {
                            Text = dr["VariationName"].ToString(),
                            Width = 25,
                            Height = 15,
                            TextAlign = ContentAlignment.BottomCenter,
                            Dock = DockStyle.Bottom,
                            BackColor = Color.White,

                        };

                        pic.Controls.Add(mealname);
                        pic.Controls.Add(price);
                        flowLayoutPanel1.Controls.Add(pic);
                        pic.Click += OnFLP1Click;
                    }
                }
            }
            dr.Close();
            conn.Close();
        }

        private void allBtn_Click(object sender, EventArgs e)
        {
            GetData();
        }        

        private void UpdateTotalPrice()
        {
            totalPrice = 0.00m;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[4].Value != null)
                {
                    decimal rowTotal = decimal.Parse(row.Cells[4].Value.ToString());
                    totalPrice += rowTotal;
                }
            }
            sbLbl.Text = "Php. " + totalPrice.ToString("0.00");
            ttlLbl.Text = sbLbl.Text;
            if (discChckBx.Checked)
            {
                decimal totalPrice = decimal.Parse(sbLbl.Text.Replace("Php. ", ""));
                decimal discount = totalPrice * 0.20m; // 20% discount
                decimal discountedTotal = totalPrice - discount;
                dscLbl.Text = "Php. " + discount.ToString("0.00");
                ttlLbl.Text = "Php. " + discountedTotal.ToString("0.00");
            }
        }

        private void AddTotalPrice(int rowIndex)
        {
            int currentQty = int.Parse(dataGridView1.Rows[rowIndex].Cells[2].Value.ToString());
            string foodName = dataGridView1.Rows[rowIndex].Cells[0].Value.ToString(); // Get the food name from DataGridView
            decimal unitPrice = GetUnitPriceForFood(foodName); // Retrieve unit price based on VariationName
            decimal totalPrice = currentQty * unitPrice;
            dataGridView1.Rows[rowIndex].Cells[4].Value = totalPrice.ToString();

            UpdateTotalPrice();
        }

        private void SubtractTotalPrice(int rowIndex)
        {
            int currentQty = int.Parse(dataGridView1.Rows[rowIndex].Cells[2].Value.ToString());
            string foodName = dataGridView1.Rows[rowIndex].Cells[0].Value.ToString();
            decimal unitPrice = GetUnitPriceForFood(foodName);

            if (currentQty > 1)
            {
                currentQty--;
                dataGridView1.Rows[rowIndex].Cells[2].Value = currentQty; // Update the quantity in the DataGridView

                decimal totalPrice = currentQty * unitPrice;
                dataGridView1.Rows[rowIndex].Cells[4].Value = totalPrice.ToString();
                UpdateTotalPrice();
            }
        }

        private decimal GetUnitPriceForFood(string foodName)
        {
            decimal unitPrice = 0;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand("SELECT VariationCost FROM mealvariation WHERE VariationName = @foodName", connection))
                {
                    command.Parameters.AddWithValue("@foodName", foodName);

                    connection.Open();

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            unitPrice = decimal.Parse(reader["VariationCost"].ToString());
                        }
                    }
                }
            }

            return unitPrice;
        }


        private byte[] GetBytesFromImage(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png); // You can specify the image format here
                return ms.ToArray();
            }
        }

        private void voidBtn_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("There are no items in your cart.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string userPosition = PositionTxtBox.Text; // Replace this with the logic to get the user's position

            DialogResult result;

            if (userPosition == "Staff")
            {
                // If the user is a staff member, prompt for manager's password
                string enteredPassword = Encryptor.HashPassword(Microsoft.VisualBasic.Interaction.InputBox("Enter manager password:", "Password Required", ""));

                string connectionString = "server=localhost;user=root;database=dashboarddb;password=";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT Position FROM employee_acc WHERE Password = @Password";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Password", enteredPassword);

                        // Execute the query
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string position = reader["Position"].ToString();

                                if (position == "Manager")
                                {
                                    result = MessageBox.Show("Do you want to void these items?", "Void Items", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                }
                                else
                                {
                                    MessageBox.Show("Invalid password. You need manager permission to void items.", "Permission Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                            else
                            {
                                MessageBox.Show("Invalid password. You need manager permission to void items.", "Permission Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                    }
                }

            }
            else // For Managers and Admins, no password is required
            {
                result = MessageBox.Show("Do you want to void these items?", "Void Items", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }

            if (result == DialogResult.Yes)
            {
                GenerateID = orderIDGenerator();
                InsertOrderData(GenerateID, true);
                InsertOrderItemsData(GenerateID, dataGridView1, true);
            }
            dataGridView1.Rows.Clear();
            sbLbl.Text = "Php. 0.00";
            ttlLbl.Text = "Php. 0.00";
            dscLbl.Text = "Php. 0.00";
            cashtxtBx.Text = "0.00";
            cashtxtBx.ForeColor = Color.LightGray;
            discChckBx.Checked = false;
            GenerateID = orderIDGenerator();
        }

        private void placeBtn_Click(object sender, EventArgs e)
        {
                GeneratePDFReceipt();         
        }
        private void GeneratePDFReceipt()
        {
            decimal subtotal = decimal.Parse(sbLbl.Text.Replace("Php. ", ""));
            decimal discount = decimal.Parse(dscLbl.Text.Replace("Php. ", ""));
            decimal totalAmount = decimal.Parse(ttlLbl.Text.Replace("Php. ", ""));
            decimal cashEntered;

            int totalQuantity = 0;

            if (!decimal.TryParse(cashtxtBx.Text, out cashEntered))
            {
                MessageBox.Show("Please enter a valid amount for payment.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (cashEntered < totalAmount)
            {
                MessageBox.Show("Please enter a valid amount for payment.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
            {
                saveFileDialog1.Filter = "PDF Files|*.pdf";
                saveFileDialog1.Title = "Save PDF File";

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string pdfFilePath = saveFileDialog1.FileName;

                    using (PdfWriter writer = new PdfWriter(new FileStream(pdfFilePath, FileMode.Create)))
                    using (PdfDocument pdf = new PdfDocument(writer))
                    using (Document doc = new Document(pdf))
                    {
                        doc.SetProperty(Property.TEXT_ALIGNMENT, TextAlignment.JUSTIFIED_ALL);
                        ImageData logoImageData = ImageDataFactory.Create(GetBytesFromImage(Properties.Resources.luna));
                        iText.Layout.Element.Image logo = new iText.Layout.Element.Image(logoImageData);
                        logo.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
                        logo.SetWidth(150);
                        logo.SetHeight(150);
                        // Add the logo to the PDF
                        doc.Add(logo);
                        doc.Add(new Paragraph($"Served by: {positionDB} {usernameDB}").SetTextAlignment(TextAlignment.LEFT));
                        doc.Add(new Paragraph("Date: " + DateTime.Now.ToString("MM/dd/yyyy   hh:mm:ss tt")).SetTextAlignment(TextAlignment.LEFT));
                        doc.Add(new Paragraph("--------------------------------------------------------------------------------------------------"));
                        doc.Add(new Paragraph($"QUANTITY                           MEAL                    PRICE"));

                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            string food = row.Cells[0].Value.ToString();
                            string quantity = row.Cells[2].Value.ToString();
                            string price = row.Cells[4].Value.ToString();
                            if (int.TryParse(quantity, out int quantityValue))
                            {
                                totalQuantity += quantityValue;
                            }
                            doc.Add(new Paragraph($"{quantity}                                   {food}                    {price}"));
                        }

                        doc.Add(new Paragraph($"---------------------------------------{totalQuantity} Item(s)-----------------------------------------"));
                        doc.Add(new Paragraph($"SUBTOTAL:                         Php. {subtotal.ToString("0.00")}"));
                        doc.Add(new Paragraph($"DISCOUNT:                         Php. {discount.ToString("0.00")}"));
                        doc.Add(new Paragraph($"TOTAL:                         Php. {totalAmount.ToString("0.00")}"));
                        doc.Add(new Paragraph($"CASH:                         Php. {cashEntered.ToString("0.00")}"));
                        decimal change = cashEntered - totalAmount;
                        doc.Add(new Paragraph($"CHANGE:                         Php. {change.ToString("0.00")}"));

                        doc.Add(new Paragraph("--------------------------------------------------------------------------------------------------"));
                        doc.Add(new Paragraph("This Receipt Serves as Your Proof of Purchase").SetTextAlignment(TextAlignment.CENTER));
                    }

                    MessageBox.Show("Receipt generated successfully and saved to:\n" + pdfFilePath, "Enjoy your meal!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    InsertOrderData(GenerateID, false);
                    InsertOrderItemsData(GenerateID, dataGridView1, false);
                    InsertSalesData(GenerateID);
                    GenerateID = orderIDGenerator();
                    dataGridView1.Rows.Clear();
                    sbLbl.Text = "Php. 0.00";
                    ttlLbl.Text = "Php. 0.00";
                    dscLbl.Text = "Php. 0.00";
                    cashtxtBx.Text = "0.00";
                    discChckBx.Checked = false;
                    cashtxtBx.ForeColor = Color.LightGray;
                    System.Diagnostics.Process.Start(pdfFilePath);
                }
            }
        }

        private void logoutBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to log-out?", "information", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                loginPanelManager.ShowPanel(LoginPanelContainer);
                dataGridView1.Rows.Clear();
                sbLbl.Text = "Php. 0.00";
                ttlLbl.Text = "Php. 0.00";
                dscLbl.Text = "Php. 0.00";
                cashtxtBx.Text = "0.00";
                cashtxtBx.ForeColor = Color.LightGray;
                discChckBx.Checked = false;
            }
        }

        private void dataGridView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    dataGridView1.SelectedRows[0].Selected = false;
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1 && e.RowIndex >= 0)
            {
                SubtractTotalPrice(e.RowIndex);
            }

            if (e.ColumnIndex == 3 && e.RowIndex >= 0)
            {
                int currentQty = int.Parse(dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString());
                currentQty++;
                dataGridView1.Rows[e.RowIndex].Cells[2].Value = currentQty;
                AddTotalPrice(e.RowIndex);
            }

            if (e.ColumnIndex == 5 && e.RowIndex >= 0)
            {
                string userPosition = PositionTxtBox.Text; // Replace this with the logic to get the user's position

                if (userPosition == "Staff")
                {
                    // If the user is a staff member, prompt for manager's password
                    string enteredPassword = Encryptor.HashPassword(Microsoft.VisualBasic.Interaction.InputBox("Enter manager password:", "Password Required", ""));

                    string connectionString = "server=localhost;user=root;database=dashboarddb;password=";

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        string query = "SELECT Position FROM employee_acc WHERE Password = @Password";

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Password", enteredPassword);

                            // Execute the query
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string position = reader["Position"].ToString();

                                    if (position == "Manager")
                                    {
                                            if (e.RowIndex < dataGridView1.Rows.Count)
                                            {
                                                // Calculate the price of the removed item
                                                decimal removedItemPrice = decimal.Parse(dataGridView1.Rows[e.RowIndex].Cells[4].Value.ToString());

                                                // Remove the selected row from the DataGridView
                                                dataGridView1.Rows.RemoveAt(e.RowIndex);

                                                // Update the total price by subtracting the removed item's price
                                                totalPrice -= removedItemPrice;
                                                sbLbl.Text = "Php. " + totalPrice.ToString("0.00");
                                                ttlLbl.Text = sbLbl.Text;

                                                if (discChckBx.Checked)
                                                {
                                                    decimal totalPrice = decimal.Parse(sbLbl.Text.Replace("Php. ", ""));
                                                    decimal discount = totalPrice * 0.20m;
                                                    decimal discountedTotal = totalPrice - discount;

                                                    dscLbl.Text = "Php. " + discount.ToString("0.00");
                                                    ttlLbl.Text = "Php. " + discountedTotal.ToString("0.00");
                                                }
                                            }                                       
                                    }
                                    else
                                    {
                                        MessageBox.Show("Invalid password. You need manager permission to remove an item.", "Permission Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Invalid password. You need manager permission to remove an item.", "Permission Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                    }
                }
                else // For Managers and Admins, no password is required
                {
                    if (e.RowIndex < dataGridView1.Rows.Count)
                    {
                        decimal removedItemPrice = decimal.Parse(dataGridView1.Rows[e.RowIndex].Cells[4].Value.ToString());
                        dataGridView1.Rows.RemoveAt(e.RowIndex);
                        totalPrice -= removedItemPrice;
                        sbLbl.Text = "Php. " + totalPrice.ToString("0.00");
                        ttlLbl.Text = sbLbl.Text;
                    }                   
                }
            }
        }


        private void CafeDeLunaDashboard_Load(object sender, EventArgs e)
        {
            SearchTxtbx.Text = "Type here to search";
            SearchTxtbx.ForeColor = Color.LightGray;
            cashtxtBx.Text = "0.00";
            cashtxtBx.ForeColor = Color.LightGray;
        }

        private void SearchTxtbx_TextChanged(object sender, EventArgs e)
        {
            string searchQuery = SearchTxtbx.Text;
            flowLayoutPanel1.Controls.Clear();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                conn.Open();
                cm = new MySqlCommand("SELECT VariationName, VariationCost, MealImage, VariationID FROM mealvariation WHERE VariationName LIKE @searchQuery", conn);
                cm.Parameters.AddWithValue("@searchQuery", "%" + searchQuery + "%");

                dr = cm.ExecuteReader();

                while (dr.Read())
                {
                    if (!dr.IsDBNull(dr.GetOrdinal("MealImage")))
                    {
                        byte[] imageBytes = (byte[])dr["MealImage"];

                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        {
                            Image mealImage = Image.FromStream(ms);
                            pic = new PictureBox
                            {
                                Width = 150,
                                Height = 150,
                                BackgroundImage = mealImage,
                                BackgroundImageLayout = ImageLayout.Stretch,
                                Tag = dr["VariationID"].ToString(),
                            };

                            price = new Label
                            {
                                Text = "Php. " + dr["VariationCost"].ToString(),
                                Width = 25,
                                Height = 15,
                                TextAlign = ContentAlignment.TopLeft,
                                Dock = DockStyle.Top,
                                BackColor = Color.White,
                            };

                            mealname = new Label
                            {
                                Text = dr["VariationName"].ToString(),
                                Width = 25,
                                Height = 15,
                                TextAlign = ContentAlignment.BottomCenter,
                                Dock = DockStyle.Bottom,
                                BackColor = Color.White,
                            };

                            pic.Controls.Add(mealname);
                            pic.Controls.Add(price);
                            flowLayoutPanel1.Controls.Add(pic);
                            pic.Click += OnFLP1Click;
                        }
                    }
                }
                dr.Close();
                conn.Close();
            }
            else
            {
                GetData();
            }
        }

        private void SearchTxtbx_Enter(object sender, EventArgs e)
        {
            if (SearchTxtbx.Text == "Type here to search")
            {
                SearchTxtbx.Text = "";
                SearchTxtbx.ForeColor = Color.Black;
            }
        }

        private void SearchTxtbx_Leave(object sender, EventArgs e)
        {
            if (SearchTxtbx.Text == "")
            {
                SearchTxtbx.Text = "Type here to search";
                SearchTxtbx.ForeColor = Color.LightGray;
                GetData();
            }
        }

        private void RefreshPlaceButtonState()
        {
            if (dataGridView1.Rows.Count == 0 || !IsAnyMealSelected() || string.IsNullOrEmpty(cashtxtBx.Text) || cashtxtBx.Text == "0.00")
            {
                placeBtn.Enabled = false;
            }
            else
            {
                placeBtn.Enabled = true;
            }
        }

        private bool IsAnyMealSelected()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[0].Value != null && row.Cells[0].Value.ToString() != "-")
                {
                    return true;
                }
            }
            return false;
        }

        private void cashtxtBx_TextChanged(object sender, EventArgs e)
        {
            RefreshPlaceButtonState();
        }

        private void cashtxtBx_Enter(object sender, EventArgs e)
        {
            if (cashtxtBx.Text == "0.00")
            {
                cashtxtBx.Text = "";
                cashtxtBx.ForeColor = Color.Black;
            }
        }

        private void cashtxtBx_Leave(object sender, EventArgs e)
        {
            if (cashtxtBx.Text == "")
            {
                cashtxtBx.Text = "0.00";
                cashtxtBx.ForeColor = Color.LightGray;
            }
            ValidateCashTextbox();
        }

        private void cashtxtBx_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            if (e.KeyChar == (char)Keys.Enter)
            {
                ValidateCashTextbox();

            }

            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }

        private void ValidateCashTextbox()
        {
            decimal cashValue;
            if (!decimal.TryParse(cashtxtBx.Text, out cashValue) || cashValue < 0)
            {
                MessageBox.Show("Please enter a valid positive decimal value (xxx.xx).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cashtxtBx.Focus();
                cashtxtBx.SelectAll();
            }
        }


        private void discChckBx_CheckedChanged(object sender, EventArgs e)
        {
            if (discChckBx.Checked)
            {
                decimal totalPrice = decimal.Parse(sbLbl.Text.Replace("Php. ", ""));
                decimal discount = totalPrice * 0.20m;
                decimal discountedTotal = totalPrice - discount;

                dscLbl.Text = "Php. " + discount.ToString("0.00");
                ttlLbl.Text = "Php. " + discountedTotal.ToString("0.00");
            }
            else
            {
                dscLbl.Text = "Php. 0.00";
                UpdateTotalPrice();
            }
        }

        //Methods for sending place order to database
        string connectionString = "server=localhost;user=root;database=dashboarddb;password=";
        private void InsertOrderData(int generatedOrderID, bool isVoided)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string orderQuery;

                if (isVoided)
                {
                    orderQuery = "INSERT INTO orders (OrderID, UserID, IsVoided) VALUES (@OrderID, @UserID, @Voided)";
                }
                else
                {
                    orderQuery = "INSERT INTO orders (OrderID, UserID) VALUES (@OrderID, @UserID)";
                }

                using (MySqlCommand orderCmd = new MySqlCommand(orderQuery, connection))
                {
                    orderCmd.Parameters.AddWithValue("@OrderID", generatedOrderID);
                    orderCmd.Parameters.AddWithValue("@UserID", employeeID);

                    if (isVoided)
                    {
                        orderCmd.Parameters.AddWithValue("@Voided", "voided");
                    }

                    orderCmd.ExecuteNonQuery();
                }
            }

            string voidedStatus = isVoided ? "Voided" : "Placed";
            MessageBox.Show($"{voidedStatus} order successfully. OrderID={generatedOrderID}, UserID={employeeID}, Amount={ttlLbl.Text}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void InsertSalesData(int generatedOrderID)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string salesQuery = "INSERT INTO sales (OrderID, Amount) VALUES (@OrderID, @Amount)";

                using (MySqlCommand salesCmd = new MySqlCommand(salesQuery, connection))
                {
                    string totalText = ttlLbl.Text;
                    string numericValue = totalText.Replace("Php.", "").Trim();
                    decimal.TryParse(numericValue, out decimal amount);

                    // Insert data into the sales table with the correct total value
                    salesCmd.Parameters.AddWithValue("@OrderID", generatedOrderID);
                    salesCmd.Parameters.AddWithValue("@Amount", amount);
                    salesCmd.ExecuteNonQuery();
                }
            }
        }

        private Tuple<int, int> GetVariationInfo(string itemName)
        {
            int variationID = -1;
            int mealID = -1;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT VariationID, MealID FROM mealvariation WHERE VariationName = @ItemName";
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ItemName", itemName);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            variationID = reader.GetInt32("VariationID");
                            mealID = reader.GetInt32("MealID");
                        }
                    }
                }
            }
            return Tuple.Create(variationID, mealID);
        }

        private void InsertOrderItemsData(int generatedOrderID, DataGridView dataGridView, bool isVoided)
        {
            bool itemNameFound = false;
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    string itemName;
                    if (row.Cells["Column1"].Value != null)
                    {
                        itemName = row.Cells["Column1"].Value.ToString();
                        itemNameFound = true;
                    }
                    else
                    {
                        continue;
                    }

                    int qty = Convert.ToInt32(row.Cells["Column3"].Value);
                    Tuple<int, int> variationInfo = GetVariationInfo(itemName);
                    int variationID = variationInfo.Item1;
                    int mealID = variationInfo.Item2;

                    string query;
                    if (isVoided)
                    {
                        query = "INSERT INTO orderitems (OrderID, MealID, VariationID, Quantity, IsVoided) VALUES (@OrderID, @MealID, @VariationID, @Qty, @voided)";
                    }
                    else
                    {
                        query = "INSERT INTO orderitems (OrderID, MealID, VariationID, Quantity) VALUES (@OrderID, @MealID, @VariationID, @Qty)";
                    }
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@OrderID", generatedOrderID);
                        cmd.Parameters.AddWithValue("@MealID", mealID);
                        cmd.Parameters.AddWithValue("@VariationID", variationID);
                        cmd.Parameters.AddWithValue("@Qty", qty);

                        if (isVoided)
                        {
                            cmd.Parameters.AddWithValue("@voided", "voided");
                        }

                        cmd.ExecuteNonQuery();
                    }
                }
                if (!itemNameFound)
                {
                    MessageBox.Show("ItemName is null. IDK why.");
                }
            }
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            RefreshPlaceButtonState();
        }

        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            RefreshPlaceButtonState();
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            RefreshPlaceButtonState();
        }
    }
}
