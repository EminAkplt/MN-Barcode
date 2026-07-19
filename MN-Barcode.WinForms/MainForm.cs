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
        // ============================================================
        // 🎨 TASARIM AYARLARI (DESIGN SETTINGS)
        // ============================================================

        // --- RENK PALETİ (MODERN DARK THEME) ---
        private readonly Color ThemeColor = Color.FromArgb(30, 41, 59);
        private readonly Color HoverColor = Color.FromArgb(51, 65, 85);
        private readonly Color ActiveColor = Color.FromArgb(37, 99, 235);
        private readonly Color HeaderColor = Color.FromArgb(20, 30, 45);
        private readonly Color ContentBg = Color.FromArgb(241, 245, 249);
        private readonly Color TextColor = Color.FromArgb(226, 232, 240);
        private readonly Color DangerColor = Color.FromArgb(220, 38, 38);

        // --- BOYUTLAR ---
        private const int HeaderHeight = 75;
        private const int SidebarMax = 280;
        private const int SidebarMin = 0;

        // --- DURUM ---
        private bool _isSidebarOpen = true;

        // ============================================================
        // 🔧 GLOBAL DEĞİŞKENLER
        // ============================================================
        private AppUser _currentUser;
        private Panel _sidebar;
        private Panel _contentPanel;
        private FlowLayoutPanel _menuContainer;

        private Timer _accordionTimer;
        private Timer _sidebarTimer;

        private Panel _activeSubMenuPanel;
        private bool _isOpeningSubMenu = false;

        // Aktif menü butonunu takip et (renk vurgulaması için)
        private Button _activeMenuButton;

        // --- FORM CACHE (PERFORMANS) ---
        // Sık kullanılan formları (SalesForm vb.) önbellekte tutuyoruz
        private readonly Dictionary<string, Form> _formCache = new Dictionary<string, Form>();

        // ============================================================
        // 🏁 BAŞLANGIÇ
        // ============================================================
        public MainForm(AppUser user)
        {
            _currentUser = user;
            SetupLayout();
        }

        // ============================================================
        // 🏗️ ANA TASARIM MOTORU
        // ============================================================
        private void SetupLayout()
        {
            this.Text = "MN POS Pro";
            this.Size = new Size(1366, 768);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;

            // ÜST BAR (HEADER)
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = HeaderHeight;
            header.BackColor = HeaderColor;
            header.MouseDown += Header_MouseDown;

            Panel borderLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(71, 85, 105) };
            header.Controls.Add(borderLine);
            this.Controls.Add(header);

            // Logo
            Label lblIcon = new Label();
            lblIcon.Text = "📦";
            lblIcon.Font = new Font("Segoe UI", 28);
            lblIcon.ForeColor = ActiveColor;
            lblIcon.Dock = DockStyle.Left;
            lblIcon.AutoSize = true;
            lblIcon.TextAlign = ContentAlignment.MiddleCenter;
            lblIcon.Padding = new Padding(10, 8, 0, 0);
            header.Controls.Add(lblIcon);

            Label lblBrand = new Label();
            lblBrand.Text = "MN-Barcode";
            lblBrand.ForeColor = Color.White;
            lblBrand.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblBrand.Dock = DockStyle.Left;
            lblBrand.TextAlign = ContentAlignment.MiddleLeft;
            lblBrand.AutoSize = true;
            lblBrand.Padding = new Padding(5, 5, 0, 0);
            header.Controls.Add(lblBrand);

            // Hamburger
            Button btnMenu = new Button();
            btnMenu.Text = "☰";
            btnMenu.Font = new Font("Segoe UI", 26);
            btnMenu.FlatStyle = FlatStyle.Flat;
            btnMenu.FlatAppearance.BorderSize = 0;
            btnMenu.ForeColor = Color.White;
            btnMenu.Size = new Size(HeaderHeight, HeaderHeight);
            btnMenu.Dock = DockStyle.Left;
            btnMenu.Cursor = Cursors.Hand;
            btnMenu.TabStop = false;
            btnMenu.Click += (s, e) => _sidebarTimer.Start();
            btnMenu.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) e.Handled = true;
            };
            header.Controls.Add(btnMenu);

            // Pencere Kontrolleri
            Button btnMin = CreateWindowButton("—", (s, e) => this.WindowState = FormWindowState.Minimized);
            header.Controls.Add(btnMin);

            Button btnMax = CreateWindowButton("⬜", (s, e) => ToggleMaximize());
            btnMax.Font = new Font("Segoe UI", 12);
            header.Controls.Add(btnMax);

            Button btnClose = CreateWindowButton("✕", (s, e) => ConfirmExit());
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = DangerColor; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = HeaderColor; btnClose.ForeColor = Color.Silver; };
            header.Controls.Add(btnClose);

            // SIDEBAR
            _sidebar = new Panel();
            _sidebar.Dock = DockStyle.Left;
            _sidebar.Width = SidebarMax;
            _sidebar.BackColor = ThemeColor;
            this.Controls.Add(_sidebar);

            _menuContainer = new FlowLayoutPanel();
            _menuContainer.Dock = DockStyle.Fill;
            _menuContainer.FlowDirection = FlowDirection.TopDown;
            _menuContainer.WrapContents = false;
            _menuContainer.AutoScroll = true;
            _menuContainer.BackColor = ThemeColor;
            _menuContainer.Padding = new Padding(0, 20, 0, 0);
            _sidebar.Controls.Add(_menuContainer);

            // İÇERİK ALANI
            _contentPanel = new Panel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = ContentBg;
            _contentPanel.Padding = new Padding(20);
            this.Controls.Add(_contentPanel);
            _contentPanel.BringToFront();

            // Timer
            _accordionTimer = new Timer { Interval = 15 };
            _accordionTimer.Tick += AccordionTimer_Tick;

            _sidebarTimer = new Timer { Interval = 10 };
            _sidebarTimer.Tick += SidebarTimer_Tick;

            BuildMenuStructure();
            ShowForm(new HomeDashboardForm());
        }

        private Button CreateWindowButton(string text, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Dock = DockStyle.Right;
            btn.Width = 60;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 14);
            btn.ForeColor = Color.Silver;
            btn.Cursor = Cursors.Hand;
            btn.TabStop = false;
            btn.Click += onClick;
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(40, 50, 70); btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = HeaderColor; btn.ForeColor = Color.Silver; };
            return btn;
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == FormWindowState.Maximized) this.WindowState = FormWindowState.Normal;
            else this.WindowState = FormWindowState.Maximized;
        }

        // ============================================================
        // 🎬 ANİMASYONLAR
        // ============================================================
        private void SidebarTimer_Tick(object sender, EventArgs e)
        {
            int step = 40;
            if (_isSidebarOpen)
            {
                _sidebar.Width -= step;
                if (_sidebar.Width <= SidebarMin) { _sidebar.Width = SidebarMin; _isSidebarOpen = false; _sidebarTimer.Stop(); }
            }
            else
            {
                _sidebar.Width += step;
                if (_sidebar.Width >= SidebarMax) { _sidebar.Width = SidebarMax; _isSidebarOpen = true; _sidebarTimer.Stop(); }
            }
        }

        private void AccordionTimer_Tick(object sender, EventArgs e)
        {
            if (_activeSubMenuPanel == null) return;
            int target = (int)_activeSubMenuPanel.Tag;
            int speed = 30;

            if (_isOpeningSubMenu)
            {
                _activeSubMenuPanel.Height += speed;
                if (_activeSubMenuPanel.Height >= target) { _activeSubMenuPanel.Height = target; _accordionTimer.Stop(); }
            }
            else
            {
                _activeSubMenuPanel.Height -= speed;
                if (_activeSubMenuPanel.Height <= 55) { _activeSubMenuPanel.Height = 55; _accordionTimer.Stop(); }
            }
        }

        // ============================================================
        // 📋 MENÜ YAPISI
        // ============================================================
        private void BuildMenuStructure()
        {
            _menuContainer.Controls.Add(CreateSingleMenuButton("📊  Ana Sayfa", (s, e) => ShowFormCached("HomeDashboard", () => new HomeDashboardForm())));
            _menuContainer.Controls.Add(CreateSingleMenuButton("⚡  Hızlı Satış", (s, e) => ShowFormCached("SalesForm", () => new SalesForm())));
            _menuContainer.Controls.Add(CreateAccordionGroup("💰  Satış Yönetimi", new string[] { "Satış Geçmişi", "İade İşlemleri" }));
            _menuContainer.Controls.Add(CreateAccordionGroup("📦  Stok Yönetimi", new string[] { "Ürün Yönetimi", "Stok Dashboard", "Barkod & Etiket" }));
            // Raporlar geçici olarak devre dışı — ileride tek satırı geri açarak aktifleştir.
            // _menuContainer.Controls.Add(CreateSingleMenuButton("📈  Raporlar", (s, e) => OpenReportsWithPassword()));
            _menuContainer.Controls.Add(CreateSingleMenuButton("💸  Giderler", (s, e) => ShowFormCached("ExpenseManager", () => new ExpenseManagerForm())));
            _menuContainer.Controls.Add(CreateSingleMenuButton("⚙️  Ayarlar", (s, e) => OpenSettingsWithPassword()));
        }

        private void OpenReportsWithPassword()
        {
            var settingsService = new SettingsService();
            using (var dialog = new PasswordDialog("🔒 Raporlar", "Raporlara erişim için şifre girin:"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (settingsService.ValidateReportsPassword(dialog.EnteredPassword) ||
                        settingsService.ValidateUserPassword(dialog.EnteredPassword) ||
                        settingsService.ValidateAdminPassword(dialog.EnteredPassword))
                    {
                        ShowFormCached("ReportsForm", () => new ReportsForm());
                    }
                    else
                    {
                        MessageBox.Show("Yanlış şifre!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenSettingsWithPassword()
        {
            var settingsService = new SettingsService();
            using (var dialog = new PasswordDialog("🛡️ Super Admin", "Ayarlara erişim için Super Admin şifresi girin:"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (settingsService.ValidateAdminPassword(dialog.EnteredPassword))
                    {
                        ShowFormCached("SettingsForm", () => new SettingsForm());
                    }
                    else
                    {
                        MessageBox.Show("Yanlış şifre! Sadece Super Admin erişebilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private Button CreateSingleMenuButton(string text, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = "   " + text;
            btn.Height = 55;
            btn.Width = SidebarMax;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = ThemeColor;
            btn.ForeColor = TextColor;
            btn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(10, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Padding(0, 0, 0, 2);
            btn.TabStop = false;
            btn.MouseEnter += (s, e) => { if (btn != _activeMenuButton) btn.BackColor = HoverColor; };
            btn.MouseLeave += (s, e) => { if (btn != _activeMenuButton) btn.BackColor = ThemeColor; };
            btn.Click += (s, e) => { SetActiveButton(btn); onClick?.Invoke(s, e); };
            return btn;
        }

        // Aktif menü butonunu vurgula, eskisinin rengini sıfırla
        private void SetActiveButton(Button btn)
        {
            if (_activeMenuButton != null)
                _activeMenuButton.BackColor = ThemeColor;
            _activeMenuButton = btn;
            btn.BackColor = ActiveColor;
        }

        private Panel CreateAccordionGroup(string title, string[] subItems)
        {
            Panel group = new Panel { Width = SidebarMax, Height = 55, BackColor = ThemeColor, Margin = new Padding(0, 0, 0, 2) };

            Button headerBtn = CreateSingleMenuButton(title, null);
            headerBtn.Dock = DockStyle.Top;

            Label arrow = new Label { Text = "▼", ForeColor = Color.Gray, AutoSize = true, Location = new Point(SidebarMax - 35, 20), BackColor = Color.Transparent };
            headerBtn.Controls.Add(arrow);

            int totalHeight = 55;

            foreach (var item in subItems)
            {
                Button subBtn = new Button();
                subBtn.Text = "        • " + item;
                subBtn.Dock = DockStyle.Top;
                subBtn.Height = 45;
                subBtn.FlatStyle = FlatStyle.Flat;
                subBtn.FlatAppearance.BorderSize = 0;
                subBtn.BackColor = Color.FromArgb(20, 30, 45);
                subBtn.ForeColor = Color.Silver;
                subBtn.Font = new Font("Segoe UI", 11);
                subBtn.TextAlign = ContentAlignment.MiddleLeft;
                subBtn.Cursor = Cursors.Hand;
                subBtn.TabStop = false;

                subBtn.Click += (s, e) =>
                {
                    SetActiveButton(subBtn);
                    if (item == "Ürün Yönetimi") ShowFormCached("ProductForm", () => new ProductForm());
                    else if (item == "Stok Dashboard") ShowFormCached("StockDashboard", () => new StockDashboardForm());
                    else if (item == "Barkod & Etiket") ShowFormCached("BarcodeLabel", () => new BarcodeLabelForm());
                    else if (item == "Satış Geçmişi") ShowFormCached("SalesHistory", () => new SalesHistoryForm());
                    else if (item == "İade İşlemleri") ShowFormCached("ReturnsForm", () => new ReturnsForm());
                    else ShowContent(item);
                };

                subBtn.MouseEnter += (s, e) => subBtn.ForeColor = Color.White;
                subBtn.MouseLeave += (s, e) => subBtn.ForeColor = Color.Silver;

                group.Controls.Add(subBtn);
                totalHeight += 45;
            }

            headerBtn.BringToFront();
            group.Controls.Add(headerBtn);
            group.Tag = totalHeight;

            headerBtn.Click += (s, e) =>
            {
                foreach (Control c in _menuContainer.Controls)
                    if (c is Panel p && p != group && p.Height > 55) p.Height = 55;

                _activeSubMenuPanel = group;
                if (group.Height == 55) { _isOpeningSubMenu = true; arrow.Text = "▲"; }
                else { _isOpeningSubMenu = false; arrow.Text = "▼"; }
                _accordionTimer.Start();
            };
            return group;
        }

        // ============================================================
        // 🔄 FORM YÖNETİMİ (NAVIGATION)
        // ============================================================

        /// <summary>
        /// Cache destekli form gösterimi. Eğer form oluşturulmuşsa tekrar kullanır.
        /// </summary>
        private void ShowFormCached(string key, Func<Form> formCreator)
        {
            bool ilkOlusturma = !_formCache.ContainsKey(key);
            if (ilkOlusturma)
            {
                _formCache[key] = formCreator();
            }

            Form form = _formCache[key];
            ShowForm(form, isCached: true);

            // Form.Shown yalnızca ilk gösterimde tetiklenir. Önbellekten gelen ekran
            // verilerini bir daha yüklemiyordu: sabah açılan Ana Sayfa, gün boyu satış
            // yapılmasına rağmen ₺0 ciro göstermeye devam ediyordu. Tekrar açılışta tazele.
            if (!ilkOlusturma && form is IEkranYenileme yenilenebilir)
            {
                try
                {
                    yenilenebilir.EkraniYenile();
                }
                catch (Exception ex)
                {
                    AppLogger.Yaz($"Ekran yenilenemedi: {key}", ex);
                }
            }
        }

        /// <summary>
        /// Bir formu içerik alanına gömer.
        /// Her geçişte eski formlar düzgün dispose edilir.
        /// </summary>
        private void ShowForm(Form form, bool isCached = false)
        {
            // Eski kontrolleri temizle ve dispose et
            for (int i = _contentPanel.Controls.Count - 1; i >= 0; i--)
            {
                var ctrl = _contentPanel.Controls[i];
                _contentPanel.Controls.RemoveAt(i);
                
                // Eğer ekrandaki form önbellekteyse dispose etme (sadece sakla)
                if (ctrl is Form oldForm && _formCache.ContainsValue(oldForm))
                {
                    oldForm.Hide();
                }
                else
                {
                    ctrl.Dispose();
                }
            }

            // Formu göm ve göster
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(form);
            form.Show();
        }

        private void ShowContent(string title)
        {
            for (int i = _contentPanel.Controls.Count - 1; i >= 0; i--)
            {
                var ctrl = _contentPanel.Controls[i];
                _contentPanel.Controls.RemoveAt(i);
                ctrl.Dispose();
            }

            Label lbl = new Label { Text = title, Font = new Font("Segoe UI", 28, FontStyle.Bold), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(50, 50) };
            _contentPanel.Controls.Add(lbl);
        }

        private void ConfirmExit()
        {
            if (MessageBox.Show("Programı kapatmak istediğinize emin misiniz?", "Çıkış Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Application.Exit();
        }

        // ============================================================
        // 🚫 GLOBAL BARKOD ENGELLEME
        // ============================================================
        private DateTime _lastScanTime = DateTime.Now;
        private string _globalScanBuffer = "";

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool isSalesFormActive = _contentPanel.Controls.Count > 0 && _contentPanel.Controls[0] is SalesForm;
            SalesForm activeSalesForm = isSalesFormActive ? (SalesForm)_contentPanel.Controls[0] : null;

            // 1. Kısayol Tuşları (F2 - F6)
            if (isSalesFormActive && activeSalesForm.ProcessShortcut(keyData))
            {
                return true; // Kısayol işlendi, tuşu yut
            }

            // 2. Barkod Okuyucu Yakalama (Hızlı giriş algılama)
            DateTime now = DateTime.Now;
            // Tuşlar arası süre uzunsa (insan yazıyorsa) tamponu sıfırla
            if ((now - _lastScanTime).TotalMilliseconds > 50)
            {
                _globalScanBuffer = "";
            }
            _lastScanTime = now;

            if (keyData == Keys.Enter)
            {
                // Tamponda yeterince karakter varsa bu bir barkod okuyucu işlemidir.
                if (_globalScanBuffer.Length >= 3)
                {
                    string code = _globalScanBuffer;
                    _globalScanBuffer = "";

                    if (isSalesFormActive)
                    {
                        activeSalesForm.ProcessScannedBarcode(code);
                    }
                    // Barkod okuyucu işlemi algılandı: Enter'ı her halükarda yutuyoruz
                    // ki başka formlardayken istenmeyen butonlara basılmasın.
                    return true; 
                }
                _globalScanBuffer = "";
            }
            else
            {
                char c = KeyToChar(keyData);
                if (c != '\0')
                {
                    _globalScanBuffer += c;
                    // Not: Karakterlerin arama vb. kutularına normal akışta yazılmasını
                    // bozmamak için burada 'return true' DEMİYORUZ. Sadece tampona ekliyoruz.
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static char KeyToChar(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key >= Keys.D0 && key <= Keys.D9) return (char)('0' + (key - Keys.D0));
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return (char)('0' + (key - Keys.NumPad0));
            if (key >= Keys.A && key <= Keys.Z) return (char)('A' + (key - Keys.A));
            if (key == Keys.OemMinus || key == Keys.Subtract) return '-';
            return '\0';
        }

        // ============================================================
        // 🖱️ PENCERE SÜRÜKLEME
        // ============================================================
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")] private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")] private extern static void SendMessage(IntPtr hwnd, int wmsg, int wparam, int lparam);

        private void Header_MouseDown(object sender, MouseEventArgs e) { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); }
    }
}