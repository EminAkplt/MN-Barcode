using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public class ReportsForm : Form
    {
        private ReportingService _reportingService;
        private DateTimePicker   _dtPicker;

        public ReportsForm()
        {
            _reportingService = new ReportingService();
            this.DoubleBuffered = true;
            InitUI();
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
                Text      = "Raporlar Merkezi",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad, 14),
                BackColor = Color.Transparent
            };
            header.Controls.Add(title);

            // Tarih seçici (sağda)
            FlowLayoutPanel ctrl = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 14, 16, 0)
            };
            header.Controls.Add(ctrl);

            ctrl.Controls.Add(new Label
            {
                Text      = "Tarih:",
                Font      = Theme.Body,
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                Margin    = new Padding(0, 6, 6, 0)
            });

            _dtPicker = Theme.MakeDatePicker(130);
            _dtPicker.Margin = new Padding(2, 2, 4, 4);
            ctrl.Controls.Add(_dtPicker);

            // ── İÇERİK ─────────────────────────────────────────────
            Panel content = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding   = new Padding(Theme.PagePad, Theme.Gap, Theme.PagePad, Theme.PagePad),
                AutoScroll= true
            };
            this.Controls.Add(content);
            content.BringToFront();

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(0)
            };
            content.Controls.Add(flow);

            // 4 rapor kartı
            flow.Controls.Add(MakeReportCard("Z Raporu",       "Günü kapatır ve günlük ciro özetini verir.",          Theme.Success, "Z"));
            flow.Controls.Add(MakeReportCard("Z Detay Raporu", "Günlük satılan ürünlerin detay listesi.",              Theme.Info,    "Zd"));
            flow.Controls.Add(MakeReportCard("X Raporu",       "Günü kapatmadan anlık ciro durumunu gösterir.",        Theme.Accent,  "X"));
            flow.Controls.Add(MakeReportCard("Ara Detay Raporu","Anlık satış detaylarını listeler.",                   Theme.Warning, "Xd"));
        }

        // ════════════════════════════════════════════════════════════
        //  RAPOR KARTI
        // ════════════════════════════════════════════════════════════
        private Panel MakeReportCard(string title, string desc, Color accentColor, string type)
        {
            Panel card = new Panel
            {
                Width     = 380,
                Height    = 200,
                BackColor = Theme.Surface,
                Margin    = new Padding(0, 0, Theme.Gap, Theme.Gap)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using (var b = new SolidBrush(accentColor))
                    e.Graphics.FillRectangle(b, 0, 0, card.Width, 4);
            };

            Label lblTitle = new Label
            {
                Text      = title,
                Font      = Theme.H2,
                ForeColor = accentColor,
                Location  = new Point(20, 18),
                AutoSize  = false,
                Width     = 340,
                Height    = 28,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            Label lblDesc = new Label
            {
                Text      = desc,
                Font      = Theme.Body,
                ForeColor = Theme.TextSecond,
                Location  = new Point(20, 52),
                AutoSize  = false,
                Width     = 340,
                Height    = 44,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblDesc);

            // Ayırıcı çizgi
            Panel sep = new Panel
            {
                Location  = new Point(20, 102),
                Size      = new Size(340, 1),
                BackColor = Theme.Border
            };
            card.Controls.Add(sep);

            // Yazdır butonu
            Button btnPrint = Theme.MakeButton("  Yazdır", Theme.SurfaceAlt, Theme.TextPrimary, 130, 40, 9.5f);
            btnPrint.Location = new Point(20, 120);
            btnPrint.FlatAppearance.BorderColor = Theme.Border;
            btnPrint.FlatAppearance.BorderSize  = 1;
            btnPrint.FlatAppearance.MouseOverBackColor = Theme.Background;
            btnPrint.Click += (s, e) => HandleReportAction(type, "Print");
            card.Controls.Add(btnPrint);

            // Excel butonu
            Button btnExport = Theme.MakeButton("  Excel'e Aktar", Theme.Success, Color.White, 150, 40, 9.5f);
            btnExport.Location = new Point(162, 120);
            btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(5, 150, 105);
            btnExport.Click += (s, e) => HandleReportAction(type, "Export");
            card.Controls.Add(btnExport);

            return card;
        }

        // ════════════════════════════════════════════════════════════
        //  RAPOR AKSİYONLARI
        // ════════════════════════════════════════════════════════════
        private void HandleReportAction(string reportType, string action)
        {
            DateTime date = _dtPicker.Value;
            if (action == "Print")
            {
                MessageBox.Show($"{reportType} yazdırılıyor... (Demo)", "Yazdır",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string csvContent;
                string fileName = $"Rapor_{reportType}_{date:yyyyMMdd}.csv";

                if (reportType == "Z" || reportType == "X")
                {
                    var s = _reportingService.GetDailySummary(date);
                    csvContent = $"Tarih;Toplam Ciro;Fis Sayisi;Nakit;Kredi Karti\n" +
                                 $"{s.Date:dd.MM.yyyy};{s.TotalRevenue};{s.TotalSalesCount};{s.CashTotal};{s.CardTotal}";
                }
                else
                {
                    var details = _reportingService.GetDailyProductDetails(date);
                    csvContent = "Urun Adi;Barkod;Adet;Toplam Tutar\n" +
                        string.Join("\n", details.Select(x => $"{x.ProductName};{x.Barcode};{x.TotalQuantity};{x.TotalRevenue}"));
                }

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                File.WriteAllText(path, csvContent, Encoding.UTF8);
                MessageBox.Show($"Rapor Masaüstüne kaydedildi:\n{fileName}", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
