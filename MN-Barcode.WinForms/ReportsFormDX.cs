using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public class ReportsFormDX : XtraForm
    {
        private ReportingService _reportingService;
        private DateEdit _dtPicker;
        private GridControl _gridSummary;
        private GridView _gridViewSummary;

        public ReportsFormDX()
        {
            _reportingService = new ReportingService();
            this.DoubleBuffered = true;
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "Raporlar Merkezi";
            this.BackColor = Color.FromArgb(240, 244, 248);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(30);
            this.LookAndFeel.SkinName = "Office 2019 Black";

            // HEADER
            PanelControl header = new PanelControl { Dock = DockStyle.Top, Height = 60, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            this.Controls.Add(header);

            LabelControl title = new LabelControl { Text = "📊 RAPORLAR MERKEZİ", Location = new Point(0, 15) };
            title.Appearance.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            title.Appearance.ForeColor = Color.FromArgb(45, 55, 72);
            header.Controls.Add(title);

            // Tarih Seçici
            LabelControl lblDate = new LabelControl { Text = "Tarih Seçimi:", Location = new Point(500, 18) };
            lblDate.Appearance.Font = new Font("Segoe UI", 11);
            header.Controls.Add(lblDate);

            _dtPicker = new DateEdit { Location = new Point(600, 15), Width = 150 };
            _dtPicker.Properties.Appearance.Font = new Font("Segoe UI", 11);
            _dtPicker.DateTime = DateTime.Today;
            _dtPicker.EditValueChanged += (s, e) => UpdateSummaryGrid();
            header.Controls.Add(_dtPicker);

            // ANA İÇERİK PANELİ
            PanelControl mainPanel = new PanelControl { Dock = DockStyle.Fill, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            this.Controls.Add(mainPanel);
            mainPanel.BringToFront();

            // SOL: CARD CONTAINER
            PanelControl leftPanel = new PanelControl { Dock = DockStyle.Left, Width = 450, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            leftPanel.Padding = new Padding(0, 20, 20, 0);
            mainPanel.Controls.Add(leftPanel);

            FlowLayoutPanel flowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 0, 0, 0), FlowDirection = FlowDirection.TopDown, WrapContents = false };
            leftPanel.Controls.Add(flowPanel);

            // Rapor Kartları
            flowPanel.Controls.Add(CreateReportCard("Z RAPORU (Gün Sonu)", "Günü kapatır ve günlük ciro özetini verir.", Color.FromArgb(49, 151, 149), "Z"));
            flowPanel.Controls.Add(CreateReportCard("Z DETAY RAPORU", "Günlük satılan ürünlerin detay listesi.", Color.FromArgb(56, 178, 172), "Zd"));
            flowPanel.Controls.Add(CreateReportCard("X RAPORU (Ara Rapor)", "Günü kapatmadan anlık ciro durumunu gösterir.", Color.FromArgb(66, 153, 225), "X"));
            flowPanel.Controls.Add(CreateReportCard("ARA DETAY RAPORU", "Anlık satış detaylarını listeler.", Color.FromArgb(99, 179, 237), "Xd"));

            // SAĞ: ÖZET TABLOSU
            PanelControl rightPanel = new PanelControl { Dock = DockStyle.Fill, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            rightPanel.Padding = new Padding(20);
            mainPanel.Controls.Add(rightPanel);
            rightPanel.BringToFront();

            // TABLO BAŞLIĞI
            LabelControl gridTitle = new LabelControl { Text = "📈 Son 7 Günlük Satış Özeti", Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.None, Height = 40 };
            gridTitle.Appearance.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            gridTitle.Appearance.ForeColor = Color.FromArgb(45, 55, 72);
            rightPanel.Controls.Add(gridTitle);

            // ÖZET TABLOSU
            _gridSummary = new GridControl { Dock = DockStyle.Fill };
            _gridViewSummary = new GridView(_gridSummary);
            _gridSummary.MainView = _gridViewSummary;

            // Grid Ayarları
            _gridViewSummary.OptionsView.ShowGroupPanel = false;
            _gridViewSummary.OptionsView.ShowIndicator = false;
            _gridViewSummary.OptionsSelection.EnableAppearanceFocusedCell = false;
            _gridViewSummary.OptionsBehavior.Editable = false;
            _gridViewSummary.RowHeight = 45;

            // Kolonlar
            _gridViewSummary.Columns.Add(new GridColumn { FieldName = "Tarih", Caption = "TARİH", VisibleIndex = 0, Width = 100 });
            _gridViewSummary.Columns.Add(new GridColumn { FieldName = "Ciro", Caption = "TOPLAM CİRO", VisibleIndex = 1, Width = 120 });
            _gridViewSummary.Columns.Add(new GridColumn { FieldName = "FisSayisi", Caption = "FİŞ SAYISI", VisibleIndex = 2, Width = 80 });
            _gridViewSummary.Columns.Add(new GridColumn { FieldName = "Nakit", Caption = "NAKİT", VisibleIndex = 3, Width = 100 });
            _gridViewSummary.Columns.Add(new GridColumn { FieldName = "Kart", Caption = "KREDİ KARTI", VisibleIndex = 4, Width = 100 });

            // Ciro renkli gösterimi  
            _gridViewSummary.RowCellStyle += (s, e) =>
            {
                if (e.Column.FieldName == "Ciro")
                {
                    e.Appearance.ForeColor = Color.FromArgb(56, 161, 105);
                    e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                }
            };

            rightPanel.Controls.Add(_gridSummary);
            _gridSummary.BringToFront();

            // Veriyi yükle
            UpdateSummaryGrid();
        }

        private void UpdateSummaryGrid()
        {
            try
            {
                // Son 7 günün verilerini al
                var data = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var date = _dtPicker.DateTime.AddDays(-i);
                        var summary = _reportingService.GetDailySummary(date);
                        return new
                        {
                            Tarih = date.ToString("dd.MM.yyyy"),
                            Ciro = summary != null ? summary.TotalRevenue.ToString("₺#,##0.00") : "₺0,00",
                            FisSayisi = summary?.TotalSalesCount ?? 0,
                            Nakit = summary != null ? summary.CashTotal.ToString("₺#,##0.00") : "₺0,00",
                            Kart = summary != null ? summary.CardTotal.ToString("₺#,##0.00") : "₺0,00"
                        };
                    })
                    .ToList();

                _gridSummary.DataSource = data;
            }
            catch { }
        }

        private PanelControl CreateReportCard(string title, string desc, Color color, string type)
        {
            PanelControl card = new PanelControl { Width = 400, Height = 180, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            card.Margin = new Padding(0, 0, 0, 15);
            card.Appearance.BackColor = Color.White;

            // Üst Şerit
            PanelControl stripe = new PanelControl { Dock = DockStyle.Top, Height = 6, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            stripe.Appearance.BackColor = color;
            card.Controls.Add(stripe);

            // Başlık
            LabelControl lblTitle = new LabelControl { Text = title, Location = new Point(20, 20) };
            lblTitle.Appearance.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.Appearance.ForeColor = Color.FromArgb(45, 55, 72);
            card.Controls.Add(lblTitle);

            // Açıklama
            LabelControl lblDesc = new LabelControl { Text = desc, Location = new Point(20, 55), AutoSizeMode = LabelAutoSizeMode.None, Width = 360, Height = 40 };
            lblDesc.Appearance.Font = new Font("Segoe UI", 10);
            lblDesc.Appearance.ForeColor = Color.Gray;
            card.Controls.Add(lblDesc);

            // Butonlar
            SimpleButton btnPrint = CreateActionButton("Yazdır", Color.FromArgb(45, 55, 72));
            btnPrint.Location = new Point(20, 120);
            btnPrint.Click += (s, e) => HandleReportAction(type, "Print");
            card.Controls.Add(btnPrint);

            SimpleButton btnExport = CreateActionButton("Excel'e Aktar", Color.FromArgb(22, 163, 74));
            btnExport.Location = new Point(150, 120);
            btnExport.Click += (s, e) => HandleReportAction(type, "Export");
            card.Controls.Add(btnExport);

            return card;
        }

        private SimpleButton CreateActionButton(string text, Color color)
        {
            SimpleButton btn = new SimpleButton
            {
                Text = text,
                Size = new Size(120, 40),
                Cursor = Cursors.Hand
            };
            btn.Appearance.BackColor = color;
            btn.Appearance.ForeColor = Color.White;
            btn.Appearance.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Appearance.Options.UseBackColor = true;
            btn.Appearance.Options.UseForeColor = true;
            btn.Appearance.Options.UseFont = true;
            return btn;
        }

        private void HandleReportAction(string reportType, string action)
        {
            DateTime date = _dtPicker.DateTime;

            if (action == "Print")
            {
                XtraMessageBox.Show($"{reportType} yazdırılıyor... (Demo)", "Yazdır", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (action == "Export")
            {
                try
                {
                    string csvContent = "";
                    string fileName = $"Rapor_{reportType}_{date:yyyyMMdd}.csv";

                    if (reportType == "Z" || reportType == "X")
                    {
                        var summary = _reportingService.GetDailySummary(date);
                        csvContent = $"Tarih;Toplam Ciro;Fis Sayisi;Nakit;Kredi Karti\n{summary.Date:dd.MM.yyyy};{summary.TotalRevenue};{summary.TotalSalesCount};{summary.CashTotal};{summary.CardTotal}";
                    }
                    else
                    {
                        var details = _reportingService.GetDailyProductDetails(date);
                        csvContent = "Urun Adi;Barkod;Adet;Toplam Tutar\n" + string.Join("\n", details.Select(x => $"{x.ProductName};{x.Barcode};{x.TotalQuantity};{x.TotalRevenue}"));
                    }

                    File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + fileName, csvContent, Encoding.UTF8);
                    XtraMessageBox.Show($"Rapor Masaüstüne kaydedildi:\n{fileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
