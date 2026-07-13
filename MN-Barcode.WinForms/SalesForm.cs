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

        // --- BARKOD OKUYUCU (Global yakalama) ---
        // USB barkod okuyucular "klavye taklidi" yapar: barkodu hızlıca yazıp Enter'a basar.
        // Aşağıdaki tampon (buffer) ve zaman damgası, gelen tuşların İNSAN mı yoksa
        // OKUYUCU mu olduğunu ayırt etmek için kullanılır (okuyucu çok hızlıdır).
        private string _scanBuffer = "";                    // Okuyucudan gelen karakterler burada birikir
        private DateTime _lastScanKey = DateTime.MinValue;  // Son tuşun zamanı
        private const int ScanGapMs = 100;                  // İki tuş arası bu süreden uzunsa "insan yazıyor" kabul edilir
        private const int MinScanLength = 3;                // Geçerli barkod için en az karakter sayısı

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

                // 4. Sepete At
                AddToCart(realProduct);
            };

            _quickButtonGrid.Controls.Add(btn);
        }

        private void AskManualPrice()
        {
            string input = Interaction.InputBox("Tutarı Giriniz:", "Serbest Tutar", "0");
            if (decimal.TryParse(input, out decimal price) && price > 0)
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
        private void ProcessScannedBarcode(string code)
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
                SafeMessageBox("Ürün Bulunamadı!\nBarkod: " + code, "Hata", true);
                _txtBarcode.SelectAll();
            }
            _txtBarcode.Focus(); // İşlem sonrası odağı tekrar barkod kutusuna al
        }

        // ============================================================
        //  GLOBAL TUŞ YAKALAMA (ProcessCmdKey)
        //  SalesForm, MainForm içine GÖMÜLÜ (TopLevel=false) çalıştığı için
        //  normal KeyPreview güvenilir değildir. ProcessCmdKey ise odak hangi
        //  kontrolde olursa olsun (buton, grid, kutu) tüm tuşları yakalar.
        //
        //  Mantık:
        //   - Hızlı (okuyucu) gelen rakam/harfler tampona biriktirilir.
        //   - Enter geldiğinde tampon yeterince uzunsa => OKUYUCU okuması olarak işlenir.
        //   - Yavaş (insan) girişlerde tampon sıfırlanır, Enter normal akışa bırakılır.
        //   - F2/F3/F4/F5/F6 kısayolları her yerde çalışır.
        // ============================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Fonksiyon tuşu kısayolları (odak neresi olursa olsun çalışır)
            switch (keyData)
            {
                case Keys.F2: CompleteSale(PaymentType.Nakit); return true;
                case Keys.F3: CompleteSale(PaymentType.KrediKarti); return true;
                case Keys.F4: SafeMessageBox("Fiş beklemeye alındı.", "Bilgi"); return true;
                case Keys.F5: ClearCart(); return true;
                case Keys.F6: ToggleReturnMode(); return true;
            }

            // İki tuş arasındaki süreyi ölç: uzun ara => insan yazıyor, tamponu sıfırla.
            DateTime now = DateTime.Now;
            if ((now - _lastScanKey).TotalMilliseconds > ScanGapMs)
                _scanBuffer = "";
            _lastScanKey = now;

            // Enter: biriken tampon yeterince uzunsa okuyucu okuması olarak işle.
            if (keyData == Keys.Enter)
            {
                if (_scanBuffer.Length >= MinScanLength)
                {
                    string code = _scanBuffer;
                    _scanBuffer = "";
                    ProcessScannedBarcode(code);
                    return true; // Tükettik; başka handler'a gitmesin
                }
                _scanBuffer = "";
                // Kısa/insan girişi => normal akışa bırak (manuel kutu Enter handler'ı çalışsın)
                return base.ProcessCmdKey(ref msg, keyData);
            }

            // Yazılabilir karakterleri (rakam/harf/tire) tampona ekle.
            char c = KeyToChar(keyData);
            if (c != '\0')
            {
                _scanBuffer += c;
                // NOT: Karakteri tüketmiyoruz (return base). Böylece odak barkod
                // kutusundaysa kullanıcı yazdığını ekranda görmeye devam eder.
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Keys değerini barkodda kullanılan karaktere çevirir (rakam, harf, tire).
        // Barkodların çoğu rakamdır; harf ve tire de Code39 gibi türler için desteklenir.
        private static char KeyToChar(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode; // Shift/Ctrl gibi ek bitleri ayıkla

            // Üst sıra rakamlar (D0-D9)
            if (key >= Keys.D0 && key <= Keys.D9) return (char)('0' + (key - Keys.D0));
            // Numpad rakamlar
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return (char)('0' + (key - Keys.NumPad0));
            // Harfler (A-Z) -> büyük harf
            if (key >= Keys.A && key <= Keys.Z) return (char)('A' + (key - Keys.A));
            // Tire / eksi
            if (key == Keys.OemMinus || key == Keys.Subtract) return '-';

            return '\0'; // Yazılamaz tuş (yok say)
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
            if (_grandTotal == 0 && !_isReturnMode) return;
            try
            {
                Sale newSale = new Sale
                {
                    TransactionCode = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    PaymentType     = _isReturnMode ? PaymentType.Iade : paymentType,
                    SaleType        = _isReturnMode ? SaleType.Iade : SaleType.Satis,
                    TotalAmount     = _grandTotal
                };
                _saleService.CompleteSale(newSale, _cartDetails);
                SafeMessageBox($"İşlem Tamamlandı.\nTutar: {_grandTotal:C2}", "Bilgi");
                ClearCart();
            }
            catch (Exception ex)
            {
                string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                SafeMessageBox("Hata: " + err, "Hata", true);
            }
        }

        private void ClearCart()
        {
            _gridSepet.Rows.Clear(); _cartDetails.Clear(); _grandTotal = 0; _lblTotal.Text = "₺ 0.00"; _txtBarcode.Focus();
        }
    }
}