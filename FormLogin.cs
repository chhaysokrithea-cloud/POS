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

            // 1. Check your special Admin rule first
            if (username == "admin" && password == "admin")
            {
                UserSession.Role = "Admin";
                MessageBox.Show("Login successful! Logged in as: Admin");

                FormOrder mainForm = new FormOrder();
                mainForm.Show();
                this.Hide();
                return; // Stop here, login is complete
            }

            // 2. Check your SQL 'login' table for cashiers (like rithea / 123)
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
                            // User found in SQL login table -> Log in as Cashier
                            UserSession.Role = "Cashier";
                            MessageBox.Show("Login successful! Logged in as: Cashier");

                            FormOrder mainForm = new FormOrder();
                            mainForm.Show();
                            this.Hide();
                        }
                        else
                        {
                            // Wrong username or password -> Fail login and DO NOT open form
                            MessageBox.Show("Login Failed! Invalid username or password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Database Error: " + ex.Message);
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
    }
}
