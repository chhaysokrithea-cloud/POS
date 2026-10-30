using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static POS.FormOrder;

namespace POS
{
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
        }
        public string connectionString = "Data Source=RITHEA-CHHAYSOK\\DATASQL;Initial Catalog=Login;Integrated Security=True";

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
            txtpass.PasswordChar = checkBox1.Checked ? '\0' : '*';
        }
        public static class UserSession
        {
            public static string Role { get; set; } // Stores "Admin" or "Cashier"
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = txtuser.Text.Trim();
            string password = txtpass.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Hardcoded Admin check (Matches "admin" or "Admin" case-insensitively)
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin")
            {
                UserSession.Role = "Admin";
                MessageBox.Show("Login successful! Logged in as: Admin", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                FormOrder mainForm = new FormOrder();
                mainForm.Show();
                this.Hide();
                return; // Stop execution here so it doesn't run SQL check
            }

            // 2. SQL Check for all other users (Cashiers)
            string query = "SELECT COUNT(1) FROM login WHERE Username = @User AND Password = @Pass";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@User", username);
                    cmd.Parameters.AddWithValue("@Pass", password);

                    try
                    {
                        conn.Open();
                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count > 0)
                        {
                            // Found in SQL -> Log in as Cashier
                            UserSession.Role = "Cashier";
                            MessageBox.Show("Login successful! Logged in as: Cashier", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            FormOrder mainForm = new FormOrder();
                            mainForm.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("Login Failed! Invalid username or password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btncancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtpass.PasswordChar = '*';
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            FormOrder mainForm = new FormOrder();
            mainForm.Show();
            FormLogin loginForm = new FormLogin();
            this.Hide();
        }
    }
}
