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

        // UI
        private TextBox txtName, txtBarcode, txtBuyPrice, txtSellPrice, txtStock;
        private ComboBox cmbCategory;

        // Renkler
        private readonly Color BgColor = Color.FromArgb(30, 41, 59);
        private readonly Color CardColor = Color.FromArgb(51, 65, 85);
        private readonly Color AccentColor = Color.FromArgb(59, 130, 246);
        private readonly Color SuccessColor = Color.FromArgb(34, 197, 94);
        private readonly Color DangerColor = Color.FromArgb(239, 68, 68);

        public ProductEditForm(Product product = null)
        {
            _productService = new ProductService();
            _categoryService = new CategoryService();
            _product = product ?? new Product();
            _isNew = product == null || product.Id == 0;

            SetupForm();
            LoadCategories();
            if (!_isNew) LoadProductData();
        }

        private void SetupForm()
        {
            // Form Ayarları
            this.Text = _isNew ? "Yeni Ürün Ekle" : "Ürün Düzenle";
            this.Size = new Size(480, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = BgColor;

            // Başlık Bar
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = CardColor
            };
            this.Controls.Add(titleBar);

            Label lblTitle = new Label
            {
                Text = _isNew ? "➕ Yeni Ürün Ekle" : "✏️ Ürün Düzenle",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            };
            titleBar.Controls.Add(lblTitle);

            // Kapatma Butonu
            Button btnClose = new Button
            {
                Text = "✕",
                Size = new Size(40, 40),
                Location = new Point(this.Width - 45, 5),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => btnClose.BackColor = DangerColor;
            btnClose.MouseLeave += (s, e) => btnClose.BackColor = CardColor;
            titleBar.Controls.Add(btnClose);

            // Form İçeriği
            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 20, 30, 20),
                BackColor = BgColor
            };
            this.Controls.Add(content);

            int y = 10;
            int labelWidth = 110;
            int inputWidth = 280;

            // ÜRÜN ADI
            content.Controls.Add(CreateLabel("Ürün Adı:", y));
            txtName = CreateTextBox(y, labelWidth, inputWidth);
            content.Controls.Add(txtName);
            y += 55;

            // BARKOD
            content.Controls.Add(CreateLabel("Barkod:", y));
            txtBarcode = CreateTextBox(y, labelWidth, inputWidth);
            content.Controls.Add(txtBarcode);
            y += 55;

            // KATEGORİ
            content.Controls.Add(CreateLabel("Kategori:", y));
            cmbCategory = new ComboBox
            {
                Location = new Point(labelWidth, y),
                Width = inputWidth,
                Height = 35,
                Font = new Font("Segoe UI", 12),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = CardColor,
                ForeColor = Color.White
            };
            content.Controls.Add(cmbCategory);
            y += 55;

            // ALIŞ FİYATI
            content.Controls.Add(CreateLabel("Alış Fiyatı:", y));
            txtBuyPrice = CreateTextBox(y, labelWidth, inputWidth);
            content.Controls.Add(txtBuyPrice);
            y += 55;

            // SATIŞ FİYATI
            content.Controls.Add(CreateLabel("Satış Fiyatı:", y));
            txtSellPrice = CreateTextBox(y, labelWidth, inputWidth);
            content.Controls.Add(txtSellPrice);
            y += 55;

            // STOK
            content.Controls.Add(CreateLabel("Stok Adedi:", y));
            txtStock = CreateTextBox(y, labelWidth, inputWidth);
            content.Controls.Add(txtStock);
            y += 70;

            // KAYDET BUTONU
            Button btnSave = new Button
            {
                Text = "💾  KAYDET",
                Location = new Point(labelWidth, y),
                Size = new Size(140, 45),
                BackColor = SuccessColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            btnSave.MouseEnter += (s, e) => btnSave.BackColor = Color.FromArgb(22, 163, 74);
            btnSave.MouseLeave += (s, e) => btnSave.BackColor = SuccessColor;
            content.Controls.Add(btnSave);

            // İPTAL BUTONU
            Button btnCancel = new Button
            {
                Text = "İPTAL",
                Location = new Point(labelWidth + 155, y),
                Size = new Size(125, 45),
                BackColor = CardColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();
            content.Controls.Add(btnCancel);
        }

        private Label CreateLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(0, y + 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
        }

        private TextBox CreateTextBox(int y, int x, int width)
        {
            TextBox txt = new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Height = 40,
                Font = new Font("Segoe UI", 13),
                BackColor = CardColor,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            return txt;
        }

        private void LoadCategories()
        {
            cmbCategory.Items.Add("Kategori Seçin");
            var categories = _categoryService.GetCategories();
            foreach (var cat in categories)
            {
                cmbCategory.Items.Add(cat);
            }
            cmbCategory.SelectedIndex = 0;
            cmbCategory.DisplayMember = "Name";
        }

        private void LoadProductData()
        {
            txtName.Text = _product.Name;
            txtBarcode.Text = _product.Barcode;
            txtBuyPrice.Text = _product.BuyingPrice.ToString("0.00");
            txtSellPrice.Text = _product.SellingPrice.ToString("0.00");
            txtStock.Text = _product.StockQuantity.ToString();

            // Kategori seç
            for (int i = 1; i < cmbCategory.Items.Count; i++)
            {
                if (cmbCategory.Items[i] is Category cat && cat.Id == _product.CategoryId)
                {
                    cmbCategory.SelectedIndex = i;
                    break;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Ürün adı boş olamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtSellPrice.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal sellPrice))
            {
                MessageBox.Show("Geçerli bir satış fiyatı girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal.TryParse(txtBuyPrice.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal buyPrice);
            double.TryParse(txtStock.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double stock);

            // Ürünü Güncelle
            _product.Name = txtName.Text.Trim();
            _product.Barcode = txtBarcode.Text.Trim();
            _product.BuyingPrice = buyPrice;
            _product.SellingPrice = sellPrice;
            _product.StockQuantity = stock;

            if (cmbCategory.SelectedIndex > 0 && cmbCategory.SelectedItem is Category selectedCat)
            {
                _product.CategoryId = selectedCat.Id;
            }

            // Kaydet
            _productService.Save(_product);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
