using KMCEvent.Client.KMCEventService;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EventRecord = KMCEvent.Client.KMCEventService.Blanket;

namespace KMCEvent.Client
{
    public partial class OrganizerDashboard : Form
    {
        private readonly ApiSoapClient _client = new ApiSoapClient();
        private int _userId;

        public OrganizerDashboard(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        private void OrganizerDashboard_Load(object sender, EventArgs e)
        {
            ApplyDashboardTheme();
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.MultiSelect = false;
            dgvProducts.ReadOnly = true;
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            btnDelete.Enabled = false;

            LoadProductData();
            ClearInputFields();
        }

        private void ApplyDashboardTheme()
        {
            ConfigureGrid(dgvProducts);

            ConfigureInput(txtName);
            ConfigureInput(txtFabric);
            ConfigureInput(txtDesc);
            ConfigureInput(txtPrice);

            StyleActionButton(btnAdd, Color.FromArgb(23, 124, 134));
            StyleActionButton(btnUpdate, Color.FromArgb(37, 99, 166));
            StyleActionButton(btnDelete, Color.FromArgb(175, 79, 72));
            StyleActionButton(btnClear, Color.FromArgb(117, 128, 140));
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

        private static void ConfigureInput(TextBox textBox)
        {
            textBox.BackColor = Color.White;
            textBox.ForeColor = Color.FromArgb(30, 49, 63);
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

        private void ClearInputFields()
        {
            dgvProducts.ClearSelection();
            txtName.Clear();
            txtFabric.Clear();
            txtDesc.Clear();
            txtPrice.Clear();
            txtName.Focus();
        }

        private void LoadProductData()
        {
            try
            {
                var allEvents = _client.Organizer_GetMyEvents();
                var myEvents = (allEvents ?? new EventRecord[0])
                    .Where(x => x.CreatedByUserID == _userId)
                    .OrderBy(x => x.EventDate)
                    .ToList();

                BindEventsToGrid(myEvents);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching event data: " + ex.Message, "Service Error");
            }
        }

        private void dgvProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count > 0)
            {
                var selectedEvent = dgvProducts.SelectedRows[0].DataBoundItem as EventRecord;
                if (selectedEvent != null)
                {
                    txtName.Text = selectedEvent.Title;
                    txtFabric.Text = selectedEvent.EventType;
                    txtDesc.Text = selectedEvent.Venue;
                    txtPrice.Text = selectedEvent.EventDate == DateTime.MinValue
                        ? string.Empty
                        : selectedEvent.EventDate.ToString("yyyy-MM-dd");
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            EventRecord newEvent;
            if (!TryBuildEventPayload(false, out newEvent))
            {
                return;
            }

            try
            {
                _client.Organizer_CreateEvent(newEvent);
                MessageBox.Show("Event created successfully.", "KMC Events");
                LoadProductData();
                ClearInputFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating event: " + ex.Message, "Error");
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an event from the list to update.", "Selection Required");
                return;
            }

            EventRecord updatedEvent;
            if (!TryBuildEventPayload(true, out updatedEvent))
            {
                return;
            }

            try
            {
                var selectedEvent = dgvProducts.SelectedRows[0].DataBoundItem as EventRecord;
                if (selectedEvent == null)
                {
                    MessageBox.Show("Please select a valid event row.", "Selection Required");
                    return;
                }

                if (selectedEvent.CreatedByUserID != _userId)
                {
                    MessageBox.Show("You can only update events that you created.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _client.Organizer_UpdateEvent(updatedEvent);
                MessageBox.Show("Event updated successfully.", "KMC Events");
                LoadProductData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating event: " + ex.Message, "Error");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "For safe deletion with ownership checks, use the API method Organizer_DeleteEvent(eventId, organizerId).\n\n" +
                "This desktop screen currently supports create and update workflows.",
                "Use API Delete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearInputFields();
        }

        private void BindEventsToGrid(List<EventRecord> events)
        {
            dgvProducts.DataSource = null;
            dgvProducts.DataSource = events;

            ConfigureColumn("EventID", "Event ID", true);
            ConfigureColumn("Title", "Title", true);
            ConfigureColumn("EventType", "Type", true);
            ConfigureColumn("Venue", "Venue", true);
            ConfigureColumn("EventDate", "Date", true);
            ConfigureColumn("SeatsRemaining", "Seats Left", true);

            ConfigureColumn("Description", "Description", false);
            ConfigureColumn("TicketPrice", "Ticket Price", false);
            ConfigureColumn("Capacity", "Capacity", false);
            ConfigureColumn("CreatedByUserID", "Creator", false);
            ConfigureColumn("OrganizerName", "Organizer", false);

            ConfigureColumn("BlanketID", "Legacy Event ID", false);
            ConfigureColumn("BlanketName", "Legacy Title", false);
            ConfigureColumn("Fabric", "Legacy Type", false);
            ConfigureColumn("DescriptionNotes", "Legacy Description", false);
            ConfigureColumn("BasePrice", "Legacy Ticket Price", false);
            ConfigureColumn("ExtensionData", "ExtensionData", false);
        }

        private void ConfigureColumn(string columnName, string headerText, bool visible)
        {
            if (!dgvProducts.Columns.Contains(columnName))
            {
                return;
            }

            var column = dgvProducts.Columns[columnName];
            column.Visible = visible;
            if (visible)
            {
                column.HeaderText = headerText;
            }
        }

        private bool TryBuildEventPayload(bool requireSelectedRow, out EventRecord payload)
        {
            payload = null;

            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtFabric.Text) || string.IsNullOrWhiteSpace(txtDesc.Text))
            {
                MessageBox.Show("Please provide title, type, and venue.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            DateTime eventDate;
            if (!DateTime.TryParse(txtPrice.Text.Trim(), out eventDate))
            {
                MessageBox.Show("Enter a valid event date in format YYYY-MM-DD.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var eventId = 0;
            if (requireSelectedRow)
            {
                var selectedEvent = dgvProducts.SelectedRows[0].DataBoundItem as EventRecord;
                if (selectedEvent == null)
                {
                    MessageBox.Show("Please select an event to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                eventId = selectedEvent.EventID;
            }

            payload = new EventRecord
            {
                EventID = eventId,
                Title = txtName.Text.Trim(),
                EventType = txtFabric.Text.Trim(),
                Description = txtDesc.Text.Trim(),
                Venue = txtDesc.Text.Trim(),
                EventDate = eventDate.Date,
                TicketPrice = 0,
                Capacity = 100,
                CreatedByUserID = _userId
            };

            return true;
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label3_Click(object sender, EventArgs e) { }
        private void label5_Click(object sender, EventArgs e) { }
        private void txtName_TextChanged(object sender, EventArgs e) { }
        private void txtFabric_TextChanged(object sender, EventArgs e) { }
        private void txtDesc_TextChanged(object sender, EventArgs e) { }
        private void txtPrice_TextChanged(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }
        private void dgvProducts_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
    }
}

