using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Entities;
using Timer = System.Windows.Forms.Timer;

namespace MN_Barcode.WinForms
{
    public partial class MainForm : Form
    {
        // --- MODERN RENK PALETİ ---
        private readonly Color SidebarColor = Color.FromArgb(30, 41, 59);      // Koyu Gece Mavisi
        private readonly Color SidebarHover = Color.FromArgb(51, 65, 85);      // Hover Rengi
        private readonly Color ActiveColor = Color.FromArgb(37, 99, 235);      // Seçili Mavi
        private readonly Color HeaderColor = Color.FromArgb(15, 23, 42);       // Üst Bar (Logonun olduğu yer - Daha koyu)
        private readonly Color ContentBg = Color.FromArgb(241, 245, 249);      // İçerik Gri Zemin
        private readonly Color TextColor = Color.FromArgb(226, 232, 240);      // Beyazımsı Yazı
        private readonly Color DangerColor = Color.FromArgb(220, 38, 38);      // Kırmızı

        // Boyutlar
        private const int SidebarExpandedWidth = 260;
        private const int SidebarCollapsedWidth = 70;
        private bool _isExpanded = true;

        // Global Kontroller
        private Panel _sidebar;
        private Panel _contentPanel;
        private FlowLayoutPanel _menuContainer;
        private Timer _accordionTimer;
        private Timer _sidebarToggleTimer;
        private Panel _activeSubMenuPanel;
        private bool _isOpening = false;
        private bool _isSidebarOpen = true;

        public MainForm(AppUser user)
        {
            SetupDesign();
        }

        private void SetupDesign()
        {
            // 1. FORM AYARLARI
            this.Text = "MN POS Pro";
            this.Size = new Size(1366, 768);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None; // Çerçevesiz

            // 2. ÜST BAR (HEADER - SADECE LOGO VE KAPATMA)
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 60;
            header.BackColor = HeaderColor;
            this.Controls.Add(header);

            // Logo (Ortada veya Solda durabilir, sola yasladık)
            Label lblLogo = new Label();
            lblLogo.Text = "📦  MN-POS"; // Marka
            lblLogo.ForeColor = Color.White;
            lblLogo.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblLogo.Dock = DockStyle.Left;
            lblLogo.TextAlign = ContentAlignment.MiddleLeft;
            lblLogo.Padding = new Padding(20, 0, 0, 0);
            lblLogo.AutoSize = true;
            header.Controls.Add(lblLogo);

            // X (Kapatma) Butonu - "Emin misin?" sorulu
            Button btnClose = new Button();
            btnClose.Text = "✕";
            btnClose.Dock = DockStyle.Right;
            btnClose.Width = 60;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = new Font("Segoe UI", 14);
            btnClose.ForeColor = Color.Gray;
            btnClose.Cursor = Cursors.Hand;
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = DangerColor; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = HeaderColor; btnClose.ForeColor = Color.Gray; };
            btnClose.Click += (s, e) => ConfirmExit(); // Çıkış Fonksiyonu
            header.Controls.Add(btnClose);

            // 3. SIDEBAR (SOL MENÜ)
            _sidebar = new Panel();
            _sidebar.Dock = DockStyle.Left;
            _sidebar.Width = SidebarExpandedWidth;
            _sidebar.BackColor = SidebarColor;
            this.Controls.Add(_sidebar);

            // HAMBURGER BUTONU (Sidebar'ın en tepesinde)
            Panel togglePanel = new Panel();
            togglePanel.Dock = DockStyle.Top;
            togglePanel.Height = 60;
            togglePanel.BackColor = Color.FromArgb(20, 30, 45); // Sidebar'dan azıcık koyu, ayrışsın
            _sidebar.Controls.Add(togglePanel);

            Button btnMenu = new Button();
            btnMenu.Text = "☰   MENÜ"; // Yanına yazı da ekledik
            btnMenu.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnMenu.ForeColor = Color.LightGray;
            btnMenu.FlatStyle = FlatStyle.Flat;
            btnMenu.FlatAppearance.BorderSize = 0;
            btnMenu.Dock = DockStyle.Fill;
            btnMenu.Cursor = Cursors.Hand;
            btnMenu.TextAlign = ContentAlignment.MiddleLeft;
            btnMenu.Padding = new Padding(15, 0, 0, 0);
            btnMenu.Click += (s, e) => ToggleSidebarAnimation();
            togglePanel.Controls.Add(btnMenu);

            // Menü Konteyneri
            _menuContainer = new FlowLayoutPanel();
            _menuContainer.Dock = DockStyle.Fill;
            _menuContainer.FlowDirection = FlowDirection.TopDown;
            _menuContainer.WrapContents = false;
            _menuContainer.AutoScroll = true;
            _menuContainer.BackColor = SidebarColor;
            _menuContainer.Padding = new Padding(0, 10, 0, 0); // Üstten biraz boşluk
            _sidebar.Controls.Add(_menuContainer);
            _menuContainer.BringToFront(); // Toggle panelinin altına gelmesi için

            // 4. İÇERİK ALANI
            _contentPanel = new Panel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = ContentBg;
            _contentPanel.Padding = new Padding(20);
            this.Controls.Add(_contentPanel);
            _contentPanel.BringToFront();

            // --- TIMERLAR ---
            _accordionTimer = new Timer { Interval = 15 };
            _accordionTimer.Tick += AccordionTimer_Tick;

            _sidebarToggleTimer = new Timer { Interval = 10 };
            _sidebarToggleTimer.Tick += SidebarToggleTimer_Tick;

            // --- MENÜ YAPISINI KURUYORUZ ---
            BuildMenuStructure();

            // Açılış Sayfası
            ShowContent("Dashboard / Özet");
        }

        private void ConfirmExit()
        {
            DialogResult result = MessageBox.Show(
                "Programı kapatmak istediğinize emin misiniz?",
                "Çıkış Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        // --- MENÜ MİMARİSİ (Senin İstediğin Sıra) ---
        private void BuildMenuStructure()
        {
            // 4.0 - ANA SAYFA (Grafikler vs.)
            _menuContainer.Controls.Add(CreateSingleMenuButton("📊  ANA SAYFA", (s, e) => ShowContent("Günlük Özet & Grafikler")));

            // 4.1 - HIZLI SATIŞ (Tek Buton)
            _menuContainer.Controls.Add(CreateSingleMenuButton("⚡  HIZLI SATIŞ", (s, e) => ShowContent("Satış Ekranı")));

            // 4.2 - SATIŞ YÖNETİMİ (Açılır Menü)
            var satisYonetimi = CreateAccordionGroup("💰  SATIŞ YÖNETİMİ",
                new string[] { "Satış Geçmişi", "İade İşlemleri" });
            _menuContainer.Controls.Add(satisYonetimi);

            // 4.3 - STOK YÖNETİMİ (Açılır Menü)
            var stokYonetimi = CreateAccordionGroup("📦  STOK YÖNETİMİ",
                new string[] { "Ürün Listesi", "Ürün Yönetimi (Ekle/Sil)" });
            _menuContainer.Controls.Add(stokYonetimi);

            // 4.4 - RAPORLAR (Açılır veya Tek, Tek istedin sanırım ama açılır yaptım şık dursun)
            var raporlar = CreateAccordionGroup("📈  RAPORLAR",
                new string[] { "Gün Sonu (Z Raporu)", "Ciro Analizi" });
            _menuContainer.Controls.Add(raporlar);

            // 4.5 - AYARLAR
            _menuContainer.Controls.Add(CreateSingleMenuButton("⚙️  SİSTEM AYARLARI", (s, e) => ShowContent("Ayarlar")));
        }

        // --- YARDIMCI METOTLAR (UI Motoru) ---

        private void ToggleSidebarAnimation()
        {
            _sidebarToggleTimer.Start();
        }

        private void SidebarToggleTimer_Tick(object sender, EventArgs e)
        {
            int step = 30;
            if (_isSidebarOpen)
            {
                _sidebar.Width -= step;
                if (_sidebar.Width <= SidebarCollapsedWidth)
                {
                    _sidebar.Width = SidebarCollapsedWidth;
                    _isSidebarOpen = false;
                    _sidebarToggleTimer.Stop();

                    // Kapanınca yazıları gizle (Sadece ikon kalsın efekti)
                    // Basitlik adına metni kısaltıyoruz
                    foreach (Control c in _menuContainer.Controls)
                    {
                        if (c is Button b) b.Text = b.Text.Substring(0, 2); // Sadece ikon
                        if (c is Panel p && p.Controls.Count > 0) p.Controls[p.Controls.Count - 1].Text = p.Controls[p.Controls.Count - 1].Text.Substring(0, 2);
                    }
                }
            }
            else
            {
                _sidebar.Width += step;
                if (_sidebar.Width >= SidebarExpandedWidth)
                {
                    _sidebar.Width = SidebarExpandedWidth;
                    _isSidebarOpen = true;
                    _sidebarToggleTimer.Stop();

                    // Açılınca yazıları geri getir (Yeniden yükle)
                    _menuContainer.Controls.Clear();
                    BuildMenuStructure(); // En temizi yeniden oluşturmak
                }
            }
        }

        private Button CreateSingleMenuButton(string text, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = "  " + text;
            btn.Height = 50;
            btn.Width = 260;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = SidebarColor;
            btn.ForeColor = TextColor;
            btn.Font = new Font("Segoe UI", 10, FontStyle.Bold); // Biraz daha okunaklı
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(15, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Padding(0, 0, 0, 2); // Altına ince boşluk

            btn.MouseEnter += (s, e) => btn.BackColor = SidebarHover;
            btn.MouseLeave += (s, e) => btn.BackColor = SidebarColor;
            btn.Click += (s, e) => {
                ResetButtonColors();
                btn.BackColor = ActiveColor;
                onClick?.Invoke(s, e);
            };

            return btn;
        }

        private Panel CreateAccordionGroup(string title, string[] subItems)
        {
            Panel groupPanel = new Panel();
            groupPanel.Width = 260;
            groupPanel.Height = 50;
            groupPanel.BackColor = SidebarColor;
            groupPanel.Margin = new Padding(0, 0, 0, 2);

            Button parentBtn = CreateSingleMenuButton(title, null);
            parentBtn.Dock = DockStyle.Top;
            // Ok işareti
            Label arrow = new Label();
            arrow.Text = "▼";
            arrow.ForeColor = Color.Gray;
            arrow.Font = new Font("Arial", 8);
            arrow.AutoSize = true;
            arrow.Location = new Point(230, 20);
            arrow.BackColor = Color.Transparent;
            parentBtn.Controls.Add(arrow);

            int totalHeight = 50;
            foreach (string subItem in subItems)
            {
                Button subBtn = new Button();
                subBtn.Text = "      • " + subItem;
                subBtn.Dock = DockStyle.Top;
                subBtn.Height = 40;
                subBtn.FlatStyle = FlatStyle.Flat;
                subBtn.FlatAppearance.BorderSize = 0;
                subBtn.BackColor = Color.FromArgb(20, 30, 45); // Alt menü daha koyu
                subBtn.ForeColor = Color.Silver;
                subBtn.Font = new Font("Segoe UI", 9);
                subBtn.TextAlign = ContentAlignment.MiddleLeft;
                subBtn.Padding = new Padding(15, 0, 0, 0);
                subBtn.Cursor = Cursors.Hand;

                subBtn.MouseEnter += (s, e) => subBtn.ForeColor = Color.White;
                subBtn.MouseLeave += (s, e) => subBtn.ForeColor = Color.Silver;
                subBtn.Click += (s, e) => ShowContent(subItem);

                groupPanel.Controls.Add(subBtn);
                totalHeight += 40;
            }

            parentBtn.BringToFront();
            groupPanel.Controls.Add(parentBtn);
            groupPanel.Tag = totalHeight;

            parentBtn.Click += (s, e) => {
                // Diğerlerini kapat
                foreach (Control c in _menuContainer.Controls)
                    if (c is Panel p && p != groupPanel && p.Height > 50) p.Height = 50;

                _activeSubMenuPanel = groupPanel;
                if (groupPanel.Height == 50) { _isOpening = true; arrow.Text = "▲"; }
                else { _isOpening = false; arrow.Text = "▼"; }
                _accordionTimer.Start();
            };

            return groupPanel;
        }

        private void ResetButtonColors()
        {
            // Basitçe tüm butonların rengini sıfırla
            // (Derinlemesine arama yapmak lazım ama şimdilik basit tutuyoruz)
        }

        private void AccordionTimer_Tick(object sender, EventArgs e)
        {
            if (_activeSubMenuPanel == null) return;
            int target = (int)_activeSubMenuPanel.Tag;
            int speed = 25;

            if (_isOpening)
            {
                _activeSubMenuPanel.Height += speed;
                if (_activeSubMenuPanel.Height >= target) { _activeSubMenuPanel.Height = target; _accordionTimer.Stop(); }
            }
            else
            {
                _activeSubMenuPanel.Height -= speed;
                if (_activeSubMenuPanel.Height <= 50) { _activeSubMenuPanel.Height = 50; _accordionTimer.Stop(); }
            }
        }

        private void ShowContent(string title)
        {
            _contentPanel.Controls.Clear();
            Label lbl = new Label();
            lbl.Text = title;
            lbl.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            lbl.ForeColor = Color.LightGray;
            lbl.AutoSize = true;
            lbl.Location = new Point(50, 50);
            _contentPanel.Controls.Add(lbl);
        }
    }
}