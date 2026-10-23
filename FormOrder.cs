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
                            item.SubItems.Add(reader["Category"].ToString());     // Index 2

                            if (decimal.TryParse(reader["Price"].ToString(), out decimal price))
                            {
                                item.SubItems.Add(price.ToString("C2"));          // Index 3: Price
                            }
                            else
                            {
                                item.SubItems.Add(reader["Price"].ToString());
                            }

                            item.SubItems.Add(reader["Stock"].ToString());        // Index 5: Stock
                            item.SubItems.Add(reader["Discount"].ToString());     // Index 6: Discount

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
            

            // Your existing load code...
            panelProduct.Hide();
            lvproduct.Hide();

            // Set up ListView columns ONCE when the form loads in the correct order:
            lvproduct.View = View.Details;
            lvproduct.FullRowSelect = true;
            lvproduct.GridLines = true;

            lvproduct.Columns.Clear(); // Clear any old definitions first
            lvproduct.Columns.Add("Product ID", 150);      // Index 0
            lvproduct.Columns.Add("Product Name", 150);   // Index 1
            lvproduct.Columns.Add("Category", 120);       // Index 2
            lvproduct.Columns.Add("Price", 80);           // Index 3
            lvproduct.Columns.Add("Stock", 80);           // Index 5
            lvproduct.Columns.Add("Discount", 80);        // Index 6

            // Load existing products from SQL database automatically!
            LoadProductsFromDatabase();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PanelOrder.Hide();
            if (UserSession.Role != "Admin")
            {
                lvproduct.Hide();
                panelProduct.Hide();
                MessageBox.Show("Only administrator can open this", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Stops here, won't open the form
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
            int qty;
            if (!decimal.TryParse(txtPrice.Text, out price))
            {
                MessageBox.Show("Price must be a number.");
                return;
            }
           

            // 2. Insert into SQL Database
            string query = "INSERT INTO Products (ProductID, ProductName, Category, Price, Stock, Discount) " +
                           "VALUES (@ID, @Name, @Category, @Price, @Stock, @Discount)";

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
            item.SubItems.Add(txtProductName.Text);                                  // Index 1: Product Name
            item.SubItems.Add(cmbCategory.Text);                                     // Index 2: Category
            item.SubItems.Add(price.ToString("C2"));                                 // Index 3: Price ($ format)
                                            
            item.SubItems.Add(string.IsNullOrEmpty(txtStock.Text) ? "0" : txtStock.Text);     // Index 5: Stock
            item.SubItems.Add("0");                                                  // Index 6: Order (or blank/default)
            item.SubItems.Add(string.IsNullOrEmpty(txtDiscount.Text) ? "0" : txtDiscount.Text); // Index 7: Discount

            lvproduct.Items.Add(item);



            // Clear fields for the next entry
            btnClear_Click(sender, e);
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
                lvproduct.Items.Remove(selected);
            string query = "DELETE FROM Products WHERE ProductID = @ID";
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
            int qty = 0;
            if (txtPrice.Text != "" && !decimal.TryParse(txtPrice.Text, out price))
            {
                MessageBox.Show("Price must be a number.");
                return;
            }
           

            // SQL Update Query (Removed [Order] = ... so it won't crash)
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


                    // Using your UnitInStock textbox for the Stock column
                    cmd.Parameters.AddWithValue("@Stock", string.IsNullOrEmpty(txtStock.Text) ? (object)DBNull.Value : txtStock.Text);

                    cmd.Parameters.AddWithValue("@Discount", string.IsNullOrEmpty(txtDiscount.Text) ? (object)DBNull.Value : txtDiscount.Text);
                    cmd.Parameters.AddWithValue("@ImagePath", string.IsNullOrEmpty(selectedImagePath) ? (object)DBNull.Value : selectedImagePath);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Product updated successfully!");
                        LoadProductsFromDatabase(); // Refresh listview
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
            lvproduct.Items.Clear(); // Clear the current list when searching

            string query = "SELECT ProductID, ProductName, Category, Price, Quantity, Stock, Discount, ImagePath " +
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

                        while (reader.Read())
                        {
                            ListViewItem item = new ListViewItem(reader["ProductID"].ToString());
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
                            item.SubItems.Add(reader["Discount"].ToString());

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





        }

        private void button8_Click(object sender, EventArgs e)
        {

        }


        string selectedImagePath = "";


        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                selectedImagePath = ofd.FileName; // <-- This saves the path temporarily
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
                return; // Stops here, won't open the form
            }
            else
            {
                //register panel
            }



        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }



        private void lvproduct_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (lvproduct.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvproduct.SelectedItems[0];

                // Fill textboxes with the selected row's data
                txtProductID.Text = selectedItem.Text;                    // Index 0: Product ID
                txtProductName.Text = selectedItem.SubItems[1].Text;      // Index 1: Product Name
                cmbCategory.Text = selectedItem.SubItems[2].Text;         // Index 2: Category
                txtPrice.Text = selectedItem.SubItems[3].Text.Replace("$", "").Replace(",", "").Trim(); // Index 3: Price
                
                txtStock.Text = selectedItem.SubItems[4].Text;            // Index 5: Stock
                txtDiscount.Text = selectedItem.SubItems[5].Text;         // Index 6: Discount

                // Fetch and load the image path from SQL for this product
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

        private void button19_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized && this.FormBorderStyle == FormBorderStyle.None)
            {
                // Switch back to Normal mode
                this.FormBorderStyle = FormBorderStyle.Sizable; // Or FormBorderStyle.FixedSingle (whatever your original style was)
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                // Switch to Full Screen mode
                this.FormBorderStyle = FormBorderStyle.None; // Removes borders and title bar
                this.WindowState = FormWindowState.Maximized; // Expands to fill the whole screen
            }
        }

        private void FormOrder_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}

       