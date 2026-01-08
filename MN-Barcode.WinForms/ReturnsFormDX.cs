using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public class ReturnsFormDX : XtraForm
    {
        private SaleService _saleService;
        private GridControl _gridReturns;
        private GridView _gridViewReturns;
        private GridControl _gridTopReturns;
        private GridView _gridViewTopReturns;
        private DateEdit _dtStart;
        private DateEdit _dtEnd;
        private LabelControl _lblTotalReturnAmount;
        private LabelControl _lblReturnCount;

        public ReturnsFormDX()
        {
            _saleService = new SaleService();
            this.DoubleBuffered = true;
            InitUI();
            this.Shown += (s, e) => LoadData();
        }

        private void InitUI()
        {
            this.Text = "İade İşlemleri";
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.LookAndFeel.SkinName = "Office 2019 Black";

            // ═══════════════════════════════════════════════════════════
            // HEADER - Basit Tasarım (İade tema - Kırmızımsı)
            // ═══════════════════════════════════════════════════════════
            PanelControl header = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 80,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            header.Appearance.BackColor = Color.FromArgb(153, 27, 27); // Koyu kırmızı
            this.Controls.Add(header);

            // Başlık
            LabelControl title = new LabelControl
            {
                Text = "↩️ İADE İŞLEMLERİ",
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

            // Filtrele Butonu
            SimpleButton btnFilter = new SimpleButton
            {
                Text = "Filtrele",
                Size = new Size(80, 30),
                Location = new Point(560, 24),
                Cursor = Cursors.Hand
            };
            btnFilter.Appearance.BackColor = Color.White;
            btnFilter.Appearance.ForeColor = Color.FromArgb(153, 27, 27);
            btnFilter.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnFilter.Appearance.Options.UseBackColor = true;
            btnFilter.Appearance.Options.UseForeColor = true;
            btnFilter.Appearance.Options.UseFont = true;
            btnFilter.Click += (s, e) => LoadData();
            header.Controls.Add(btnFilter);

            // İade Toplamı (Sağda)
            _lblTotalReturnAmount = new LabelControl
            {
                Text = "İade: ₺0",
                Location = new Point(700, 25)
            };
            _lblTotalReturnAmount.Appearance.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            _lblTotalReturnAmount.Appearance.ForeColor = Color.FromArgb(254, 202, 202);
            header.Controls.Add(_lblTotalReturnAmount);

            // İade Sayısı
            _lblReturnCount = new LabelControl
            {
                Text = "(0 iade)",
                Location = new Point(850, 28)
            };
            _lblReturnCount.Appearance.Font = new Font("Segoe UI", 12);
            _lblReturnCount.Appearance.ForeColor = Color.FromArgb(252, 165, 165);
            header.Controls.Add(_lblReturnCount);

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
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            this.Controls.Add(content);
            content.BringToFront();

            // ═══════════════════════════════════════════════════════════
            // SOL PANEL - İADE LİSTESİ
            // ═══════════════════════════════════════════════════════════
            PanelControl leftPanel = CreateCardPanel("🧾 İADE HAREKETLERİ", Color.FromArgb(239, 68, 68));
            content.Controls.Add(leftPanel, 0, 0);

            PanelControl gridContainer1 = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(15, 0, 15, 15)
            };
            leftPanel.Controls.Add(gridContainer1);
            gridContainer1.BringToFront();

            _gridReturns = new GridControl { Dock = DockStyle.Fill };
            _gridViewReturns = new GridView(_gridReturns);
            _gridReturns.MainView = _gridViewReturns;

            _gridViewReturns.OptionsView.ShowGroupPanel = false;
            _gridViewReturns.OptionsView.ShowIndicator = false;
            _gridViewReturns.OptionsBehavior.Editable = false;
            _gridViewReturns.RowHeight = 45;

            _gridViewReturns.Columns.Add(new GridColumn { FieldName = "Id", Caption = "ID", Visible = false });
            _gridViewReturns.Columns.Add(new GridColumn { FieldName = "Date", Caption = "TARİH", VisibleIndex = 0, Width = 150 });
            _gridViewReturns.Columns.Add(new GridColumn { FieldName = "Code", Caption = "FİŞ NO", VisibleIndex = 1, Width = 120 });
            _gridViewReturns.Columns.Add(new GridColumn { FieldName = "Amount", Caption = "İADE TUTARI", VisibleIndex = 2, Width = 120 });

            // İade tutarı kırmızı
            _gridViewReturns.RowCellStyle += (s, e) =>
            {
                if (e.Column.FieldName == "Amount")
                {
                    e.Appearance.ForeColor = Color.FromArgb(220, 38, 38);
                    e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                }
            };

            gridContainer1.Controls.Add(_gridReturns);

            // ═══════════════════════════════════════════════════════════
            // SAĞ PANEL - EN ÇOK İADE EDİLENLER
            // ═══════════════════════════════════════════════════════════
            PanelControl rightPanel = CreateCardPanel("⚠️ EN ÇOK İADE EDİLENLER", Color.FromArgb(245, 158, 11));
            content.Controls.Add(rightPanel, 1, 0);

            PanelControl gridContainer2 = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(15, 0, 15, 15)
            };
            rightPanel.Controls.Add(gridContainer2);
            gridContainer2.BringToFront();

            _gridTopReturns = new GridControl { Dock = DockStyle.Fill };
            _gridViewTopReturns = new GridView(_gridTopReturns);
            _gridTopReturns.MainView = _gridViewTopReturns;

            _gridViewTopReturns.OptionsView.ShowGroupPanel = false;
            _gridViewTopReturns.OptionsView.ShowIndicator = false;
            _gridViewTopReturns.OptionsBehavior.Editable = false;
            _gridViewTopReturns.RowHeight = 45;

            _gridViewTopReturns.Columns.Add(new GridColumn { FieldName = "Name", Caption = "ÜRÜN ADI", VisibleIndex = 0, Width = 180 });
            _gridViewTopReturns.Columns.Add(new GridColumn { FieldName = "Qty", Caption = "ADET", VisibleIndex = 1, Width = 60 });
            _gridViewTopReturns.Columns.Add(new GridColumn { FieldName = "Amount", Caption = "TUTAR", VisibleIndex = 2, Width = 100 });

            // Sıra renklendirme
            _gridViewTopReturns.RowCellStyle += (s, e) =>
            {
                if (e.RowHandle == 0)
                    e.Appearance.BackColor = Color.FromArgb(254, 226, 226);
            };

            gridContainer2.Controls.Add(_gridTopReturns);
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

                // İade Listesi
                var returns = _saleService.GetReturnsHistory(start, end);
                var returnData = returns.Select(r => new
                {
                    r.Id,
                    Date = r.CreatedDate?.ToString("dd.MM.yyyy HH:mm") ?? "-",
                    Code = r.TransactionCode ?? "-",
                    Amount = Math.Abs(r.TotalAmount).ToString("₺#,##0.00")
                }).ToList();

                _gridReturns.DataSource = returnData;

                // Toplam
                decimal totalRet = returns.Sum(x => Math.Abs(x.TotalAmount));
                _lblTotalReturnAmount.Text = $"İade: {totalRet:₺#,##0.00}";
                _lblReturnCount.Text = $"({returns.Count} iade)";

                // Top Returned
                var top = _saleService.GetTopReturnedProducts(10, start, end);
                var topData = top.Select(t => new
                {
                    Name = t.ProductName,
                    Qty = t.TotalQuantity.ToString("N0"),
                    Amount = t.TotalRevenue.ToString("₺#,##0.00")
                }).ToList();

                _gridTopReturns.DataSource = topData;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Veri yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
