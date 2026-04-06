using KMCEvent.Client.KMCEventService;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace KMCEvent.Client
{
    public partial class LoginWindow : Form
    {
        public LoginWindow()
        {
            InitializeComponent();
            ApplyLoginTheme();
        }

        private void ApplyLoginTheme()
        {
            txtUser.BackColor = Color.White;
            txtPass.BackColor = Color.White;
            txtUser.ForeColor = Color.FromArgb(30, 49, 63);
            txtPass.ForeColor = Color.FromArgb(30, 49, 63);

            btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 118, 104);
            btnLogin.FlatAppearance.MouseDownBackColor = Color.FromArgb(194, 75, 64);
            ActiveControl = txtUser;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
            {
                MessageBox.Show("Username and password are required.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var client = new ApiSoapClient();
            try
            {
                User authenticatedUser = client.Login(txtUser.Text.Trim(), txtPass.Text.Trim());

                if (authenticatedUser != null)
                {
                    this.Hide();
                    OpenDashboardForRole(authenticatedUser.Role, authenticatedUser.UserID);
                }
                else
                {
                    MessageBox.Show("Invalid credentials. Please try again.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to the service. Please ensure the backend is running.\n\n" + ex.Message, "Connection Error");
            }
        }

        private void OpenDashboardForRole(string role, int userId)
        {
            Form dashboard = null;
            string normalizedRole = (role ?? string.Empty).Trim().ToLowerInvariant();
            string welcomeRole = string.Empty;

            switch (normalizedRole)
            {
                case "organizer":
                    dashboard = new OrganizerDashboard(userId);
                    welcomeRole = "Organizer";
                    break;
                case "participant":
                    dashboard = new ParticipantDashboard(userId);
                    welcomeRole = "Participant";
                    break;
                case "public":
                    dashboard = new PublicEventsDashboard(userId);
                    welcomeRole = "Public User";
                    break;
            }

            if (dashboard != null)
            {
                MessageBox.Show(string.Format("Welcome {0}! (User ID: {1})", welcomeRole, userId), "KMC Events");
                dashboard.FormClosed += (s, args) => this.Show();
                dashboard.Show();
            }
            else
            {
                MessageBox.Show("Your user role is not supported by this client.", "Access Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Show();
            }
        }

        private void txtPass_TextChanged(object sender, EventArgs e) { }
        private void txtUser_TextChanged(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
    }
}

