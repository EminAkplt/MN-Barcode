using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;
using Timer = System.Windows.Forms.Timer;

namespace MN_Barcode.WinForms
{
    public partial class MainForm : Form
    {
        // ── GLOBAL DEĞİŞKENLER ──────────────────────────────────────
        private AppUser _currentUser;
        private Panel   _sidebar;
        private Panel   _contentPanel;
        private Panel   _menuContainer;

        private Timer _sidebarTimer;
        private bool  _isSidebarOpen = true;

        private Button _activeMenuButton;
        private readonly Dictionary<string, Form> _formCache = new Dictionary<string, Form>();

        // ── PENCERE SÜRÜKLEME ────────────────────────────────────────
        [DllImport("user32.dll")] static extern void ReleaseCapture();
        [DllImport("user32.dll")] static extern void SendMessage(IntPtr h, int m, int w, int l);

        // ════════════════════════════════════════════════════════════
        public MainForm(AppUser user)
        {
            _currentUser = user;
            SetupLayout();
        }

        // ════════════════════════════════════════════════════════════
        //  ANA TASARIM
        // ════════════════════════════════════════════════════════════
        private void SetupLayout()
        {
            this.Text              = "MN POS Pro";
            this.Size              = new Size(1366, 768);
            this.StartPosition     = FormStartPosition.CenterScreen;
            this.FormBorderStyle   = FormBorderStyle.None;
            this.WindowState       = FormWindowState.Maximized;
            this.MaximizedBounds   = Screen.FromHandle(this.Handle).WorkingArea;
            this.BackColor         = Theme.Background;
            this.DoubleBuffered    = true;

            // ── HEADER ──────────────────────────────────────────────
            Panel header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = Theme.HeaderH,
                BackColor = Theme.Header
            };
            header.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); };
            header.Paint     += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            this.Controls.Add(header);

            // Hamburger
            Button btnMenu = new Button
            {
                Text      = "☰",
                Font      = new Font("Segoe UI", 18),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.TextSecond,
                BackColor = Color.Transparent,
                Size      = new Size(Theme.HeaderH, Theme.HeaderH),
                Dock      = DockStyle.Left,
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnMenu.FlatAppearance.BorderSize           = 0;
            btnMenu.FlatAppearance.MouseOverBackColor   = Theme.SurfaceAlt;
            btnMenu.Click      += (s, e) => _sidebarTimer.Start();
            btnMenu.MouseEnter += (s, e) => btnMenu.ForeColor = Theme.TextPrimary;
            btnMenu.MouseLeave += (s, e) => btnMenu.ForeColor = Theme.TextSecond;
            btnMenu.KeyDown    += (s, e) => { if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) e.Handled = true; };
            header.Controls.Add(btnMenu);

            // Logo
            Label lblIcon = new Label
            {
                Text      = "📦",
                Font      = new Font("Segoe UI", 20),
                ForeColor = Theme.Accent,
                Dock      = DockStyle.Left,
                AutoSize  = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding   = new Padding(8, 0, 0, 0),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblIcon);

            Label lblBrand = new Label
            {
                Text      = "MN-Barcode",
                Font      = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Dock      = DockStyle.Left,
                AutoSize  = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblBrand);

            // Pencere butonları (sağ)
            Button btnClose = CreateWinBtn("✕", (s, e) => ConfirmExit());
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Theme.Danger; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Theme.Header; btnClose.ForeColor = Theme.TextSecond; };
            header.Controls.Add(btnClose);

            Button btnMax = CreateWinBtn("⬜", (s, e) => ToggleMaximize());
            header.Controls.Add(btnMax);

            Button btnMin = CreateWinBtn("—", (s, e) => this.WindowState = FormWindowState.Minimized);
            header.Controls.Add(btnMin);

            // Kullanıcı adı (sağda)
            Label lblUser = new Label
            {
                Text      = $"👤  {_currentUser?.Username ?? "Kullanıcı"}",
                Font      = Theme.Caption,
                ForeColor = Theme.TextSecond,
                Dock      = DockStyle.Right,
                AutoSize  = false,
                Width     = 130,
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 8, 0),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblUser);

            // ── SIDEBAR ─────────────────────────────────────────────
            _sidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = Theme.SidebarW,
                BackColor = Theme.Sidebar
            };
            _sidebar.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
            };
            this.Controls.Add(_sidebar);

            // Sidebar iç scroll panel
            _menuContainer = new Panel
            {
                Dock      = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.Sidebar
            };
            _sidebar.Controls.Add(_menuContainer);

            // ── İÇERİK ALANI ────────────────────────────────────────
            _contentPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding   = new Padding(0)
            };
            this.Controls.Add(_contentPanel);
            _contentPanel.BringToFront();

            // Timer
            _sidebarTimer = new Timer { Interval = 12 };
            _sidebarTimer.Tick += SidebarTimer_Tick;

            BuildMenuStructure();
            ShowForm(new HomeDashboardForm());
        }

        // ════════════════════════════════════════════════════════════
        //  PENCERE YARDIMCI
        // ════════════════════════════════════════════════════════════
        private Button CreateWinBtn(string text, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text      = text,
                Dock      = DockStyle.Right,
                Width     = Theme.HeaderH,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Header,
                ForeColor = Theme.TextSecond,
                Font      = new Font("Segoe UI", 11),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click      += onClick;
            btn.MouseEnter += (s, e) => { btn.BackColor = Theme.SurfaceAlt; btn.ForeColor = Theme.TextPrimary; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Theme.Header; btn.ForeColor = Theme.TextSecond; };
            return btn;
        }

        private void ToggleMaximize()
        {
            this.WindowState = this.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        // ════════════════════════════════════════════════════════════
        //  SİDEBAR ANİMASYON
        // ════════════════════════════════════════════════════════════
        private void SidebarTimer_Tick(object sender, EventArgs e)
        {
            const int step = 32;
            if (_isSidebarOpen)
            {
                _sidebar.Width -= step;
                if (_sidebar.Width <= 0) { _sidebar.Width = 0; _isSidebarOpen = false; _sidebarTimer.Stop(); }
            }
            else
            {
                _sidebar.Width += step;
                if (_sidebar.Width >= Theme.SidebarW) { _sidebar.Width = Theme.SidebarW; _isSidebarOpen = true; _sidebarTimer.Stop(); }
            }
        }

        // ════════════════════════════════════════════════════════════
        //  MENÜ YAPISI
        // ════════════════════════════════════════════════════════════
        private void BuildMenuStructure()
        {
            int y = 16;
            AddMenuSection("GENEL", ref y);
            AddMenuBtn("📊   Ana Sayfa",     ref y, (s, e) => ShowFormCached("HomeDashboard", () => new HomeDashboardForm()));
            AddMenuBtn("⚡   Hızlı Satış",   ref y, (s, e) => ShowFormCached("SalesForm",     () => new SalesForm()));

            AddMenuSection("SATIŞ", ref y);
            AddMenuBtn("📋   Satış Geçmişi",  ref y, (s, e) => ShowForm(new SalesHistoryForm()));
            AddMenuBtn("↩️   İade İşlemleri", ref y, (s, e) => ShowForm(new ReturnsForm()));

            AddMenuSection("STOK", ref y);
            AddMenuBtn("📦   Ürün Yönetimi",  ref y, (s, e) => ShowForm(new ProductForm()));
            AddMenuBtn("📈   Stok Dashboard", ref y, (s, e) => ShowForm(new StockDashboardForm()));

            AddMenuSection("DİĞER", ref y);
            AddMenuBtn("📊   Raporlar",        ref y, (s, e) => OpenReportsWithPassword());
            AddMenuBtn("💸   Giderler",         ref y, (s, e) => ShowForm(new ExpenseManagerForm()));
            AddMenuBtn("⚙️   Ayarlar",          ref y, (s, e) => OpenSettingsWithPassword());
        }

        private void AddMenuSection(string title, ref int y)
        {
            var lbl = new Label
            {
                Text      = title,
                Location  = new Point(16, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent
            };
            _menuContainer.Controls.Add(lbl);
            y += 26;
        }

        private void AddMenuBtn(string text, ref int y, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text      = text,
                Location  = new Point(0, y),
                Size      = new Size(Theme.SidebarW, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Sidebar,
                ForeColor = Theme.TextSecond,
                Font      = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SidebarHov;
            btn.MouseEnter += (s, e) => { if (btn != _activeMenuButton) { btn.ForeColor = Theme.TextPrimary; } };
            btn.MouseLeave += (s, e) => { if (btn != _activeMenuButton) { btn.ForeColor = Theme.TextSecond; btn.BackColor = Theme.Sidebar; } };
            btn.Click      += (s, e) => { SetActiveButton(btn); onClick?.Invoke(s, e); };

            // Sol aktif şerit çizimi (Paint ile)
            btn.Paint += (s, e) =>
            {
                if (btn == _activeMenuButton)
                {
                    using (var b = new SolidBrush(Theme.Accent))
                        e.Graphics.FillRectangle(b, 0, 8, 3, btn.Height - 16);
                }
            };

            _menuContainer.Controls.Add(btn);
            y += 48;
        }

        private void SetActiveButton(Button btn)
        {
            if (_activeMenuButton != null)
            {
                _activeMenuButton.BackColor = Theme.Sidebar;
                _activeMenuButton.ForeColor = Theme.TextSecond;
                _activeMenuButton.Invalidate();
            }
            _activeMenuButton = btn;
            btn.BackColor = Theme.SidebarHov;
            btn.ForeColor = Theme.TextPrimary;
            btn.Invalidate();
        }

        // ════════════════════════════════════════════════════════════
        //  ŞİFRE KORUMALARI
        // ════════════════════════════════════════════════════════════
        private void OpenReportsWithPassword()
        {
            var svc = new SettingsService();
            using (var dlg = new PasswordDialog("🔒 Raporlar", "Raporlara erişim için şifre girin:"))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (svc.ValidateReportsPassword(dlg.EnteredPassword) ||
                        svc.ValidateUserPassword(dlg.EnteredPassword) ||
                        svc.ValidateAdminPassword(dlg.EnteredPassword))
                        ShowForm(new ReportsForm());
                    else
                        MessageBox.Show("Yanlış şifre!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void OpenSettingsWithPassword()
        {
            var svc = new SettingsService();
            using (var dlg = new PasswordDialog("🛡️ Super Admin", "Ayarlara erişim için Super Admin şifresi girin:"))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (svc.ValidateAdminPassword(dlg.EnteredPassword))
                        ShowForm(new SettingsForm());
                    else
                        MessageBox.Show("Yanlış şifre! Sadece Super Admin erişebilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        //  FORM YÖNETİMİ
        // ════════════════════════════════════════════════════════════
        private void ShowFormCached(string key, Func<Form> creator)
        {
            if (!_formCache.ContainsKey(key))
                _formCache[key] = creator();
            ShowForm(_formCache[key], isCached: true);
        }

        private void ShowForm(Form form, bool isCached = false)
        {
            for (int i = _contentPanel.Controls.Count - 1; i >= 0; i--)
            {
                var ctrl = _contentPanel.Controls[i];
                _contentPanel.Controls.RemoveAt(i);
                if (ctrl is Form old && _formCache.ContainsValue(old))
                    old.Hide();
                else
                    ctrl.Dispose();
            }

            form.TopLevel        = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock            = DockStyle.Fill;
            _contentPanel.Controls.Add(form);
            form.Show();
        }

        private void ConfirmExit()
        {
            if (MessageBox.Show("Programı kapatmak istediğinize emin misiniz?", "Çıkış",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Application.Exit();
        }

        // ════════════════════════════════════════════════════════════
        //  GLOBAL BARKOD YAKALAMA
        // ════════════════════════════════════════════════════════════
        private DateTime _lastScanTime = DateTime.Now;
        private string   _globalScanBuffer = "";

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool isSalesActive = _contentPanel.Controls.Count > 0 && _contentPanel.Controls[0] is SalesForm;
            SalesForm activeSales = isSalesActive ? (SalesForm)_contentPanel.Controls[0] : null;

            if (isSalesActive && activeSales.ProcessShortcut(keyData)) return true;

            DateTime now = DateTime.Now;
            if ((now - _lastScanTime).TotalMilliseconds > 50) _globalScanBuffer = "";
            _lastScanTime = now;

            if (keyData == Keys.Enter)
            {
                if (_globalScanBuffer.Length >= 3)
                {
                    string code = _globalScanBuffer;
                    _globalScanBuffer = "";
                    if (isSalesActive) activeSales.ProcessScannedBarcode(code);
                    return true;
                }
                _globalScanBuffer = "";
            }
            else
            {
                char c = KeyToChar(keyData);
                if (c != '\0') _globalScanBuffer += c;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static char KeyToChar(Keys kd)
        {
            Keys k = kd & Keys.KeyCode;
            if (k >= Keys.D0 && k <= Keys.D9) return (char)('0' + (k - Keys.D0));
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return (char)('0' + (k - Keys.NumPad0));
            if (k >= Keys.A && k <= Keys.Z) return (char)('A' + (k - Keys.A));
            if (k == Keys.OemMinus || k == Keys.Subtract) return '-';
            return '\0';
        }
    }
}