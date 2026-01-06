using System;
using System.Drawing;
using System.Windows.Forms;

namespace MN_Barcode.WinForms
{
    public class ExpenseManagerForm : Form
    {
        public ExpenseManagerForm()
        {
            InitializeComponent();
            LoadMockData(); 
        }

        private void InitializeComponent()
        {
            this.Text = "Gider Yönetimi";
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(20);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450F)); 
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  
            mainLayout.RowCount = 1;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.Controls.Add(mainLayout);

            // Left
            Panel pnlLeftCard = new Panel();
            pnlLeftCard.Dock = DockStyle.Fill;
            pnlLeftCard.BackColor = Color.White;
            pnlLeftCard.Padding = new Padding(25);
            pnlLeftCard.Margin = new Padding(0, 0, 15, 0); 
            mainLayout.Controls.Add(pnlLeftCard, 0, 0);

            Label lblHeader = new Label();
            lblHeader.Text = "Yeni Gider Oluştur";
            lblHeader.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblHeader.ForeColor = Color.FromArgb(17, 24, 39);
            lblHeader.Dock = DockStyle.Top;
            lblHeader.Height = 40;
            pnlLeftCard.Controls.Add(lblHeader);

            Label lblSubHeader = new Label();
            lblSubHeader.Text = "Gider detaylarını aşağıya giriniz.";
            lblSubHeader.Font = new Font("Segoe UI", 10F);
            lblSubHeader.ForeColor = Color.FromArgb(107, 114, 128);
            lblSubHeader.Dock = DockStyle.Top;
            lblSubHeader.Height = 30;
            pnlLeftCard.Controls.Add(lblSubHeader);

            Panel pnlSep = new Panel { Dock = DockStyle.Top, Height = 20 }; 
            pnlLeftCard.Controls.Add(pnlSep);

            FlowLayoutPanel flowInputs = new FlowLayoutPanel();
            flowInputs.Dock = DockStyle.Fill;
            flowInputs.FlowDirection = FlowDirection.TopDown;
            flowInputs.WrapContents = false;
            flowInputs.AutoScroll = true;
            pnlLeftCard.Controls.Add(flowInputs);

            _dtExpenseDate = new DateTimePicker { Width = 380, Height = 35, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 11F) };
            flowInputs.Controls.Add(CreateStyledInputGroup("Tarih", _dtExpenseDate));

            _txtTitle = new TextBox { Width = 380, Font = new Font("Segoe UI", 11F), BorderStyle = BorderStyle.FixedSingle };
            flowInputs.Controls.Add(CreateStyledInputGroup("Başlık / Konu", _txtTitle));

            _numAmount = new NumericUpDown { Width = 380, Font = new Font("Segoe UI", 12F), Maximum = 999999, DecimalPlaces = 2, TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.FixedSingle };
            flowInputs.Controls.Add(CreateStyledInputGroup("Tutar (₺)", _numAmount));

            _txtSpender = new TextBox { Width = 380, Font = new Font("Segoe UI", 11F), Text = "Kasa", BorderStyle = BorderStyle.FixedSingle };
            flowInputs.Controls.Add(CreateStyledInputGroup("Harcayan Kişi", _txtSpender));

            _txtDesc = new TextBox { Width = 380, Height = 80, Multiline = true, Font = new Font("Segoe UI", 10F), BorderStyle = BorderStyle.FixedSingle };
            flowInputs.Controls.Add(CreateStyledInputGroup("Açıklama (Opsiyonel)", _txtDesc, 110));

            Button btnSave = new Button();
            btnSave.Text = "KAYDET";
            btnSave.Size = new Size(380, 50);
            btnSave.BackColor = Color.FromArgb(59, 130, 246);
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnSave.Cursor = Cursors.Hand;
            btnSave.Margin = new Padding(0, 25, 0, 0); 
            btnSave.Click += (s, e) => SaveMock();
            flowInputs.Controls.Add(btnSave);

            // Right
            Panel pnlRightCard = new Panel();
            pnlRightCard.Dock = DockStyle.Fill;
            pnlRightCard.BackColor = Color.White;
            pnlRightCard.Padding = new Padding(20);
            pnlRightCard.Margin = new Padding(0);
            mainLayout.Controls.Add(pnlRightCard, 1, 0);

            TableLayoutPanel rightLayout = new TableLayoutPanel();
            rightLayout.Dock = DockStyle.Fill;
            rightLayout.RowCount = 3;
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); 
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F)); 
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  
            pnlRightCard.Controls.Add(rightLayout);

            // Header
            Panel pnlListHeader = new Panel { Dock = DockStyle.Fill };
            rightLayout.Controls.Add(pnlListHeader, 0, 0);

            Label lblListTitle = new Label { Text = "Gider Listesi", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(17, 24, 39), AutoSize = true, Location = new Point(0, 10) };
            pnlListHeader.Controls.Add(lblListTitle);

            _lblTotal = new Label { Text = "Toplam: ₺0.00", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68), AutoSize = true, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight };
            pnlListHeader.Controls.Add(_lblTotal);

            // Filters
            FlowLayoutPanel pnlFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AlignContent = FlowLayoutPanelAlignment.Center };
            rightLayout.Controls.Add(pnlFilters, 0, 1);

            pnlFilters.Controls.Add(new Label { Text = "Başlangıç:", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = new Padding(0, 8, 5, 0) });
            _dtStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Font = new Font("Segoe UI", 10F), Value = DateTime.Now.AddDays(-30) };
            pnlFilters.Controls.Add(_dtStart);

            pnlFilters.Controls.Add(new Label { Text = "Bitiş:", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = new Padding(20, 8, 5, 0) });
            _dtEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Font = new Font("Segoe UI", 10F) };
            pnlFilters.Controls.Add(_dtEnd);

            Button btnFilter = new Button { Text = "FİLTRELE", BackColor = Color.FromArgb(17, 24, 39), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(100, 30), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(15, 2, 0, 0), Cursor = Cursors.Hand };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadMockData();
            pnlFilters.Controls.Add(btnFilter);

            // Grid
            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.None;
            _grid.RowHeadersVisible = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.ReadOnly = true;
            _grid.RowTemplate.Height = 45; 
            _grid.ColumnHeadersHeight = 50;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.EnableHeadersVisualStyles = false;
            
            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle 
            { 
                BackColor = Color.FromArgb(241, 245, 249), 
                ForeColor = Color.FromArgb(107, 114, 128), 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), 
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(241, 245, 249)
            };

            _grid.DefaultCellStyle = new DataGridViewCellStyle 
            { 
                Font = new Font("Segoe UI", 10F), 
                SelectionBackColor = Color.FromArgb(239, 246, 255), 
                SelectionForeColor = Color.FromArgb(17, 24, 39),
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.White
            };
            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(249, 250, 251) }; 
            _grid.GridColor = Color.FromArgb(229, 231, 235); 

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "TARİH", FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "KONU", FillWeight = 30 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "User", HeaderText = "HARCAYAN", FillWeight = 20 });
            
            var colAmt = new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "TUTAR", FillWeight = 20 };
            colAmt.DefaultCellStyle = new DataGridViewCellStyle { Format = "C2", ForeColor = Color.FromArgb(239, 68, 68), Font = new Font("Segoe UI", 10F, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleRight };
            colAmt.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns.Add(colAmt);
            
            var btnDel = new DataGridViewButtonColumn 
            { 
                Name = "Del", HeaderText = "", Text = "Sil", UseColumnTextForButtonValue = true, 
                FlatStyle = FlatStyle.Flat, FillWeight = 10 
            };
            btnDel.CellTemplate.Style.ForeColor = Color.FromArgb(239, 68, 68);
            btnDel.CellTemplate.Style.SelectionForeColor = Color.FromArgb(239, 68, 68);
            _grid.Columns.Add(btnDel);

            _grid.CellClick += (s, e) => { if(e.RowIndex >= 0 && e.ColumnIndex == _grid.Columns["Del"].Index) MessageBox.Show("Silme işlemi (Mock)", "Bilgi"); };

            rightLayout.Controls.Add(_grid, 0, 2);
        }

        private Panel CreateStyledInputGroup(string labelText, Control control, int height = 80)
        {
            Panel p = new Panel { Size = new Size(380, height), Margin = new Padding(0, 0, 0, 15) };
            Label lbl = new Label { Text = labelText, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(107, 114, 128), Location = new Point(0, 0), AutoSize = true };
            p.Controls.Add(lbl);
            control.Location = new Point(0, 25);
            p.Controls.Add(control);
            return p;
        }

        private void SaveMock()
        {
            if (string.IsNullOrWhiteSpace(_txtTitle.Text) || _numAmount.Value <= 0)
            {
                MessageBox.Show("Lütfen başlık ve tutar giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show("Kayıt Başarılı (Simülasyon)\n\n" + $"Başlık: {_txtTitle.Text}\n" + $"Tutar: {_numAmount.Value:C2}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _grid.Rows.Add(0, _dtExpenseDate.Value.ToShortDateString(), _txtTitle.Text, _txtSpender.Text, _numAmount.Value);
            _txtTitle.Clear();
            _numAmount.Value = 0;
            _txtDesc.Clear();
        }

        private void LoadMockData()
        {
            _grid.Rows.Clear();
            _grid.Rows.Add(1, DateTime.Now.ToShortDateString(), "Ofis Kirası", "Kasa", 5000.00);
            _grid.Rows.Add(2, DateTime.Now.AddDays(-1).ToShortDateString(), "Elektrik Faturası", "Kasa", 1250.50);
            _grid.Rows.Add(3, DateTime.Now.AddDays(-2).ToShortDateString(), "Mutfak Alışverişi", "Ahmet Y.", 450.00);
            _grid.Rows.Add(4, DateTime.Now.AddDays(-5).ToShortDateString(), "Kargo Ödemesi", "Kasa", 85.00);
            _lblTotal.Text = "Toplam: ₺6,785.50";
        }
        
        // Members
        private DataGridView _grid;
        private Label _lblTotal;
        private TextBox _txtTitle;
        private NumericUpDown _numAmount;
        private TextBox _txtDesc;
        private TextBox _txtSpender;
        private DateTimePicker _dtExpenseDate;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
    }
}
