using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class ExpenseManagerForm : Form
    {
        private ExpenseService _expenseService;

        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private TextBox        _txtSearchPerson;
        private Label          _lblTotalExpense;
        private Label          _lblExpenseCount;

        private DataGridView _grid;

        private TextBox        _txtTitle;
        private NumericUpDown  _numAmount;
        private TextBox        _txtDescription;
        private TextBox        _txtCreatedBy;
        private DateTimePicker _dtExpenseDate;

        public ExpenseManagerForm()
        {
            _expenseService = new ExpenseService();
            this.DoubleBuffered = true;
            InitUI();
            this.Shown += (s, e) => LoadData();
        }

        // ════════════════════════════════════════════════════════════
        private void InitUI()
        {
            this.BackColor       = Theme.Background;
            this.Dock            = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ── HEADER ──────────────────────────────────────────────
            Panel header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Theme.Surface
            };
            header.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            this.Controls.Add(header);

            Label title = new Label
            {
                Text      = "Gider Yönetimi",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad, 14),
                BackColor = Color.Transparent
            };
            header.Controls.Add(title);

            // Filtre grubu (sağa akış)
            FlowLayoutPanel ctrl = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 13, 8, 0)
            };
            header.Controls.Add(ctrl);

            _dtStart = Theme.MakeDatePicker(105);
            _dtStart.Value  = DateTime.Now.AddDays(-30);
            _dtStart.Margin = new Padding(4, 2, 2, 4);
            ctrl.Controls.Add(_dtStart);

            ctrl.Controls.Add(new Label { Text = "→", Font = Theme.Body, ForeColor = Theme.TextSecond, AutoSize = true, Margin = new Padding(0, 6, 0, 0) });

            _dtEnd = Theme.MakeDatePicker(105);
            _dtEnd.Margin = new Padding(2, 2, 4, 4);
            ctrl.Controls.Add(_dtEnd);

            _txtSearchPerson = new TextBox
            {
                Width       = 90,
                Font        = Theme.Body,
                BackColor   = Theme.Background,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Kişi...",
                Margin      = new Padding(4, 2, 4, 4)
            };
            ctrl.Controls.Add(_txtSearchPerson);

            Button btnFilter = Theme.MakeButton("Filtrele", Theme.Accent, Color.White, 85, 32, 9.5f);
            btnFilter.Margin = new Padding(4, 2, 4, 4);
            btnFilter.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            btnFilter.Click += (s, e) => LoadData();
            ctrl.Controls.Add(btnFilter);

            // Toplam gider + sayaç
            _lblTotalExpense = new Label
            {
                Text      = "Toplam: ₺0",
                Font      = Theme.H2,
                ForeColor = Theme.Danger,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize  = true,
                Location  = new Point(500, 18),
                BackColor = Color.Transparent
            };
            header.Controls.Add(_lblTotalExpense);

            _lblExpenseCount = new Label
            {
                Text      = "(0 kayıt)",
                Font      = Theme.Body,
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                Location  = new Point(700, 20),
                BackColor = Color.Transparent
            };
            header.Controls.Add(_lblExpenseCount);

            // ── İÇERİK (2 sütun) ────────────────────────────────────
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                Padding     = new Padding(Theme.PagePad, Theme.Gap, Theme.PagePad, Theme.PagePad)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            this.Controls.Add(content);
            content.BringToFront();

            // ── SOL: Gider listesi ───────────────────────────────────
            Panel left = Theme.MakeCard("Gider Listesi", Theme.Danger);
            content.Controls.Add(left, 0, 0);

            _grid = Theme.MakeDarkGrid();
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id",          Visible = false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",        HeaderText = "TARİH",     FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title",       HeaderText = "BAŞLIK",    FillWeight = 30 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy",   HeaderText = "HARCAYAN",  FillWeight = 20 });
            var colAmt = new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "TUTAR", FillWeight = 20 };
            colAmt.DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.Danger,
                Font      = new Font("Consolas", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleRight
            };
            _grid.Columns.Add(colAmt);
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "AÇIKLAMA", FillWeight = 30 });

            var btnDel = new DataGridViewButtonColumn
            {
                Name = "Del", HeaderText = "", Text = "Sil",
                UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, FillWeight = 10
            };
            btnDel.DefaultCellStyle.ForeColor = Theme.Danger;
            _grid.Columns.Add(btnDel);

            _grid.ReadOnly = false;
            _grid.Columns["Del"].ReadOnly = false;
            foreach (DataGridViewColumn c in _grid.Columns)
                if (c.Name != "Del") c.ReadOnly = true;

            _grid.CellClick += Grid_CellClick;

            Panel gw = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
            gw.Controls.Add(_grid);
            left.Controls.Add(gw);

            // ── SAĞ: Yeni gider formu ────────────────────────────────
            Panel right = Theme.MakeCard("Yeni Gider Ekle", Theme.Accent);
            content.Controls.Add(right, 1, 0);

            Panel formPanel = new Panel
            {
                Dock       = DockStyle.Fill,
                Padding    = new Padding(Theme.CardPad, 8, Theme.CardPad, Theme.CardPad),
                AutoScroll = true,
                BackColor  = Color.Transparent
            };
            right.Controls.Add(formPanel);

            int y = 8;

            AddFormRow(formPanel, "Başlık *", ref y);
            _txtTitle = MakeTxt(ref y);
            _txtTitle.PlaceholderText = "Örn: Elektrik Faturası";
            formPanel.Controls.Add(_txtTitle);

            AddFormRow(formPanel, "Tutar (₺) *", ref y);
            _numAmount = new NumericUpDown
            {
                Location    = new Point(0, y),
                Size        = new Size(280, 34),
                Font        = Theme.Body,
                Maximum     = 9999999,
                DecimalPlaces = 2,
                TextAlign   = HorizontalAlignment.Right,
                BackColor   = Theme.Background,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            formPanel.Controls.Add(_numAmount); y += 46;

            AddFormRow(formPanel, "Kim Harcadı *", ref y);
            _txtCreatedBy = MakeTxt(ref y);
            _txtCreatedBy.PlaceholderText = "Örn: Ahmet";
            formPanel.Controls.Add(_txtCreatedBy);

            AddFormRow(formPanel, "Tarih", ref y);
            _dtExpenseDate = Theme.MakeDatePicker(280);
            _dtExpenseDate.Location = new Point(0, y);
            _dtExpenseDate.Value    = DateTime.Now;
            formPanel.Controls.Add(_dtExpenseDate); y += 46;

            AddFormRow(formPanel, "Açıklama", ref y);
            _txtDescription = new TextBox
            {
                Location    = new Point(0, y),
                Size        = new Size(280, 72),
                Font        = Theme.Body,
                Multiline   = true,
                BackColor   = Theme.Background,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            formPanel.Controls.Add(_txtDescription); y += 86;

            Button btnSave = Theme.MakeButton("  GİDER EKLE", Theme.Success, Color.White, 280, 48, 12);
            btnSave.Location = new Point(0, y);
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(5, 150, 105);
            btnSave.Click += BtnSave_Click;
            formPanel.Controls.Add(btnSave);
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI
        // ════════════════════════════════════════════════════════════
        private void AddFormRow(Panel p, string label, ref int y)
        {
            p.Controls.Add(new Label
            {
                Text      = label,
                Location  = new Point(0, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.TextSecond,
                BackColor = Color.Transparent
            });
            y += 22;
        }

        private TextBox MakeTxt(ref int y)
        {
            var txt = new TextBox
            {
                Location    = new Point(0, y),
                Size        = new Size(280, 34),
                Font        = Theme.Body,
                BackColor   = Theme.Background,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            y += 46;
            return txt;
        }

        // ════════════════════════════════════════════════════════════
        //  VERİ YÜKLEME
        // ════════════════════════════════════════════════════════════
        private void LoadData()
        {
            try
            {
                DateTime start  = _dtStart.Value.Date;
                DateTime end    = _dtEnd.Value.Date.AddDays(1).AddSeconds(-1);
                string   person = _txtSearchPerson.Text?.Trim() ?? "";

                var expenses = _expenseService.GetExpenses(start, end);
                if (!string.IsNullOrEmpty(person))
                    expenses = expenses.Where(x => x.CreatedBy.ToLower().Contains(person.ToLower())).ToList();

                _grid.Rows.Clear();
                foreach (var e in expenses)
                    _grid.Rows.Add(e.Id, e.ExpenseDate.ToString("dd.MM.yyyy"),
                        e.Title, e.CreatedBy, e.Amount.ToString("₺#,##0.00"), e.Description);

                decimal total = expenses.Sum(x => x.Amount);
                _lblTotalExpense.Text = $"Toplam: {total:₺#,##0.00}";
                _lblExpenseCount.Text = $"({expenses.Count} kayıt)";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri yüklenirken hata: " + (ex.InnerException?.Message ?? ex.Message),
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtTitle.Text))
            { MessageBox.Show("Başlık alanı zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_numAmount.Value <= 0)
            { MessageBox.Show("Geçerli bir tutar giriniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(_txtCreatedBy.Text))
            { MessageBox.Show("Harcayan kişi zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                _expenseService.Add(new Expense
                {
                    Title       = _txtTitle.Text.Trim(),
                    Amount      = _numAmount.Value,
                    CreatedBy   = _txtCreatedBy.Text.Trim(),
                    ExpenseDate = _dtExpenseDate.Value,
                    Description = _txtDescription.Text?.Trim() ?? ""
                });
                MessageBox.Show("Gider başarıyla eklendi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtTitle.Clear(); _numAmount.Value = 0; _txtCreatedBy.Clear();
                _txtDescription.Clear(); _dtExpenseDate.Value = DateTime.Now;
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + (ex.InnerException?.Message ?? ex.Message),
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Del") return;
            int    id    = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["Id"].Value);
            string title = _grid.Rows[e.RowIndex].Cells["Title"].Value?.ToString() ?? "";
            if (MessageBox.Show($"'{title}' gideri silinsin mi?", "Onay",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try { _expenseService.Delete(id); LoadData(); }
                catch (Exception ex) { MessageBox.Show("Silme hatası: " + (ex.InnerException?.Message ?? ex.Message), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
}
