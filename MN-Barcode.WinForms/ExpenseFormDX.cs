using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class ExpenseFormDX : XtraForm
    {
        private ExpenseService _expenseService;
        
        // Üst Kısım
        private DateEdit _dtStart;
        private DateEdit _dtEnd;
        private TextEdit _txtSearchPerson;
        private LabelControl _lblTotalExpense;
        private LabelControl _lblExpenseCount;
        
        // Grid
        private GridControl _gridExpenses;
        private GridView _gridViewExpenses;
        
        // Alt Kısım - Form Alanları
        private TextEdit _txtTitle;
        private SpinEdit _txtAmount;
        private MemoEdit _txtDescription;
        private TextEdit _txtCreatedBy;
        private DateEdit _dtExpenseDate;

        public ExpenseFormDX()
        {
            _expenseService = new ExpenseService();
            InitUI();
            this.Shown += (s, e) => LoadData();
        }

        private void InitUI()
        {
            this.Text = "Gider Yönetimi";
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.LookAndFeel.SkinName = "Office 2019 Black";

            // ═══════════════════════════════════════════════════════════
            // HEADER - Basit Tasarım
            // ═══════════════════════════════════════════════════════════
            PanelControl header = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 80,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            header.Appearance.BackColor = Color.FromArgb(45, 55, 72);
            this.Controls.Add(header);

            // Başlık
            LabelControl title = new LabelControl
            {
                Text = "💸 GİDER YÖNETİMİ",
                Location = new Point(30, 25)
            };
            title.Appearance.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.Appearance.ForeColor = Color.White;
            header.Controls.Add(title);

            // Tarih Seçimi
            _dtStart = new DateEdit
            {
                Location = new Point(280, 25),
                Size = new Size(110, 28)
            };
            _dtStart.Properties.Appearance.Font = new Font("Segoe UI", 10);
            _dtStart.DateTime = DateTime.Now.AddDays(-30);
            header.Controls.Add(_dtStart);

            LabelControl lblTo = new LabelControl
            {
                Text = "➡",
                Location = new Point(400, 28)
            };
            lblTo.Appearance.ForeColor = Color.White;
            lblTo.Appearance.Font = new Font("Segoe UI", 11);
            header.Controls.Add(lblTo);

            _dtEnd = new DateEdit
            {
                Location = new Point(430, 25),
                Size = new Size(110, 28)
            };
            _dtEnd.Properties.Appearance.Font = new Font("Segoe UI", 10);
            _dtEnd.DateTime = DateTime.Now;
            header.Controls.Add(_dtEnd);

            // Kişi Filtresi
            _txtSearchPerson = new TextEdit
            {
                Location = new Point(560, 25),
                Size = new Size(100, 28)
            };
            _txtSearchPerson.Properties.Appearance.Font = new Font("Segoe UI", 10);
            _txtSearchPerson.Properties.NullValuePrompt = "Kişi...";
            header.Controls.Add(_txtSearchPerson);

            // Filtrele Butonu
            SimpleButton btnFilter = new SimpleButton
            {
                Text = "Filtrele",
                Size = new Size(80, 30),
                Location = new Point(680, 24),
                Cursor = Cursors.Hand
            };
            btnFilter.Appearance.BackColor = Color.FromArgb(66, 153, 225);
            btnFilter.Appearance.ForeColor = Color.White;
            btnFilter.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnFilter.Appearance.Options.UseBackColor = true;
            btnFilter.Appearance.Options.UseForeColor = true;
            btnFilter.Appearance.Options.UseFont = true;
            btnFilter.Click += (s, e) => LoadData();
            header.Controls.Add(btnFilter);

            // Toplam Gider (Sağda)
            _lblTotalExpense = new LabelControl
            {
                Text = "Toplam: ₺0",
                Location = new Point(800, 25)
            };
            _lblTotalExpense.Appearance.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            _lblTotalExpense.Appearance.ForeColor = Color.FromArgb(236, 72, 153);
            header.Controls.Add(_lblTotalExpense);

            // Gider Sayısı
            _lblExpenseCount = new LabelControl
            {
                Text = "(0 kayıt)",
                Location = new Point(960, 28)
            };
            _lblExpenseCount.Appearance.Font = new Font("Segoe UI", 12);
            _lblExpenseCount.Appearance.ForeColor = Color.FromArgb(160, 174, 192);
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
            PanelControl leftPanel = CreateCardPanel("📋 GİDER LİSTESİ", Color.FromArgb(139, 92, 246));
            content.Controls.Add(leftPanel, 0, 0);

            PanelControl gridContainer = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(15, 0, 15, 15)
            };
            leftPanel.Controls.Add(gridContainer);
            gridContainer.BringToFront();

            _gridExpenses = new GridControl { Dock = DockStyle.Fill };
            _gridViewExpenses = new GridView(_gridExpenses);
            _gridExpenses.MainView = _gridViewExpenses;

            _gridViewExpenses.OptionsView.ShowGroupPanel = false;
            _gridViewExpenses.OptionsView.ShowIndicator = false;
            _gridViewExpenses.OptionsBehavior.Editable = true;
            _gridViewExpenses.RowHeight = 50;

            _gridViewExpenses.Columns.Add(new GridColumn { FieldName = "Id", Caption = "ID", Visible = false });
            _gridViewExpenses.Columns.Add(new GridColumn { FieldName = "Date", Caption = "TARİH", VisibleIndex = 0, Width = 100 });
            _gridViewExpenses.Columns.Add(new GridColumn { FieldName = "Title", Caption = "BAŞLIK", VisibleIndex = 1, Width = 150 });
            _gridViewExpenses.Columns.Add(new GridColumn { FieldName = "CreatedBy", Caption = "HARCAYAN", VisibleIndex = 2, Width = 100 });
            _gridViewExpenses.Columns.Add(new GridColumn { FieldName = "Amount", Caption = "TUTAR", VisibleIndex = 3, Width = 100 });
            _gridViewExpenses.Columns.Add(new GridColumn { FieldName = "Description", Caption = "AÇIKLAMA", VisibleIndex = 4, Width = 200 });

            // Sil butonu
            var btnDelCol = new GridColumn
            {
                FieldName = "Delete",
                Caption = "",
                VisibleIndex = 5,
                Width = 60,
                UnboundType = DevExpress.Data.UnboundColumnType.Object
            };
            btnDelCol.OptionsColumn.AllowEdit = true;
            _gridViewExpenses.Columns.Add(btnDelCol);

            var deleteRepo = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            deleteRepo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            deleteRepo.Buttons.Clear();
            var delBtn = new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            delBtn.Caption = "🗑️";
            delBtn.Appearance.BackColor = Color.FromArgb(239, 68, 68);
            delBtn.Appearance.ForeColor = Color.White;
            delBtn.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            delBtn.Appearance.Options.UseBackColor = true;
            delBtn.Appearance.Options.UseForeColor = true;
            delBtn.Appearance.Options.UseFont = true;
            delBtn.Width = 50;
            deleteRepo.Buttons.Add(delBtn);
            deleteRepo.ButtonClick += DeleteExpense_Click;
            btnDelCol.ColumnEdit = deleteRepo;
            _gridExpenses.RepositoryItems.Add(deleteRepo);

            // Diğer kolonları readonly yap
            foreach (GridColumn col in _gridViewExpenses.Columns)
            {
                if (col.FieldName != "Delete")
                    col.OptionsColumn.AllowEdit = false;
            }

            // Tutar renklendirme
            _gridViewExpenses.RowCellStyle += (s, e) =>
            {
                if (e.Column.FieldName == "Amount")
                {
                    e.Appearance.ForeColor = Color.FromArgb(190, 24, 93);
                    e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                }
            };

            gridContainer.Controls.Add(_gridExpenses);

            // ═══════════════════════════════════════════════════════════
            // SAĞ PANEL - YENİ GİDER EKLE
            // ═══════════════════════════════════════════════════════════
            PanelControl rightPanel = CreateCardPanel("➕ YENİ GİDER EKLE", Color.FromArgb(34, 197, 94));
            content.Controls.Add(rightPanel, 1, 0);

            PanelControl formContainer = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(20, 10, 20, 20)
            };
            rightPanel.Controls.Add(formContainer);
            formContainer.BringToFront();

            int y = 10;

            // Başlık
            LabelControl lblTitle = new LabelControl
            {
                Text = "Başlık *",
                Location = new Point(0, y)
            };
            lblTitle.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblTitle.Appearance.ForeColor = Color.FromArgb(55, 65, 81);
            formContainer.Controls.Add(lblTitle);
            y += 25;

            _txtTitle = new TextEdit
            {
                Location = new Point(0, y),
                Size = new Size(280, 32)
            };
            _txtTitle.Properties.Appearance.Font = new Font("Segoe UI", 11);
            _txtTitle.Properties.NullValuePrompt = "Örn: Elektrik Faturası";
            formContainer.Controls.Add(_txtTitle);
            y += 45;

            // Tutar
            LabelControl lblAmount = new LabelControl
            {
                Text = "Tutar (₺) *",
                Location = new Point(0, y)
            };
            lblAmount.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblAmount.Appearance.ForeColor = Color.FromArgb(55, 65, 81);
            formContainer.Controls.Add(lblAmount);
            y += 25;

            _txtAmount = new SpinEdit
            {
                Location = new Point(0, y),
                Size = new Size(280, 32)
            };
            _txtAmount.Properties.Appearance.Font = new Font("Segoe UI", 11);
            _txtAmount.Properties.IsFloatValue = true;
            _txtAmount.Properties.Mask.EditMask = "n2";
            _txtAmount.Properties.MinValue = 0;
            _txtAmount.Properties.MaxValue = 9999999;
            formContainer.Controls.Add(_txtAmount);
            y += 45;

            // Kim Harcadı
            LabelControl lblCreatedBy = new LabelControl
            {
                Text = "Kim Harcadı *",
                Location = new Point(0, y)
            };
            lblCreatedBy.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblCreatedBy.Appearance.ForeColor = Color.FromArgb(55, 65, 81);
            formContainer.Controls.Add(lblCreatedBy);
            y += 25;

            _txtCreatedBy = new TextEdit
            {
                Location = new Point(0, y),
                Size = new Size(280, 32)
            };
            _txtCreatedBy.Properties.Appearance.Font = new Font("Segoe UI", 11);
            _txtCreatedBy.Properties.NullValuePrompt = "Örn: Ahmet";
            formContainer.Controls.Add(_txtCreatedBy);
            y += 45;

            // Tarih
            LabelControl lblDate = new LabelControl
            {
                Text = "Tarih",
                Location = new Point(0, y)
            };
            lblDate.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblDate.Appearance.ForeColor = Color.FromArgb(55, 65, 81);
            formContainer.Controls.Add(lblDate);
            y += 25;

            _dtExpenseDate = new DateEdit
            {
                Location = new Point(0, y),
                Size = new Size(280, 32)
            };
            _dtExpenseDate.Properties.Appearance.Font = new Font("Segoe UI", 11);
            _dtExpenseDate.DateTime = DateTime.Now;
            formContainer.Controls.Add(_dtExpenseDate);
            y += 45;

            // Açıklama
            LabelControl lblDesc = new LabelControl
            {
                Text = "Açıklama",
                Location = new Point(0, y)
            };
            lblDesc.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblDesc.Appearance.ForeColor = Color.FromArgb(55, 65, 81);
            formContainer.Controls.Add(lblDesc);
            y += 25;

            _txtDescription = new MemoEdit
            {
                Location = new Point(0, y),
                Size = new Size(280, 80)
            };
            _txtDescription.Properties.Appearance.Font = new Font("Segoe UI", 10);
            _txtDescription.Properties.NullValuePrompt = "Detaylı açıklama...";
            formContainer.Controls.Add(_txtDescription);
            y += 95;

            // Kaydet Butonu
            SimpleButton btnSave = new SimpleButton
            {
                Text = "💾 GİDER EKLE",
                Location = new Point(0, y),
                Size = new Size(280, 50),
                Cursor = Cursors.Hand
            };
            btnSave.Appearance.BackColor = Color.FromArgb(34, 197, 94);
            btnSave.Appearance.ForeColor = Color.White;
            btnSave.Appearance.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            btnSave.Appearance.Options.UseBackColor = true;
            btnSave.Appearance.Options.UseForeColor = true;
            btnSave.Appearance.Options.UseFont = true;
            btnSave.Click += BtnSave_Click;
            formContainer.Controls.Add(btnSave);
        }

        private PanelControl CreateCardPanel(string title, Color accentColor)
        {
            PanelControl panel = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Margin = new Padding(8)
            };
            panel.Appearance.BackColor = Color.White;

            PanelControl stripe = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 4,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            stripe.Appearance.BackColor = accentColor;
            panel.Controls.Add(stripe);

            LabelControl lbl = new LabelControl
            {
                Text = title,
                Dock = DockStyle.Top,
                AutoSizeMode = LabelAutoSizeMode.None,
                Height = 45,
                Padding = new Padding(15, 12, 0, 0)
            };
            lbl.Appearance.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lbl.Appearance.ForeColor = Color.FromArgb(30, 41, 59);
            panel.Controls.Add(lbl);

            return panel;
        }

        private void LoadData()
        {
            try
            {
                DateTime start = _dtStart.DateTime.Date;
                DateTime end = _dtEnd.DateTime.Date.AddDays(1).AddSeconds(-1);
                string personFilter = _txtSearchPerson.Text?.Trim() ?? "";

                var expenses = _expenseService.GetExpenses(start, end);
                
                // Kişi filtresi
                if (!string.IsNullOrEmpty(personFilter))
                {
                    expenses = expenses.Where(x => 
                        x.CreatedBy.ToLower().Contains(personFilter.ToLower())).ToList();
                }

                var expenseData = expenses.Select(e => new
                {
                    e.Id,
                    Date = e.ExpenseDate.ToString("dd.MM.yyyy"),
                    e.Title,
                    e.CreatedBy,
                    Amount = e.Amount.ToString("₺#,##0.00"),
                    e.Description
                }).ToList();

                _gridExpenses.DataSource = expenseData;

                // Toplam
                decimal total = expenses.Sum(x => x.Amount);
                _lblTotalExpense.Text = $"Toplam: {total:₺#,##0.00}";
                _lblExpenseCount.Text = $"({expenses.Count} kayıt)";
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Veri yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(_txtTitle.Text))
            {
                XtraMessageBox.Show("Başlık alanı zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_txtAmount.Value <= 0)
            {
                XtraMessageBox.Show("Geçerli bir tutar giriniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtCreatedBy.Text))
            {
                XtraMessageBox.Show("Harcayan kişi alanı zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var expense = new Expense
                {
                    Title = _txtTitle.Text.Trim(),
                    Amount = (decimal)_txtAmount.Value,
                    CreatedBy = _txtCreatedBy.Text.Trim(),
                    ExpenseDate = _dtExpenseDate.DateTime,
                    Description = _txtDescription.Text?.Trim() ?? ""
                };

                _expenseService.Add(expense);
                XtraMessageBox.Show("Gider başarıyla eklendi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Formu temizle
                _txtTitle.Text = "";
                _txtAmount.Value = 0;
                _txtCreatedBy.Text = "";
                _txtDescription.Text = "";
                _dtExpenseDate.DateTime = DateTime.Now;

                // Listeyi yenile
                LoadData();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteExpense_Click(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            int rowHandle = _gridViewExpenses.FocusedRowHandle;
            if (rowHandle < 0) return;
            
            int id = Convert.ToInt32(_gridViewExpenses.GetRowCellValue(rowHandle, "Id"));
            string title = _gridViewExpenses.GetRowCellValue(rowHandle, "Title")?.ToString() ?? "";

            if (XtraMessageBox.Show($"'{title}' gideri silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _expenseService.Delete(id);
                LoadData();
            }
        }
    }
}
