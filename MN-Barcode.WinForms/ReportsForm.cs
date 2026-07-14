using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;
using System.IO;
using System.Text;
using System.Linq;

namespace MN_Barcode.WinForms
{
    public class ReportsForm : Form
    {
        private ReportingService _reportingService;
        private DateTimePicker _dtPicker;

        public ReportsForm()
        {
            _reportingService = new ReportingService();
            this.DoubleBuffered = true;
            InitUI();
        }

        private void InitUI()
        {
            this.BackColor = Color.FromArgb(240, 244, 248);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(30);

            // HEADER
            Panel header = new Panel { Dock = DockStyle.Top, Height = 60 };
            this.Controls.Add(header);

            Label title = new Label { Text = "📊 RAPORLAR MERKEZİ", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(45, 55, 72), Location = new Point(0, 5), AutoSize = true };
            header.Controls.Add(title);

            // Tarih Seçici
            Label lblDate = new Label { Text = "Tarih:", AutoSize = true, Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(45, 55, 72),
                Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(header.Width - 220, 15) };
            header.Controls.Add(lblDate);
            
            _dtPicker = new DateTimePicker { Width = 150, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(header.Width - 165, 12) };
            header.Controls.Add(_dtPicker);

            // CARD CONTAINER (FlowLayout ile responsive gibi davransın)
            FlowLayoutPanel flowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 20, 0, 0) };
            this.Controls.Add(flowPanel);
            header.SendToBack(); // Flow panel header'ın altına gelmesin

            // 1. Z RAPORU (Gün Sonu)
            flowPanel.Controls.Add(CreateReportCard("Z RAPORU (Gün Sonu)", "Günü kapatır ve günlük ciro özetini verir.", Color.FromArgb(49, 151, 149), "Z"));

            // 2. Z DETAY RAPORU
            flowPanel.Controls.Add(CreateReportCard("Z DETAY RAPORU", "Günlük satılan ürünlerin detay listesi.", Color.FromArgb(56, 178, 172), "Zd"));

            // 3. X RAPORU (Ara Rapor)
            flowPanel.Controls.Add(CreateReportCard("X RAPORU (Ara Rapor)", "Günü kapatmadan anlık ciro durumunu gösterir.", Color.FromArgb(66, 153, 225), "X"));

            // 4. ARA DETAY RAPORU
            flowPanel.Controls.Add(CreateReportCard("ARA DETAY RAPORU", "Anlık satış detaylarını listeler.", Color.FromArgb(99, 179, 237), "Xd"));
        }

        private Panel CreateReportCard(string title, string desc, Color color, string type)
        {
            Panel card = new Panel { Width = 400, Height = 220, BackColor = Color.White, Margin = new Padding(0, 0, 20, 20) };
            
            // Üst Şerit
            Panel stripe = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = color };
            card.Controls.Add(stripe);

            // Başlık
            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(45, 55, 72), Location = new Point(20, 25), AutoSize = false, Width = 360, Height = 30 };
            card.Controls.Add(lblTitle);

            // Açıklama
            Label lblDesc = new Label { Text = desc, Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, Location = new Point(20, 60), AutoSize = false, Width = 360, Height = 50 };
            card.Controls.Add(lblDesc);

            // Butonlar
            Button btnPrint = CreateActionButton("Yazdır", Color.FromArgb(45, 55, 72));
            btnPrint.Location = new Point(20, 150);
            btnPrint.Click += (s, e) => HandleReportAction(type, "Print");
            card.Controls.Add(btnPrint);

            Button btnExport = CreateActionButton("Excel'e Aktar", Color.FromArgb(22, 163, 74)); // Yeşil
            btnExport.Location = new Point(150, 150);
            btnExport.Click += (s, e) => HandleReportAction(type, "Export");
            card.Controls.Add(btnExport);

            return card;
        }

        private Button CreateActionButton(string text, Color color)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(120, 40);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void HandleReportAction(string reportType, string action)
        {
            DateTime date = _dtPicker.Value;
            
            if (action == "Print")
            {
                MessageBox.Show($"{reportType} yazdırılıyor... (Demo)", "Yazdır", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (action == "Export")
            {
                // Basit CSV Export
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
                    MessageBox.Show($"Rapor Masaüstüne kaydedildi:\n{fileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
