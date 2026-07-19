using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// BARKOD & ETİKET — yeni barkod üretme + etiket yazdırma.
    /// Ürün seçilebilir veya elle girilir; barkod Code128 ile çizilir;
    /// canlı önizleme gösterilir ve istenen adette etiket yazdırılır.
    /// </summary>
    public class BarcodeLabelForm : Form
    {
        private static readonly Color ColBg       = Color.FromArgb(245, 247, 250);
        private static readonly Color ColSurface  = Color.White;
        private static readonly Color ColBorder   = Color.FromArgb(214, 221, 230);
        private static readonly Color ColText      = Color.FromArgb(30, 41, 59);
        private static readonly Color ColStrong   = Color.FromArgb(15, 23, 42);
        private static readonly Color ColMuted    = Color.FromArgb(100, 116, 139);
        private static readonly Color ColBlue      = Color.FromArgb(37, 99, 235);
        private static readonly Color ColBlueDark = Color.FromArgb(29, 78, 188);
        private static readonly Color ColGreen     = Color.FromArgb(22, 163, 74);
        private static readonly Color ColGreenDark = Color.FromArgb(21, 128, 61);

        /// <summary>Etiket yazıcısı adının saklandığı ayar anahtarı.</summary>
        private const string KEY_LABEL_PRINTER = "LabelPrinter";

        /// <summary>1 mm = 3.937 birim (yazdırma birimi: 1/100 inch).</summary>
        private const float MmTo100 = 3.9370079f;

        private readonly ProductService _productService;
        private readonly SettingsService _settingsService;
        private readonly Random _rnd = new Random();

        private ComboBox _cmbProduct;
        private List<Product> _products = new List<Product>();
        private TextBox _txtBarcode;
        private TextBox _txtName;
        private TextBox _txtPrice;
        private NumericUpDown _numCount;
        private NumericUpDown _numWidth;
        private NumericUpDown _numHeight;
        private CheckBox _chkThermal;
        private PictureBox _picPreview;

        private readonly PrintDocument _printDoc = new PrintDocument();
        private int _printRemaining;

        /// <summary>Seçili etiket yazıcısı — baskı yolunda veritabanına gidilmemesi için bellekte tutulur.</summary>
        private string _cachedPrinter;

        /// <summary>Önizleme yenilemesini geciktiren zamanlayıcı (her tuşta değil, yazma bitince çizer).</summary>
        private System.Windows.Forms.Timer _previewTimer;

        /// <summary>Etiket metinlerinin ortalama biçimi — her çizimde yeniden üretilmez.</summary>
        private static readonly StringFormat FmtCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        public BarcodeLabelForm()
        {
            _productService = new ProductService();
            _settingsService = new SettingsService();
            this.DoubleBuffered = true;
            InitUI();

            // Sayaç her baskı başlangıcında sıfırlanır. Böylece önizleme penceresindeki
            // yazdır düğmesi de doğru adette etiket basar (aksi halde sayaç önizlemede
            // tükendiği için hiçbir şey basılmıyordu).
            _printDoc.BeginPrint += PrintDoc_BeginPrint;
            _printDoc.PrintPage += PrintDoc_PrintPage;
            RestoreSavedPrinter();

            // Yazma durduktan 150 ms sonra tek sefer çizer — her tuşta değil.
            _previewTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _previewTimer.Tick += (s, e) => { _previewTimer.Stop(); RebuildPreview(); };

            this.Shown += (s, e) =>
            {
                LoadProducts();
                RebuildPreview();
            };
        }

        private void InitUI()
        {
            this.BackColor = ColBg;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ── İÇERİK (önce eklenir) ──
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ColBg,
                Padding = new Padding(16)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.Controls.Add(content);

            content.Controls.Add(BuildInputCard(), 0, 0);
            content.Controls.Add(BuildPreviewCard(), 1, 0);

            // ── HEADER (en son → en üstte) ──
            this.Controls.Add(BuildHeader());
        }

        private Panel BuildHeader()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = ColSurface };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            Label title = new Label
            {
                Text = "Barkod & Etiket",
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = ColStrong,
                AutoSize = true,
                Location = new Point(24, 20)
            };
            header.Controls.Add(title);
            return header;
        }

        private Panel BuildInputCard()
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = ColSurface, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(20, 16, 20, 16) };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            int y = 12;
            const int lblH = 22, gap = 8, rowGap = 16, fieldW = 358;

            // Ürün seç
            card.Controls.Add(Caption("Ürün Seç (opsiyonel)", 0, ref y, lblH, gap));
            _cmbProduct = new ComboBox
            {
                Location = new Point(0, y),
                Width = fieldW,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbProduct.SelectedIndexChanged += CmbProduct_SelectedIndexChanged;
            card.Controls.Add(_cmbProduct);
            y += _cmbProduct.Height + rowGap;

            // Barkod + Üret
            card.Controls.Add(Caption("Barkod", 0, ref y, lblH, gap));
            _txtBarcode = new TextBox { Location = new Point(0, y), Width = 250, Font = new Font("Segoe UI", 12), BorderStyle = BorderStyle.FixedSingle };
            _txtBarcode.TextChanged += (s, e) => SchedulePreview();
            card.Controls.Add(_txtBarcode);

            Button btnGen = new Button
            {
                Text = "Üret",
                Location = new Point(258, y - 1),
                Size = new Size(100, 29),
                BackColor = ColBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGen.FlatAppearance.BorderSize = 0;
            btnGen.FlatAppearance.MouseOverBackColor = ColBlueDark;
            btnGen.Click += (s, e) => { _txtBarcode.Text = GenerateUniqueBarcode(); };
            card.Controls.Add(btnGen);
            y += 29 + rowGap;

            // Ürün adı
            card.Controls.Add(Caption("Ürün Adı", 0, ref y, lblH, gap));
            _txtName = new TextBox { Location = new Point(0, y), Width = fieldW, Font = new Font("Segoe UI", 12), BorderStyle = BorderStyle.FixedSingle };
            _txtName.TextChanged += (s, e) => SchedulePreview();
            card.Controls.Add(_txtName);
            y += _txtName.Height + rowGap;

            // Fiyat
            card.Controls.Add(Caption("Fiyat (₺)", 0, ref y, lblH, gap));
            _txtPrice = new TextBox { Location = new Point(0, y), Width = 170, Font = new Font("Segoe UI", 12), BorderStyle = BorderStyle.FixedSingle };
            _txtPrice.TextChanged += (s, e) => SchedulePreview();
            card.Controls.Add(_txtPrice);
            y += _txtPrice.Height + rowGap;

            // Etiket adedi + boyut
            card.Controls.Add(Caption("Etiket Adedi", 0, ref y, lblH, gap));
            _numCount = new NumericUpDown { Location = new Point(0, y), Width = 110, Font = new Font("Segoe UI", 12), Minimum = 1, Maximum = 1000, Value = 1 };
            card.Controls.Add(_numCount);

            card.Controls.Add(new Label { Text = "Genişlik(mm)", Location = new Point(130, y - 20), AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = ColMuted });
            _numWidth = new NumericUpDown { Location = new Point(130, y), Width = 100, Font = new Font("Segoe UI", 12), Minimum = 20, Maximum = 150, Value = 50 };
            _numWidth.ValueChanged += (s, e) => SchedulePreview();
            card.Controls.Add(_numWidth);

            card.Controls.Add(new Label { Text = "Yükseklik(mm)", Location = new Point(250, y - 20), AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = ColMuted });
            _numHeight = new NumericUpDown { Location = new Point(250, y), Width = 100, Font = new Font("Segoe UI", 12), Minimum = 15, Maximum = 120, Value = 30 };
            _numHeight.ValueChanged += (s, e) => SchedulePreview();
            card.Controls.Add(_numHeight);
            y += _numHeight.Height + rowGap;

            // Termal mod: etiketi resim olarak değil, TSPL komutu olarak gönderir.
            _chkThermal = new CheckBox
            {
                Text = "Termal etiket yazıcısı (TSPL)",
                Location = new Point(0, y),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 10),
                ForeColor = ColText,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(_chkThermal);
            y += 26 + 8;

            // Butonlar
            Button btnPreview = MakeButton("Önizleme", ColBlue, ColBlueDark, new Point(0, y), 170);
            btnPreview.Click += (s, e) => ShowPrintPreview();
            card.Controls.Add(btnPreview);

            Button btnPrint = MakeButton("Yazdır", ColGreen, ColGreenDark, new Point(188, y), 170);
            btnPrint.Click += (s, e) => DoPrint(btnPrint);
            card.Controls.Add(btnPrint);
            y += 46 + 12;

            Button btnPrinter = MakeButton("Yazıcı Ayarları", Color.FromArgb(100, 116, 139), Color.FromArgb(71, 85, 105), new Point(0, y), fieldW);
            btnPrinter.Click += (s, e) => ChoosePrinter();
            card.Controls.Add(btnPrinter);
            y += 46 + 12;

            Button btnSave = MakeButton("Barkodu Ürüne Kaydet", Color.FromArgb(71, 85, 105), Color.FromArgb(51, 65, 85), new Point(0, y), fieldW);
            btnSave.Click += (s, e) => SaveBarcodeToProduct();
            card.Controls.Add(btnSave);

            return card;
        }

        private Panel BuildPreviewCard()
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = ColSurface, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(20) };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            Label lbl = new Label
            {
                Text = "Önizleme",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColStrong,
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lbl);

            _picPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            card.Controls.Add(_picPreview);
            _picPreview.BringToFront();

            return card;
        }

        // ─────────────────────────────────────────────────────────────
        // Küçük yardımcılar
        // ─────────────────────────────────────────────────────────────
        private Label Caption(string text, int x, ref int y, int h, int gap)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = ColMuted };
            y += h + gap - 6;
            return lbl;
        }

        private Button MakeButton(string text, Color bg, Color hover, Point loc, int w)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(w, 46),
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hover;
            return btn;
        }

        // ─────────────────────────────────────────────────────────────
        // Veri
        // ─────────────────────────────────────────────────────────────
        private void LoadProducts()
        {
            _products = _productService.GetProducts();
            _cmbProduct.Items.Clear();
            _cmbProduct.Items.Add("— Ürün seçin —");
            foreach (var p in _products)
                _cmbProduct.Items.Add($"{p.Name}  ({(string.IsNullOrEmpty(p.Barcode) ? "barkodsuz" : p.Barcode)})");
            _cmbProduct.SelectedIndex = 0;
        }

        private void CmbProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = _cmbProduct.SelectedIndex - 1;
            if (i < 0 || i >= _products.Count) return;
            var p = _products[i];
            _txtName.Text = p.Name ?? "";
            _txtPrice.Text = p.SellingPrice.ToString("0.00");
            _txtBarcode.Text = p.Barcode ?? "";
            RebuildPreview();
        }

        private string GenerateUniqueBarcode()
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                // 200 = mağaza içi kullanım öneki + 9 rastgele hane + EAN-13 kontrol hanesi
                var digits = new char[12];
                digits[0] = '2'; digits[1] = '0'; digits[2] = '0';
                for (int k = 3; k < 12; k++) digits[k] = (char)('0' + _rnd.Next(10));
                string base12 = new string(digits);
                string code = base12 + Ean13CheckDigit(base12);
                if (_productService.GetByBarcode(code) == null)
                    return code;
            }
            return "200" + DateTime.Now.ToString("HHmmssfff");
        }

        private static char Ean13CheckDigit(string twelve)
        {
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int d = twelve[i] - '0';
                sum += (i % 2 == 0) ? d : d * 3;
            }
            int check = (10 - (sum % 10)) % 10;
            return (char)('0' + check);
        }

        private void SaveBarcodeToProduct()
        {
            int i = _cmbProduct.SelectedIndex - 1;
            if (i < 0 || i >= _products.Count)
            {
                MessageBox.Show("Önce yukarıdan bir ürün seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string code = _txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Barkod boş olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = _productService.GetByBarcode(code);
            if (existing != null && existing.Id != _products[i].Id)
            {
                MessageBox.Show($"Bu barkod zaten '{existing.Name}' ürününde kullanılıyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var product = _productService.GetById(_products[i].Id);
            product.Barcode = code;
            _productService.Save(product);
            _products[i].Barcode = code;
            MessageBox.Show("Barkod ürüne kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ─────────────────────────────────────────────────────────────
        // Etiket çizimi + önizleme
        // ─────────────────────────────────────────────────────────────
        private string PriceText()
        {
            string raw = _txtPrice.Text.Trim().Replace(",", ".");
            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal p))
                return p.ToString("₺#,##0.00");
            return _txtPrice.Text.Trim();
        }

        /// <summary>
        /// Önizlemeyi geciktirmeli olarak yeniler.
        ///
        /// Her tuş vuruşunda anında yeniden çizmek, 150x120 mm'lik bir etikette
        /// 1800x1440x4 ≈ 10 MB'lık bitmap ayırıyordu. 13 haneli bir barkod yazmak
        /// ~130 MB'lık büyük nesne yığını (LOH) çöpü üretip Gen2 toplamalarını
        /// tetikliyor, 4 GB'lık bir makinede saniyelerce donmaya yol açıyordu.
        /// Yazma durduktan 150 ms sonra tek sefer çizilir.
        /// </summary>
        private void SchedulePreview()
        {
            if (_previewTimer == null) return;
            _previewTimer.Stop();
            _previewTimer.Start();
        }

        private void RebuildPreview()
        {
            if (_picPreview == null) return;

            // Önizleme ekranda küçültülerek gösteriliyor; baskı çözünürlüğünde
            // çizmenin görsel faydası yok. Ekrana yetecek kadar büyük tut.
            const int MaxKenarPx = 900;

            float mmW = (float)_numWidth.Value;
            float mmH = (float)_numHeight.Value;
            float olcek = Math.Min(12f, MaxKenarPx / Math.Max(mmW, mmH));

            int wPx = Math.Max(1, (int)(mmW * olcek));
            int hPx = Math.Max(1, (int)(mmH * olcek));

            var bmp = new Bitmap(wPx, hPx);
            using (var g = Graphics.FromImage(bmp))
                RenderLabel(g, new RectangleF(0, 0, wPx, hPx), true);

            var eski = _picPreview.Image;
            _picPreview.Image = bmp;
            eski?.Dispose();          // yenisi atandıktan sonra bırak — çizim sırasında erişim olmasın
        }

        // Etiketi verilen dikdörtgene çizer (hem önizleme hem baskıda kullanılır).
        private void RenderLabel(Graphics g, RectangleF r, bool drawCutBorder)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            using (var white = new SolidBrush(Color.White))
                g.FillRectangle(white, r);

            if (drawCutBorder)
                using (var pen = new Pen(Color.FromArgb(220, 226, 232), 1))
                    g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);

            float pad = r.Height * 0.06f;
            RectangleF inner = new RectangleF(r.X + pad, r.Y + pad, r.Width - 2 * pad, r.Height - 2 * pad);

            string name = string.IsNullOrWhiteSpace(_txtName.Text) ? "Ürün Adı" : _txtName.Text.Trim();
            string code = _txtBarcode.Text.Trim();
            string price = PriceText();

            // StringFormat eskiden burada her çağrıda üretilip hiç bırakılmıyordu.
            // RenderLabel hem her önizlemede hem basılan her etiket için çalışır;
            // 1000 etiket basmak 1000 GDI tanıtıcısı sızdırıyordu. Tek statik örnek yeterli.
            StringFormat fmtCenter = FmtCenter;

            // Ürün adı (üst)
            RectangleF nameR = new RectangleF(inner.X, inner.Y, inner.Width, inner.Height * 0.20f);
            DrawFittedString(g, name, "Segoe UI", FontStyle.Bold, nameR, fmtCenter, ColStrong, inner.Height * 0.14f);

            // Fiyat (alt)
            RectangleF priceR = new RectangleF(inner.X, inner.Bottom - inner.Height * 0.22f, inner.Width, inner.Height * 0.22f);
            if (!string.IsNullOrWhiteSpace(price))
                DrawFittedString(g, price, "Segoe UI", FontStyle.Bold, priceR, fmtCenter, ColStrong, inner.Height * 0.18f);

            // Barkod altındaki okunur kod
            RectangleF codeR = new RectangleF(inner.X, priceR.Y - inner.Height * 0.14f, inner.Width, inner.Height * 0.14f);
            if (!string.IsNullOrEmpty(code))
                DrawFittedString(g, code, "Consolas", FontStyle.Regular, codeR, fmtCenter, ColText, inner.Height * 0.11f);

            // Barkod (orta)
            float barTop = nameR.Bottom + inner.Height * 0.02f;
            RectangleF barR = new RectangleF(inner.X, barTop, inner.Width, codeR.Y - barTop - inner.Height * 0.02f);
            if (!string.IsNullOrEmpty(code) && barR.Height > 4)
                Code128.Draw(g, code, barR);
            else if (string.IsNullOrEmpty(code))
                DrawFittedString(g, "Barkod girin / üretin", "Segoe UI", FontStyle.Italic, barR, fmtCenter, ColMuted, inner.Height * 0.10f);
        }

        private void DrawFittedString(Graphics g, string text, string family, FontStyle style, RectangleF rect, StringFormat fmt, Color color, float startEm)
        {
            if (string.IsNullOrEmpty(text) || rect.Width <= 1 || rect.Height <= 1) return;
            float em = Math.Max(4f, startEm);
            using var brush = new SolidBrush(color);
            for (; em >= 4f; em -= 1f)
            {
                using var font = new Font(family, em, style, GraphicsUnit.Pixel);
                SizeF sz = g.MeasureString(text, font, (int)rect.Width, fmt);
                if (sz.Height <= rect.Height || em <= 4f)
                {
                    g.DrawString(text, font, brush, rect, fmt);
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Yazdırma
        // ─────────────────────────────────────────────────────────────
        private bool ValidateForPrint()
        {
            if (string.IsNullOrWhiteSpace(_txtBarcode.Text))
            {
                MessageBox.Show("Yazdırmadan önce bir barkod girin veya üretin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (PrinterSettings.InstalledPrinters.Count == 0)
            {
                MessageBox.Show("Bilgisayarda kurulu yazıcı bulunamadı.\nEtiket yazıcısının sürücüsünü kurup tekrar deneyin.",
                    "Yazıcı yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        /// <summary>Daha önce seçilen etiket yazıcısını geri yükler (hâlâ kuruluysa).</summary>
        private void RestoreSavedPrinter()
        {
            string saved = _settingsService.GetSetting(KEY_LABEL_PRINTER);
            if (string.IsNullOrWhiteSpace(saved)) return;
            _printDoc.PrinterSettings.PrinterName = saved;
            if (!_printDoc.PrinterSettings.IsValid)
                _printDoc.PrinterSettings = new PrinterSettings();   // yazıcı kaldırılmış → varsayılana dön
        }

        /// <summary>Yazıcı + kağıt (etiket formu) seçimi; seçim kalıcı olarak saklanır.</summary>
        private void ChoosePrinter()
        {
            ApplyPageSettings();
            using var dlg = new PageSetupDialog
            {
                Document = _printDoc,
                AllowPrinter = true,
                AllowMargins = true,
                AllowOrientation = true,
                AllowPaper = true,
                EnableMetric = true
            };
            if (dlg.ShowDialog(this.FindForm()) != DialogResult.OK) return;

            _cachedPrinter = _printDoc.PrinterSettings.PrinterName;
            _settingsService.SetSetting(KEY_LABEL_PRINTER, _cachedPrinter);
            TsplLabelPrinter.ResetConfigCache();   // yeni cihaza ayarlar baştan gitsin
            MessageBox.Show(
                $"Etiket yazıcısı kaydedildi:\n{_printDoc.PrinterSettings.PrinterName}\n\n" +
                $"Kağıt: {_printDoc.DefaultPageSettings.PaperSize.PaperName} " +
                $"({_printDoc.DefaultPageSettings.PaperSize.Width / MmTo100:0} x {_printDoc.DefaultPageSettings.PaperSize.Height / MmTo100:0} mm)",
                "Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Baskı öncesi sayfa ayarlarını etikete uygun hale getirir.
        /// Etiket yazıcılarında kenar boşluğu yoktur; .NET'in varsayılan 1 inçlik (25 mm)
        /// boşluğu 50x30 mm'lik bir etiketi tamamen sayfa dışına ittiği için çıktı boş kalır.
        /// </summary>
        private void ApplyPageSettings()
        {
            var page = _printDoc.DefaultPageSettings;
            page.Margins = new Margins(0, 0, 0, 0);
            _printDoc.OriginAtMargins = false;

            // Sürücüde etiket ölçüsüyle eşleşen bir kağıt formu tanımlıysa onu seç.
            // Eşleşme yoksa kullanıcının/sürücünün ayarına dokunmayız (A4 etiket sayfası
            // gibi kullanımlar bozulmasın diye).
            int wantW = (int)Math.Round((double)_numWidth.Value * MmTo100);
            int wantH = (int)Math.Round((double)_numHeight.Value * MmTo100);
            PaperSize best = null;
            int bestDiff = int.MaxValue;
            try
            {
                foreach (PaperSize ps in _printDoc.PrinterSettings.PaperSizes)
                {
                    int diff = Math.Abs(ps.Width - wantW) + Math.Abs(ps.Height - wantH);
                    if (diff < bestDiff) { bestDiff = diff; best = ps; }
                }
            }
            catch { /* bazı sürücüler kağıt listesi vermez */ }

            if (best != null && bestDiff <= 20)   // ~5 mm tolerans
                page.PaperSize = best;
        }

        private void ShowPrintPreview()
        {
            if (!ValidateForPrint()) return;
            ApplyPageSettings();
            using var dlg = new PrintPreviewDialog { Document = _printDoc, WindowState = FormWindowState.Maximized };
            dlg.ShowDialog(this.FindForm());
        }

        private void DoPrint(Button trigger = null)
        {
            if (!ValidateForPrint()) return;
            if (_chkThermal.Checked) { DoPrintThermal(trigger); return; }

            using var dlg = new PrintDialog { Document = _printDoc, UseEXDialog = true };
            if (dlg.ShowDialog(this.FindForm()) != DialogResult.OK) return;

            ApplyPageSettings();
            _settingsService.SetSetting(KEY_LABEL_PRINTER, _printDoc.PrinterSettings.PrinterName);
            try { _printDoc.Print(); }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Yazdırma hatası:\n{ex.Message}\n\nYazıcı: {_printDoc.PrinterSettings.PrinterName}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Termal yol: etiketi resim olarak değil, TSPL komutu olarak yazıcıya gönderir.
        /// Barkodu cihazın kendi motoru çizer — sürücünün kağıt/kenar boşluğu ayarları devre dışı kalır.
        /// Gönderim arka planda yapılır; kasada kuyruk varken ekran donmaz.
        /// </summary>
        private async void DoPrintThermal(Button trigger)
        {
            string printer = ResolveThermalPrinter();
            if (printer == null) return;

            // Değerleri UI iş parçacığında oku — arka plandan kontrol erişimi güvenli değil.
            string name = _txtName.Text.Trim();
            string code = _txtBarcode.Text.Trim();
            string price = TsplLabelPrinter.FormatPrice(_txtPrice.Text);
            int wMm = (int)_numWidth.Value;
            int hMm = (int)_numHeight.Value;
            int copies = (int)_numCount.Value;

            string oldText = trigger?.Text;
            if (trigger != null) { trigger.Enabled = false; trigger.Text = "Yazdırılıyor…"; }

            try
            {
                string error = await Task.Run(() =>
                    TsplLabelPrinter.Print(printer, name, code, price, wMm, hMm, copies));

                if (error != null)
                    MessageBox.Show($"Etiket gönderilemedi:\n{error}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (trigger != null) { trigger.Enabled = true; trigger.Text = oldText; }
            }
        }

        /// <summary>
        /// Kayıtlı etiket yazıcısını döndürür; yoksa kullanıcıya seçtirir (iptal edilirse null).
        /// Yazıcı adı bellekte tutulur — her baskıda veritabanına gidilmez (soğuk LocalDB
        /// erişimi tek başına ~700 ms sürüyordu).
        /// </summary>
        private string ResolveThermalPrinter()
        {
            if (_cachedPrinter != null) return _cachedPrinter;

            string saved = _settingsService.GetSetting(KEY_LABEL_PRINTER);
            if (!string.IsNullOrWhiteSpace(saved) && PrinterExists(saved))
            {
                _cachedPrinter = saved;
                return _cachedPrinter;
            }

            using var dlg = new PrintDialog { UseEXDialog = true, AllowSomePages = false };
            if (dlg.ShowDialog(this.FindForm()) != DialogResult.OK) return null;

            _cachedPrinter = dlg.PrinterSettings.PrinterName;
            _settingsService.SetSetting(KEY_LABEL_PRINTER, _cachedPrinter);   // yalnızca değiştiğinde yazılır
            TsplLabelPrinter.ResetConfigCache();
            return _cachedPrinter;
        }

        private static bool PrinterExists(string name)
        {
            foreach (string p in PrinterSettings.InstalledPrinters)
                if (string.Equals(p, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void PrintDoc_BeginPrint(object sender, PrintEventArgs e)
        {
            _printRemaining = (int)_numCount.Value;
            if (_printRemaining <= 0) e.Cancel = true;
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float labelW = (float)_numWidth.Value * MmTo100;
            float labelH = (float)_numHeight.Value * MmTo100;
            float gap = 8f;

            // Kenar boşlukları sıfırlandığı için MarginBounds sayfanın tamamına eşittir.
            // Yine de (sürücü boşluk dayatırsa) etiket sığmıyorsa tüm sayfayı kullan.
            RectangleF area = e.MarginBounds;
            if (area.Width < labelW || area.Height < labelH)
                area = new RectangleF(0, 0, e.PageBounds.Width, e.PageBounds.Height);

            // İstenen etiket kağıttan büyükse kırpmak yerine sayfaya sığdır.
            float scale = Math.Min(1f, Math.Min(area.Width / labelW, area.Height / labelH));
            float lw = labelW * scale;
            float lh = labelH * scale;

            int cols = Math.Max(1, (int)((area.Width + gap) / (lw + gap)));
            int rows = Math.Max(1, (int)((area.Height + gap) / (lh + gap)));
            int perPage = cols * rows;

            for (int idx = 0; idx < perPage && _printRemaining > 0; idx++)
            {
                float x = area.Left + (idx % cols) * (lw + gap);
                float y = area.Top + (idx / cols) * (lh + gap);
                RenderLabel(g, new RectangleF(x, y, lw, lh), true);
                _printRemaining--;
            }

            e.HasMorePages = _printRemaining > 0;
        }
    }
}
