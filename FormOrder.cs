using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static POS.FormLogin;
using System.Drawing.Printing;

namespace POS
{
    public partial class FormOrder : Form
    {
        private string connectionString = @"Data Source=RITHEA-CHHAYSOK\DATASQL;Initial Catalog=Login;Integrated Security=True";
        string selectedImagePath = "";

        public FormOrder()
        {
            InitializeComponent();
        }

        private void FormOrder_Load(object sender, EventArgs e)
        {
            SyncProductsToList();
            LoadProductsFromDatabase();

            panelRegister.Hide();
            panelProduct.Hide();
            lvproduct.Hide();

            // Set up ListView columns ONCE when the form loads in the correct order:
            lvproduct.View = View.Details;
            lvproduct.FullRowSelect = true;
            lvproduct.GridLines = true;

            lvproduct.Columns.Clear();
            lvproduct.Columns.Add("Product ID", 150);
            lvproduct.Columns.Add("Product Name", 150);
            lvproduct.Columns.Add("Category", 120);
            lvproduct.Columns.Add("Price", 80);
            lvproduct.Columns.Add("Stock", 80);

            lvProducts.View = View.Details;
            lvProducts.FullRowSelect = true;
            lvProducts.GridLines = true;
            lvProducts.Columns.Clear();
            lvProducts.Columns.Add("Image", 100);
            lvProducts.Columns.Add("Name", 150);
            lvProducts.Columns.Add("Price", 80);
            lvProducts.Columns.Add("Category", 120);
        }

        private void LoadProductsFromDatabase()
        {
            ImageList imgList = new ImageList();
            lvproduct.Items.Clear();
            imgList.Images.Clear();

            imgList.ImageSize = new Size(64, 64);
            lvproduct.SmallImageList = imgList;

            string query = "SELECT ProductID, ProductName, Category, Price, Stock, ImagePath FROM Products";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    try
                    {
                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        int imageIndex = 0;
                        while (reader.Read())
                        {
                            string imgPath = reader["ImagePath"]?.ToString();
                            if (!string.IsNullOrEmpty(imgPath) && File.Exists(imgPath))
                            {
                                using (Image originalImage = Image.FromFile(imgPath))
                                {
                                    Bitmap b = new Bitmap(originalImage, new Size(64, 64));
                                    imgList.Images.Add(b);
                                }
                            }
                            else
                            {
                                Bitmap b = new Bitmap(64, 64);
                                imgList.Images.Add(b);
                            }

                            ListViewItem item = new ListViewItem(reader["ProductID"].ToString());
                            item.ImageIndex = imageIndex++;

                            item.SubItems.Add(reader["ProductName"].ToString());
                            item.SubItems.Add(reader["Category"].ToString());

                            if (decimal.TryParse(reader["Price"].ToString(), out decimal price))
                            {
                                item.SubItems.Add(price.ToString("C2"));
                            }
                            else
                            {
                                item.SubItems.Add(reader["Price"].ToString());
                            }

                            item.SubItems.Add(reader["Stock"].ToString());

                            lvproduct.Items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading data from database: " + ex.Message);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (UserSession.Role != "Admin")
            {
                PanelOrder.Hide();
                lvproduct.Hide();
                panelProduct.Hide();
                MessageBox.Show("Only administrator can open this", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                panelRegister.Hide();
                PanelOrder.Hide();
                panelProduct.Show();
                lvproduct.Show();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (txtProductID.Text.Trim() == "" || txtProductName.Text.Trim() == "" || txtPrice.Text.Trim() == "" ||
                txtStock.Text.Trim() == "" || cmbCategory.SelectedIndex == -1)
            {
                MessageBox.Show("Please fill in all required fields.");
                return;
            }

            decimal price;
            if (!decimal.TryParse(txtPrice.Text, out price))
            {
                MessageBox.Show("Price must be a number.");
                return;
            }

            string query = "INSERT INTO Products (ProductID, ProductName, Category, Price, Stock, ImagePath) " +
                           "VALUES (@ID, @Name, @Category, @Price, @Stock, @ImagePath)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", txtProductID.Text);
                    cmd.Parameters.AddWithValue("@Name", txtProductName.Text);
                    cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Stock", string.IsNullOrEmpty(txtStock.Text) ? 0 : Convert.ToInt32(txtStock.Text));
                    cmd.Parameters.AddWithValue("@ImagePath", string.IsNullOrEmpty(selectedImagePath) ? (object)DBNull.Value : selectedImagePath);
                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Product saved to database successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving to database: " + ex.Message);
                        return;
                    }
                }
            }

            ListViewItem item = new ListViewItem(txtProductID.Text);
            item.SubItems.Add(txtProductName.Text);
            item.SubItems.Add(cmbCategory.Text);
            item.SubItems.Add(price.ToString("C2"));
            item.SubItems.Add(string.IsNullOrEmpty(txtStock.Text) ? "0" : txtStock.Text);

            lvproduct.Items.Add(item);

            btnClear_Click(sender, e);
            SyncProductsToList();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtProductID.Clear();
            txtProductName.Clear();
            txtPrice.Clear();
            txtStock.Clear();
            cmbCategory.SelectedIndex = -1;
            txtProductID.Focus();
           
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem selected in lvproduct.SelectedItems)
            {
                string productId = selected.Text;
                string query = "DELETE FROM Products WHERE ProductID = @ID";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", productId);
                        try
                        {
                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Delete Error: " + ex.Message);
                        }
                    }
                }
                lvproduct.Items.Remove(selected);
            }
            SyncProductsToList();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (lvproduct.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select an item to update.");
                return;
            }

            ListViewItem item = lvproduct.SelectedItems[0];
            string productId = item.Text;

            decimal price = 0;
            if (txtPrice.Text != "" && !decimal.TryParse(txtPrice.Text, out price))
            {
                MessageBox.Show("Price must be a number.");
                return;
            }

            string query = "UPDATE Products SET " +
                           "ProductName = ISNULL(NULLIF(@Name, ''), ProductName), " +
                           "Category = ISNULL(NULLIF(@Category, ''), Category), " +
                           "Price = CASE WHEN @Price = 0 THEN Price ELSE @Price END, " +
                           "Stock = ISNULL(NULLIF(@Stock, ''), Stock), " +
                           "ImagePath = ISNULL(NULLIF(@ImagePath, ''), ImagePath) " +
                           "WHERE ProductID = @ID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", productId);
                    cmd.Parameters.AddWithValue("@Name", txtProductName.Text);
                    cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Stock", string.IsNullOrEmpty(txtStock.Text) ? (object)DBNull.Value : txtStock.Text);
                    cmd.Parameters.AddWithValue("@ImagePath", string.IsNullOrEmpty(selectedImagePath) ? (object)DBNull.Value : selectedImagePath);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Product updated successfully!");
                        LoadProductsFromDatabase();
                        SyncProductsToList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database Error: " + ex.Message);
                    }
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            lvproduct.Items.Clear();

            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(64, 64);
            lvproduct.SmallImageList = imgList;

            string query = "SELECT ProductID, ProductName, Category, Price, Stock, ImagePath " +
                           "FROM Products WHERE ProductName LIKE @Search OR ProductID LIKE @Search";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + txtSearch.Text.Trim() + "%");

                    try
                    {
                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        int imageIndex = 0;
                        while (reader.Read())
                        {
                            string imgPath = reader["ImagePath"]?.ToString();
                            if (!string.IsNullOrEmpty(imgPath) && File.Exists(imgPath))
                            {
                                using (Image originalImage = Image.FromFile(imgPath))
                                {
                                    Bitmap b = new Bitmap(originalImage, new Size(64, 64));
                                    imgList.Images.Add(b);
                                }
                            }
                            else
                            {
                                Bitmap b = new Bitmap(64, 64);
                                imgList.Images.Add(b);
                            }

                            ListViewItem item = new ListViewItem(reader["ProductID"].ToString());
                            item.ImageIndex = imageIndex++;

                            item.SubItems.Add(reader["ProductName"].ToString());
                            item.SubItems.Add(reader["Category"].ToString());

                            if (decimal.TryParse(reader["Price"].ToString(), out decimal price))
                            {
                                item.SubItems.Add(price.ToString("C2"));
                            }
                            else
                            {
                                item.SubItems.Add(reader["Price"].ToString());
                            }

                            item.SubItems.Add(reader["Stock"].ToString());

                            lvproduct.Items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error searching database: " + ex.Message);
                    }
                }
            }
        }

        private void btnOrder_Click(object sender, EventArgs e)
        {
            lvproduct.Hide();
            panelProduct.Hide();
            lvProducts.Show();
            lvCart.Show();
            PanelOrder.Show();
            panelRegister.Hide();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                selectedImagePath = ofd.FileName;
                pictureBoxProduct.Image = Image.FromFile(selectedImagePath);
                pictureBoxProduct.SizeMode = PictureBoxSizeMode.StretchImage;

                if (lblFileName != null)
                {
                    lblFileName.Text = Path.GetFileName(selectedImagePath);
                }
            }
        }
        private void btnRegister_Click_1(object sender, EventArgs e)
        {
            string query = "SELECT COUNT(1) FROM login WHERE Username = @User AND Password = @Pass";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@User", txtUsername.Text);
                    cmd.Parameters.AddWithValue("@Pass", txtPassword.Text);
                    try
                    {
                        conn.Open();
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Username already exists. Please choose a different username.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        else
                        {
                            string insertQuery = "INSERT INTO login (Username, Password) VALUES (@User, @Pass)";
                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@User", txtUsername.Text);
                                insertCmd.Parameters.AddWithValue("@Pass", txtPassword.Text);
                                insertCmd.ExecuteNonQuery();
                                MessageBox.Show("User registered successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                txtUsername.Clear();
                                txtPassword.Clear();
                                
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            
            if (UserSession.Role != "Admin")
            {
                lvproduct.Hide();
                panelProduct.Hide();
                MessageBox.Show("Only administrator can open this", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                lvproduct.Hide();
                panelProduct.Hide();
                lvCart.Hide();
                lvProducts.Hide();
                PanelOrder.Hide();
                panelRegister.Show();
            }
            loaduserfromdatabase();

        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            string info = "POINT OF SALE SYSTEM\n" +
                            "School Assignment Project\n\n" +
                            "Developed By Team Members:\n" +
                            "1. Mean Sreyli - ID: 67695\n" +
                            "2. San Chenda - ID: 72469\n" +
                            "3. Chhem Touch - ID: 68550\n" +
                            "4. Thou Ponlork - ID: 68251\n" +
                            "5. Chhay sothearithea - ID: 67675\n" +
                            "6. K. Rachnasoriyareaksmey - ID: 68977\n\n" +
                            "© 2026 All Rights Reserved";

            MessageBox.Show(info, "About System", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void button11_Click(object sender, EventArgs e)
        {
            if (lvCart.Items.Count == 0)
            {
                MessageBox.Show("There is no order to print.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }




            decimal cashGiven = 0;
            decimal.TryParse(txtCost.Text.Replace("$", "").Replace(",", "").Trim(), out cashGiven);

            // 1. BUILD THE RECEIPT TEXT FOR MESSAGEBOX PREVIEW
            StringBuilder receipt = new StringBuilder();
            receipt.AppendLine("==================================");
            receipt.AppendLine("\t\t  POS SYSTEM");
            receipt.AppendLine("\t\tOFFICIAL RECEIPT");
            receipt.AppendLine("==================================");
            receipt.AppendLine($"Date/Time: {DateTime.Now}");
            receipt.AppendLine("----------------------------------------");
            receipt.AppendLine(string.Format("{0,-18} {1,4} {2,14}", "Item", "Qty", "Amount"));
            receipt.AppendLine("----------------------------------------\n");

            foreach (ListViewItem item in lvCart.Items)
            {
                string name = item.SubItems[1].Text;       // Item Name
                string qty = item.SubItems[3].Text;        // Quantity
                string amount = item.SubItems[4].Text;     // Total Amount

                if (name.Length > 18) name = name.Substring(0, 15) + "...";

                receipt.AppendLine(string.Format("{0,-18} {1,4} {2,14}", name, qty, amount));
            }

            receipt.AppendLine("----------------------------------------\n");
            receipt.AppendLine($"SubTotal: \t".PadRight(24) + txtSubTotal.Text);
            receipt.AppendLine($"Tax (10%): \t".PadRight(24) + txtTax.Text);
            receipt.AppendLine($"TOTAL : \t\t".PadRight(24) + txtTotal.Text);
            receipt.AppendLine($"Cash Given\t".PadRight(24) + cashGiven.ToString("C2"));
            receipt.AppendLine($"Change: \t\t".PadRight(24) + txtChange.Text);
            receipt.AppendLine("\n================================");
            receipt.AppendLine("\t\tTHANK YOU");
            receipt.AppendLine("================================");

            // 2. SHOW YOUR ORIGINAL MESSAGEBOX PREVIEW FIRST
            MessageBox.Show(receipt.ToString(), "POS Receipt Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Create the Print Document object
            PrintDocument pd = new PrintDocument();

            // SPECIFY YOUR PRINTER NAME HERE
            pd.PrinterSettings.PrinterName = "BIXOLON SRP-E300"; // Replace with your exact printer name if different

            // Check if the printer exists before trying to print
            if (!pd.PrinterSettings.IsValid)
            {
                MessageBox.Show("The Bixolon printer ('BIXOLON SRP-E300') could not be found. Please check your printer connection.", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Hook up the event that handles what text gets printed
            pd.PrintPage += new PrintPageEventHandler(PrintReceiptPage);

            try
            {
                // Send to printer
                pd.Print();
                MessageBox.Show("Receipt printed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }
        private void PrintReceiptPage(object sender, PrintPageEventArgs e)
        {
            // Receipt styling (Monospaced font keeps columns straight)
            Font printFont = new Font("Courier New", 9, FontStyle.Regular);
            Font boldFont = new Font("Courier New", 10, FontStyle.Bold);
            Brush brush = Brushes.Black;

            float leftMargin = 10; // Left padding on thermal paper
            float topMargin = 10;
            float yPos = topMargin;

            // Helper function to draw text lines sequentially
            Action<string, Font> drawLine = (text, fontToUse) =>
            {
                e.Graphics.DrawString(text, fontToUse, brush, leftMargin, yPos);
                yPos += 15; // Move down line by line (15 pixels per line)
            };

            // --- RECEIPT HEADER ---
            drawLine("==================================", boldFont);
            drawLine("           POS SYSTEM", boldFont);
            drawLine("         OFFICIAL RECEIPT", boldFont);
            drawLine("==================================", boldFont);
            drawLine($"Date/Time: {DateTime.Now}", printFont);
            drawLine("----------------------------------", printFont);
            drawLine(string.Format("{0,-16} {1,3} {2,11}", "Item", "Qty", "Amount"), boldFont);
            drawLine("----------------------------------", printFont);

            // --- CART ITEMS ---
            foreach (ListViewItem item in lvCart.Items)
            {
                string name = item.SubItems[1].Text;       // Item Name
                string qty = item.SubItems[3].Text;        // Quantity
                string amount = item.SubItems[4].Text;     // Total Amount

                if (name.Length > 16) name = name.Substring(0, 13) + "...";

                drawLine(string.Format("{0,-16} {1,3} {2,11}", name, qty, amount), printFont);
            }

            // --- TOTALS & FOOTER ---
            decimal cashGiven = 0;
            decimal.TryParse(txtCost.Text.Replace("$", "").Replace(",", "").Trim(), out cashGiven);

            drawLine("----------------------------------", printFont);
            drawLine($"SubTotal:        {txtSubTotal.Text}", printFont);
            drawLine($"Tax (10%):       {txtTax.Text}", printFont);
            drawLine($"TOTAL:           {txtTotal.Text}", boldFont);
            drawLine($"Cash Given:      {cashGiven.ToString("C2")}", printFont);
            drawLine($"Change:          {txtChange.Text}", printFont);
            drawLine("================================--", boldFont);
            drawLine("          THANK YOU!", boldFont);
            drawLine("================================--", boldFont);
        }

        private void lvproduct_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (lvproduct.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvproduct.SelectedItems[0];

                txtProductID.Text = selectedItem.Text;
                txtProductName.Text = selectedItem.SubItems[1].Text;
                cmbCategory.Text = selectedItem.SubItems[2].Text;
                txtPrice.Text = selectedItem.SubItems[3].Text.Replace("$", "").Replace(",", "").Trim();

                txtStock.Text = selectedItem.SubItems[4].Text;

                string query = "SELECT ImagePath FROM Products WHERE ProductID = @ID";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", txtProductID.Text);

                        try
                        {
                            conn.Open();
                            object result = cmd.ExecuteScalar();

                            if (result != DBNull.Value && result != null)
                            {
                                string imagePath = result.ToString();
                                selectedImagePath = imagePath;

                                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                                {
                                    if (pictureBoxProduct.Image != null)
                                    {
                                        pictureBoxProduct.Image.Dispose();
                                    }

                                    using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                                    {
                                        pictureBoxProduct.Image = Image.FromStream(stream);
                                    }
                                    pictureBoxProduct.SizeMode = PictureBoxSizeMode.StretchImage;

                                    if (lblFileName != null)
                                        lblFileName.Text = Path.GetFileName(imagePath);
                                }
                                else
                                {
                                    pictureBoxProduct.Image = null;
                                    if (lblFileName != null) lblFileName.Text = "No image found";
                                }
                            }
                            else
                            {
                                pictureBoxProduct.Image = null;
                                selectedImagePath = "";
                                if (lblFileName != null) lblFileName.Text = "No image attached";
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error loading image: " + ex.Message);
                        }
                    }
                }
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (UserSession.Role != "Admin")
            {
                lvproduct.Hide();
                panelProduct.Hide();
                MessageBox.Show("Only administrator can open this", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                lvproduct.Hide();
                panelProduct.Hide();
                lvCart.Hide();
                lvProducts.Hide();
                PanelOrder.Hide();
                panelRegister.Show();
            }
        }
     

        private void FormOrder_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void button11_Click_1(object sender, EventArgs e)
        {
            if (lvCart.Items.Count == 0)
            {
                MessageBox.Show("There is no order to print.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal cashGiven = 0;
            decimal.TryParse(txtCost.Text.Replace("$", "").Replace(",", "").Trim(), out cashGiven);

            StringBuilder receipt = new StringBuilder();
            receipt.AppendLine("==================================");
            receipt.AppendLine("\t\t  POS SYSTEM");
            receipt.AppendLine("\t\tOFFICIAL RECEIPT");
            receipt.AppendLine("==================================");
            receipt.AppendLine($"Date/Time: {DateTime.Now}");
            receipt.AppendLine("----------------------------------------");
            receipt.AppendLine(string.Format("{0,-18} {1,4} {2,14}", "Item", "Qty", "Amount"));
            receipt.AppendLine("----------------------------------------\n");

            foreach (ListViewItem item in lvCart.Items)
            {
                string name = item.SubItems[1].Text;
                string qty = item.SubItems[3].Text;
                string amount = item.SubItems[4].Text;

                if (name.Length > 18) name = name.Substring(0, 15) + "...";

                receipt.AppendLine(string.Format("{0,-18} {1,4} {2,14}", name, qty, amount));
            }

            receipt.AppendLine("----------------------------------------\n");
            receipt.AppendLine($"SubTotal: \t".PadRight(24) + txtSubTotal.Text);
            receipt.AppendLine($"Tax (10%): \t".PadRight(24) + txtTax.Text);
            receipt.AppendLine($"TOTAL : \t\t".PadRight(24) + txtTotal.Text);
            receipt.AppendLine($"Cash Given\t".PadRight(24) + cashGiven.ToString("C2"));
            receipt.AppendLine($"Change: \t\t".PadRight(24) + txtChange.Text);
            receipt.AppendLine("\n================================");
            receipt.AppendLine("\t\tTHANK YOU");
            receipt.AppendLine("================================");

            MessageBox.Show(receipt.ToString(), "POS Receipt Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void btnReset_Click(object sender, EventArgs e)

        {

            txtProductID.Clear();

            txtProductName.Clear();

            txtPrice.Clear();

            txtStock.Clear();
            txtChange.Clear();
            txtCost.Clear();
            txtTotal.Clear();
            txtTax.Clear();
            txtSubTotal.Clear();

            cmbCategory.SelectedIndex = -1;

            txtProductID.Focus();
            
            lvCart.Items.Clear();
            

        }

        private void SyncProductsToList()
        {
            lvProducts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvProducts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            lvProducts.Items.Clear();

            ImageList orderImgList = new ImageList();
            orderImgList.ImageSize = new Size(64, 64);
            lvProducts.SmallImageList = orderImgList;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "SELECT ProductName, Price, Category, ImagePath FROM Products";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int imageIndex = 0;
                        while (reader.Read())
                        {
                            string name = reader["ProductName"].ToString();
                            decimal price = Convert.ToDecimal(reader["Price"]);
                            string category = reader["Category"].ToString();
                            string imgPath = reader["ImagePath"]?.ToString();

                            if (!string.IsNullOrEmpty(imgPath) && File.Exists(imgPath))
                            {
                                using (Image originalImage = Image.FromFile(imgPath))
                                {
                                    Bitmap b = new Bitmap(originalImage, new Size(64, 64));
                                    orderImgList.Images.Add(b);
                                }
                            }
                            else
                            {
                                Bitmap b = new Bitmap(64, 64);
                                orderImgList.Images.Add(b);
                            }

                            ListViewItem item = new ListViewItem("");
                            item.ImageIndex = imageIndex++;
                            item.SubItems.Add(name);
                            item.SubItems.Add(price.ToString("C2"));
                            item.SubItems.Add(category);

                            item.Tag = price;

                            lvProducts.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sync Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalculateTotals()
        {
            decimal subTotal = 0;

            foreach (ListViewItem item in lvCart.Items)
            {
                if (item.SubItems.Count > 4)
                {
                    string amountText = item.SubItems[4].Text.Replace("$", "").Replace(",", "").Trim();

                    if (decimal.TryParse(amountText, out decimal amount))
                    {
                        subTotal += amount;
                    }
                }
            }

            decimal tax = subTotal * 0.10m;
            decimal total = subTotal + tax;

            txtSubTotal.Text = subTotal.ToString("C2");
            txtTax.Text = tax.ToString("C2");
            txtTotal.Text = total.ToString("C2");
        }

        private void AddToCart(string itemName, decimal price)
        {
            bool itemExists = false;

            foreach (ListViewItem item in lvCart.Items)
            {
                if (item.SubItems[1].Text == itemName)
                {
                    int currentQty = Convert.ToInt32(item.SubItems[3].Text);
                    currentQty++;
                    item.SubItems[3].Text = currentQty.ToString();

                    decimal totalAmount = currentQty * price;
                    item.SubItems[4].Text = totalAmount.ToString("C2");

                    itemExists = true;
                    break;
                }
            }

            if (!itemExists)
            {
                int rowNum = lvCart.Items.Count + 1;
                ListViewItem newItem = new ListViewItem(rowNum.ToString());
                newItem.SubItems.Add(itemName);
                newItem.SubItems.Add(price.ToString("C2"));
                newItem.SubItems.Add("1");
                newItem.SubItems.Add(price.ToString("C2"));
                newItem.Tag = price;

                lvCart.Items.Add(newItem);
            }

            CalculateTotals();
        }

        private void lvProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvProducts.SelectedItems.Count > 0)
            {
                ListViewItem selected = lvProducts.SelectedItems[0];

                string itemName = selected.SubItems[1].Text;
                decimal price = Convert.ToDecimal(selected.Tag ?? 0);

                AddToCart(itemName, price);

                lvProducts.SelectedItems.Clear();
            }
        }

        private void lvproduct_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void btnClearCalc_Click(object sender, EventArgs e)
        {
            txtCost.Text = "";
        }

        private void button13_Click_1(object sender, EventArgs e)
        {
            Button btn = (Button)sender;

            if (btn.Text == "." && txtCost.Text.Contains("."))
            {
                return;
            }

            if ((txtCost.Text == "0" || string.IsNullOrEmpty(txtCost.Text)) && btn.Text != ".")
            {
                txtCost.Text = btn.Text;
            }
            else
            {
                txtCost.Text += btn.Text;
            }
        }

        private void btnPay_Click(object sender, EventArgs e)
        {
            string totalText = txtTotal.Text.Replace("$", "").Replace(",", "").Trim();
            if (string.IsNullOrEmpty(totalText) || !decimal.TryParse(totalText, out decimal totalAmount))
            {
                MessageBox.Show("There is no total amount to pay.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtCost.Text) || !decimal.TryParse(txtCost.Text, out decimal cashGiven))
            {
                MessageBox.Show("Please enter the cash amount given by the customer in the cost box.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cashGiven < totalAmount)
            {
                MessageBox.Show("The cash given is less than the total amount due!", "Insufficient Funds", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show("Payment successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            decimal change = cashGiven - totalAmount;

            txtChange.Text = change.ToString("C2");
        }

        private void lvUser_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            

        }
        private void loaduserfromdatabase()
        {
            lvUser.Items.Clear();
            string query = "SELECT username FROM login";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    try
                    {
                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        lvUser.Items.Clear();
                        while (reader.Read())
                        {
                            int rowNumber = 1; // Start counter at 1
                            while (reader.Read())
                            {
                                // 1. The first parameter is the row index number (1, 2, 3...) for your "No" column
                                ListViewItem item = new ListViewItem(rowNumber.ToString());
                               item.SubItems.Add(reader["username"].ToString());
                                lvUser.Items.Add(item);
                                rowNumber++;
                            }

                            
                        }
                    }
                    


                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading user data: " + ex.Message);
                    }
                }
            }
        }

        private void button11_Click_2(object sender, EventArgs e)
        {
            string query = "DELETE FROM login WHERE Username = @User";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@User", txtUsername.Text);
                    try
                    {
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("User deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtUsername.Clear();
                            txtPassword.Clear();
                        }
                        else
                        {
                            MessageBox.Show("User not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {

            FormLogin loginForm = new FormLogin();
            loginForm.Show();
            this.Hide(); 

        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            
            if (lvCart.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select an item from the cart to remove.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (ListViewItem selectedItem in lvCart.SelectedItems)
            {
                lvCart.Items.Remove(selectedItem);
            }

            for (int i = 0; i < lvCart.Items.Count; i++)
            {
                lvCart.Items[i].Text = (i + 1).ToString();
            }

            CalculateTotals();
        }
    }
}