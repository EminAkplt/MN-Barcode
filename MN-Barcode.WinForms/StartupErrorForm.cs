using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// Başlangıç hatalarını gösteren pencere.
    ///
    /// NEDEN AYRI BİR PENCERE:
    /// Sade bir MessageBox "hata kaydı şu dosyada" deyip bırakıyordu. Sahada bu
    /// işe yaramıyor — %LOCALAPPDATA% gizli bir klasör, müşteri oraya ulaşamıyor,
    /// telefonda tarif etmek dakikalar alıyor. Teknik rapor doğrudan ekranda
    /// gösterilir; tek tıkla panoya kopyalanır veya MASAÜSTÜNE kaydedilir.
    /// Böylece destek "şu dosyayı bana gönder" diyebilir ve iş biter.
    /// </summary>
    public sealed class StartupErrorForm : Form
    {
        private static readonly Color ColBg      = Color.FromArgb(245, 247, 250);
        private static readonly Color ColSurface = Color.White;
        private static readonly Color ColBorder  = Color.FromArgb(214, 221, 230);
        private static readonly Color ColStrong  = Color.FromArgb(15, 23, 42);
        private static readonly Color ColMuted   = Color.FromArgb(100, 116, 139);
        private static readonly Color ColRed     = Color.FromArgb(220, 38, 38);
        private static readonly Color ColBlue    = Color.FromArgb(37, 99, 235);
        private static readonly Color ColBlueDark= Color.FromArgb(29, 78, 188);

        private readonly string _teknikRapor;
        private TextBox _txtRapor;

        public StartupErrorForm(string baslik, string kullaniciMesaji, string teknikRapor)
        {
            _teknikRapor = teknikRapor ?? "";
            InitUI(baslik, kullaniciMesaji);
        }

        private void InitUI(string baslik, string kullaniciMesaji)
        {
            Text = "MN-Barcode";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            BackColor = ColBg;
            ClientSize = new Size(760, 560);

            // ── Üst: başlık + açıklama ──────────────────────────────
            Panel ust = new Panel { Dock = DockStyle.Top, Height = 190, BackColor = ColSurface, Padding = new Padding(24, 20, 24, 16) };
            ust.Paint += (s, e) =>
            {
                using var kalem = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(kalem, 0, ust.Height - 1, ust.Width, ust.Height - 1);
            };
            Controls.Add(ust);

            Label lblBaslik = new Label
            {
                Text = baslik,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = ColRed,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 36
            };

            Label lblMesaj = new Label
            {
                Text = kullaniciMesaji,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = ColStrong,
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            ust.Controls.Add(lblMesaj);
            ust.Controls.Add(lblBaslik);

            // ── Alt: butonlar ───────────────────────────────────────
            Panel alt = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = ColSurface, Padding = new Padding(24, 12, 24, 12) };
            alt.Paint += (s, e) =>
            {
                using var kalem = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(kalem, 0, 0, alt.Width, 0);
            };
            Controls.Add(alt);

            Button btnKapat = Buton("Kapat", Color.FromArgb(100, 116, 139), Color.FromArgb(71, 85, 105));
            btnKapat.Dock = DockStyle.Right;
            btnKapat.Click += (s, e) => Close();

            Button btnKaydet = Buton("Raporu Masaüstüne Kaydet", ColBlue, ColBlueDark);
            btnKaydet.Dock = DockStyle.Right;
            btnKaydet.Width = 230;
            btnKaydet.Margin = new Padding(0, 0, 8, 0);
            btnKaydet.Click += (s, e) => MasaustuneKaydet();

            Button btnKopyala = Buton("Panoya Kopyala", Color.FromArgb(71, 85, 105), Color.FromArgb(51, 65, 85));
            btnKopyala.Dock = DockStyle.Right;
            btnKopyala.Width = 160;
            btnKopyala.Click += (s, e) => PanoyaKopyala();

            // Dock.Right sirali eklenir: en son eklenen en solda kalir.
            alt.Controls.Add(btnKapat);
            alt.Controls.Add(BosaltmaAyraci(8));
            alt.Controls.Add(btnKaydet);
            alt.Controls.Add(BosaltmaAyraci(8));
            alt.Controls.Add(btnKopyala);

            // ── Orta: teknik rapor ──────────────────────────────────
            Panel orta = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Padding = new Padding(24, 12, 24, 12) };
            Controls.Add(orta);
            orta.BringToFront();

            _txtRapor = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9),
                BackColor = Color.White,
                ForeColor = ColStrong,
                BorderStyle = BorderStyle.FixedSingle,
                Text = string.IsNullOrWhiteSpace(_teknikRapor) ? "(teknik ayrıntı yok)" : _teknikRapor
            };

            Label lblRaporBaslik = new Label
            {
                Text = "Teknik rapor — destek ekibine bunu gönderin:",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColMuted
            };

            orta.Controls.Add(_txtRapor);
            orta.Controls.Add(lblRaporBaslik);
        }

        private static Control BosaltmaAyraci(int genislik)
        {
            return new Panel { Dock = DockStyle.Right, Width = genislik, BackColor = Color.Transparent };
        }

        private Button Buton(string yazi, Color arka, Color uzerine)
        {
            var b = new Button
            {
                Text = yazi,
                Width = 110,
                BackColor = arka,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = uzerine;
            return b;
        }

        private void PanoyaKopyala()
        {
            try
            {
                Clipboard.SetText(string.IsNullOrWhiteSpace(_teknikRapor) ? "(rapor yok)" : _teknikRapor);
                MessageBox.Show("Rapor panoya kopyalandı.\nWhatsApp veya e-postaya yapıştırabilirsiniz.",
                    "Kopyalandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Panoya kopyalanamadı: " + ex.Message,
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MasaustuneKaydet()
        {
            try
            {
                string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string ad = $"MN-Barcode-hata-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
                string yol = Path.Combine(masaustu, ad);

                File.WriteAllText(yol, _teknikRapor, System.Text.Encoding.UTF8);

                MessageBox.Show(
                    $"Rapor masaüstüne kaydedildi:\n\n{ad}\n\nBu dosyayı destek ekibine gönderin.",
                    "Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Masaüstüne kaydedilemedi: " + ex.Message +
                    "\n\nBunun yerine 'Panoya Kopyala' düğmesini kullanın.",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Kısayol: hatayı göster ve kapanmasını bekle.</summary>
        public static void Goster(string baslik, string kullaniciMesaji, string teknikRapor)
        {
            using var f = new StartupErrorForm(baslik, kullaniciMesaji, teknikRapor);
            f.ShowDialog();
        }
    }
}
