using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public class SettingsForm : Form
    {
        private SettingsService _settingsService;
        
        // Şifre textboxları
        private TextBox _txtUserPassword;
        private TextBox _txtAdminPassword;
        private TextBox _txtReportsPassword;

        // Renk Paleti
        private readonly Color BgColor = Color.FromArgb(248, 250, 252);
        private readonly Color CardBg = Color.White;
        private readonly Color HeaderBg = Color.FromArgb(30, 41, 59);
        private readonly Color PrimaryBlue = Color.FromArgb(59, 130, 246);
        private readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);
        private readonly Color DangerRed = Color.FromArgb(239, 68, 68);
        private readonly Color WarningOrange = Color.FromArgb(245, 158, 11);
        private readonly Color TextDark = Color.FromArgb(30, 41, 59);
        private readonly Color TextMuted = Color.FromArgb(100, 116, 139);
        private readonly Color BorderColor = Color.FromArgb(226, 232, 240);

        public SettingsForm()
        {
            _settingsService = new SettingsService();
            InitUI();
            LoadCurrentPasswords();
        }

        private void InitUI()
        {
            this.BackColor = BgColor;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ═══════════════════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════════════════
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = HeaderBg
            };
            this.Controls.Add(header);

            Label title = new Label
            {
                Text = "⚙️ AYARLAR",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 20),
                AutoSize = true
            };
            header.Controls.Add(title);

            // ═══════════════════════════════════════════════════════════
            // ANA İÇERİK
            // ═══════════════════════════════════════════════════════════
            Panel mainContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                BackColor = BgColor
            };
            this.Controls.Add(mainContent);
            mainContent.BringToFront();

            // Grid layout
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            mainContent.Controls.Add(layout);

            // ═══════════════════════════════════════════════════════════
            // ŞİFRE AYARLARI KARTI
            // ═══════════════════════════════════════════════════════════
            Panel passwordCard = CreateCard("🔐 ŞİFRE AYARLARI", PrimaryBlue);
            layout.Controls.Add(passwordCard, 0, 0);

            Panel passwordContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 20)
            };
            passwordCard.Controls.Add(passwordContent);
            passwordContent.BringToFront();

            // Kullanıcı Şifresi
            Label lblUser = new Label
            {
                Text = "👤 Kullanıcı Şifresi",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(0, 10),
                AutoSize = true
            };
            passwordContent.Controls.Add(lblUser);

            _txtUserPassword = CreatePasswordTextBox(0, 35);
            passwordContent.Controls.Add(_txtUserPassword);

            // Admin Şifresi
            Label lblAdmin = new Label
            {
                Text = "🛡️ Super Admin Şifresi",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(0, 85),
                AutoSize = true
            };
            passwordContent.Controls.Add(lblAdmin);

            _txtAdminPassword = CreatePasswordTextBox(0, 110);
            passwordContent.Controls.Add(_txtAdminPassword);

            // Raporlar Şifresi
            Label lblReports = new Label
            {
                Text = "📊 Raporlar Şifresi",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(0, 160),
                AutoSize = true
            };
            passwordContent.Controls.Add(lblReports);

            _txtReportsPassword = CreatePasswordTextBox(0, 185);
            passwordContent.Controls.Add(_txtReportsPassword);

            // Şifreleri Kaydet Butonu
            Button btnSavePasswords = new Button
            {
                Text = "💾 Şifreleri Kaydet",
                Size = new Size(200, 45),
                Location = new Point(0, 240),
                BackColor = SuccessGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSavePasswords.FlatAppearance.BorderSize = 0;
            btnSavePasswords.Click += BtnSavePasswords_Click;
            passwordContent.Controls.Add(btnSavePasswords);

            // ═══════════════════════════════════════════════════════════
            // BİLGİ KARTI
            // ═══════════════════════════════════════════════════════════
            Panel infoCard = CreateCard("ℹ️ BİLGİLENDİRME", WarningOrange);
            layout.Controls.Add(infoCard, 1, 0);

            Panel infoContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 20)
            };
            infoCard.Controls.Add(infoContent);
            infoContent.BringToFront();

            Label lblInfo1 = new Label
            {
                Text = "• Kullanıcı Şifresi: Raporlar bölümüne\n  erişim için kullanılır.",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextDark,
                Location = new Point(0, 10),
                Size = new Size(300, 50)
            };
            infoContent.Controls.Add(lblInfo1);

            Label lblInfo2 = new Label
            {
                Text = "• Super Admin Şifresi: Ayarlar modülüne\n  erişim için kullanılır.",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextDark,
                Location = new Point(0, 65),
                Size = new Size(300, 50)
            };
            infoContent.Controls.Add(lblInfo2);

            Label lblInfo3 = new Label
            {
                Text = "• Raporlar Şifresi: Alternatif olarak\n  raporlara erişim için kullanılabilir.",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextDark,
                Location = new Point(0, 120),
                Size = new Size(300, 50)
            };
            infoContent.Controls.Add(lblInfo3);

            Label lblDefault = new Label
            {
                Text = "🔒 Şifreniz varsayılan ise mutlaka değiştirin",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = PrimaryBlue,
                Location = new Point(0, 185),
                AutoSize = true
            };
            infoContent.Controls.Add(lblDefault);

            // ═══════════════════════════════════════════════════════════
            // SİSTEM SIFIRLAMA KARTI
            // ═══════════════════════════════════════════════════════════
            Panel resetCard = CreateCard("⚠️ TEHLİKELİ BÖLGE", DangerRed);
            layout.Controls.Add(resetCard, 0, 1);
            layout.SetColumnSpan(resetCard, 2);

            Panel resetContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 20)
            };
            resetCard.Controls.Add(resetContent);
            resetContent.BringToFront();

            Label lblResetWarning = new Label
            {
                Text = "⚠️ Sistemi sıfırlamak TÜM verileri silecektir! (Ürünler, Satışlar, Giderler, Kategoriler)\nSadece kullanıcı tablosu korunacaktır. Bu işlem GERİ ALINAMAZ!",
                Font = new Font("Segoe UI", 10),
                ForeColor = DangerRed,
                Location = new Point(0, 5),
                Size = new Size(600, 50)
            };
            resetContent.Controls.Add(lblResetWarning);

            Button btnReset = new Button
            {
                Text = "🗑️ TÜM SİSTEMİ SIFIRLA",
                Size = new Size(250, 50),
                Location = new Point(0, 55),
                BackColor = DangerRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += BtnReset_Click;
            resetContent.Controls.Add(btnReset);
        }

        private Panel CreateCard(string title, Color accentColor)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                BackColor = CardBg
            };

            card.Paint += (s, e) =>
            {
                // Üst kenar accent
                using (SolidBrush brush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, card.Width, 4);
                }
                // Border
                using (Pen pen = new Pen(BorderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 5, 0, 0)
            };
            card.Controls.Add(lblTitle);

            return card;
        }

        private TextBox CreatePasswordTextBox(int x, int y)
        {
            TextBox txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(250, 35),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle,
                // Şifreler ekranda okunur halde duruyordu: terminalin başına
                // geçen herkes admin şifresini görebiliyordu. Artık maskeli.
                UseSystemPasswordChar = true,
                PlaceholderText = "Değiştirmek için yeni şifre girin"
            };
            return txt;
        }

        /// <summary>
        /// Şifre kutularını boşaltır.
        ///
        /// Eskiden mevcut şifreler kutulara YAZILIYORDU ve maskesiz gösteriliyordu —
        /// yani Ayarlar ekranını açan herkes üç şifreyi de okuyabiliyordu.
        /// Artık kutular boş gelir: boş bırakılan şifre değişmez, yalnızca
        /// doldurulan alan güncellenir.
        /// </summary>
        private void LoadCurrentPasswords()
        {
            _txtUserPassword.Text = "";
            _txtAdminPassword.Text = "";
            _txtReportsPassword.Text = "";
        }

        /// <summary>
        /// Yalnızca DOLDURULAN şifreleri günceller. Boş bırakılan alan
        /// "değiştirme" anlamına gelir — kutular artık mevcut şifreyi göstermediği
        /// için hepsini birden yazmak zorunda kalmamak gerekiyor.
        /// </summary>
        private void BtnSavePasswords_Click(object sender, EventArgs e)
        {
            string kullanici = _txtUserPassword.Text.Trim();
            string yonetici  = _txtAdminPassword.Text.Trim();
            string raporlar  = _txtReportsPassword.Text.Trim();

            if (kullanici.Length == 0 && yonetici.Length == 0 && raporlar.Length == 0)
            {
                MessageBox.Show("Değiştirmek istediğiniz şifreyi yazın.\nBoş bıraktığınız şifreler değişmez.",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Zayıf şifre uyarısı: varsayılan "123" ile satışa çıkmak kabul edilemez.
            foreach (string s in new[] { kullanici, yonetici, raporlar })
            {
                if (s.Length > 0 && s.Length < 3)
                {
                    MessageBox.Show("Şifre en az 3 karakter olmalı.", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                var degisenler = new System.Collections.Generic.List<string>();
                if (kullanici.Length > 0) { _settingsService.UpdateUserPassword(kullanici);   degisenler.Add("Kullanıcı"); }
                if (yonetici.Length  > 0) { _settingsService.UpdateAdminPassword(yonetici);   degisenler.Add("Yönetici"); }
                if (raporlar.Length  > 0) { _settingsService.UpdateReportsPassword(raporlar); degisenler.Add("Raporlar"); }

                LoadCurrentPasswords();   // kutuları temizle
                MessageBox.Show($"Güncellenen şifreler: {string.Join(", ", degisenler)}",
                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Yaz("Şifre güncellenemedi", ex);
                MessageBox.Show("Şifre güncellenemedi. Ayrıntı hata kaydına yazıldı.",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ DİKKAT!\n\nTüm veriler silinecektir:\n• Ürünler\n• Kategoriler\n• Satışlar\n• Giderler\n\nSadece kullanıcı tablosu korunacaktır.\n\nDevam etmek istiyor musunuz?",
                "Sistem Sıfırlama Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var result2 = MessageBox.Show(
                    "🔴 SON UYARI!\n\nBu işlem GERİ ALINAMAZ!\n\nGerçekten devam etmek istiyor musunuz?",
                    "Son Onay",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Stop);

                if (result2 == DialogResult.Yes)
                {
                    try
                    {
                        _settingsService.ResetSystem();
                        MessageBox.Show("Sistem başarıyla sıfırlandı!\nTüm şifreler varsayılan değere (123) döndürüldü.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadCurrentPasswords();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
