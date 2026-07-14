using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class ProductEditForm : Form
    {
        private Product         _product;
        private ProductService  _productService;
        private CategoryService _categoryService;
        private bool            _isNew;

        private TextBox  _txtBarcode;
        private TextBox  _txtName;
        private ComboBox _cmbCategory;
        private TextBox  _txtBuyPrice;
        private TextBox  _txtSellPrice;
        private TextBox  _txtStock;
        private ComboBox _cmbUnit;

        public ProductEditForm(Product product = null)
        {
            _productService  = new ProductService();
            _categoryService = new CategoryService();
            _product = product ?? new Product();
            _isNew   = product == null || product.Id == 0;

            BuildForm();
            LoadCategoryDropdown();
            if (!_isNew) FillFields();
            else if (!string.IsNullOrEmpty(_product.Barcode)) _txtBarcode.Text = _product.Barcode;

            this.Shown += (s, e) =>
            {
                _txtBarcode.Focus();
                if (!string.IsNullOrEmpty(_txtBarcode.Text)) _txtBarcode.SelectAll();
            };
        }

        // ════════════════════════════════════════════════════════════
        private void BuildForm()
        {
            this.Text            = _isNew ? "Yeni Ürün Ekle" : "Ürün Düzenle";
            this.Size            = new Size(520, 600);
            this.MinimumSize     = new Size(520, 600);
            this.MaximumSize     = new Size(520, 600);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor       = Theme.Background;
            this.DoubleBuffered  = true;

            // ── BAŞLIK ÇUBUĞU ────────────────────────────────────────
            Panel titleBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 54,
                BackColor = Theme.Surface
            };
            titleBar.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
            };

            // Sol aksan şerit
            Panel accentBar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = Theme.Accent };
            titleBar.Controls.Add(accentBar);

            Label lblTitle = new Label
            {
                Text      = _isNew ? "  Yeni Ürün Ekle" : "  Ürün Düzenle",
                Font      = Theme.H2,
                ForeColor = Theme.TextPrimary,
                Dock      = DockStyle.Left,
                AutoSize  = true,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            titleBar.Controls.Add(lblTitle);

            Button btnX = new Button
            {
                Text      = "✕",
                Size      = new Size(44, 44),
                Location  = new Point(this.Width - 49, 5),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.TextSecond,
                Font      = new Font("Segoe UI", 12),
                Cursor    = Cursors.Hand,
                TabStop   = false,
                BackColor = Color.Transparent
            };
            btnX.FlatAppearance.BorderSize         = 0;
            btnX.FlatAppearance.MouseOverBackColor = Theme.Danger;
            btnX.Click      += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            btnX.MouseEnter += (s, e) => btnX.ForeColor = Color.White;
            btnX.MouseLeave += (s, e) => btnX.ForeColor = Theme.TextSecond;
            titleBar.Controls.Add(btnX);

            this.Controls.Add(titleBar);

            // ── İÇERİK ──────────────────────────────────────────────
            Panel content = new Panel
            {
                Location  = new Point(0, 54),
                Size      = new Size(520, 546),
                BackColor = Theme.Background,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(content);

            // Sabitleri
            int lx = 24, lw = 116, ix = 148, iw = 330, rh = 60, y = 18;

            // 1. Barkod
            content.Controls.Add(MakeLabel("Barkod:", lx, y, lw));
            _txtBarcode = MakeInput(ix, y, iw);
            _txtBarcode.PlaceholderText = "Barkod okutun veya yazın";
            _txtBarcode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; _txtName.Focus(); }
            };
            content.Controls.Add(_txtBarcode); y += rh;

            // 2. Ürün Adı
            content.Controls.Add(MakeLabel("Ürün Adı: *", lx, y, lw));
            _txtName = MakeInput(ix, y, iw);
            _txtName.PlaceholderText = "Ürün adını yazın";
            _txtName.KeyDown += BlockEnter;
            content.Controls.Add(_txtName); y += rh;

            // 3. Kategori
            content.Controls.Add(MakeLabel("Kategori:", lx, y, lw));
            _cmbCategory = new ComboBox
            {
                Location      = new Point(ix, y + 4),
                Size          = new Size(iw, 36),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = Theme.Body,
                BackColor     = Theme.Surface,
                ForeColor     = Theme.TextPrimary,
                FlatStyle     = FlatStyle.Flat
            };
            content.Controls.Add(_cmbCategory); y += rh;

            // 4. Birim
            content.Controls.Add(MakeLabel("Birim:", lx, y, lw));
            _cmbUnit = new ComboBox
            {
                Location      = new Point(ix, y + 4),
                Size          = new Size(150, 36),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = Theme.Body,
                BackColor     = Theme.Surface,
                ForeColor     = Theme.TextPrimary,
                FlatStyle     = FlatStyle.Flat
            };
            _cmbUnit.Items.AddRange(new object[] { "Adet", "Kg", "Lt", "Paket", "Kutu", "Metre" });
            _cmbUnit.SelectedIndex = 0;
            content.Controls.Add(_cmbUnit); y += rh;

            // 5. Alış Fiyatı
            content.Controls.Add(MakeLabel("Alış Fiyatı:", lx, y, lw));
            _txtBuyPrice = MakeInput(ix, y, 180);
            _txtBuyPrice.PlaceholderText = "0.00";
            _txtBuyPrice.KeyDown += BlockEnter;
            content.Controls.Add(_txtBuyPrice);
            content.Controls.Add(MakeCurrLabel(ix + 188, y + 8)); y += rh;

            // 6. Satış Fiyatı
            content.Controls.Add(MakeLabel("Satış Fiyatı: *", lx, y, lw));
            _txtSellPrice = MakeInput(ix, y, 180);
            _txtSellPrice.PlaceholderText = "0.00";
            _txtSellPrice.KeyDown += BlockEnter;
            content.Controls.Add(_txtSellPrice);
            content.Controls.Add(MakeCurrLabel(ix + 188, y + 8)); y += rh;

            // 7. Stok
            content.Controls.Add(MakeLabel("Stok Adedi:", lx, y, lw));
            _txtStock = MakeInput(ix, y, 180);
            _txtStock.PlaceholderText = "0";
            _txtStock.KeyDown += BlockEnter;
            content.Controls.Add(_txtStock); y += rh + 4;

            // Not
            content.Controls.Add(new Label
            {
                Text      = "* Zorunlu alan",
                Location  = new Point(lx, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent
            }); y += 28;

            // KAYDET
            Button btnSave = new Button
            {
                Text      = "  KAYDET",
                Location  = new Point(ix, y),
                Size      = new Size(155, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Success,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnSave.FlatAppearance.BorderSize         = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(5, 150, 105);
            btnSave.Click      += BtnSave_Click;
            btnSave.MouseEnter += (s, e) => btnSave.BackColor = Color.FromArgb(5, 150, 105);
            btnSave.MouseLeave += (s, e) => btnSave.BackColor = Theme.Success;
            content.Controls.Add(btnSave);

            // İPTAL
            Button btnCancel = new Button
            {
                Text      = "İPTAL",
                Location  = new Point(ix + 163, y),
                Size      = new Size(120, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.SurfaceAlt,
                ForeColor = Theme.TextPrimary,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnCancel.FlatAppearance.BorderSize         = 0;
            btnCancel.FlatAppearance.MouseOverBackColor = Theme.Border;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            content.Controls.Add(btnCancel);
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI
        // ════════════════════════════════════════════════════════════
        private Label MakeLabel(string text, int x, int y, int w) => new Label
        {
            Text      = text,
            Location  = new Point(x, y + 12),
            Size      = new Size(w, 24),
            Font      = Theme.Body,
            ForeColor = Theme.TextSecond,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        private TextBox MakeInput(int x, int y, int w) => new TextBox
        {
            Location    = new Point(x, y + 4),
            Size        = new Size(w, 36),
            Font        = new Font("Segoe UI", 12),
            BackColor   = Theme.Surface,
            ForeColor   = Theme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };

        private Label MakeCurrLabel(int x, int y) => new Label
        {
            Text      = "₺",
            Location  = new Point(x, y),
            AutoSize  = true,
            Font      = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Theme.TextSecond,
            BackColor = Color.Transparent
        };

        private static void BlockEnter(object s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; }
        }

        // ════════════════════════════════════════════════════════════
        //  VERİ YÜKLEMELERİ
        // ════════════════════════════════════════════════════════════
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
                if (_cmbCategory.Items[i] is Category cat && cat.Id == _product.CategoryId)
                { _cmbCategory.SelectedIndex = i; break; }
        }

        // ════════════════════════════════════════════════════════════
        //  KAYDET
        // ════════════════════════════════════════════════════════════
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Ürün adı zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus(); return;
            }

            if (!decimal.TryParse(_txtSellPrice.Text.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal sellPrice) || sellPrice < 0)
            {
                MessageBox.Show("Geçerli bir satış fiyatı giriniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSellPrice.Focus(); return;
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
