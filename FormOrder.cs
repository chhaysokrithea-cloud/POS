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

namespace POS
{
    public partial class FormOrder : Form
    {

        private string connectionString = @"Data Source=RITHEA-CHHAYSOK\DATASQL;Initial Catalog=Login;Integrated Security=True";
        public FormOrder()
        {
            InitializeComponent();
        }
        private void LoadProductsFromDatabase()
        {
            ImageList imgList = new ImageList();
            lvproduct.Items.Clear();
            imgList.Images.Clear();

            imgList.ImageSize = new Size(64, 64);
            lvproduct.SmallImageList = imgList;

            string query = "SELECT ProductID, ProductName, Category, Price, Quantity, Stock, Discount, ImagePath FROM Products";

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

                            item.SubItems.Add(reader["ProductName"].ToString());  // Index 1
                            item.SubItems.Add(reader["Category"].ToString());      // Index 2

                            if (decimal.TryParse(reader["Price"].ToString(), out decimal price))
                            {
                                item.SubItems.Add(price.ToString("C2"));          // Index 3: Price
                            }
                            else
                            {
                                item.SubItems.Add(reader["Price"].ToString());
                            }
                            if (decimal.TryParse(reader["Discount"].ToString(), out decimal discount))
                            {
                                item.SubItems.Add(discount.ToString("P2")); // Index 5: Discount
                            }
                            else
                            {
                                item.SubItems.Add(reader["Discount"].ToString());
                            }

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

            lvproduct.Columns.Clear(); // Clear any old definitions first
            lvproduct.Columns.Add("Product ID", 150);      // Index 0
            lvproduct.Columns.Add("Product Name", 150);   // Index 1
            lvproduct.Columns.Add("Category", 120);        // Index 2
            lvproduct.Columns.Add("Price", 80);            // Index 3
            lvproduct.Columns.Add("Stock", 80);            // Index 4
            lvproduct.Columns.Add("Discount", 80);         // Index 5


            lvProducts.View = View.Details;
            lvProducts.FullRowSelect = true;
            lvProducts.GridLines = true;
            lvProducts.Columns.Clear(); // Clear any old definitions first
            lvProducts.Columns.Add("Image", 100);
            lvProducts.Columns.Add("Name", 150);
            lvProducts.Columns.Add("Price", 80);
            lvProducts.Columns.Add("Category", 120);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PanelOrder.Hide();
            if (UserSession.Role != "Admin")
            {
                lvproduct.Hide();
                panelProduct.Hide();
                MessageBox.Show("Only administrator can open this", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else
            {
                panelProduct.Show();
                lvproduct.Show();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (txtProductID.Text.Trim() == "" || txtProductName.Text.Trim() == "" || txtPrice.Text.Trim() == "" ||
               txtDiscount.Text.Trim() == "" || txtStock.Text.Trim() == "" || cmbCategory.SelectedIndex == -1)
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

            // 2. Insert into SQL Database
            string query = "INSERT INTO Products (ProductID, ProductName, Category, Price, Stock, Discount, ImagePath) " +
                           "VALUES (@ID, @Name, @Category, @Price, @Stock, @Discount, @ImagePath)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", txtProductID.Text);
                    cmd.Parameters.AddWithValue("@Name", txtProductName.Text);
                    cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Stock", string.IsNullOrEmpty(txtStock.Text) ? 0 : Convert.ToInt32(txtStock.Text));
                    cmd.Parameters.AddWithValue("@Discount", string.IsNullOrEmpty(txtDiscount.Text) ? 0 : Convert.ToDecimal(txtDiscount.Text));
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

            ListViewItem item = new ListViewItem(txtProductID.Text);                  // Index 0: Product ID
            item.SubItems.Add(txtProductName.Text);                                    // Index 1: Product Name
            item.SubItems.Add(cmbCategory.Text);                                       // Index 2: Category
            item.SubItems.Add(price.ToString("C2"));                                   // Index 3: Price ($ format)
            item.SubItems.Add(string.IsNullOrEmpty(txtStock.Text) ? "0" : txtStock.Text);      // Index 4: Stock
            item.SubItems.Add(string.IsNullOrEmpty(txtDiscount.Text) ? "0" : txtDiscount.Text); // Index 5: Discount

            lvproduct.Items.Add(item);

            // Clear fields for the next entry
            btnClear_Click(sender, e);
            SyncProductsToList(); // Refresh order list view too
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtProductID.Clear();
            txtProductName.Clear();
            txtPrice.Clear();
            txtStock.Clear();
            cmbCategory.SelectedIndex = -1;
            txtDiscount.Clear();
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
                           "Discount = ISNULL(NULLIF(@Discount, ''), Discount), " +
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
                    cmd.Parameters.AddWithValue("@Discount", string.IsNullOrEmpty(txtDiscount.Text) ? (object)DBNull.Value : txtDiscount.Text);
                    cmd.Parameters.AddWithValue("@ImagePath", string.IsNullOrEmpty(selectedImagePath) ? (object)DBNull.Value : selectedImagePath);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Product updated successfully!");
                        LoadProductsFromDatabase(); // Refresh listview
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

            // Re-initialize ImageList for filtered items
            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(64, 64);
            lvproduct.SmallImageList = imgList;

            string query = "SELECT ProductID, ProductName, Category, Price, Stock, Discount, ImagePath " +
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
                            // 1. Load Image into ImageList
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

                            // 2. Create ListViewItem and bind ImageIndex
                            ListViewItem item = new ListViewItem(reader["ProductID"].ToString());
                            item.ImageIndex = imageIndex++; // Link image index

                            item.SubItems.Add(reader["ProductName"].ToString()); // Index 1
                            item.SubItems.Add(reader["Category"].ToString());    // Index 2

                            if (decimal.TryParse(reader["Price"].ToString(), out decimal price))
                            {
                                item.SubItems.Add(price.ToString("C2"));        // Index 3
                            }
                            else
                            {
                                item.SubItems.Add(reader["Price"].ToString());
                            }

                            item.SubItems.Add(reader["Stock"].ToString());        // Index 4

                            if (decimal.TryParse(reader["Discount"].ToString(), out decimal discount))
                            {
                                item.SubItems.Add(discount.ToString("P2"));    // Index 5
                            }
                            else
                            {
                                item.SubItems.Add(reader["Discount"].ToString());
                            }

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
            PanelOrder.Show();
            panelRegister.Hide();
        }

        string selectedImagePath = "";

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

            

        }
        

        private void ToggleDarkMode()
        {
            
        }

        // Helper method to recursively update colors on panels and child controls
        
        
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

        private void lvproduct_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (lvproduct.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvproduct.SelectedItems[0];

                txtProductID.Text = selectedItem.Text;                 // Index 0: Product ID
                txtProductName.Text = selectedItem.SubItems[1].Text;      // Index 1: Product Name
                cmbCategory.Text = selectedItem.SubItems[2].Text;          // Index 2: Category
                txtPrice.Text = selectedItem.SubItems[3].Text.Replace("$", "").Replace(",", "").Trim(); // Index 3: Price

                txtStock.Text = selectedItem.SubItems[4].Text;            // Index 4: Stock
                txtDiscount.Text = selectedItem.SubItems[5].Text;         // Index 5: Discount

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

       

        private void FormOrder_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (lvCart.Items.Count == 0)
            {
                MessageBox.Show("There is no order to print.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Safely parse cash given to format it nicely with currency
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

            MessageBox.Show(receipt.ToString(), "POS Receipt Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SyncProductsToList()

        {
            lvProducts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvProducts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            lvProducts.Items.Clear();

            // Create and attach ImageList to lvProducts so images show up on the order screen
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

                            // Load image into the list view's ImageList
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

                            // Create ListViewItem and link image index
                            ListViewItem item = new ListViewItem("");
                            item.ImageIndex = imageIndex++;
                            item.SubItems.Add(name);              // Name
                            item.SubItems.Add(price.ToString("C2"));  // Price
                            item.SubItems.Add(category);              // Category

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
                // Ensure the item has the Amount column (Index 4)
                if (item.SubItems.Count > 4)
                {
                    // Remove "$" and "," so C# can safely convert the string to a decimal
                    string amountText = item.SubItems[4].Text.Replace("$", "").Replace(",", "").Trim();

                    if (decimal.TryParse(amountText, out decimal amount))
                    {
                        subTotal += amount;
                    }
                }
            }

            decimal tax = subTotal * 0.10m; // 10% Tax rate
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
                    // Quantity is at Column index 3
                    int currentQty = Convert.ToInt32(item.SubItems[3].Text);
                    currentQty++;
                    item.SubItems[3].Text = currentQty.ToString();

                    // Amount is at Column index 4 (Qty * Unit Price)
                    decimal totalAmount = currentQty * price;
                    item.SubItems[4].Text = totalAmount.ToString("C2");

                    itemExists = true;
                    break;
                }
            }

            if (!itemExists)
            {
                int rowNum = lvCart.Items.Count + 1;
                ListViewItem newItem = new ListViewItem(rowNum.ToString()); // Column 0: No
                newItem.SubItems.Add(itemName);                             // Column 1: Items
                newItem.SubItems.Add(price.ToString("C2"));                 // Column 2: Price (Unit Price)
                newItem.SubItems.Add("1");                                  // Column 3: Qty (starts at 1)
                newItem.SubItems.Add(price.ToString("C2"));                 // Column 4: Amount (1 * Price)
                newItem.Tag = price;                                        // Store base unit price reference

                lvCart.Items.Add(newItem);
            }

            CalculateTotals();
        }

        private void lvProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvProducts.SelectedItems.Count > 0)
            {
                ListViewItem selected = lvProducts.SelectedItems[0];

                string itemName = selected.SubItems[1].Text; // Name column
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

            // Prevent adding more than one decimal point (.)
            if (btn.Text == "." && txtCost.Text.Contains("."))
            {
                return;
            }

            // If txtCost is empty or just "0", replace it (unless clicking "."), otherwise append
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

            // 2. Check if the customer entered the cash given (txtCost)
            if (string.IsNullOrEmpty(txtCost.Text) || !decimal.TryParse(txtCost.Text, out decimal cashGiven))
            {
                MessageBox.Show("Please enter the cash amount given by the customer in the cost box.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Check if the cash given is enough to cover the total
            if (cashGiven < totalAmount)
            {
                MessageBox.Show("The cash given is less than the total amount due!", "Insufficient Funds", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 4. Calculate Change (Cash Given - Total)
            decimal change = cashGiven - totalAmount;

            // 5. Display change in txtChange (formatted with dollar sign)
            txtChange.Text = change.ToString("C2");
            

            // Optional: Show success message or finalize order here
            MessageBox.Show("Payment successful!\nNow you can print for receipt.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void lvCart_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            lvCart.Items.Clear();
            txtSubTotal.Clear();
            txtTax.Clear();
            txtTotal.Clear();
            txtCost.Clear();
            txtChange.Clear();
            MessageBox.Show("Cart has been reset.", "Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRegister_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter both username and password.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            // 2. Check if username already exists in the 'login' table to avoid duplicates
            string checkQuery = "SELECT COUNT(*) FROM login WHERE Username = @Username";
            string insertQuery = "INSERT INTO login (Username, Password) VALUES (@Username, @Password)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Check existing user
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Username", username);
                        int userExists = (int)checkCmd.ExecuteScalar();

                        if (userExists > 0)
                        {
                            MessageBox.Show("This username is already taken. Choose another one.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    // Insert new user directly into the 'login' table
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@Username", username);
                        insertCmd.Parameters.AddWithValue("@Password", password);

                        insertCmd.ExecuteNonQuery();
                        MessageBox.Show("Registration successful! New user added to the login table.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Clear fields and close form
                        txtUsername.Clear();
                        txtPassword.Clear();
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}