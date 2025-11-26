using System;
using System.Collections.Generic; // List için gerekli
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public partial class SalesForm : Form
    {
        // Servisler (Motorlar)
        private ProductService _productService;
        private SaleService _saleService; // YENİ: Satışı kaydetmek için

        // UI Elemanları
        private DataGridView _gridSepet;
        private Label _lblTotal;
        private TextBox _txtBarcode;

        // Veri Tutucular
        private decimal _grandTotal = 0;
        private List<SaleDetail> _cartDetails; // YENİ: Sepetteki ürünleri veritabanına göndermek için tutuyoruz

        public SalesForm()
        {
            _productService = new ProductService();
            _saleService = new SaleService(); // YENİ
            _cartDetails = new List<SaleDetail>(); // YENİ

            SetupSalesUI();
        }

        private void SetupSalesUI()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Dock = DockStyle.Fill;

            // 1. SOL PANEL (SEPET)
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            this.Controls.Add(leftPanel);

            // Barkod Kutusu
            Panel barcodePanel = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White, Padding = new Padding(15) };
            leftPanel.Controls.Add(barcodePanel);

            _txtBarcode = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 24, FontStyle.Bold), PlaceholderText = "Barkod Okutun...", BorderStyle = BorderStyle.None };
            _txtBarcode.KeyDown += Barcode_KeyDown;
            barcodePanel.Controls.Add(_txtBarcode);

            // Araya Boşluk
            Panel spacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
            leftPanel.Controls.Add(spacer);

            // Sepet Grid
            _gridSepet = new DataGridView();
            _gridSepet.Dock = DockStyle.Fill; // EKRANI KAPLASIN
            _gridSepet.BackgroundColor = Color.White;
            _gridSepet.BorderStyle = BorderStyle.None;
            _gridSepet.RowHeadersVisible = false;
            _gridSepet.AllowUserToAddRows = false;
            _gridSepet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _gridSepet.RowTemplate.Height = 50;
            _gridSepet.DefaultCellStyle.Font = new Font("Segoe UI", 14);
            _gridSepet.ColumnHeadersHeight = 50;
            _gridSepet.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            _gridSepet.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            _gridSepet.Columns.Add("Name", "Ürün Adı");
            _gridSepet.Columns.Add("Qty", "Adet");
            _gridSepet.Columns.Add("Price", "Fiyat");
            _gridSepet.Columns.Add("Total", "Tutar");

            leftPanel.Controls.Add(_gridSepet);
            _gridSepet.BringToFront(); // En öne al

            // 2. SAĞ PANEL (ÖDEME)
            Panel rightPanel = new Panel { Dock = DockStyle.Right, Width = 450, BackColor = Color.White, Padding = new Padding(20) };
            this.Controls.Add(rightPanel);

            // Toplam Göstergesi
            Panel totalPanel = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Color.FromArgb(30, 41, 59) };
            rightPanel.Controls.Add(totalPanel);

            Label lblTitle = new Label { Text = "TOPLAM TUTAR", ForeColor = Color.Gray, Font = new Font("Segoe UI", 12), Location = new Point(20, 20), AutoSize = true };
            totalPanel.Controls.Add(lblTitle);

            _lblTotal = new Label { Text = "₺ 0.00", ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Consolas", 48, FontStyle.Bold), Location = new Point(15, 50), AutoSize = true };
            totalPanel.Controls.Add(_lblTotal);

            // Butonlar
            Panel actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 400 };
            rightPanel.Controls.Add(actionPanel);

            actionPanel.Controls.Add(CreateActionButton("İPTAL (F5)", Color.FromArgb(192, 57, 43), (s, e) => ClearCart()));
            actionPanel.Controls.Add(CreateActionButton("BEKLET (F4)", Color.FromArgb(243, 156, 18), (s, e) => { MessageBox.Show("Bekletme özelliği yakında!"); }));
            actionPanel.Controls.Add(CreateActionButton("KREDİ KARTI (F3)", Color.FromArgb(41, 128, 185), (s, e) => CompleteSale("Kredi Kartı")));
            actionPanel.Controls.Add(CreateActionButton("NAKİT (F2)", Color.FromArgb(39, 174, 96), (s, e) => CompleteSale("Nakit")));
        }

        private Button CreateActionButton(string text, Color color, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Dock = DockStyle.Bottom;
            btn.Height = 90;
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;

            Panel spacer = new Panel { Height = 15, Dock = DockStyle.Bottom };
            // Butonu ekleyeceğimiz panel (actionPanel) henüz scope dışı olduğu için
            // Burada parent'a ekleyemiyoruz, o yüzden bu metot sadece butonu dönüyor.
            // Boşluk eklemek için yukarıdaki kullanımda araya panel eklememiz lazım ama 
            // basitlik için şimdilik böyle kalsın, butonlar bitişik durur.
            return btn;
        }

        // --- BARKOD OKUTMA MANTIĞI ---
        private void Barcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string code = _txtBarcode.Text.Trim();
                if (string.IsNullOrEmpty(code)) return;

                // Backend'e Sor
                var product = _productService.GetByBarcode(code);

                if (product != null)
                {
                    AddToCart(product);
                    _txtBarcode.Clear();
                }
                else
                {
                    MessageBox.Show("Ürün Bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtBarcode.SelectAll();
                }
                e.SuppressKeyPress = true; // Bip sesini kes
            }
        }

        private void AddToCart(Product p)
        {
            // 1. Grid'e Görsel Olarak Ekle
            _gridSepet.Rows.Add(p.Name, "1", p.SellingPrice.ToString("C2"), p.SellingPrice.ToString("C2"));

            // 2. Hafızadaki Listeye Ekle (Veritabanı için)
            var detay = new SaleDetail
            {
                ProductId = p.Id,
                Quantity = 1,
                SellingPrice = p.SellingPrice, // O anki satış fiyatı
                TotalPrice = p.SellingPrice * 1
            };
            _cartDetails.Add(detay);

            // 3. Toplamı güncelle
            _grandTotal += p.SellingPrice;
            _lblTotal.Text = _grandTotal.ToString("C2");
        }

        private void CompleteSale(string type)
        {
            if (_grandTotal == 0) return;

            try
            {
                // 1. Satış Fişini Oluştur
                Sale newSale = new Sale
                {
                    TransactionCode = DateTime.Now.ToString("yyyyMMddHHmmss"), // Benzersiz Kod
                    PaymentType = type,
                    TotalAmount = _grandTotal
                };

                // 2. Backend'e Gönder (Kaydet ve Stoktan Düş)
                _saleService.CompleteSale(newSale, _cartDetails);

                // 3. Başarılı
                MessageBox.Show($"Satış Tamamlandı!\nÖdeme: {type}\nTutar: {_grandTotal:C2}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata Oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearCart()
        {
            _gridSepet.Rows.Clear();   // Görseli temizle
            _cartDetails.Clear();      // Hafızayı temizle
            _grandTotal = 0;
            _lblTotal.Text = "₺ 0.00";
            _txtBarcode.Focus();       // İmleci kutuya al
        }
    }
}