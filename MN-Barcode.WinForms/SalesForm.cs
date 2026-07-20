using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;
using Microsoft.VisualBasic;

namespace MN_Barcode.WinForms
{
    public partial class SalesForm : Form
    {
        private ProductService _productService;
        private SaleService _saleService;

        // UI
        private DataGridView _gridSepet;
        private Label _lblTotal;
        private TextBox _txtBarcode;
        private Panel _modePanel;
        private Label _lblMode;
        private TableLayoutPanel _quickButtonGrid;

        // Veri
        private decimal _grandTotal = 0;
        private List<SaleDetail> _cartDetails;
        private bool _isReturnMode = false;



        public SalesForm()
        {
            _productService = new ProductService();
            _saleService = new SaleService();
            _cartDetails = new List<SaleDetail>();

            SetupSalesUI();
            LoadQuickButtons();
        }

        private void SetupSalesUI()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Dock = DockStyle.Fill;

            // --- ANA İSKELET (3 SÜTUNLU TABLO) ---
            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 3;
            mainLayout.RowCount = 1;

            // ORANLAR: Sol Geniş (%50), Orta (%25), Sağ (%25)
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            this.Controls.Add(mainLayout);

            // ============================================================
            // 1. SOL PANEL (SEPET)
            // ============================================================
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            mainLayout.Controls.Add(leftPanel, 0, 0);

            // Mod Göstergesi
            _modePanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(46, 204, 113) };
            _lblMode = new Label { Text = "SATIŞ MODU", ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            _modePanel.Controls.Add(_lblMode);
            leftPanel.Controls.Add(_modePanel);

            // Barkod Alanı
            Panel barcodePanel = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White, Padding = new Padding(10) };
            _txtBarcode = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 24, FontStyle.Bold), PlaceholderText = "Barkod...", BorderStyle = BorderStyle.None };
            _txtBarcode.KeyDown += Barcode_KeyDown;
            barcodePanel.Controls.Add(_txtBarcode);
            leftPanel.Controls.Add(barcodePanel);

            leftPanel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });

            // Grid
            _gridSepet = new DataGridView();
            _gridSepet.Dock = DockStyle.Fill;
            _gridSepet.BackgroundColor = Color.White;
            _gridSepet.BorderStyle = BorderStyle.None;
            _gridSepet.RowHeadersVisible = false;
            _gridSepet.AllowUserToAddRows = false;
            _gridSepet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Grid Makyajı
            _gridSepet.RowTemplate.Height = 50;
            _gridSepet.DefaultCellStyle.Font = new Font("Segoe UI", 14);
            _gridSepet.ColumnHeadersHeight = 45;
            _gridSepet.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            _gridSepet.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            _gridSepet.Columns.Add("Name", "Ürün Adı");
            _gridSepet.Columns.Add("Qty", "Adet");
            _gridSepet.Columns.Add("Price", "Fiyat");
            _gridSepet.Columns.Add("Total", "Tutar");

            DataGridViewButtonColumn btnDel = new DataGridViewButtonColumn();
            btnDel.HeaderText = ""; btnDel.Text = "X"; btnDel.UseColumnTextForButtonValue = true; btnDel.Width = 50;
            btnDel.FlatStyle = FlatStyle.Flat; btnDel.DefaultCellStyle.ForeColor = Color.Red;
            _gridSepet.Columns.Add(btnDel);
            _gridSepet.CellClick += GridSepet_CellClick;

            leftPanel.Controls.Add(_gridSepet);
            _gridSepet.BringToFront();


            // ============================================================
            // 2. ORTA PANEL (HIZLI TUŞLAR)
            // ============================================================
            Panel centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            mainLayout.Controls.Add(centerPanel, 1, 0);

            // Başlık
            Label lblQuick = new Label { Text = "HIZLI ÜRÜNLER", Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, Height = 30, TextAlign = ContentAlignment.BottomCenter };
            centerPanel.Controls.Add(lblQuick);

            // Grid Yapısı
            _quickButtonGrid = new TableLayoutPanel();
            _quickButtonGrid.Dock = DockStyle.Fill;
            _quickButtonGrid.ColumnCount = 2;
            _quickButtonGrid.RowCount = 5;
            _quickButtonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _quickButtonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Satır Yükseklikleri Eşit
            for (int i = 0; i < 5; i++) _quickButtonGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

            _quickButtonGrid.Padding = new Padding(5, 0, 0, 0);
            centerPanel.Controls.Add(_quickButtonGrid);


            // ============================================================
            // 3. SAĞ PANEL (ÖDEME)
            // ============================================================
            Panel rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            mainLayout.Controls.Add(rightPanel, 2, 0);

            Panel totalPanel = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.FromArgb(30, 41, 59) };
            Label lblTotalText = new Label { Text = "TOPLAM", ForeColor = Color.Silver, Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 12) };
            _lblTotal = new Label { Text = "₺ 0.00", ForeColor = Color.FromArgb(46, 204, 113), Font = new Font("Consolas", 42, FontStyle.Bold), Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleRight, Height = 80 };
            totalPanel.Controls.Add(lblTotalText);
            totalPanel.Controls.Add(_lblTotal);
            rightPanel.Controls.Add(totalPanel);

            // İşlem Butonları
            TableLayoutPanel actionGrid = new TableLayoutPanel();
            actionGrid.Dock = DockStyle.Fill;
            actionGrid.ColumnCount = 2;
            actionGrid.RowCount = 3;
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            actionGrid.Padding = new Padding(0, 15, 0, 0);

            actionGrid.Controls.Add(CreateGridButton("NAKİT\n(F2)", Color.FromArgb(39, 174, 96), (s, e) => CompleteSale(PaymentType.Nakit)), 0, 0);
            actionGrid.Controls.Add(CreateGridButton("KART\n(F3)", Color.FromArgb(41, 128, 185), (s, e) => CompleteSale(PaymentType.KrediKarti)), 1, 0);
            actionGrid.Controls.Add(CreateGridButton("BEKLET\n(F4)", Color.FromArgb(149, 165, 166), (s, e) => SafeMessageBox("Fiş beklemeye alındı.", "Bilgi")), 0, 1);
            actionGrid.Controls.Add(CreateGridButton("İADE\n(F6)", Color.FromArgb(230, 126, 34), (s, e) => ToggleReturnMode()), 1, 1);

            Button btnCancel = CreateGridButton("SATIŞI İPTAL ET (F5)", Color.FromArgb(192, 57, 43), (s, e) => ClearCart());
            actionGrid.Controls.Add(btnCancel, 0, 2);
            actionGrid.SetColumnSpan(btnCancel, 2);

            rightPanel.Controls.Add(actionGrid);
            actionGrid.BringToFront();
        }

        // =============================================================
        // HIZLI BUTONLAR (DÜZELTİLEN KISIM BURASI)
        // =============================================================
        private void LoadQuickButtons()
        {
            _quickButtonGrid.Controls.Clear();
            _quickButtonGrid.RowStyles.Clear();

            // Grid Ayarı (5 Satır)
            _quickButtonGrid.RowCount = 5;
            for (int i = 0; i < 5; i++) _quickButtonGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

            Color btnColor = Color.White;

            // Hızlı Ürünler (Veritabanından Gerçek Ürün Çekecek)
            AddQuickButtonToGrid("EKMEK\n8.00 ₺", 8.00m, btnColor);
            AddQuickButtonToGrid("SU (0.5L)\n5.00 ₺", 5.00m, btnColor);
            AddQuickButtonToGrid("ÇAY\n10.00 ₺", 10.00m, btnColor);
            AddQuickButtonToGrid("SİMİT\n12.50 ₺", 12.50m, btnColor);
            AddQuickButtonToGrid("POŞET\n0.25 ₺", 0.25m, btnColor);

            AddQuickButtonToGrid("10 TL Ürün", 10.00m, btnColor);
            AddQuickButtonToGrid("20 TL Ürün", 20.00m, btnColor);
            AddQuickButtonToGrid("50 TL Ürün", 50.00m, btnColor);

            // MANUEL TUTAR (En Alt - Yatay)
            Button btnManual = CreateBaseButton("⌨️ TUTAR GİR", Color.FromArgb(52, 73, 94));
            btnManual.ForeColor = Color.White;
            btnManual.Click += (s, e) => AskManualPrice();

            _quickButtonGrid.Controls.Add(btnManual, 0, 4);
            _quickButtonGrid.SetColumnSpan(btnManual, 2);
        }

        private void AddQuickButtonToGrid(string text, decimal price, Color color)
        {
            Button btn = CreateBaseButton(text, color);
            btn.ForeColor = Color.FromArgb(64, 64, 64);
            btn.FlatAppearance.BorderColor = Color.Silver;
            btn.FlatAppearance.BorderSize = 1;

            btn.Click += (s, e) => {
                // 1. İsimden Fiyatı Temizle: "EKMEK\n8.00" -> "EKMEK"
                string cleanName = text.Split('\n')[0].Trim();
                // 2. Barkod Üret: "QUICK-EKMEK"
                string barcode = "QUICK-" + cleanName.ToUpper().Replace(" ", "").Replace("(", "").Replace(")", "").Replace(".", "");

                // 3. MOTORA SOR (Yoksa oluşturur, Varsa getirir)
                // ARTIK ID=0 OLAN SAHTE ÜRÜN DEĞİL, DB'DEN GERÇEK ÜRÜN GELİYOR.
                var realProduct = _productService.GetOrCreateQuickProduct(cleanName, barcode, price);

                // 4. Butondaki fiyat güncel mi? Ürün Yönetimi'nden fiyat değiştirilmişse
                //    buton eski fiyatı gösterip yeni fiyatı tahsil ediyordu — müşteriyle
                //    fiyat tartışması çıkarır. Yazıyı gerçek fiyata eşitle.
                bool fiyatliButon = text.Contains('\n');
                if (fiyatliButon && realProduct != null && realProduct.SellingPrice != price)
                    btn.Text = $"{cleanName}\n{realProduct.SellingPrice:0.00} ₺";

                // 5. Sepete At
                AddToCart(realProduct);
            };

            _quickButtonGrid.Controls.Add(btn);
        }

        private void AskManualPrice()
        {
            string input = Interaction.InputBox("Tutarı Giriniz:", "Serbest Tutar", "0");
            if (string.IsNullOrWhiteSpace(input)) return;

            // Kültürden bağımsız çözümleme şart: tr-TR'de decimal.TryParse("12.50")
            // 1250 döndürüyordu ve kasa yanlış tutar tahsil ediyordu.
            if (!TutarParser.TryParse(input, out decimal price) || price <= 0)
            {
                SafeMessageBox("Geçerli bir tutar girin. Örnek: 12,50", "Hatalı tutar", isError: true);
                return;
            }

            {
                // Manuel satış için "MANUAL" barkodlu ürünü çağır/oluştur
                var genericProduct = _productService.GetOrCreateQuickProduct("Serbest Ürün", "MANUAL", 0);

                // Fiyatı o anlık değiştir (DB'yi bozmadan)
                genericProduct.SellingPrice = price;

                AddToCart(genericProduct);
            }
        }

        // =============================================================
        // DİĞER YARDIMCILAR
        // =============================================================

        private void SafeMessageBox(string msg, string title, bool isError = false)
        {
            try { MessageBox.Show(msg, title, MessageBoxButtons.OK, isError ? MessageBoxIcon.Error : MessageBoxIcon.Information); }
            catch { }
        }

        private Button CreateBaseButton(string text, Color color)
        {
            return new Button { Text = text, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold), Dock = DockStyle.Fill, Margin = new Padding(3), Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 } };
        }

        private Button CreateGridButton(string text, Color color, EventHandler onClick)
        {
            Button btn = CreateBaseButton(text, color);
            btn.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btn.Click += onClick;
            return btn;
        }

        private void GridSepet_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 4)
            {
                var removedItem = _cartDetails[e.RowIndex];
                _grandTotal -= removedItem.TotalPrice;
                _cartDetails.RemoveAt(e.RowIndex);
                _gridSepet.Rows.RemoveAt(e.RowIndex);
                _lblTotal.Text = _grandTotal.ToString("C2");
                _txtBarcode.Focus();
            }
        }

        private void ToggleReturnMode()
        {
            _isReturnMode = !_isReturnMode;
            if (_isReturnMode) { _modePanel.BackColor = Color.FromArgb(230, 126, 34); _lblMode.Text = "⚠️ İADE MODU"; }
            else { _modePanel.BackColor = Color.FromArgb(46, 204, 113); _lblMode.Text = "SATIŞ MODU"; }
            ClearCart();
        }

        // Barkod kutusuna MANUEL yazıp Enter'a basıldığında çalışır.
        // (Hızlı okuyucu girişi ise ayrıca ProcessCmdKey ile global yakalanır.)
        private void Barcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string code = _txtBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    ProcessScannedBarcode(code); // Ortak işleme metodu
                    e.SuppressKeyPress = true;    // "ding" sesini engelle
                }
            }
        }

        // ============================================================
        //  BARKOD İŞLEME (Tek merkez)
        //  Hem manuel girişten hem de okuyucudan gelen barkodu işler:
        //  ürünü bul, sepete ekle; bulamazsa uyar.
        // ============================================================
        public void ProcessScannedBarcode(string code)
        {
            code = (code ?? "").Trim();
            if (string.IsNullOrEmpty(code)) return;

            var product = _productService.GetByBarcode(code);
            if (product != null)
            {
                AddToCart(product);
                _txtBarcode.Clear();
            }
            else
            {
                // Ürün bulunamadı, hızlı ekleme formunu aç.
                // using: eskiden form hiç bırakılmıyordu; bilinmeyen her barkod
                // okutmasında bir Form (ve içindeki tüm kontroller) sızıyordu.
                var newProduct = new Product { Barcode = code };
                using (var addForm = new ProductEditForm(newProduct))
                {
                    if (addForm.ShowDialog(this.FindForm()) == DialogResult.OK)
                    {
                        // Eklendikten sonra sepete at
                        var savedProduct = _productService.GetByBarcode(code);
                        if (savedProduct != null)
                        {
                            AddToCart(savedProduct);
                            _txtBarcode.Clear();
                        }
                    }
                    else
                    {
                        _txtBarcode.SelectAll();
                    }
                }
            }
            _txtBarcode.Focus(); // İşlem sonrası odağı tekrar barkod kutusuna al
        }

        // ============================================================
        //  FONKSİYON KISAYOLLARI (F2-F6)
        //  MainForm üzerinden yönlendirilir.
        // ============================================================
        public bool ProcessShortcut(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F2: CompleteSale(PaymentType.Nakit); return true;
                case Keys.F3: CompleteSale(PaymentType.KrediKarti); return true;
                case Keys.F4: SafeMessageBox("Fiş beklemeye alındı.", "Bilgi"); return true;
                case Keys.F5: ClearCart(); return true;
                case Keys.F6: ToggleReturnMode(); return true;
            }
            return false;
        }

        // Ekran (gömülü form) görünür olduğunda odağı barkod kutusuna al.
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && _txtBarcode != null)
                _txtBarcode.Focus();
        }

        private void AddToCart(Product p)
        {
            // Hızlı tuş veya barkod yolu ürün bulamamış olabilir; null gelirse
            // aşağıdaki p.Name erişimi uygulamayı çökertirdi.
            if (p == null)
            {
                SafeMessageBox("Ürün bilgisi alınamadı. Tekrar deneyin.", "Uyarı", true);
                return;
            }

            int qty = _isReturnMode ? -1 : 1;
            decimal price = _isReturnMode ? p.SellingPrice * -1 : p.SellingPrice;
            _gridSepet.Rows.Add(p.Name, qty, p.SellingPrice.ToString("C2"), price.ToString("C2"));

            //Artık ID'si olan gerçek bir Product geliyor.
            _cartDetails.Add(new SaleDetail { ProductId = p.Id, Quantity = qty, SellingPrice = p.SellingPrice, TotalPrice = price });
            _grandTotal += price;
            _lblTotal.Text = _grandTotal.ToString("C2");
        }

        private void CompleteSale(PaymentType paymentType)
        {
            // Sepet boşken fiş kesilmemeli. Eskiden iade modunda bu kontrol
            // atlanıyordu ve boş bir iade fişi (0 TL, satırsız) kaydedilebiliyordu.
            if (_cartDetails.Count == 0)
            {
                SafeMessageBox("Sepet boş. Önce ürün ekleyin.", "Uyarı", true);
                return;
            }

            // İade, satılmış bir ürün için yapılmalıdır. Doğrulama olmadığında
            // hiç satılmamış bir ürün defalarca "iade" edilip stok şişirilebiliyordu.
            if (_isReturnMode && !IadeOnayiAl()) return;

            try
            {
                Sale newSale = new Sale
                {
                    TransactionCode = FisNumarasiUret(),
                    PaymentType     = _isReturnMode ? PaymentType.Iade : paymentType,
                    SaleType        = _isReturnMode ? SaleType.Iade : SaleType.Satis,
                    TotalAmount     = _grandTotal
                };
                _saleService.CompleteSale(newSale, _cartDetails);
                SafeMessageBox($"İşlem Tamamlandı.\nFiş No: {newSale.TransactionCode}\nTutar: {_grandTotal:C2}", "Bilgi");
                ClearCart();
            }
            catch (InsufficientStockException ex)
            {
                // Stok yetersiz — kullanıcıya ne yapacağını söyleyen özel mesaj.
                SafeMessageBox(ex.Message, "Yetersiz stok", true);
            }
            catch (Exception ex)
            {
                string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                AppLogger.Yaz("Satış kaydedilemedi", ex);
                SafeMessageBox("İşlem kaydedilemedi:\n" + err, "Hata", true);
            }
        }

        /// <summary>
        /// İade edilmek istenen ürünlerin gerçekten satılmış olup olmadığını kontrol eder.
        ///
        /// Satılmamış ürün için iade yapmak stoğu haksız yere artırır ve ciroyu düşürür.
        /// Hiç satış kaydı olmayan ürünler varsa kasiyere sorulur; yine de devam etmek
        /// isteyebilir (elden satış, devir stoğu gibi durumlar), ama artık bunu bilerek yapar.
        /// </summary>
        private bool IadeOnayiAl()
        {
            try
            {
                var satilmamis = new System.Collections.Generic.List<string>();

                foreach (var grup in _cartDetails.GroupBy(d => d.ProductId))
                {
                    if (_saleService.UrunSatildiMi(grup.Key)) continue;

                    var urun = _productService.GetById(grup.Key);
                    satilmamis.Add(urun?.Name ?? $"#{grup.Key}");
                }

                if (satilmamis.Count == 0) return true;

                var cevap = MessageBox.Show(
                    "Aşağıdaki ürünlerin satış kaydı bulunamadı:\n\n" +
                    string.Join("\n", satilmamis.Select(a => "  • " + a)) +
                    "\n\nYine de iade edilsin mi?\n" +
                    "(İade edilirse bu ürünlerin stoğu artacaktır.)",
                    "İade doğrulaması",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                return cevap == DialogResult.Yes;
            }
            catch (Exception ex)
            {
                // Doğrulama yapılamıyorsa iadeyi engellemeyelim; sadece kayda geçir.
                AppLogger.Yaz("İade doğrulaması yapılamadı", ex);
                return true;
            }
        }

        /// <summary>
        /// Benzersiz fiş numarası üretir.
        ///
        /// Eskiden yalnızca "yyyyMMddHHmmss" kullanılıyordu — saniye çözünürlüğü.
        /// Aynı saniyede iki satış aynı numarayı alıyor, Satış Geçmişi ve İade
        /// ekranları fişleri TransactionCode'a göre grupladığı için iki ayrı satış
        /// tek fiş gibi görünüyordu. İade takibi de belirsizleşiyordu.
        /// Sona milisaniye + sayaç eklendi.
        /// </summary>
        private static int _fisSayaci;
        private static string FisNumarasiUret()
        {
            int sira = System.Threading.Interlocked.Increment(ref _fisSayaci) % 1000;
            return DateTime.Now.ToString("yyyyMMddHHmmssfff") + sira.ToString("000");
        }

        private void ClearCart()
        {
            _gridSepet.Rows.Clear(); _cartDetails.Clear(); _grandTotal = 0; _lblTotal.Text = "₺ 0.00"; _txtBarcode.Focus();
        }
    }
}