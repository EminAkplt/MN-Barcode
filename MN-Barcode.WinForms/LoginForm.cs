using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public partial class LoginForm : Form
    {
        private TextBox _txtUsername;
        private TextBox _txtPassword;
        private Button  _btnLogin;

        // Pencere sürükleme (FormBorderStyle = None)
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern int  SendMessage(IntPtr h, int m, int w, int l);

        public LoginForm()
        {
            InitializeComponent();
            BuildUI();
        }

        // ════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.Size            = new Size(820, 500);
            this.BackColor       = Theme.Background;
            this.DoubleBuffered  = true;

            // ── SOL PANEL ──────────────────────────────────────────
            Panel left = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 340,
                BackColor = Theme.Sidebar
            };
            left.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); };
            this.Controls.Add(left);

            // Sol panel içi gradient çizim
            left.Paint += (s, e) =>
            {
                var g = e.Graphics;
                // İnce sağ kenar çizgisi
                using (var p = new Pen(Theme.Border, 1))
                    g.DrawLine(p, left.Width - 1, 0, left.Width - 1, left.Height);

                // Logo arka aydınlatması
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(80, 80, 180, 180);
                    using (var br = new PathGradientBrush(path))
                    {
                        br.CenterColor    = Color.FromArgb(40, Theme.Accent);
                        br.SurroundColors = new[] { Color.Transparent };
                        g.FillEllipse(br, 80, 80, 180, 180);
                    }
                }
            };

            // Logo metni
            Label lblLogo = new Label
            {
                Text      = "MN",
                Font      = new Font("Segoe UI", 56, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            left.Controls.Add(lblLogo);
            lblLogo.Location = new Point((340 - lblLogo.PreferredWidth) / 2, 110);

            Label lblSub = new Label
            {
                Text      = "POS",
                Font      = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            left.Controls.Add(lblSub);
            lblSub.Location = new Point((340 - lblSub.PreferredWidth) / 2, 186);

            Label lblTagline = new Label
            {
                Text      = "Hızlı Satış Sistemi",
                Font      = Theme.Body,
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            left.Controls.Add(lblTagline);
            lblTagline.Location = new Point((340 - lblTagline.PreferredWidth) / 2, 230);

            // Versiyon
            Label lblVer = new Label
            {
                Text      = "v2.0",
                Font      = Theme.Caption,
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                BackColor = Color.Transparent,
                Location  = new Point(16, 460)
            };
            left.Controls.Add(lblVer);

            // ── SAĞ PANEL ──────────────────────────────────────────
            Panel right = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding   = new Padding(60, 50, 60, 0)
            };
            right.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); };
            this.Controls.Add(right);

            // Kapatma butonu
            Button btnClose = new Button
            {
                Text      = "✕",
                Size      = new Size(36, 36),
                Location  = new Point(820 - 340 - 46, 12),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.TextMuted,
                Font      = new Font("Segoe UI", 12),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnClose.FlatAppearance.BorderSize           = 0;
            btnClose.FlatAppearance.MouseOverBackColor   = Theme.Danger;
            btnClose.Click      += (s, e) => Application.Exit();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Theme.TextMuted;
            right.Controls.Add(btnClose);

            // Başlık
            Label lblTitle = new Label
            {
                Text      = "Hoş Geldiniz",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(60, 60)
            };
            right.Controls.Add(lblTitle);

            Label lblDesc = new Label
            {
                Text      = "Devam etmek için giriş yapın",
                Font      = Theme.Body,
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                Location  = new Point(60, 96)
            };
            right.Controls.Add(lblDesc);

            // ── Kullanıcı Adı ──────────────────────────────────────
            Label lblUser = new Label
            {
                Text      = "KULLANICI ADI",
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                Location  = new Point(60, 142)
            };
            right.Controls.Add(lblUser);

            _txtUsername = new TextBox
            {
                Location    = new Point(60, 162),
                Size        = new Size(320, 36),
                Font        = new Font("Segoe UI", 12),
                BackColor   = Theme.Surface,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            right.Controls.Add(_txtUsername);

            // ── Şifre ──────────────────────────────────────────────
            Label lblPass = new Label
            {
                Text      = "ŞİFRE",
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                Location  = new Point(60, 218)
            };
            right.Controls.Add(lblPass);

            _txtPassword = new TextBox
            {
                Location      = new Point(60, 238),
                Size          = new Size(320, 36),
                Font          = new Font("Segoe UI", 12),
                BackColor     = Theme.Surface,
                ForeColor     = Theme.TextPrimary,
                BorderStyle   = BorderStyle.FixedSingle,
                PasswordChar  = '●'
            };
            _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(null, null); };
            right.Controls.Add(_txtPassword);

            // ── Giriş Butonu ───────────────────────────────────────
            _btnLogin = new Button
            {
                Text      = "GİRİŞ YAP",
                Location  = new Point(60, 304),
                Size      = new Size(320, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            _btnLogin.FlatAppearance.BorderSize         = 0;
            _btnLogin.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            _btnLogin.Click      += BtnLogin_Click;
            _btnLogin.MouseEnter += (s, e) => _btnLogin.BackColor = Theme.AccentHover;
            _btnLogin.MouseLeave += (s, e) => _btnLogin.BackColor = Theme.Accent;
            right.Controls.Add(_btnLogin);

            // ── Alt bilgi ──────────────────────────────────────────
            Label lblInfo = new Label
            {
                Text      = "MN-Barcode POS Sistemi  •  © 2024",
                Font      = Theme.Caption,
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                Location  = new Point(60, 380)
            };
            right.Controls.Add(lblInfo);

            // Sağ panel üste gelsin
            right.BringToFront();

            this.AcceptButton = _btnLogin;
        }

        // ════════════════════════════════════════════════════════════
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            _btnLogin.Enabled = false;
            _btnLogin.Text    = "Giriş yapılıyor...";
            try
            {
                var userService = new UserService();
                var user        = userService.Login(_txtUsername.Text, _txtPassword.Text);

                if (user != null)
                {
                    this.Hide();
                    var main = new MainForm(user);
                    main.ShowDialog();
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show("Hatalı Kullanıcı Adı veya Şifre!",
                        "Giriş Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtPassword.Clear();
                    _txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message, "Sistem Hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnLogin.Enabled = true;
                _btnLogin.Text    = "GİRİŞ YAP";
            }
        }

        private void LoginForm_Load(object sender, EventArgs e) { }
    }
}