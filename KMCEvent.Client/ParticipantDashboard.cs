using KMCEvent.Client.KMCEventService;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using EventRecord = KMCEvent.Client.KMCEventService.Blanket;
using RegistrationRecord = KMCEvent.Client.KMCEventService.Inventory;

namespace KMCEvent.Client
{
    public partial class ParticipantDashboard : Form
    {
        private readonly ApiSoapClient _client = new ApiSoapClient();
        private readonly int _participantId;
        private const string AllTypesLabel = "All Types";

        public ParticipantDashboard(int participantId)
        {
            InitializeComponent();
            _participantId = participantId;
        }

        private void ParticipantDashboard_Load(object sender, EventArgs e)
        {
            ApplyDashboardTheme();
            dgvInventory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvInventory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvInventory.MultiSelect = false;
            dgvInventory.ReadOnly = true;
            numGiveQuantity.Minimum = 1;
            numGiveQuantity.Maximum = 10;
            numGiveQuantity.Value = 1;

            LoadEventTypesIntoComboBox();
            LoadEventResults();
        }

        private void ApplyDashboardTheme()
        {
            ConfigureGrid(dgvInventory);

            txtSearch.BackColor = Color.White;
            txtSearch.ForeColor = Color.FromArgb(30, 49, 63);

            cmbSellers.BackColor = Color.White;
            cmbSellers.ForeColor = Color.FromArgb(30, 49, 63);

            numGiveQuantity.BackColor = Color.White;
            numGiveQuantity.ForeColor = Color.FromArgb(30, 49, 63);

            StyleActionButton(btnSearch, Color.FromArgb(23, 124, 134));
            StyleActionButton(btnGiveStock, Color.FromArgb(23, 124, 134));
            StyleActionButton(btnLogout, Color.FromArgb(222, 93, 79));
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Color.FromArgb(223, 231, 238);
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(33, 51, 66);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 234, 244);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 52, 78);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(17, 64, 98);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Candara", 11.25F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Candara", 10.5F, FontStyle.Regular);
            grid.RowTemplate.Height = 30;
        }

        private static void StyleActionButton(Button button, Color color)
        {
            button.BackColor = color;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.08f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.1f);
            button.ForeColor = Color.White;
        }

        private void LoadEventResults()
        {
            try
            {
                string query = string.IsNullOrWhiteSpace(txtSearch.Text) ? string.Empty : txtSearch.Text.Trim();
                var eventResults = _client.Participant_SearchEvents(_participantId, query) ?? new RegistrationRecord[0];

                var selectedType = cmbSellers.SelectedItem as string;
                if (!string.IsNullOrWhiteSpace(selectedType) && !selectedType.Equals(AllTypesLabel, StringComparison.OrdinalIgnoreCase))
                {
                    eventResults = eventResults
                        .Where(x => string.Equals(x.EventType, selectedType, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                }

                dgvInventory.DataSource = null;
                dgvInventory.DataSource = eventResults.ToList();

                ConfigureColumn("EventID", "Event ID", true);
                ConfigureColumn("EventTitle", "Event Title", true);
                ConfigureColumn("EventType", "Type", true);
                ConfigureColumn("EventDate", "Date", true);
                ConfigureColumn("Venue", "Venue", true);
                ConfigureColumn("SeatCount", "Seats Remaining", true);

                ConfigureColumn("RegistrationID", "Registration ID", false);
                ConfigureColumn("ParticipantUserID", "Participant", false);
                ConfigureColumn("RegisteredOn", "Registered On", false);

                ConfigureColumn("InventoryID", "Legacy Registration ID", false);
                ConfigureColumn("ProductID", "Legacy Event ID", false);
                ConfigureColumn("BlanketName", "Legacy Event Title", false);
                ConfigureColumn("OwnerUserID", "Legacy Participant", false);
                ConfigureColumn("UnitsInStock", "Legacy Seats", false);
                ConfigureColumn("ExtensionData", "ExtensionData", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading events: " + ex.Message, "Service Error");
            }
        }

        private void LoadEventTypesIntoComboBox()
        {
            try
            {
                cmbSellers.DataSource = null;
                var events = _client.Public_SearchEvents(string.Empty) ?? new EventRecord[0];
                var typeFilters = events
                    .Select(e => e.EventType)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t)
                    .ToList();

                typeFilters.Insert(0, AllTypesLabel);

                cmbSellers.DataSource = typeFilters;
                cmbSellers.Enabled = true;
                btnGiveStock.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("A critical error occurred while loading event types.\n\nDetails: " + ex.Message, "Service Error");
                cmbSellers.Enabled = false;
                btnGiveStock.Enabled = false;
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                LoadEventResults();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching events: " + ex.Message, "Service Error");
            }
        }

        private void btnGiveStock_Click(object sender, EventArgs e)
        {
            if (dgvInventory.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an event to register.", "Selection Required");
                return;
            }

            var selectedEvent = dgvInventory.SelectedRows[0].DataBoundItem as RegistrationRecord;
            if (selectedEvent == null)
            {
                MessageBox.Show("Please select a valid event row.", "Selection Required");
                return;
            }

            int quantityToRegister = (int)numGiveQuantity.Value;

            if (quantityToRegister <= 0)
            {
                MessageBox.Show("Seats must be greater than zero.", "Invalid Quantity");
                return;
            }

            if (quantityToRegister > selectedEvent.SeatCount)
            {
                MessageBox.Show("Not enough seats remaining for this event.", "Insufficient Seats");
                return;
            }

            try
            {
                _client.Participant_Register(_participantId, selectedEvent.EventID, quantityToRegister);

                var myRegistrations = _client.Participant_GetMyRegistrations(_participantId) ?? new RegistrationRecord[0];
                MessageBox.Show(
                    string.Format("Successfully registered {0} seat(s) for '{1}'.\nYou now have {2} registration(s).",
                    quantityToRegister,
                    selectedEvent.EventTitle,
                    myRegistrations.Length),
                    "Registration Complete");

                LoadEventResults();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while registering: " + ex.Message, "Error");
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConfigureColumn(string columnName, string headerText, bool visible)
        {
            if (!dgvInventory.Columns.Contains(columnName))
            {
                return;
            }

            var column = dgvInventory.Columns[columnName];
            column.Visible = visible;
            if (visible)
            {
                column.HeaderText = headerText;
            }
        }

        private void numGiveQuantity_ValueChanged(object sender, EventArgs e) { }
        private void cmbSellers_SelectedIndexChanged(object sender, EventArgs e) { LoadEventResults(); }
        private void groupBox1_Enter(object sender, EventArgs e) { }
        private void dgvInventory_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void txtSearch_TextChanged(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
    }
}

