using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Entities;
using Timer = System.Windows.Forms.Timer;

namespace MN_Barcode.WinForms
{
    public partial class MainForm : Form
    {
        // --- RENK PALETİ ---
        private readonly Color SidebarColor = Color.FromArgb(30, 41, 59);
        private readonly Color SidebarHover = Color.FromArgb(51, 65, 85);
        private readonly Color ActiveColor = Color.FromArgb(37, 99, 235);
        private readonly Color HeaderColor = Color.FromArgb(15, 23, 42);
        private readonly Color ContentBg = Color.FromArgb(241, 245, 249);
        private readonly Color TextColor = Color.FromArgb(226, 232, 240);
        private readonly Color DangerColor = Color.FromArgb(220, 38, 38);

        // BOYUTLAR
        private const int SidebarMax = 260;
        private const int SidebarMin = 70; // Kapalıyken kalacak genişlik
        private bool _isSidebarOpen = true;

        // Global Değişkenler
        private AppUser _currentUser;
        private Panel _contentPanel;
        private FlowLayoutPanel _menuContainer;
        private Panel _sidebar;
        private Label _lblLogo;

        // Timerlar
        private Timer _accordionTimer;
        private Timer _sidebarTimer; // Menü Aç/Kapa için
        private Panel _activeSubMenuPanel;
        private bool _isOpeningSubMenu = false;

        public MainForm(AppUser user)
        {
            _currentUser = user;
            SetupDesign();
        }

        private void SetupDesign()
        {
            this.Text = "MN POS Pro";
            this.Size = new Size(1366, 768);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;

            // 1. ÜST BAR (HEADER)
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 60;
            header.BackColor = HeaderColor;
            this.Controls.Add(header);

            // Logo (Header'da Sol Tarafta)
            _lblLogo = new Label();
            _lblLogo.Text = "📦  MN-POS";
            _lblLogo.ForeColor = Color.White;
            _lblLogo.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            _lblLogo.Dock = DockStyle.Left;
            _lblLogo.TextAlign = ContentAlignment.MiddleLeft;
            _lblLogo.Padding = new Padding(10, 0, 0, 0);
            _lblLogo.AutoSize = true;
            header.Controls.Add(_lblLogo);

            // X (Kapatma) Butonu
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
            btnClose.Click += (s, e) => ConfirmExit();
            header.Controls.Add(btnClose);

            // 2. SIDEBAR (SOL MENÜ)
            _sidebar = new Panel();
            _sidebar.Dock = DockStyle.Left;
            _sidebar.Width = SidebarMax;
            _sidebar.BackColor = SidebarColor;
            this.Controls.Add(_sidebar);

            // --- HAMBURGER BUTONU ALANI ---
            Panel togglePanel = new Panel();
            togglePanel.Dock = DockStyle.Top;
            togglePanel.Height = 60;
            togglePanel.BackColor = Color.FromArgb(20, 30, 45); // Sidebar'dan koyu
            _sidebar.Controls.Add(togglePanel);

            Button btnMenu = new Button();
            btnMenu.Text = "☰   MENÜ"; // İkon + Yazı
            btnMenu.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnMenu.ForeColor = Color.LightGray;
            btnMenu.FlatStyle = FlatStyle.Flat;
            btnMenu.FlatAppearance.BorderSize = 0;
            btnMenu.Dock = DockStyle.Fill;
            btnMenu.Cursor = Cursors.Hand;
            btnMenu.TextAlign = ContentAlignment.MiddleLeft;
            btnMenu.Padding = new Padding(15, 0, 0, 0);
            // TIKLAYINCA MENÜYÜ AÇ/KAPA
            btnMenu.Click += (s, e) => _sidebarTimer.Start();
            togglePanel.Controls.Add(btnMenu);

            // Menü Konteyneri
            _menuContainer = new FlowLayoutPanel();
            _menuContainer.Dock = DockStyle.Fill;
            _menuContainer.FlowDirection = FlowDirection.TopDown;
            _menuContainer.WrapContents = false; // Yan yana dizmesin
            _menuContainer.AutoScroll = true; // Sığmazsa kaydır
            _menuContainer.BackColor = SidebarColor;
            _menuContainer.Padding = new Padding(0, 10, 0, 0);
            _sidebar.Controls.Add(_menuContainer);
            _menuContainer.BringToFront();

            // 3. İÇERİK ALANI
            _contentPanel = new Panel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = ContentBg;
            _contentPanel.Padding = new Padding(20);
            this.Controls.Add(_contentPanel);
            _contentPanel.BringToFront();

            // --- TIMERLAR ---
            _accordionTimer = new Timer { Interval = 15 };
            _accordionTimer.Tick += AccordionTimer_Tick;

            _sidebarTimer = new Timer { Interval = 10 };
            _sidebarTimer.Tick += SidebarTimer_Tick;

            // Menüyü Kur
            BuildMenuStructure();

            ShowContent("Ana Sayfa");
        }

        // --- MENÜYÜ AÇIP KAPATAN MOTOR ---
        private void SidebarTimer_Tick(object sender, EventArgs e)
        {
            if (_isSidebarOpen)
            {
                // Kapatıyoruz...
                _sidebar.Width -= 30;
                if (_sidebar.Width <= SidebarMin)
                {
                    _sidebar.Width = SidebarMin;
                    _isSidebarOpen = false;
                    _sidebarTimer.Stop();
                    _lblLogo.Text = "MN"; // Logo küçülsün

                    // Yazıları Gizle (Sadece İkon Kalsın)
                    // FlowLayout içindeki butonları döngüye sokup textlerini kırpabiliriz
                    // Ama basitlik adına şimdilik böyle kalsın, sadece daralıyor.
                }
            }
            else
            {
                // Açıyoruz...
                _sidebar.Width += 30;
                if (_sidebar.Width >= SidebarMax)
                {
                    _sidebar.Width = SidebarMax;
                    _isSidebarOpen = true;
                    _sidebarTimer.Stop();
                    _lblLogo.Text = "📦  MN-POS"; // Logo büyüsün
                }
            }
        }

        // --- MENÜ YAPISI ---
        private void BuildMenuStructure()
        {
            _menuContainer.Controls.Add(CreateSingleMenuButton("📊  Ana Sayfa", (s, e) => ShowContent("Günlük Özet")));
            _menuContainer.Controls.Add(CreateSingleMenuButton("⚡  Hızlı Satış", (s, e) => ShowForm(new SalesForm())));

            _menuContainer.Controls.Add(CreateAccordionGroup("💰  Satış Yönetimi", new string[] { "Satış Geçmişi", "İade İşlemleri" }));
            _menuContainer.Controls.Add(CreateAccordionGroup("📦  Stok Yönetimi", new string[] { "Ürün Listesi", "Ürün Ekle/Düzenle" }));

            _menuContainer.Controls.Add(CreateSingleMenuButton("📈  Raporlar", (s, e) => ShowContent("Raporlar")));
            _menuContainer.Controls.Add(CreateSingleMenuButton("⚙️  Ayarlar", (s, e) => ShowContent("Ayarlar")));
        }

        // --- YARDIMCI METOTLAR ---
        private Button CreateSingleMenuButton(string text, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = "  " + text;
            btn.Height = 50;
            btn.Width = 260; // Genişlik sabit kalsın, panel daralınca otomatik kesilir
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = SidebarColor;
            btn.ForeColor = TextColor;
            btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(15, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Padding(0, 0, 0, 2);

            btn.MouseEnter += (s, e) => btn.BackColor = SidebarHover;
            btn.MouseLeave += (s, e) => btn.BackColor = SidebarColor;
            btn.Click += (s, e) => { ResetButtonColors(); btn.BackColor = ActiveColor; onClick?.Invoke(s, e); };
            return btn;
        }

        private Panel CreateAccordionGroup(string title, string[] subItems)
        {
            Panel group = new Panel { Width = 260, Height = 50, BackColor = SidebarColor, Margin = new Padding(0, 0, 0, 2) };

            Button headerBtn = CreateSingleMenuButton(title, null);
            headerBtn.Dock = DockStyle.Top;
            Label arrow = new Label { Text = "▼", ForeColor = Color.Gray, AutoSize = true, Location = new Point(230, 20), BackColor = Color.Transparent };
            headerBtn.Controls.Add(arrow);

            int totalHeight = 50;
            foreach (var item in subItems)
            {
                Button subBtn = new Button();
                subBtn.Text = "      • " + item;
                subBtn.Dock = DockStyle.Top;
                subBtn.Height = 40;
                subBtn.FlatStyle = FlatStyle.Flat;
                subBtn.FlatAppearance.BorderSize = 0;
                subBtn.BackColor = Color.FromArgb(20, 30, 45);
                subBtn.ForeColor = Color.Silver;
                subBtn.TextAlign = ContentAlignment.MiddleLeft;
                subBtn.Cursor = Cursors.Hand;
                subBtn.Click += (s, e) => ShowContent(item);
                subBtn.MouseEnter += (s, e) => subBtn.ForeColor = Color.White;
                subBtn.MouseLeave += (s, e) => subBtn.ForeColor = Color.Silver;
                group.Controls.Add(subBtn);
                totalHeight += 40;
            }

            headerBtn.BringToFront();
            group.Controls.Add(headerBtn);
            group.Tag = totalHeight;

            headerBtn.Click += (s, e) => {
                foreach (Control c in _menuContainer.Controls) if (c is Panel p && p != group && p.Height > 50) p.Height = 50;
                _activeSubMenuPanel = group;
                if (group.Height == 50) { _isOpeningSubMenu = true; arrow.Text = "▲"; } else { _isOpeningSubMenu = false; arrow.Text = "▼"; }
                _accordionTimer.Start();
            };
            return group;
        }

        private void AccordionTimer_Tick(object sender, EventArgs e)
        {
            if (_activeSubMenuPanel == null) return;
            int target = (int)_activeSubMenuPanel.Tag;
            int speed = 25;
            if (_isOpeningSubMenu)
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

        private void ShowForm(Form form)
        {
            _contentPanel.Controls.Clear();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(form);
            form.Show();
        }

        private void ShowContent(string title)
        {
            _contentPanel.Controls.Clear();
            Label lbl = new Label { Text = title, Font = new Font("Segoe UI", 28, FontStyle.Bold), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(50, 50) };
            _contentPanel.Controls.Add(lbl);
        }

        private void ResetButtonColors() { }

        private void ConfirmExit()
        {
            if (MessageBox.Show("Çıkmak istediğinize emin misiniz?", "Çıkış", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Application.Exit();
        }
    }
}