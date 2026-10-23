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
            lvproduct.Items.Clear(); // Clear the list first to avoid duplicates

            string query = "SELECT ProductID, ProductName, Category, Price, Quantity, Stock, [Order], Discount FROM Products";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    try
                    {
                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            // Create a ListViewItem for each row found in SQL
                            ListViewItem item = new ListViewItem(reader["ProductID"].ToString()); // Index 0
                            item.SubItems.Add(reader["ProductName"].ToString());                  // Index 1
                            item.SubItems.Add(reader["Category"].ToString());                     // Index 2

                            // Format price with commas/decimals
                            if (decimal.TryParse(reader["Price"].ToString(), out decimal price))
                            {
                                item.SubItems.Add(price.ToString("N2"));                          // Index 3
                            }
                            else
                            {
                                item.SubItems.Add(reader["Price"].ToString());
                            }

                            item.SubItems.Add(reader["Quantity"].ToString());                   // Index 4
                            item.SubItems.Add(reader["Stock"].ToString());                      // Index 5
                            item.SubItems.Add(reader["Order"].ToString());                      // Index 6
                            item.SubItems.Add(reader["Discount"].ToString());                   // Index 7

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
            panelProduct.Hide();
            lvproduct.Hide();

            // Set up ListView columns ONCE when the form loads
            lvproduct.View = View.Details;
            lvproduct.FullRowSelect = true;
            lvproduct.GridLines = true;

            lvproduct.Columns.Add("Product ID", 90);      // Index 0
            lvproduct.Columns.Add("Product Name", 150);   // Index 1
            lvproduct.Columns.Add("Category", 110);       // Index 2
            lvproduct.Columns.Add("Price", 80);           // Index 3
            lvproduct.Columns.Add("Quantity", 80);        // Index 4
            lvproduct.Columns.Add("Stock", 80);           // Index 5
            lvproduct.Columns.Add("Order", 80);           // Index 6
            lvproduct.Columns.Add("Discount", 80);        // Index 7

            // Load existing products from SQL database automatically!
            LoadProductsFromDatabase();
        }

        private void button1_Click(object sender, EventArgs e)
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
                panelProduct.Show();
                lvproduct.Show();
                
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            panelProduct.Hide();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
           
            if (txtProductID.Text.Trim() == "" || txtProductName.Text.Trim() == "" || txtPrice.Text.Trim() == "" ||
                txtQuantity.Text.Trim() == "" || cmbCategory.Text.Trim() == "")
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
            if (!int.TryParse(txtQuantity.Text, out qty))
            {
                MessageBox.Show("Quantity must be a whole number.");
                return;
            }

            // 2. Insert into SQL Database
            string query = "INSERT INTO Products (ProductID, ProductName, Category, Price, Quantity, Stock, [Order], Discount) " +
                           "VALUES (@ID, @Name, @Category, @Price, @Qty, @Stock, @Order, @Discount)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", txtProductID.Text);
                    cmd.Parameters.AddWithValue("@Name", txtProductName.Text);
                    cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Qty", qty);
                    cmd.Parameters.AddWithValue("@Stock", string.IsNullOrEmpty(txtStock.Text) ? 0 : Convert.ToInt32(txtStock.Text));
                    cmd.Parameters.AddWithValue("@Order", string.IsNullOrEmpty(txtOrder.Text) ? 0 : Convert.ToInt32(txtOrder.Text));
                    cmd.Parameters.AddWithValue("@Discount", string.IsNullOrEmpty(txtDiscount.Text) ? 0 : Convert.ToDecimal(txtDiscount.Text));

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

            // 3. (Optional) Also add it to your ListView so you can see it right away
            ListViewItem item = new ListViewItem(txtProductID.Text); // Index 0
            item.SubItems.Add(txtProductName.Text);                  // Index 1
            item.SubItems.Add(cmbCategory.Text);                     // Index 2
            item.SubItems.Add(price.ToString("N2"));                 // Index 3
            item.SubItems.Add(qty.ToString());                       // Index 4
            item.SubItems.Add(txtStock.Text);                        // Index 5
            item.SubItems.Add(txtOrder.Text);                        // Index 6
            item.SubItems.Add(txtDiscount.Text);                     // Index 7

            lvproduct.Items.Add(item);

            // Clear fields for the next entry
            btnClear_Click(sender, e);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtProductID.Clear();
            txtProductName.Clear();
            txtPrice.Clear();
            txtQuantity.Clear();
            txtOrder.Clear();
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
            if (txtQuantity.Text != "" && !int.TryParse(txtQuantity.Text, out qty))
            {
                MessageBox.Show("Quantity must be a whole number.");
                return;
            }

            // 3. Update the SQL Database
            string query = "UPDATE Products SET " +
                           "ProductName = ISNULL(NULLIF(@Name, ''), ProductName), " +
                           "Category = ISNULL(NULLIF(@Category, ''), Category), " +
                           "Price = CASE WHEN @Price = 0 THEN Price ELSE @Price END, " +
                           "Quantity = CASE WHEN @Qty = 0 THEN Quantity ELSE @Qty END, " +
                           "Stock = ISNULL(NULLIF(@Stock, ''), Stock), " +
                           "[Order] = ISNULL(NULLIF(@Order, ''), [Order]), " +
                           "Discount = ISNULL(NULLIF(@Discount, ''), Discount) " +
                           "WHERE ProductID = @ID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", productId);
                    cmd.Parameters.AddWithValue("@Name", txtProductName.Text);
                    cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Qty", qty);
                    cmd.Parameters.AddWithValue("@Stock", string.IsNullOrEmpty(txtStock.Text) ? (object)DBNull.Value : txtStock.Text);
                    cmd.Parameters.AddWithValue("@Order", string.IsNullOrEmpty(txtOrder.Text) ? (object)DBNull.Value : txtOrder.Text);
                    cmd.Parameters.AddWithValue("@Discount", string.IsNullOrEmpty(txtDiscount.Text) ? (object)DBNull.Value : txtDiscount.Text);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database Error: " + ex.Message);
                        return;
                    }
                }
            }

      
            if (txtProductID.Text != "") item.Text = txtProductID.Text;                         // Index 0: Product ID
            if (txtProductName.Text != "") item.SubItems[1].Text = txtProductName.Text;         // Index 1: Product Name
            if (cmbCategory.Text != "") item.SubItems[2].Text = cmbCategory.Text;               // Index 2: Category
            if (txtPrice.Text != "") item.SubItems[3].Text = price.ToString("N2");              // Index 3: Price
            if (txtQuantity.Text != "") item.SubItems[4].Text = qty.ToString();                 // Index 4: Quantity
            if (txtStock.Text != "") item.SubItems[5].Text = txtStock.Text;                     // Index 5: Stock
            if (txtOrder.Text != "") item.SubItems[6].Text = txtOrder.Text;                     // Index 6: Order
            if (txtDiscount.Text != "") item.SubItems[7].Text = txtDiscount.Text;               // Index 7: Discount

            MessageBox.Show("Updated successfully in database and list!");

            
            btnClear_Click(sender, e);
        
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            lvproduct.Items.Clear(); // Clear the current list

            
            string query = "SELECT ProductID, ProductName, Category, Price, Quantity, Stock, [Order], Discount " +
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
                                item.SubItems.Add(price.ToString("N2"));
                            }
                            else
                            {
                                item.SubItems.Add(reader["Price"].ToString());
                            }

                            item.SubItems.Add(reader["Quantity"].ToString());
                            item.SubItems.Add(reader["Stock"].ToString());
                            item.SubItems.Add(reader["Order"].ToString());
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
                selectedImagePath = ofd.FileName;
                pictureBoxProduct.Image = Image.FromFile(selectedImagePath);
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
    }
    
}