using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class ExpenseManagerForm : Form, IEkranYenileme
    {
        /// <summary>Önbellekten tekrar açıldığında verileri tazeler (bkz. IEkranYenileme).</summary>
        public void EkraniYenile() => LoadData();

        private ExpenseService _expenseService;

        // Filtre
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private TextBox _txtSearchPerson;
        private Label _lblTotalExpense;
        private Label _lblExpenseCount;

        // Grid
        private DataGridView _grid;

        // Form alanları
        private TextBox _txtTitle;
        private NumericUpDown _numAmount;
        private TextBox _txtDescription;
        private TextBox _txtCreatedBy;
        private DateTimePicker _dtExpenseDate;

        public ExpenseManagerForm()
        {
            _expenseService = new ExpenseService();
            this.DoubleBuffered = true;
            InitUI();
            this.Shown += (s, e) => LoadData();
        }

        private void InitUI()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ═══════════════════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════════════════
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(45, 55, 72)
            };
            this.Controls.Add(header);

            Label title = new Label
            {
                Text = "💸 GİDER YÖNETİMİ",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 25),
                AutoSize = true
            };
            header.Controls.Add(title);

            // Tarih Seçimi
            _dtStart = new DateTimePicker
            {
                Location = new Point(280, 28),
                Format = DateTimePickerFormat.Short,
                Width = 110,
                Font = new Font("Segoe UI", 10)
            };
            _dtStart.Value = DateTime.Now.AddDays(-30);
            header.Controls.Add(_dtStart);

            Label lblArrow = new Label
            {
                Text = "➡",
                ForeColor = Color.White,
                Location = new Point(400, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 11)
            };
            header.Controls.Add(lblArrow);

            _dtEnd = new DateTimePicker
            {
                Location = new Point(430, 28),
                Format = DateTimePickerFormat.Short,
                Width = 110,
                Font = new Font("Segoe UI", 10)
            };
            _dtEnd.Value = DateTime.Now;
            header.Controls.Add(_dtEnd);

            // Kişi filtresi
            _txtSearchPerson = new TextBox
            {
                Location = new Point(560, 28),
                Size = new Size(100, 28),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Kişi..."
            };
            header.Controls.Add(_txtSearchPerson);

            Button btnFilter = new Button
            {
                Text = "Filtrele",
                Size = new Size(80, 30),
                Location = new Point(680, 27),
                BackColor = Color.FromArgb(66, 153, 225),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadData();
            header.Controls.Add(btnFilter);

            // Toplam
            _lblTotalExpense = new Label
            {
                Text = "Toplam: ₺0",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(236, 72, 153),
                Location = new Point(800, 25),
                AutoSize = true
            };
            header.Controls.Add(_lblTotalExpense);

            _lblExpenseCount = new Label
            {
                Text = "(0 kayıt)",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(160, 174, 192),
                Location = new Point(960, 28),
                AutoSize = true
            };
            header.Controls.Add(_lblExpenseCount);

            // ═══════════════════════════════════════════════════════════
            // İÇERİK - 2 SÜTUN
            // ═══════════════════════════════════════════════════════════
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            this.Controls.Add(content);
            content.BringToFront();

            // ═══════════════════════════════════════════════════════════
            // SOL PANEL - GİDER LİSTESİ
            // ═══════════════════════════════════════════════════════════
            Panel leftPanel = CreateCardPanel("📋 GİDER LİSTESİ");
            content.Controls.Add(leftPanel, 0, 0);

            Panel gridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 15, 15) };
            leftPanel.Controls.Add(gridContainer);
            gridContainer.BringToFront();

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            _grid.RowTemplate.Height = 50;
            _grid.ColumnHeadersHeight = 50;
            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            _grid.DefaultCellStyle.ForeColor = Color.FromArgb(45, 55, 72);
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(237, 242, 247);
            _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(45, 55, 72);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(113, 128, 150);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(237, 242, 247);
            _grid.EnableHeadersVisualStyles = false;
            _grid.GridColor = Color.FromArgb(226, 232, 240);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "TARİH", FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "BAŞLIK", FillWeight = 30 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy", HeaderText = "HARCAYAN", FillWeight = 20 });

            var colAmt = new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "TUTAR", FillWeight = 20 };
            colAmt.DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Color.FromArgb(190, 24, 93),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleRight
            };
            _grid.Columns.Add(colAmt);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "AÇIKLAMA", FillWeight = 30 });

            var btnDel = new DataGridViewButtonColumn
            {
                Name = "Del", HeaderText = "", Text = "Sil",
                UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, FillWeight = 10
            };
            btnDel.DefaultCellStyle.ForeColor = Color.FromArgb(239, 68, 68);
            _grid.Columns.Add(btnDel);

            _grid.ReadOnly = false;
            _grid.Columns["Del"].ReadOnly = false;
            foreach (DataGridViewColumn c in _grid.Columns)
                if (c.Name != "Del") c.ReadOnly = true;

            _grid.CellClick += Grid_CellClick;
            gridContainer.Controls.Add(_grid);

            // ═══════════════════════════════════════════════════════════
            // SAĞ PANEL - YENİ GİDER EKLE
            // ═══════════════════════════════════════════════════════════
            Panel rightPanel = CreateCardPanel("➕ YENİ GİDER EKLE");
            content.Controls.Add(rightPanel, 1, 0);

            Panel formContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 20),
                AutoScroll = true
            };
            rightPanel.Controls.Add(formContainer);
            formContainer.BringToFront();

            int y = 10;

            Label lblTitle = CreateLabel("Başlık *", ref y);
            formContainer.Controls.Add(lblTitle);
            _txtTitle = new TextBox { Location = new Point(0, y), Size = new Size(280, 32), Font = new Font("Segoe UI", 11), PlaceholderText = "Örn: Elektrik Faturası", BorderStyle = BorderStyle.FixedSingle };
            formContainer.Controls.Add(_txtTitle);
            y += 45;

            Label lblAmt = CreateLabel("Tutar (₺) *", ref y);
            formContainer.Controls.Add(lblAmt);
            _numAmount = new NumericUpDown { Location = new Point(0, y), Size = new Size(280, 32), Font = new Font("Segoe UI", 11), Maximum = 9999999, DecimalPlaces = 2, TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.FixedSingle };
            formContainer.Controls.Add(_numAmount);
            y += 45;

            Label lblBy = CreateLabel("Kim Harcadı *", ref y);
            formContainer.Controls.Add(lblBy);
            _txtCreatedBy = new TextBox { Location = new Point(0, y), Size = new Size(280, 32), Font = new Font("Segoe UI", 11), PlaceholderText = "Örn: Ahmet", BorderStyle = BorderStyle.FixedSingle };
            formContainer.Controls.Add(_txtCreatedBy);
            y += 45;

            Label lblDate = CreateLabel("Tarih", ref y);
            formContainer.Controls.Add(lblDate);
            _dtExpenseDate = new DateTimePicker { Location = new Point(0, y), Size = new Size(280, 32), Font = new Font("Segoe UI", 11), Format = DateTimePickerFormat.Short };
            _dtExpenseDate.Value = DateTime.Now;
            formContainer.Controls.Add(_dtExpenseDate);
            y += 45;

            Label lblDesc = CreateLabel("Açıklama", ref y);
            formContainer.Controls.Add(lblDesc);
            _txtDescription = new TextBox { Location = new Point(0, y), Size = new Size(280, 75), Font = new Font("Segoe UI", 10), Multiline = true, BorderStyle = BorderStyle.FixedSingle };
            formContainer.Controls.Add(_txtDescription);
            y += 90;

            Button btnSave = new Button
            {
                Text = "💾 GİDER EKLE",
                Location = new Point(0, y),
                Size = new Size(280, 50),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            formContainer.Controls.Add(btnSave);
        }

        private Label CreateLabel(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81)
            };
            y += 25;
            return lbl;
        }

        private Panel CreateCardPanel(string title)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(8) };

            Panel stripe = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Color.FromArgb(139, 92, 246) };
            panel.Controls.Add(stripe);

            Label lbl = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 45,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };
            panel.Controls.Add(lbl);

            return panel;
        }

        private void LoadData()
        {
            try
            {
                DateTime start = _dtStart.Value.Date;
                DateTime end = _dtEnd.Value.Date.AddDays(1).AddSeconds(-1);
                string person = _txtSearchPerson.Text?.Trim() ?? "";

                var expenses = _expenseService.GetExpenses(start, end);

                if (!string.IsNullOrEmpty(person))
                    expenses = expenses.Where(x => x.CreatedBy.ToLower().Contains(person.ToLower())).ToList();

                _grid.Rows.Clear();
                foreach (var e in expenses)
                {
                    _grid.Rows.Add(
                        e.Id,
                        e.ExpenseDate.ToString("dd.MM.yyyy"),
                        e.Title,
                        e.CreatedBy,
                        e.Amount.ToString("₺#,##0.00"),
                        e.Description
                    );
                }

                decimal total = expenses.Sum(x => x.Amount);
                _lblTotalExpense.Text = $"Toplam: {total:₺#,##0.00}";
                _lblExpenseCount.Text = $"({expenses.Count} kayıt)";
            }
            catch (Exception ex)
            {
                string err = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show("Veri yüklenirken hata: " + err, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtTitle.Text))
            {
                MessageBox.Show("Başlık alanı zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_numAmount.Value <= 0)
            {
                MessageBox.Show("Geçerli bir tutar giriniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtCreatedBy.Text))
            {
                MessageBox.Show("Harcayan kişi alanı zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var expense = new Expense
                {
                    Title = _txtTitle.Text.Trim(),
                    Amount = _numAmount.Value,
                    CreatedBy = _txtCreatedBy.Text.Trim(),
                    ExpenseDate = _dtExpenseDate.Value,
                    Description = _txtDescription.Text?.Trim() ?? ""
                };

                _expenseService.Add(expense);
                MessageBox.Show("Gider başarıyla eklendi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _txtTitle.Clear();
                _numAmount.Value = 0;
                _txtCreatedBy.Clear();
                _txtDescription.Clear();
                _dtExpenseDate.Value = DateTime.Now;

                LoadData();
            }
            catch (Exception ex)
            {
                string err = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show("Hata: " + err, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Del") return;

            int id = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["Id"].Value);
            string title = _grid.Rows[e.RowIndex].Cells["Title"].Value?.ToString() ?? "";

            if (MessageBox.Show($"'{title}' gideri silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    _expenseService.Delete(id);
                    LoadData();
                }
                catch (Exception ex)
                {
                    string err = ex.InnerException?.Message ?? ex.Message;
                    MessageBox.Show("Silme hatası: " + err, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
