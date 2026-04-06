using KMCEvent.Client.KMCEventService;
using System;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using EventRecord = KMCEvent.Client.KMCEventService.Blanket;

namespace KMCEvent.Client
{
    public partial class PublicEventsDashboard : Form
    {
        private readonly ApiSoapClient _client = new ApiSoapClient();

        public PublicEventsDashboard(int publicUserId)
        {
            InitializeComponent();
        }

        private void PublicEventsDashboard_Load(object sender, EventArgs e)
        {
            ApplyDashboardTheme();
            SetupDataGridViews();
            RefreshMyStock();
            PerformSearch();
        }

        private void ApplyDashboardTheme()
        {
            txtSearchQuery.BackColor = Color.White;
            txtSearchQuery.ForeColor = Color.FromArgb(30, 49, 63);

            StyleActionButton(btnSearch, Color.FromArgb(23, 124, 134));
            StyleActionButton(btnRefreshStock, Color.FromArgb(23, 124, 134));
            StyleActionButton(btnLogout, Color.FromArgb(222, 93, 79));
        }

        private void SetupDataGridViews()
        {
            dgvMyStock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvMyStock.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMyStock.MultiSelect = false;
            dgvMyStock.ReadOnly = true;
            ConfigureGrid(dgvMyStock);

            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.MultiSelect = false;
            dgvResults.ReadOnly = true;
            ConfigureGrid(dgvResults);
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

        private void RefreshMyStock()
        {
            try
            {
                var upcomingEvents = _client.Public_SearchEvents(string.Empty) ?? new EventRecord[0];
                BindEventsToGrid(dgvMyStock, upcomingEvents.OrderBy(x => x.EventDate).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading public events: " + ex.Message, "Service Error");
            }
        }

        private void PerformSearch()
        {
            try
            {
                var query = string.IsNullOrWhiteSpace(txtSearchQuery.Text) ? string.Empty : txtSearchQuery.Text.Trim();
                var searchResults = _client.Public_SearchEvents(query) ?? new EventRecord[0];
                BindEventsToGrid(dgvResults, searchResults.OrderBy(x => x.EventDate).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching events: " + ex.Message, "Service Error");
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            PerformSearch();
        }

        private void btnRefreshStock_Click(object sender, EventArgs e)
        {
            RefreshMyStock();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BindEventsToGrid(DataGridView grid, List<EventRecord> events)
        {
            grid.DataSource = null;
            grid.DataSource = events;

            ConfigureColumn(grid, "EventID", "Event ID", true);
            ConfigureColumn(grid, "Title", "Event Title", true);
            ConfigureColumn(grid, "EventType", "Type", true);
            ConfigureColumn(grid, "EventDate", "Date", true);
            ConfigureColumn(grid, "Venue", "Venue", true);
            ConfigureColumn(grid, "OrganizerName", "Organizer", true);
            ConfigureColumn(grid, "SeatsRemaining", "Seats Left", true);

            ConfigureColumn(grid, "Description", "Description", false);
            ConfigureColumn(grid, "TicketPrice", "Ticket Price", false);
            ConfigureColumn(grid, "Capacity", "Capacity", false);
            ConfigureColumn(grid, "CreatedByUserID", "CreatedBy", false);

            ConfigureColumn(grid, "BlanketID", "Legacy Event ID", false);
            ConfigureColumn(grid, "BlanketName", "Legacy Event Title", false);
            ConfigureColumn(grid, "Fabric", "Legacy Type", false);
            ConfigureColumn(grid, "DescriptionNotes", "Legacy Description", false);
            ConfigureColumn(grid, "BasePrice", "Legacy Ticket Price", false);
            ConfigureColumn(grid, "ExtensionData", "ExtensionData", false);
        }

        private static void ConfigureColumn(DataGridView grid, string columnName, string headerText, bool visible)
        {
            if (!grid.Columns.Contains(columnName))
            {
                return;
            }

            var column = grid.Columns[columnName];
            column.Visible = visible;
            if (visible)
            {
                column.HeaderText = headerText;
            }
        }
    }
}

