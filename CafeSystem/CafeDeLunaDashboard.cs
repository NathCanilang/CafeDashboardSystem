using iText.IO.Image;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace CafeSystem
{
    public partial class CafeDeLunaDashboard : Form
    {
        private readonly MySqlConnection conn;
        private readonly AdminMethods adminMethods = new AdminMethods();
        private DailySalesReportMethod dailySalesReportMethod = new DailySalesReportMethod();
        private WeeklySalesReportMethod weeklySalesReportMethod = new WeeklySalesReportMethod();
        private MonthlySalesReportMethod monthlySalesReportMethod = new MonthlySalesReportMethod();
        public static CafeDeLunaDashboard cafeDeLunaInstance;
        private readonly LoginPanelManager loginPanelManager;
        private readonly AdminPanelManager adminPanelManager;
        private readonly SalesPanelManager salesPanelManager;
        private byte[] imageData;
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

            //Admin Panel
            FoodTbl.DataError += new DataGridViewDataErrorEventHandler(adminMethods.FoodTable_DataError);
            FoodTbl.RowPostPaint += new DataGridViewRowPostPaintEventHandler(adminMethods.FoodTable_RowPostPaint);
            PositionComB_AP.Items.AddRange(position);
            PositionComB_AP.DropDownStyle = ComboBoxStyle.DropDownList;
            MenuSelectComB.DropDownStyle = ComboBoxStyle.DropDownList;
            adminMethods.PopulateMealComboBox();
            UserBirthdate.ValueChanged += CalculateAge;
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

                /*TxtPlaceholder.SetPlaceholder(LastNTxtB_AP, "Last name");
                TxtPlaceholder.SetPlaceholder(FirstNTxtB_AP, "First name");
                TxtPlaceholder.SetPlaceholder(MiddleNTxtB_AP, "Middle name");*/

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
            //string userImagePath = ImgTxtB.Text;

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

            /*TxtPlaceholder.SetPlaceholder(LastNTxtB_AP, "Last name");
            TxtPlaceholder.SetPlaceholder(FirstNTxtB_AP, "First name");
            TxtPlaceholder.SetPlaceholder(MiddleNTxtB_AP, "Middle name");*/
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

                        VariationPicB.Image = selectedImage;
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

                    /*TxtPlaceholder.SetPlaceholder(VariationNmTxtB, "Food Name");
                    TxtPlaceholder.SetPlaceholder(VariationDescTxtB, "Description");
                    TxtPlaceholder.SetPlaceholder(VariationCostTxtB, "Price");
                    VarietyFilePathTxtB.Text = "";*/
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

            }
            UpdateMealBtn.Show();
            CancelMealBtn.Show();
            DeleteFoodlBtn.Hide();
            EditMealBtn.Hide();
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

            /*TxtPlaceholder.SetPlaceholder(VariationNmTxtB, "Food Name");
            TxtPlaceholder.SetPlaceholder(VariationDescTxtB, "Description");
            TxtPlaceholder.SetPlaceholder(VariationCostTxtB, "Price");*/
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

            /*TxtPlaceholder.SetPlaceholder(VariationNmTxtB, "Food Name");
            TxtPlaceholder.SetPlaceholder(VariationDescTxtB, "Description");
            TxtPlaceholder.SetPlaceholder(VariationCostTxtB, "Price");*/
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
    }
}
