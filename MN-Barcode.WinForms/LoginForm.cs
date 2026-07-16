using System;
using System.Drawing; // Renkler ve Çizim için şart
using System.Windows.Forms;
using MN_Barcode.Business; // Servisler
using MN_Barcode.Entities; // Kullanıcı Modeli

namespace MN_Barcode.WinForms
{
    public partial class LoginForm : Form
    {
        // Ekrandaki elemanları burada tanımlıyoruz
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnClose;

        public LoginForm()
        {
            InitializeComponent();
            SetupModernUI(); // Kendi tasarım motorumuzu çalıştırıyoruz
        }

        private void SetupModernUI()
        {
            // 1. FORMUN ANA AYARLARI (Çerçevesiz Yapı)
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(750, 450);
            this.BackColor = Color.White;

            // 2. SOL PANEL (LOGO ALANI)
            Panel leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Left;
            leftPanel.Width = 300;
            leftPanel.BackColor = Color.FromArgb(44, 62, 80); // Koyu Lacivert
            this.Controls.Add(leftPanel);

            // Logo Yazısı
            Label lblLogo = new Label();
            lblLogo.Text = "MN\nPOS";
            lblLogo.Font = new Font("Segoe UI", 48, FontStyle.Bold);
            lblLogo.ForeColor = Color.White;
            lblLogo.AutoSize = true;
            lblLogo.Location = new Point(50, 100);
            leftPanel.Controls.Add(lblLogo);

            Label lblSub = new Label();
            lblSub.Text = "Hızlı Satış Sistemi";
            lblSub.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            lblSub.ForeColor = Color.LightGray;
            lblSub.AutoSize = true;
            lblSub.Location = new Point(60, 200);
            leftPanel.Controls.Add(lblSub);

            // 3. SAĞ PANEL (GİRİŞ FORMU)
            // Başlık
            Label lblTitle = new Label();
            lblTitle.Text = "GİRİŞ YAP";
            lblTitle.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(44, 62, 80);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(350, 50);
            this.Controls.Add(lblTitle);

            // Kullanıcı Adı
            Label lblUser = new Label();
            lblUser.Text = "Kullanıcı Adı";
            lblUser.Location = new Point(350, 120);
            lblUser.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblUser.ForeColor = Color.Gray;
            this.Controls.Add(lblUser);

            txtUsername = new TextBox();
            txtUsername.Location = new Point(350, 145);
            txtUsername.Size = new Size(300, 30);
            txtUsername.Font = new Font("Segoe UI", 12);
            txtUsername.Text = "";
            this.Controls.Add(txtUsername);

            // Şifre
            Label lblPass = new Label();
            lblPass.Text = "Şifre";
            lblPass.Location = new Point(350, 200);
            lblPass.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblPass.ForeColor = Color.Gray;
            this.Controls.Add(lblPass);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(350, 225);
            txtPassword.Size = new Size(300, 30);
            txtPassword.Font = new Font("Segoe UI", 12);
            txtPassword.PasswordChar = '●'; // Şifre gizlensin
            txtPassword.Text = "";
            this.Controls.Add(txtPassword);

            // Giriş Butonu
            btnLogin = new Button();
            btnLogin.Text = "SİSTEME GİRİŞ";
            btnLogin.Location = new Point(350, 300);
            btnLogin.Size = new Size(300, 50);
            btnLogin.BackColor = Color.FromArgb(52, 152, 219); // Mavi
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Click += BtnLogin_Click; // Tıklama olayını bağlıyoruz
            this.Controls.Add(btnLogin);

            // Kapatma Butonu (Sağ Üstteki X)
            btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Location = new Point(710, 10);
            btnClose.Size = new Size(30, 30);
            btnClose.BackColor = Color.White;
            btnClose.ForeColor = Color.Red;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = new Font("Arial", 12, FontStyle.Bold);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => Application.Exit(); // Basınca programı kapatır
            this.Controls.Add(btnClose);
        }

        // GİRİŞ BUTONUNA BASILINCA ÇALIŞACAK KOD
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                UserService userService = new UserService();
                var user = userService.Login(txtUsername.Text, txtPassword.Text);

                if (user != null)
                {
                    // 1. Login formunu gizle
                    this.Hide();

                    // 2. Ana formu aç (Kullanıcıyı içeri gönderiyoruz)
                    MainForm main = new MainForm(user);
                    main.ShowDialog(); // Ana form kapanana kadar bekle

                    // 3. Ana form kapanınca uygulamayı komple kapat
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show("Hatalı Kullanıcı Adı veya Şifre!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

    }
}