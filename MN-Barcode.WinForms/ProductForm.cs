using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;
using System.Collections.Generic;

namespace MN_Barcode.WinForms
{
    public partial class ProductForm : Form
    {
        private ProductService  _productService;
        private CategoryService _categoryService;

        private Panel       _productPanel;
        private Panel       _categoryPanel;
        private Button      _tabProducts;
        private Button      _tabCategories;
        private Button      _btnAction;

        private DataGridView _gridProducts;
        private TextBox      _txtSearch;
        private ComboBox     _cmbCategory;
        private Label        _lblCount;

        private DataGridView _gridCategories;

        private bool _isProductsTab = true;

        public ProductForm()
        {
            _productService  = new ProductService();
            _categoryService = new CategoryService();
            this.DoubleBuffered = true;
            InitUI();
            this.Shown += (s, e) =>
            {
                LoadCategoryDropdown();
                LoadProducts();
                LoadCategories();
            };
        }

        // ════════════════════════════════════════════════════════════
        private void InitUI()
        {
            this.BackColor       = Theme.Background;
            this.Dock            = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ── HEADER (Tab + Aksiyon butonu) ───────────────────────
            Panel header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Theme.Surface
            };
            header.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            this.Controls.Add(header);

            // Sayfa başlığı
            Label lblPage = new Label
            {
                Text      = "Ürün Yönetimi",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad, 14),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblPage);

            // YENİ EKLE butonu (sağa sabit)
            _btnAction = new Button
            {
                Text      = "+ YENİ ÜRÜN",
                Size      = new Size(150, 38),
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            _btnAction.FlatAppearance.BorderSize         = 0;
            _btnAction.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            _btnAction.Location = new Point(header.Width - 170, 11);
            _btnAction.Click   += BtnAction_Click;
            _btnAction.MouseEnter += (s, e) => _btnAction.BackColor = Theme.AccentHover;
            _btnAction.MouseLeave += (s, e) => _btnAction.BackColor = Theme.Accent;
            header.Controls.Add(_btnAction);
            header.Resize += (s, e) => _btnAction.Location = new Point(header.Width - 170, 11);

            // ── TAB BAR ─────────────────────────────────────────────
            Panel tabBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 46,
                BackColor = Theme.Background
            };
            this.Controls.Add(tabBar);

            _tabProducts   = CreateTabBtn("📦  Ürünler",    true,  0);
            _tabCategories = CreateTabBtn("📂  Kategoriler", false, 120);
            tabBar.Controls.Add(_tabProducts);
            tabBar.Controls.Add(_tabCategories);

            _tabProducts.Click   += (s, e) => SwitchTab(true);
            _tabCategories.Click += (s, e) => SwitchTab(false);

            // ── FİLTRE ALANI (ürünler sekmesi) ──────────────────────
            Panel filterBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 58,
                BackColor = Theme.Surface
            };
            filterBar.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, 0, filterBar.Height - 1, filterBar.Width, filterBar.Height - 1);
            };

            Label lblSearch = new Label
            {
                Text      = "Ara:",
                Location  = new Point(Theme.PagePad, 19),
                AutoSize  = true,
                Font      = Theme.H3,
                ForeColor = Theme.TextSecond,
                BackColor = Color.Transparent
            };
            filterBar.Controls.Add(lblSearch);

            _txtSearch = new TextBox
            {
                Location    = new Point(Theme.PagePad + 42, 16),
                Size        = new Size(280, 30),
                Font        = Theme.Body,
                BackColor   = Theme.Background,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.TextChanged += (s, e) => LoadProducts();
            filterBar.Controls.Add(_txtSearch);

            Label lblCat = new Label
            {
                Text      = "Kategori:",
                Location  = new Point(_txtSearch.Right + 20, 19),
                AutoSize  = true,
                Font      = Theme.H3,
                ForeColor = Theme.TextSecond,
                BackColor = Color.Transparent
            };
            filterBar.Controls.Add(lblCat);

            _cmbCategory = new ComboBox
            {
                Location      = new Point(lblCat.Location.X + 80, 16),
                Size          = new Size(200, 30),
                Font          = Theme.Body,
                BackColor     = Theme.Background,
                ForeColor     = Theme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat
            };
            _cmbCategory.SelectedIndexChanged += (s, e) => LoadProducts();
            filterBar.Controls.Add(_cmbCategory);

            _lblCount = new Label
            {
                Location  = new Point(_cmbCategory.Right + 24, 19),
                AutoSize  = true,
                Font      = Theme.H3,
                ForeColor = Theme.Accent,
                BackColor = Color.Transparent
            };
            filterBar.Controls.Add(_lblCount);

            // ── ÜRÜNLER PANELİ ──────────────────────────────────────
            _productPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            this.Controls.Add(_productPanel);

            _productPanel.Controls.Add(filterBar);

            Panel gridWrap1 = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(Theme.PagePad)
            };
            _productPanel.Controls.Add(gridWrap1);
            gridWrap1.BringToFront();

            _gridProducts = CreateProductGrid();
            gridWrap1.Controls.Add(_gridProducts);

            // ── KATEGORİLER PANELİ ──────────────────────────────────
            _categoryPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Visible = false };
            this.Controls.Add(_categoryPanel);

            Panel gridWrap2 = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(Theme.PagePad)
            };
            _categoryPanel.Controls.Add(gridWrap2);

            _gridCategories = CreateCategoryGrid();
            gridWrap2.Controls.Add(_gridCategories);

            _productPanel.BringToFront();
        }

        private Button CreateTabBtn(string text, bool active, int x)
        {
            Color bg = active ? Theme.Accent : Theme.Background;
            var btn = new Button
            {
                Text      = text,
                Location  = new Point(x + Theme.PagePad, 6),
                Size      = new Size(115, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = active ? Color.White : Theme.TextSecond,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ════════════════════════════════════════════════════════════
        //  GRİD OLUŞTURMA
        // ════════════════════════════════════════════════════════════
        private DataGridView CreateProductGrid()
        {
            var grid = Theme.MakeDarkGrid();
            grid.Columns.Add("Id",       "ID");         grid.Columns["Id"].Visible       = false;
            grid.Columns.Add("Barcode",  "BARKOD");     grid.Columns["Barcode"].FillWeight  = 80;
            grid.Columns.Add("Name",     "ÜRÜN ADI");   grid.Columns["Name"].FillWeight     = 150;
            grid.Columns.Add("Category", "KATEGORİ");   grid.Columns["Category"].FillWeight = 80;
            grid.Columns.Add("BuyPrice", "ALIŞ");       grid.Columns["BuyPrice"].FillWeight = 60;
            grid.Columns.Add("SellPrice","SATIŞ");      grid.Columns["SellPrice"].FillWeight= 60;
            grid.Columns.Add("Stock",    "STOK");       grid.Columns["Stock"].FillWeight    = 50;

            var btnEdit = new DataGridViewButtonColumn { Name = "Edit", HeaderText = "", Text = "Düzenle", UseColumnTextForButtonValue = true, Width = 90 };
            var btnDel  = new DataGridViewButtonColumn { Name = "Del",  HeaderText = "", Text = "Sil",     UseColumnTextForButtonValue = true, Width = 70 };
            grid.Columns.Add(btnEdit);
            grid.Columns.Add(btnDel);

            grid.CellClick      += GridProducts_CellClick;
            grid.CellFormatting += GridProducts_CellFormatting;
            return grid;
        }

        private DataGridView CreateCategoryGrid()
        {
            var grid = Theme.MakeDarkGrid();
            grid.Columns.Add("Id",    "ID");            grid.Columns["Id"].Visible       = false;
            grid.Columns.Add("Name",  "KATEGORİ ADI");  grid.Columns["Name"].FillWeight  = 200;
            grid.Columns.Add("Count", "ÜRÜN SAYISI");   grid.Columns["Count"].FillWeight = 100;

            var btnDel = new DataGridViewButtonColumn { Name = "Del", HeaderText = "", Text = "Sil", UseColumnTextForButtonValue = true, Width = 100 };
            grid.Columns.Add(btnDel);

            grid.CellClick      += GridCategories_CellClick;
            grid.CellFormatting += GridCategories_CellFormatting;
            return grid;
        }

        // ════════════════════════════════════════════════════════════
        //  TAB GEÇİŞİ
        // ════════════════════════════════════════════════════════════
        private void SwitchTab(bool showProducts)
        {
            _isProductsTab = showProducts;
            _productPanel.Visible   = showProducts;
            _categoryPanel.Visible  = !showProducts;

            _tabProducts.BackColor   = showProducts  ? Theme.Accent      : Theme.Background;
            _tabProducts.ForeColor   = showProducts  ? Color.White       : Theme.TextSecond;
            _tabCategories.BackColor = !showProducts ? Theme.Accent      : Theme.Background;
            _tabCategories.ForeColor = !showProducts ? Color.White       : Theme.TextSecond;

            _btnAction.Text = showProducts ? "+ YENİ ÜRÜN" : "+ YENİ KATEGORİ";

            if (showProducts) _productPanel.BringToFront();
            else { _categoryPanel.BringToFront(); LoadCategories(); }
        }

        // ════════════════════════════════════════════════════════════
        //  AKSİYON (YENİ EKLE)
        // ════════════════════════════════════════════════════════════
        private void BtnAction_Click(object sender, EventArgs e)
        {
            if (_isProductsTab)
            {
                var owner = this.FindForm();
                using (var f = new ProductEditForm())
                    if (f.ShowDialog(owner) == DialogResult.OK) { LoadProducts(); LoadCategoryDropdown(); }
            }
            else
            {
                using (Form inputForm = new Form())
                {
                    inputForm.Text            = "Yeni Kategori";
                    inputForm.Size            = new Size(400, 180);
                    inputForm.StartPosition   = FormStartPosition.CenterParent;
                    inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    inputForm.MaximizeBox     = false;
                    inputForm.MinimizeBox     = false;
                    inputForm.BackColor       = Theme.Surface;

                    Label lbl   = new Label { Text = "Kategori Adı:", Location = new Point(30, 35), AutoSize = true, Font = Theme.Body, ForeColor = Theme.TextPrimary };
                    TextBox txt = new TextBox { Location = new Point(140, 30), Width = 220, Font = Theme.Body, BackColor = Theme.Background, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
                    Button btnOk = new Button { Text = "Ekle", Location = new Point(140, 80), Size = new Size(100, 40), BackColor = Theme.Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold), DialogResult = DialogResult.OK, Cursor = Cursors.Hand };
                    btnOk.FlatAppearance.BorderSize = 0;
                    Button btnCan = new Button { Text = "İptal", Location = new Point(250, 80), Size = new Size(100, 40), BackColor = Theme.SurfaceAlt, ForeColor = Theme.TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold), DialogResult = DialogResult.Cancel, Cursor = Cursors.Hand };
                    btnCan.FlatAppearance.BorderSize = 0;

                    inputForm.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCan });
                    inputForm.AcceptButton = btnOk;
                    inputForm.CancelButton = btnCan;

                    if (inputForm.ShowDialog(this.FindForm()) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                    {
                        _categoryService.Add(new Category { Name = txt.Text.Trim() });
                        LoadCategories(); LoadCategoryDropdown();
                    }
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        //  VERİ YÜKLEME
        // ════════════════════════════════════════════════════════════
        private void LoadCategoryDropdown()
        {
            _cmbCategory.Items.Clear();
            _cmbCategory.Items.Add("Tümü");
            foreach (var c in _categoryService.GetCategories())
                _cmbCategory.Items.Add(c.Name);
            _cmbCategory.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            _gridProducts.Rows.Clear();
            var list = _productService.GetProducts(_txtSearch.Text.Trim());
            if (_cmbCategory.SelectedIndex > 0)
            {
                string cat = _cmbCategory.SelectedItem.ToString();
                list = list.Where(p => p.Category?.Name == cat).ToList();
            }
            foreach (var p in list)
                _gridProducts.Rows.Add(p.Id, p.Barcode ?? "-", p.Name, p.Category?.Name ?? "-",
                    p.BuyingPrice.ToString("₺#,##0.00"), p.SellingPrice.ToString("₺#,##0.00"),
                    p.StockQuantity.ToString("N0"));
            _lblCount.Text = $"{list.Count} ürün";
        }

        private void LoadCategories()
        {
            _gridCategories.Rows.Clear();
            foreach (var item in _categoryService.GetCategoriesWithCount())
                _gridCategories.Rows.Add(item.Category.Id, item.Category.Name, $"{item.ProductCount} ürün");
        }

        // ════════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ════════════════════════════════════════════════════════════
        private void GridProducts_CellClick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = Convert.ToInt32(_gridProducts.Rows[e.RowIndex].Cells["Id"].Value);
            if (_gridProducts.Columns[e.ColumnIndex].Name == "Edit")
            {
                var p     = _productService.GetById(id);
                var owner = this.FindForm();
                using (var f = new ProductEditForm(p))
                    if (f.ShowDialog(owner) == DialogResult.OK) { LoadProducts(); LoadCategoryDropdown(); }
            }
            else if (_gridProducts.Columns[e.ColumnIndex].Name == "Del")
            {
                string name = _gridProducts.Rows[e.RowIndex].Cells["Name"].Value.ToString();
                if (MessageBox.Show($"'{name}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { _productService.Delete(id); LoadProducts(); }
            }
        }

        private void GridProducts_CellFormatting(object s, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == 6)
            {
                string val = e.Value?.ToString()?.Replace(".", "").Replace(",", ".") ?? "0";
                if (double.TryParse(val, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double stk))
                {
                    if (stk <= 5)       e.CellStyle.ForeColor = Theme.Danger;
                    else if (stk <= 20) e.CellStyle.ForeColor = Theme.Warning;
                }
            }
            else if (e.ColumnIndex == 7)
            { e.CellStyle.BackColor = Theme.Accent;  e.CellStyle.ForeColor = Color.White; e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); }
            else if (e.ColumnIndex == 8)
            { e.CellStyle.BackColor = Theme.Danger;  e.CellStyle.ForeColor = Color.White; e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); }
        }

        private void GridCategories_CellClick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _gridCategories.Columns[e.ColumnIndex].Name != "Del") return;
            int id     = Convert.ToInt32(_gridCategories.Rows[e.RowIndex].Cells["Id"].Value);
            string name = _gridCategories.Rows[e.RowIndex].Cells["Name"].Value.ToString();
            if (MessageBox.Show($"'{name}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            { _categoryService.Delete(id); LoadCategories(); LoadCategoryDropdown(); }
        }

        private void GridCategories_CellFormatting(object s, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == 3)
            { e.CellStyle.BackColor = Theme.Danger; e.CellStyle.ForeColor = Color.White; e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold); }
        }
    }
}