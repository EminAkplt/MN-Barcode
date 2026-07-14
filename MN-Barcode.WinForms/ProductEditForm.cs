using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class ProductEditForm : Form
    {
        private Product _product;
        private ProductService _productService;
        private CategoryService _categoryService;
        private bool _isNew;

        private TextBox _txtBarcode;
        private TextBox _txtName;
        private ComboBox _cmbCategory;
        private TextBox _txtBuyPrice;
        private TextBox _txtSellPrice;
        private TextBox _txtStock;
        private ComboBox _cmbUnit;

        private static readonly Color BgColor      = Color.FromArgb(30, 41, 59);
        private static readonly Color CardColor    = Color.FromArgb(51, 65, 85);
        private static readonly Color InputBg      = Color.FromArgb(71, 85, 105);
        private static readonly Color SuccessGreen = Color.FromArgb(34, 197, 94);
        private static readonly Color DangerRed    = Color.FromArgb(239, 68, 68);
        private static readonly Color TextMuted    = Color.FromArgb(148, 163, 184);
        private static readonly Color TextWhite    = Color.FromArgb(226, 232, 240);

        public ProductEditForm(Product product = null)
        {
            _productService  = new ProductService();
            _categoryService = new CategoryService();
            _product = product ?? new Product();
            _isNew   = product == null || product.Id == 0;

            BuildForm();
            LoadCategoryDropdown();
            if (!_isNew) FillFields();

            this.Shown += (s, e) =>
            {
                _txtBarcode.Focus();
                if (!string.IsNullOrEmpty(_txtBarcode.Text))
                    _txtBarcode.SelectAll();
            };
        }

        private void BuildForm()
        {
            this.Text            = _isNew ? "Yeni Ürün Ekle" : "Ürün Düzenle";
            this.Size            = new Size(520, 600);
            this.MinimumSize     = new Size(520, 600);
            this.MaximumSize     = new Size(520, 600);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor       = BgColor;

            // ── BAŞLIK ──────────────────────────────────────────────────
            Panel titleBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = CardColor
            };

            Label lblTitle = new Label
            {
                Text      = _isNew ? "➕  Yeni Ürün Ekle" : "✏️  Ürün Düzenle",
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(18, 13)
            };
            titleBar.Controls.Add(lblTitle);

            Button btnX = new Button
            {
                Text      = "✕",
                Size      = new Size(42, 42),
                Location  = new Point(this.Width - 47, 5),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Silver,
                Font      = new Font("Segoe UI", 12),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnX.FlatAppearance.BorderSize           = 0;
            btnX.FlatAppearance.MouseOverBackColor   = DangerRed;
            btnX.Click      += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            btnX.MouseEnter += (s, e) => btnX.ForeColor = Color.White;
            btnX.MouseLeave += (s, e) => btnX.ForeColor = Color.Silver;
            titleBar.Controls.Add(btnX);

            this.Controls.Add(titleBar);

            // ── İÇERİK (Padding YOK — absolute kontrollerle çakışır) ──
            Panel content = new Panel
            {
                Location  = new Point(0, 52),
                Size      = new Size(520, 548),
                BackColor = BgColor,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(content);

            // Sabit kolon ölçüleri
            int lx = 20;   // Label X
            int lw = 120;  // Label genişliği
            int ix = 150;  // Input X
            int iw = 320;  // Input genişliği
            int rh = 60;   // Satır yüksekliği
            int y  = 15;   // İlk satır Y — panel sıfırından güvenli aralıkta

            // ── 1. BARKOD ─────────────────────────────────────────────
            content.Controls.Add(RowLabel("Barkod:", lx, y, lw));
            _txtBarcode = RowInput(ix, y, iw);
            _txtBarcode.PlaceholderText = "Barkod okutun veya yazın";
            _txtBarcode.KeyDown += (s, e) =>   // Enter → sonraki alana geç
            {
                if (e.KeyCode == Keys.Enter)
                { e.Handled = true; e.SuppressKeyPress = true; _txtName.Focus(); }
            };
            content.Controls.Add(_txtBarcode);
            y += rh;

            // ── 2. ÜRÜN ADI ───────────────────────────────────────────
            content.Controls.Add(RowLabel("Ürün Adı: *", lx, y, lw));
            _txtName = RowInput(ix, y, iw);
            _txtName.PlaceholderText = "Ürün adını yazın";
            _txtName.KeyDown += BlockEnter;
            content.Controls.Add(_txtName);
            y += rh;

            // ── 3. KATEGORİ ───────────────────────────────────────────
            content.Controls.Add(RowLabel("Kategori:", lx, y, lw));
            _cmbCategory = new ComboBox
            {
                Location      = new Point(ix, y + 4),
                Size          = new Size(iw, 36),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12),
                BackColor     = InputBg,
                ForeColor     = TextWhite,
                FlatStyle     = FlatStyle.Flat
            };
            content.Controls.Add(_cmbCategory);
            y += rh;

            // ── 4. BİRİM ──────────────────────────────────────────────
            content.Controls.Add(RowLabel("Birim:", lx, y, lw));
            _cmbUnit = new ComboBox
            {
                Location      = new Point(ix, y + 4),
                Size          = new Size(150, 36),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12),
                BackColor     = InputBg,
                ForeColor     = TextWhite,
                FlatStyle     = FlatStyle.Flat
            };
            _cmbUnit.Items.AddRange(new object[] { "Adet", "Kg", "Lt", "Paket", "Kutu", "Metre" });
            _cmbUnit.SelectedIndex = 0;
            content.Controls.Add(_cmbUnit);
            y += rh;

            // ── 5. ALIŞ FİYATI ────────────────────────────────────────
            content.Controls.Add(RowLabel("Alış Fiyatı:", lx, y, lw));
            _txtBuyPrice = RowInput(ix, y, 180);
            _txtBuyPrice.PlaceholderText = "0.00";
            _txtBuyPrice.KeyDown += BlockEnter;
            content.Controls.Add(_txtBuyPrice);
            content.Controls.Add(new Label
            {
                Text      = "₺",
                Location  = new Point(ix + 188, y + 9),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = TextMuted
            });
            y += rh;

            // ── 6. SATIŞ FİYATI ───────────────────────────────────────
            content.Controls.Add(RowLabel("Satış Fiyatı: *", lx, y, lw));
            _txtSellPrice = RowInput(ix, y, 180);
            _txtSellPrice.PlaceholderText = "0.00";
            _txtSellPrice.KeyDown += BlockEnter;
            content.Controls.Add(_txtSellPrice);
            content.Controls.Add(new Label
            {
                Text      = "₺",
                Location  = new Point(ix + 188, y + 9),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = TextMuted
            });
            y += rh;

            // ── 7. STOK ───────────────────────────────────────────────
            content.Controls.Add(RowLabel("Stok Adedi:", lx, y, lw));
            _txtStock = RowInput(ix, y, 180);
            _txtStock.PlaceholderText = "0";
            _txtStock.KeyDown += BlockEnter;
            content.Controls.Add(_txtStock);
            y += rh + 5;

            // ── NOT ───────────────────────────────────────────────────
            content.Controls.Add(new Label
            {
                Text      = "* Zorunlu alan",
                Location  = new Point(lx, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = TextMuted
            });
            y += 30;

            // ── KAYDET ────────────────────────────────────────────────
            Button btnSave = new Button
            {
                Text      = "💾  KAYDET",
                Location  = new Point(ix, y),
                Size      = new Size(155, 46),
                BackColor = SuccessGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click      += BtnSave_Click;
            btnSave.MouseEnter += (s, e) => btnSave.BackColor = Color.FromArgb(22, 163, 74);
            btnSave.MouseLeave += (s, e) => btnSave.BackColor = SuccessGreen;
            content.Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text      = "İPTAL",
                Location  = new Point(ix + 165, y),
                Size      = new Size(120, 46),
                BackColor = CardColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click      += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = Color.FromArgb(71, 85, 105);
            btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = CardColor;
            content.Controls.Add(btnCancel);
        }

        // ─────────────────────────────────────────────────────────────────
        // YARDIMCI METODLAR
        // ─────────────────────────────────────────────────────────────────
        private Label RowLabel(string text, int x, int y, int w)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(x, y + 11),
                Size      = new Size(w, 26),
                Font      = new Font("Segoe UI", 11),
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private TextBox RowInput(int x, int y, int w)
        {
            return new TextBox
            {
                Location    = new Point(x, y + 4),
                Size        = new Size(w, 36),
                Font        = new Font("Segoe UI", 13),
                BackColor   = InputBg,
                ForeColor   = TextWhite,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static void BlockEnter(object s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            { e.Handled = true; e.SuppressKeyPress = true; }
        }

        // ─────────────────────────────────────────────────────────────────
        // VERİ YÜKLEMELERİ
        // ─────────────────────────────────────────────────────────────────
        private void LoadCategoryDropdown()
        {
            _cmbCategory.Items.Clear();
            _cmbCategory.Items.Add("-- Kategori Seçin --");
            foreach (var cat in _categoryService.GetCategories())
                _cmbCategory.Items.Add(cat);
            _cmbCategory.SelectedIndex = 0;
            _cmbCategory.DisplayMember = "Name";
        }

        private void FillFields()
        {
            _txtBarcode.Text   = _product.Barcode ?? "";
            _txtName.Text      = _product.Name    ?? "";
            _txtBuyPrice.Text  = _product.BuyingPrice.ToString("0.00");
            _txtSellPrice.Text = _product.SellingPrice.ToString("0.00");
            _txtStock.Text     = _product.StockQuantity.ToString("0.##");

            int unitIdx = _cmbUnit.Items.IndexOf(_product.Unit ?? "Adet");
            _cmbUnit.SelectedIndex = unitIdx >= 0 ? unitIdx : 0;

            for (int i = 1; i < _cmbCategory.Items.Count; i++)
            {
                if (_cmbCategory.Items[i] is Category cat && cat.Id == _product.CategoryId)
                { _cmbCategory.SelectedIndex = i; break; }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // KAYDET
        // ─────────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Ürün adı zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            if (!decimal.TryParse(
                    _txtSellPrice.Text.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal sellPrice) || sellPrice < 0)
            {
                MessageBox.Show("Geçerli bir satış fiyatı giriniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSellPrice.Focus();
                return;
            }

            decimal.TryParse(_txtBuyPrice.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal buyPrice);

            double.TryParse(_txtStock.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double stock);

            _product.Name          = _txtName.Text.Trim();
            _product.Barcode       = string.IsNullOrWhiteSpace(_txtBarcode.Text) ? null : _txtBarcode.Text.Trim();
            _product.BuyingPrice   = buyPrice;
            _product.SellingPrice  = sellPrice;
            _product.StockQuantity = stock;
            _product.Unit          = _cmbUnit.SelectedItem?.ToString() ?? "Adet";
            _product.CategoryId    = (_cmbCategory.SelectedIndex > 0 && _cmbCategory.SelectedItem is Category c) ? c.Id : (int?)null;

            try
            {
                _productService.Save(_product);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt hatası:\n" + (ex.InnerException?.Message ?? ex.Message),
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
