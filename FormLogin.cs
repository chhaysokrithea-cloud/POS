using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace POS
{
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
            txtpass.PasswordChar = checkBox1.Checked ? '\0' : '*';
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            SqlConnection con = new SqlConnection("Data Source=RITHEA-CHHAYSOK\\DATASQL;Initial Catalog=Login;Integrated Security=True;TrustServerCertificate=True");
            con.Open();
            string query = "SELECT COUNT (*) FROM login WHERE username=@username AND password=@password";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@username", txtuser.Text);
            cmd.Parameters.AddWithValue("@password", txtpass.Text);
            int count = (int)cmd.ExecuteScalar();
            con.Close();
            if (count > 0)
            {
                MessageBox.Show("Login successful!", "info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FormOrder formOrder = new FormOrder();
                formOrder.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Login failed!");
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
