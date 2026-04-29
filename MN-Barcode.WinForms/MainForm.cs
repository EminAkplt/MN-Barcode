using System;
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
        private readonly Color ThemeColor = Color.FromArgb(30, 41, 59);        // Ana Koyu Renk (Sidebar ve Header Arkaplanı)
        private readonly Color HoverColor = Color.FromArgb(51, 65, 85);        // Mouse üzerine gelince (Hover)
        private readonly Color ActiveColor = Color.FromArgb(37, 99, 235);      // Seçili menü rengi (Mavi)
        private readonly Color HeaderColor = Color.FromArgb(20, 30, 45);       // Üst Bar rengi (Biraz daha koyu)
        private readonly Color ContentBg = Color.FromArgb(241, 245, 249);      // İçerik alanı gri zemin
        private readonly Color TextColor = Color.FromArgb(226, 232, 240);      // Menü yazı rengi (Kırık Beyaz)
        private readonly Color DangerColor = Color.FromArgb(220, 38, 38);      // Kırmızı (Çıkış butonu için)

        // --- BOYUTLAR (DIMENSIONS) ---
        private const int HeaderHeight = 75; // Üst şeridin yüksekliği
        private const int SidebarMax = 280;  // Menü açıkken genişliği
        private const int SidebarMin = 0;    // Menü kapalıyken genişliği (0 = Tamamen gizli)

        // --- DURUM KONTROLLERİ (STATE FLAGS) ---
        private bool _isSidebarOpen = true; // Menü şu an açık mı?

        // ============================================================
        // 🔧 GLOBAL DEĞİŞKENLER (GLOBAL VARIABLES)
        // ============================================================
        private AppUser _currentUser;           // Giriş yapan kullanıcı bilgisi
        private Panel _sidebar;                 // Sol Menü Paneli
        private Panel _contentPanel;            // Sayfaların yükleneceği orta alan
        private FlowLayoutPanel _menuContainer; // Menü butonlarını tutan liste

        // --- ANİMASYON ZAMANLAYICILARI (TIMERS) ---
        private Timer _accordionTimer;      // Alt menülerin (Accordion) açılıp kapanması için
        private Timer _sidebarTimer;        // Sol menünün (Sidebar) komple açılıp kapanması için

        private Panel _activeSubMenuPanel;  // O an işlem yapılan alt menü paneli
        private bool _isOpeningSubMenu = false; // Alt menü açılıyor mu, kapanıyor mu?

        // ============================================================
        // 🏁 BAŞLANGIÇ (CONSTRUCTOR)
        // ============================================================
        public MainForm(AppUser user)
        {
            _currentUser = user;
            SetupLayout(); // Tasarımı kod ile çiziyoruz (Designer kullanmıyoruz)
        }

        // ============================================================
        // 🏗️ ANA TASARIM MOTORU (MAIN UI ENGINE)
        // ============================================================
        private void SetupLayout()
        {
            // 1. FORM AYARLARI
            this.Text = "MN POS Pro";
            this.Size = new Size(1366, 768);
            this.StartPosition = FormStartPosition.CenterScreen; // Ortada başla
            this.FormBorderStyle = FormBorderStyle.None;         // Çerçevesiz (Modern Görünüm)
            this.WindowState = FormWindowState.Maximized;        // Tam Ekran
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea; // Taskbar'ın üstüne binmesin

            // -------------------------------------------------------------
            // 2. ÜST BAR (HEADER) 
            // Form'a ilk eklenen kontrol, Dock mantığı gereği en üstte kalır.
            // -------------------------------------------------------------
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = HeaderHeight;
            header.BackColor = HeaderColor;
            header.MouseDown += Header_MouseDown; // Sürükleme özelliğini bağla

            // Altına İnce Çizgi (Ayırıcı)
            Panel borderLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(71, 85, 105) };
            header.Controls.Add(borderLine);

            this.Controls.Add(header); // !!! KRİTİK NOKTA: Header ilk eklenmeli

            // --- HEADER SOL TARAFI (SOLA YASLI) ---

           

            // 2. Logo İkonu (Kutu)
            Label lblIcon = new Label();
            lblIcon.Text = "📦";
            lblIcon.Font = new Font("Segoe UI", 28);
            lblIcon.ForeColor = ActiveColor; // Mavi Logo
            lblIcon.Dock = DockStyle.Left;
            lblIcon.AutoSize = true;
            lblIcon.TextAlign = ContentAlignment.MiddleCenter;
            lblIcon.Padding = new Padding(10, 8, 0, 0); // Hizalama
            header.Controls.Add(lblIcon);

            // 3. Marka Yazısı
            Label lblBrand = new Label();
            lblBrand.Text = "MN-Barcode";
            lblBrand.ForeColor = Color.White;
            lblBrand.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblBrand.Dock = DockStyle.Left;
            lblBrand.TextAlign = ContentAlignment.MiddleLeft;
            lblBrand.AutoSize = true;
            lblBrand.Padding = new Padding(5, 5, 0, 0);
            header.Controls.Add(lblBrand);

            // 1. Hamburger Butonu (En Sol)
            Button btnMenu = new Button();
            btnMenu.Text = "☰";
            btnMenu.Font = new Font("Segoe UI", 26);
            btnMenu.FlatStyle = FlatStyle.Flat;
            btnMenu.FlatAppearance.BorderSize = 0;
            btnMenu.ForeColor = Color.White;
            btnMenu.Size = new Size(HeaderHeight, HeaderHeight); // Kare buton
            btnMenu.Dock = DockStyle.Left;   // Sola Yasla
            btnMenu.Cursor = Cursors.Hand;
            btnMenu.Click += (s, e) => _sidebarTimer.Start(); // Tıklayınca menüyü aç/kapa
            header.Controls.Add(btnMenu);

            // --- HEADER SAĞ TARAFI (PENCERE KONTROLLERİ) ---
            


            // 3. KÜÇÜLT ( - ) -> Karenin Soluna gider
            Button btnMin = CreateWindowButton("—", (s, e) => this.WindowState = FormWindowState.Minimized);
            header.Controls.Add(btnMin);

           

            // 2. TAM EKRAN ( Kare ) -> X'in Soluna gider
            Button btnMax = CreateWindowButton("⬜", (s, e) => ToggleMaximize());
            btnMax.Font = new Font("Segoe UI", 12);
            header.Controls.Add(btnMax);

            // 1. KAPAT (X) -> En Sağa gider
            Button btnClose = CreateWindowButton("✕", (s, e) => ConfirmExit());
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = DangerColor; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = HeaderColor; btnClose.ForeColor = Color.Silver; };
            header.Controls.Add(btnClose);




            // -------------------------------------------------------------
            // 3. SIDEBAR (SOL MENÜ)
            // Header'dan sonra eklediğimiz için, Header'ın altında kalan alana yerleşir.
            // -------------------------------------------------------------
            _sidebar = new Panel();
            _sidebar.Dock = DockStyle.Left;
            _sidebar.Width = SidebarMax;
            _sidebar.BackColor = ThemeColor;
            this.Controls.Add(_sidebar);

            // Menü Listesi (Butonları Alt Alta Dizen Panel)
            _menuContainer = new FlowLayoutPanel();
            _menuContainer.Dock = DockStyle.Fill;
            _menuContainer.FlowDirection = FlowDirection.TopDown; // Dikey dizilim
            _menuContainer.WrapContents = false; // Yan yana taşma yapmasın
            _menuContainer.AutoScroll = true;    // Menü uzarsa scroll çıksın
            _menuContainer.BackColor = ThemeColor;
            _menuContainer.Padding = new Padding(0, 20, 0, 0); // Üstten boşluk
            _sidebar.Controls.Add(_menuContainer);


            // 4. İÇERİK ALANI (CONTENT) - Geriye Kalan Boşluk
            _contentPanel = new Panel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = ContentBg;
            _contentPanel.Padding = new Padding(20);
            this.Controls.Add(_contentPanel);
            _contentPanel.BringToFront(); // Öne getir

            // --- TIMER AYARLARI ---
            _accordionTimer = new Timer { Interval = 15 };
            _accordionTimer.Tick += AccordionTimer_Tick;

            _sidebarTimer = new Timer { Interval = 10 };
            _sidebarTimer.Tick += SidebarTimer_Tick;

            // Menüyü Kur
            BuildMenuStructure();

            // Açılış Sayfası - Yeni Ana Sayfa Dashboard
            ShowForm(new HomeDashboardForm());
        }

        // --- PENCERE BUTONU OLUŞTURUCU (Helper) ---
        private Button CreateWindowButton(string text, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Dock = DockStyle.Right;
            btn.Width = 60; // Geniş butonlar (Web tarzı)
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 14);
            btn.ForeColor = Color.Silver;
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            // Standart Hover Efekti
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(40, 50, 70); btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = HeaderColor; btn.ForeColor = Color.Silver; };
            return btn;
        }

        // --- TAM EKRAN / KÜÇÜLTME ---
        private void ToggleMaximize()
        {
            if (this.WindowState == FormWindowState.Maximized) this.WindowState = FormWindowState.Normal;
            else this.WindowState = FormWindowState.Maximized;
        }

        // ============================================================
        // 🎬 MENÜ ANİMASYONLARI (ANIMATIONS)
        // ============================================================

        // Sidebar Açma/Kapama (Sola Kayma Efekti)
        private void SidebarTimer_Tick(object sender, EventArgs e)
        {
            int step = 40; // Hız
            if (_isSidebarOpen)
            {
                // KAPATIYORUZ -> Hedef 0px
                _sidebar.Width -= step;
                if (_sidebar.Width <= SidebarMin) { _sidebar.Width = SidebarMin; _isSidebarOpen = false; _sidebarTimer.Stop(); }
            }
            else
            {
                // AÇIYORUZ -> Hedef 280px
                _sidebar.Width += step;
                if (_sidebar.Width >= SidebarMax) { _sidebar.Width = SidebarMax; _isSidebarOpen = true; _sidebarTimer.Stop(); }
            }
        }

        // ============================================================
        // 📋 MENÜ YAPISI (MENU STRUCTURE)
        // ============================================================
        private void BuildMenuStructure()
        {
            // 1. ANA SAYFA - Yeni Home Dashboard
            _menuContainer.Controls.Add(CreateSingleMenuButton("📊  Ana Sayfa", (s, e) => ShowForm(new HomeDashboardForm())));

            // 2. HIZLI SATIŞ
            _menuContainer.Controls.Add(CreateSingleMenuButton("⚡  Hızlı Satış", (s, e) => ShowForm(new SalesForm())));

            // 3. SATIŞ YÖNETİMİ (Alt Menülü)
            _menuContainer.Controls.Add(CreateAccordionGroup("💰  Satış Yönetimi", new string[] { "Satış Geçmişi", "İade İşlemleri" }));

            // 4. STOK YÖNETİMİ (Alt Menülü)
            _menuContainer.Controls.Add(CreateAccordionGroup("📦  Stok Yönetimi", new string[] { "Ürün Yönetimi", "Stok Dashboard" }));

            // 5. RAPORLAR - Şifre korumalı
            _menuContainer.Controls.Add(CreateSingleMenuButton("📈  Raporlar", (s, e) => OpenReportsWithPassword()));

            // 6. GİDERLER
            _menuContainer.Controls.Add(CreateSingleMenuButton("💸  Giderler", (s, e) => ShowForm(new ExpenseManagerForm())));

            // 7. AYARLAR - Super Admin şifresi gerekli
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
                        ShowForm(new ReportsForm());
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
                        ShowForm(new SettingsForm());
                    }
                    else
                    {
                        MessageBox.Show("Yanlış şifre! Sadece Super Admin erişebilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // --- TEKLİ MENÜ BUTONU OLUŞTURUCU ---
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
            btn.Font = new Font("Segoe UI", 12, FontStyle.Bold); // Büyük Font
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(10, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Padding(0, 0, 0, 2); // Alt Boşluk
            // Hover Efekti
            btn.MouseEnter += (s, e) => btn.BackColor = HoverColor;
            btn.MouseLeave += (s, e) => btn.BackColor = ThemeColor;
            // Tıklama Efekti
            btn.Click += (s, e) => { ResetButtonColors(); btn.BackColor = ActiveColor; onClick?.Invoke(s, e); };
            return btn;
        }

        // --- AÇILIR MENÜ GRUBU OLUŞTURUCU (ACCORDION) ---
        private Panel CreateAccordionGroup(string title, string[] subItems)
        {
            Panel group = new Panel { Width = SidebarMax, Height = 55, BackColor = ThemeColor, Margin = new Padding(0, 0, 0, 2) };

            Button headerBtn = CreateSingleMenuButton(title, null);
            headerBtn.Dock = DockStyle.Top;

            // Ok İşareti (▼)
            Label arrow = new Label { Text = "▼", ForeColor = Color.Gray, AutoSize = true, Location = new Point(SidebarMax - 35, 20), BackColor = Color.Transparent };
            headerBtn.Controls.Add(arrow);

            int totalHeight = 55; // Başlangıç yüksekliği

            // Alt Menüleri Ekle
            foreach (var item in subItems)
            {
                Button subBtn = new Button();
                subBtn.Text = "        • " + item; // Girintili
                subBtn.Dock = DockStyle.Top;
                subBtn.Height = 45;
                subBtn.FlatStyle = FlatStyle.Flat;
                subBtn.FlatAppearance.BorderSize = 0;
                subBtn.BackColor = Color.FromArgb(20, 30, 45); // Alt menü daha koyu
                subBtn.ForeColor = Color.Silver;
                subBtn.Font = new Font("Segoe UI", 11); // Alt menü fontu
                subBtn.TextAlign = ContentAlignment.MiddleLeft;
                subBtn.Cursor = Cursors.Hand;

                // >>> KRİTİK GÜNCELLEME BURASI <<<
                // Hangi butona basıldıysa ilgili formu aç
                subBtn.Click += (s, e) =>
                {
                    if (item == "Ürün Yönetimi") ShowForm(new ProductForm());
                    else if (item == "Stok Dashboard") ShowForm(new StockDashboardForm());
                    else if (item == "Satış Geçmişi") ShowForm(new SalesHistoryForm());
                    else if (item == "İade İşlemleri") ShowForm(new ReturnsForm());
                    else ShowContent(item);
                };

                subBtn.MouseEnter += (s, e) => subBtn.ForeColor = Color.White;
                subBtn.MouseLeave += (s, e) => subBtn.ForeColor = Color.Silver;

                group.Controls.Add(subBtn);
                totalHeight += 45;
            }

            headerBtn.BringToFront();
            group.Controls.Add(headerBtn);
            group.Tag = totalHeight; // Hedef yüksekliği sakla

            // Tıklayınca Aç/Kapa
            headerBtn.Click += (s, e) => {
                // Diğer açık menüleri kapat
                foreach (Control c in _menuContainer.Controls) if (c is Panel p && p != group && p.Height > 55) p.Height = 55;

                _activeSubMenuPanel = group;
                if (group.Height == 55) { _isOpeningSubMenu = true; arrow.Text = "▲"; } else { _isOpeningSubMenu = false; arrow.Text = "▼"; }
                _accordionTimer.Start();
            };
            return group;
        }

        // --- ALT MENÜ ANİMASYONU ---
        private void AccordionTimer_Tick(object sender, EventArgs e)
        {
            if (_activeSubMenuPanel == null) return;
            int target = (int)_activeSubMenuPanel.Tag;
            int speed = 30; // Animasyon Hızı

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
        // 🔄 FORM YÖNETİMİ (NAVIGATION)
        // ============================================================

        // Bir Formu (SalesForm vb.) içeri gömer
        private void ShowForm(Form form)
        {
            _contentPanel.Controls.Clear();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(form);
            form.Show();
        }

        // Geçici İçerik Gösterici (Yapım Aşamasında Olanlar İçin)
        private void ShowContent(string title)
        {
            _contentPanel.Controls.Clear();
            Label lbl = new Label { Text = title, Font = new Font("Segoe UI", 28, FontStyle.Bold), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(50, 50) };
            _contentPanel.Controls.Add(lbl);
        }

        private void ResetButtonColors() { } // Renk sıfırlama (Basitlik için boş bırakıldı)

        private void ConfirmExit()
        {
            if (MessageBox.Show("Programı kapatmak istediğinize emin misiniz?", "Çıkış Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Application.Exit();
        }

        // ============================================================
        // 🖱️ PENCERE SÜRÜKLEME (DRAG WINDOW)
        // ============================================================
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")] private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")] private extern static void SendMessage(IntPtr hwnd, int wmsg, int wparam, int lparam);

        // Header'a basılı tutup sürükleyince pencereyi hareket ettir
        private void Header_MouseDown(object sender, MouseEventArgs e) { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); }
    }
}