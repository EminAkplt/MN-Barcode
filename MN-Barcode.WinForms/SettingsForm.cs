using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public class SettingsForm : Form
    {
        private SettingsService _settingsService;

        private TextBox _txtUserPassword;
        private TextBox _txtAdminPassword;
        private TextBox _txtReportsPassword;

        public SettingsForm()
        {
            _settingsService = new SettingsService();
            InitUI();
            LoadCurrentPasswords();
        }

        // ════════════════════════════════════════════════════════════
        private void InitUI()
        {
            this.BackColor       = Theme.Background;
            this.Dock            = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered  = true;

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
                Text      = "Ayarlar",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad, 14),
                BackColor = Color.Transparent
            };
            header.Controls.Add(title);

            // ── İÇERİK ─────────────────────────────────────────────
            Panel mainContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding   = new Padding(Theme.PagePad)
            };
            this.Controls.Add(mainContent);
            mainContent.BringToFront();

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 2,
                BackColor   = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 72F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
            mainContent.Controls.Add(layout);

            // ── ŞİFRE KARTI ─────────────────────────────────────────
            Panel passCard = Theme.MakeCard("Şifre Ayarları", Theme.Accent);
            layout.Controls.Add(passCard, 0, 0);

            Panel passContent = new Panel
            {
                Dock       = DockStyle.Fill,
                Padding    = new Padding(Theme.CardPad, 8, Theme.CardPad, Theme.CardPad),
                BackColor  = Color.Transparent
            };
            passCard.Controls.Add(passContent);

            int y = 8;
            AddPassField(passContent, "Kullanıcı Şifresi", ref y, out _txtUserPassword);
            y += 4;
            AddPassField(passContent, "Super Admin Şifresi", ref y, out _txtAdminPassword);
            y += 4;
            AddPassField(passContent, "Raporlar Şifresi", ref y, out _txtReportsPassword);
            y += 12;

            Button btnSave = Theme.MakeButton("  Şifreleri Kaydet", Theme.Success, Color.White, 200, 44, 10.5f);
            btnSave.Location = new Point(0, y);
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(5, 150, 105);
            btnSave.Click += BtnSavePasswords_Click;
            passContent.Controls.Add(btnSave);

            // ── BİLGİLENDİRME KARTI ─────────────────────────────────
            Panel infoCard = Theme.MakeCard("Bilgilendirme", Theme.Info);
            layout.Controls.Add(infoCard, 1, 0);

            Panel infoContent = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(Theme.CardPad, 8, Theme.CardPad, Theme.CardPad),
                BackColor = Color.Transparent
            };
            infoCard.Controls.Add(infoContent);

            AddInfoLine(infoContent, "• Kullanıcı Şifresi: Raporlar bölümüne erişim için kullanılır.", 10);
            AddInfoLine(infoContent, "• Super Admin Şifresi: Ayarlar modülüne erişim sağlar.", 56);
            AddInfoLine(infoContent, "• Raporlar Şifresi: Raporlara alternatif erişim için kullanılır.", 102);

            Label lblDefault = new Label
            {
                Text      = "Varsayılan tüm şifreler: 123",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize  = true,
                Location  = new Point(0, 166),
                BackColor = Color.Transparent
            };
            infoContent.Controls.Add(lblDefault);

            // ── TEHLİKELİ BÖLGE (alt, 2 sütun kaplayan) ───────────
            Panel dangerCard = Theme.MakeCard("Tehlikeli Bölge", Theme.Danger);
            layout.Controls.Add(dangerCard, 0, 1);
            layout.SetColumnSpan(dangerCard, 2);

            Panel dangerContent = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(Theme.CardPad, 8, Theme.CardPad, Theme.CardPad),
                BackColor = Color.Transparent
            };
            dangerCard.Controls.Add(dangerContent);

            Label lblWarn = new Label
            {
                Text      = "⚠  Sistemi sıfırlamak TÜM verileri siler! (Ürünler, Satışlar, Giderler, Kategoriler)\n" +
                            "Sadece kullanıcı tablosu korunacaktır. Bu işlem GERİ ALINAMAZ!",
                Font      = Theme.Body,
                ForeColor = Theme.Danger,
                Location  = new Point(0, 6),
                Size      = new Size(700, 42),
                BackColor = Color.Transparent
            };
            dangerContent.Controls.Add(lblWarn);

            Button btnReset = Theme.MakeButton("  TÜM SİSTEMİ SIFIRLA", Theme.Danger, Color.White, 240, 44, 10.5f);
            btnReset.Location = new Point(0, 54);
            btnReset.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
            btnReset.Click += BtnReset_Click;
            dangerContent.Controls.Add(btnReset);
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI
        // ════════════════════════════════════════════════════════════
        private void AddPassField(Panel parent, string label, ref int y, out TextBox txt)
        {
            parent.Controls.Add(new Label
            {
                Text      = label,
                Location  = new Point(0, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.TextSecond,
                BackColor = Color.Transparent
            });
            y += 22;

            txt = new TextBox
            {
                Location    = new Point(0, y),
                Size        = new Size(260, 34),
                Font        = new Font("Segoe UI", 12),
                BackColor   = Theme.Background,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(txt);
            y += 46;
        }

        private void AddInfoLine(Panel parent, string text, int y)
        {
            parent.Controls.Add(new Label
            {
                Text      = text,
                Font      = Theme.Body,
                ForeColor = Theme.TextSecond,
                Location  = new Point(0, y),
                Size      = new Size(380, 40),
                BackColor = Color.Transparent
            });
        }

        // ════════════════════════════════════════════════════════════
        //  VERİ
        // ════════════════════════════════════════════════════════════
        private void LoadCurrentPasswords()
        {
            _txtUserPassword.Text    = _settingsService.GetSetting(SettingsService.KEY_USER_PASSWORD);
            _txtAdminPassword.Text   = _settingsService.GetSetting(SettingsService.KEY_ADMIN_PASSWORD);
            _txtReportsPassword.Text = _settingsService.GetSetting(SettingsService.KEY_REPORTS_PASSWORD);
        }

        private void BtnSavePasswords_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtUserPassword.Text) ||
                string.IsNullOrWhiteSpace(_txtAdminPassword.Text) ||
                string.IsNullOrWhiteSpace(_txtReportsPassword.Text))
            {
                MessageBox.Show("Şifreler boş olamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _settingsService.UpdateUserPassword(_txtUserPassword.Text.Trim());
            _settingsService.UpdateAdminPassword(_txtAdminPassword.Text.Trim());
            _settingsService.UpdateReportsPassword(_txtReportsPassword.Text.Trim());
            MessageBox.Show("Şifreler başarıyla güncellendi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "⚠  DİKKAT!\n\nTüm veriler silinecektir:\n• Ürünler\n• Kategoriler\n• Satışlar\n• Giderler\n\n" +
                    "Sadece kullanıcı tablosu korunacaktır.\n\nDevam etmek istiyor musunuz?",
                    "Sistem Sıfırlama Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (MessageBox.Show(
                        "🔴 SON UYARI!\n\nBu işlem GERİ ALINAMAZ!\n\nGerçekten devam etmek istiyor musunuz?",
                        "Son Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) == DialogResult.Yes)
                {
                    try
                    {
                        _settingsService.ResetSystem();
                        MessageBox.Show(
                            "Sistem başarıyla sıfırlandı!\nTüm şifreler varsayılan değere (123) döndürüldü.",
                            "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
