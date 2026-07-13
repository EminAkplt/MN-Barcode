using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.WinForms
{
    public partial class ProductForm : Form
    {
        private ProductService _productService;
        private CategoryService _categoryService;

        private Panel _productPanel;
        private Panel _categoryPanel;
        private Button _tabProducts;
        private Button _tabCategories;
        private Button _btnAction;

        private DataGridView _gridProducts;
        private TextBox _txtSearch;
        private ComboBox _cmbCategory;
        private Label _lblCount;

        private DataGridView _gridCategories;

        private bool _isProductsTab = true;

        public ProductForm()
        {
            _productService = new ProductService();
            _categoryService = new CategoryService();
            this.DoubleBuffered = true;
            InitUI();
            
            // Form gösterildikten SONRA veri yükle (performans için)
            this.Shown += (s, e) =>
            {
                LoadCategoryDropdown();
                LoadProducts();
                LoadCategories();
            };
        }

        private void InitUI()
        {
            this.BackColor = Color.White;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ═══════════════════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════════════════
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(45, 55, 72)
            };
            this.Controls.Add(header);

            // ÜRÜNLER TAB
            _tabProducts = new Button
            {
                Text = "📦 ÜRÜNLER",
                Size = new Size(160, 50),
                Location = new Point(30, 10),
                BackColor = Color.FromArgb(72, 187, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _tabProducts.FlatAppearance.BorderSize = 0;
            _tabProducts.Click += (s, e) => SwitchTab(true);
            header.Controls.Add(_tabProducts);

            // KATEGORİLER TAB
            _tabCategories = new Button
            {
                Text = "📂 KATEGORİLER",
                Size = new Size(180, 50),
                Location = new Point(200, 10),
                BackColor = Color.FromArgb(113, 128, 150),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _tabCategories.FlatAppearance.BorderSize = 0;
            _tabCategories.Click += (s, e) => SwitchTab(false);
            header.Controls.Add(_tabCategories);

            // YENİ EKLE BUTONU
            _btnAction = new Button
            {
                Text = "+ YENİ ÜRÜN",
                Size = new Size(160, 45),
                Location = new Point(header.Width - 200, 12),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(56, 161, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnAction.FlatAppearance.BorderSize = 0;
            _btnAction.Click += BtnAction_Click;
            header.Controls.Add(_btnAction);

            // ═══════════════════════════════════════════════════════════
            // ÜRÜNLER PANELİ
            // ═══════════════════════════════════════════════════════════
            _productPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(247, 250, 252) };
            this.Controls.Add(_productPanel);

            // FİLTRE ALANI
            Panel filterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 20)
            };
            _productPanel.Controls.Add(filterBar);

            // ARAMA LABEL
            Label lblSearch = new Label
            {
                Text = "🔍 Ara:",
                Location = new Point(30, 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 55, 72)
            };
            filterBar.Controls.Add(lblSearch);

            // ARAMA KUTUSU
            _txtSearch = new TextBox
            {
                Location = new Point(110, 22),
                Size = new Size(350, 38),
                Font = new Font("Segoe UI", 13),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.TextChanged += (s, e) => LoadProducts();
            filterBar.Controls.Add(_txtSearch);

            // KATEGORİ LABEL
            Label lblCat = new Label
            {
                Text = "Kategori:",
                Location = new Point(490, 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 55, 72)
            };
            filterBar.Controls.Add(lblCat);

            // KATEGORİ DROPDOWN
            _cmbCategory = new ComboBox
            {
                Location = new Point(580, 22),
                Size = new Size(220, 38),
                Font = new Font("Segoe UI", 12),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbCategory.SelectedIndexChanged += (s, e) => LoadProducts();
            filterBar.Controls.Add(_cmbCategory);

            // SAYAÇ
            _lblCount = new Label
            {
                Location = new Point(830, 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 161, 105)
            };
            filterBar.Controls.Add(_lblCount);

            // ÜRÜN GRİD
            Panel gridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            _productPanel.Controls.Add(gridContainer);
            gridContainer.BringToFront();

            _gridProducts = CreateProductGrid();
            gridContainer.Controls.Add(_gridProducts);

            // ═══════════════════════════════════════════════════════════
            // KATEGORİLER PANELİ
            // ═══════════════════════════════════════════════════════════
            _categoryPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(247, 250, 252), Visible = false };
            this.Controls.Add(_categoryPanel);

            Panel catGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            _categoryPanel.Controls.Add(catGridContainer);

            _gridCategories = CreateCategoryGrid();
            catGridContainer.Controls.Add(_gridCategories);

            _productPanel.BringToFront();
        }

        private DataGridView CreateProductGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true
            };
            grid.RowTemplate.Height = 50;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(45, 55, 72);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(237, 242, 247);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(45, 55, 72);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 252);

            grid.ColumnHeadersHeight = 55;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(113, 128, 150);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(237, 242, 247);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Color.FromArgb(226, 232, 240);

            grid.Columns.Add("Id", "ID"); grid.Columns["Id"].Visible = false;
            grid.Columns.Add("Barcode", "BARKOD"); grid.Columns["Barcode"].FillWeight = 80;
            grid.Columns.Add("Name", "ÜRÜN ADI"); grid.Columns["Name"].FillWeight = 150;
            grid.Columns.Add("Category", "KATEGORİ"); grid.Columns["Category"].FillWeight = 80;
            grid.Columns.Add("BuyPrice", "ALIŞ"); grid.Columns["BuyPrice"].FillWeight = 60;
            grid.Columns.Add("SellPrice", "SATIŞ"); grid.Columns["SellPrice"].FillWeight = 60;
            grid.Columns.Add("Stock", "STOK"); grid.Columns["Stock"].FillWeight = 50;

            // DÜZENLE - MAVİ
            var btnEdit = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "",
                Text = "Düzenle",
                UseColumnTextForButtonValue = true,
                Width = 90
            };
            grid.Columns.Add(btnEdit);

            // SİL - KIRMIZI
            var btnDel = new DataGridViewButtonColumn
            {
                Name = "Del",
                HeaderText = "",
                Text = "Sil",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            grid.Columns.Add(btnDel);

            grid.CellClick += GridProducts_CellClick;
            grid.CellFormatting += GridProducts_CellFormatting;

            return grid;
        }

        private DataGridView CreateCategoryGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true
            };
            grid.RowTemplate.Height = 60;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 13);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(45, 55, 72);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(237, 242, 247);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(45, 55, 72);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 252);

            grid.ColumnHeadersHeight = 60;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(113, 128, 150);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(237, 242, 247);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Color.FromArgb(226, 232, 240);

            grid.Columns.Add("Id", "ID"); grid.Columns["Id"].Visible = false;
            grid.Columns.Add("Name", "KATEGORİ ADI"); grid.Columns["Name"].FillWeight = 200;
            grid.Columns.Add("Count", "ÜRÜN SAYISI"); grid.Columns["Count"].FillWeight = 100;

            var btnDel = new DataGridViewButtonColumn
            {
                Name = "Del",
                HeaderText = "",
                Text = "Sil",
                UseColumnTextForButtonValue = true,
                Width = 100
            };
            grid.Columns.Add(btnDel);

            grid.CellClick += GridCategories_CellClick;
            grid.CellFormatting += GridCategories_CellFormatting;

            return grid;
        }

        private void SwitchTab(bool showProducts)
        {
            _isProductsTab = showProducts;
            _productPanel.Visible = showProducts;
            _categoryPanel.Visible = !showProducts;

            if (showProducts)
            {
                _tabProducts.BackColor = Color.FromArgb(72, 187, 120);
                _tabCategories.BackColor = Color.FromArgb(113, 128, 150);
                _btnAction.Text = "+ YENİ ÜRÜN";
                _productPanel.BringToFront();
            }
            else
            {
                _tabCategories.BackColor = Color.FromArgb(72, 187, 120);
                _tabProducts.BackColor = Color.FromArgb(113, 128, 150);
                _btnAction.Text = "+ YENİ KATEGORİ";
                _categoryPanel.BringToFront();
                LoadCategories();
            }
        }

        private void BtnAction_Click(object sender, EventArgs e)
        {
            if (_isProductsTab)
            {
                // ProductForm TopLevel=false gömülü bir form olduğundan
                // ShowDialog'a TopLevel owner vermek gerekir; FindForm() MainForm'u döner.
                var owner = this.FindForm();
                using (var f = new ProductEditForm())
                {
                    if (f.ShowDialog(owner) == DialogResult.OK)
                    {
                        LoadProducts();
                        LoadCategoryDropdown();
                    }
                }
            }
            else
            {
                using (Form inputForm = new Form())
                {
                    inputForm.Text = "Yeni Kategori";
                    inputForm.Size = new Size(400, 180);
                    inputForm.StartPosition = FormStartPosition.CenterParent;
                    inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    inputForm.MaximizeBox = false;
                    inputForm.MinimizeBox = false;
                    inputForm.BackColor = Color.White;

                    Label lbl = new Label { Text = "Kategori Adı:", Location = new Point(30, 35), AutoSize = true, Font = new Font("Segoe UI", 12) };
                    TextBox txt = new TextBox { Location = new Point(140, 30), Width = 220, Font = new Font("Segoe UI", 12) };
                    Button btnOk = new Button
                    {
                        Text = "Ekle",
                        Location = new Point(140, 80),
                        Size = new Size(100, 40),
                        BackColor = Color.FromArgb(56, 161, 105),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 11, FontStyle.Bold),
                        DialogResult = DialogResult.OK
                    };
                    btnOk.FlatAppearance.BorderSize = 0;

                    Button btnCancel = new Button
                    {
                        Text = "İptal",
                        Location = new Point(250, 80),
                        Size = new Size(100, 40),
                        BackColor = Color.FromArgb(226, 232, 240),
                        ForeColor = Color.FromArgb(45, 55, 72),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 11, FontStyle.Bold),
                        DialogResult = DialogResult.Cancel
                    };
                    btnCancel.FlatAppearance.BorderSize = 0;

                    inputForm.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                    inputForm.AcceptButton = btnOk;
                    inputForm.CancelButton = btnCancel;

                    if (inputForm.ShowDialog(this.FindForm()) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                    {
                        _categoryService.Add(new Category { Name = txt.Text.Trim() });
                        LoadCategories();
                        LoadCategoryDropdown();
                    }
                }
            }
        }

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
            {
                _gridProducts.Rows.Add(
                    p.Id,
                    p.Barcode ?? "-",
                    p.Name,
                    p.Category?.Name ?? "-",
                    p.BuyingPrice.ToString("₺#,##0.00"),
                    p.SellingPrice.ToString("₺#,##0.00"),
                    p.StockQuantity.ToString("N0")
                );
            }
            _lblCount.Text = $"{list.Count} ürün";
        }

        private void LoadCategories()
        {
            _gridCategories.Rows.Clear();
            var cats = _categoryService.GetCategories();
            var prods = _productService.GetProducts();

            foreach (var c in cats)
            {
                int cnt = prods.Count(p => p.CategoryId == c.Id);
                _gridCategories.Rows.Add(c.Id, c.Name, $"{cnt} ürün");
            }
        }

        private void GridProducts_CellClick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = Convert.ToInt32(_gridProducts.Rows[e.RowIndex].Cells["Id"].Value);

            if (_gridProducts.Columns[e.ColumnIndex].Name == "Edit")
            {
                var p = _productService.GetById(id);
                var owner = this.FindForm();
                using (var f = new ProductEditForm(p))
                    if (f.ShowDialog(owner) == DialogResult.OK) { LoadProducts(); LoadCategoryDropdown(); }
            }
            else if (_gridProducts.Columns[e.ColumnIndex].Name == "Del")
            {
                string name = _gridProducts.Rows[e.RowIndex].Cells["Name"].Value.ToString();
                if (MessageBox.Show($"'{name}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _productService.Delete(id);
                    LoadProducts();
                }
            }
        }

        private void GridProducts_CellFormatting(object s, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // STOK RENK
            if (e.ColumnIndex == 6)
            {
                string val = e.Value?.ToString()?.Replace(".", "").Replace(",", ".") ?? "0";
                if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double stk))
                {
                    if (stk <= 5) e.CellStyle.ForeColor = Color.FromArgb(229, 62, 62);
                    else if (stk <= 20) e.CellStyle.ForeColor = Color.FromArgb(237, 137, 54);
                }
            }
            // DÜZENLE BUTON - MAVİ
            else if (e.ColumnIndex == 7)
            {
                e.CellStyle.BackColor = Color.FromArgb(66, 153, 225);
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }
            // SİL BUTON - KIRMIZI
            else if (e.ColumnIndex == 8)
            {
                e.CellStyle.BackColor = Color.FromArgb(229, 62, 62);
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }
        }

        private void GridCategories_CellClick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _gridCategories.Columns[e.ColumnIndex].Name != "Del") return;
            int id = Convert.ToInt32(_gridCategories.Rows[e.RowIndex].Cells["Id"].Value);
            string name = _gridCategories.Rows[e.RowIndex].Cells["Name"].Value.ToString();

            if (MessageBox.Show($"'{name}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _categoryService.Delete(id);
                LoadCategories();
                LoadCategoryDropdown();
            }
        }

        private void GridCategories_CellFormatting(object s, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // SİL BUTON - KIRMIZI
            if (e.ColumnIndex == 3)
            {
                e.CellStyle.BackColor = Color.FromArgb(229, 62, 62);
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            }
        }
    }
}