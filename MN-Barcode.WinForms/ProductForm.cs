using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.WinForms
{
    public partial class ProductForm : Form, IEkranYenileme
    {
        // ── Klasik/açık palet (Satış Geçmişi & İade sayfalarıyla uyumlu) ──
        private static readonly Color ColBg       = Color.FromArgb(245, 247, 250);
        private static readonly Color ColSurface  = Color.White;
        private static readonly Color ColBorder   = Color.FromArgb(214, 221, 230);
        private static readonly Color ColText      = Color.FromArgb(30, 41, 59);
        private static readonly Color ColStrong   = Color.FromArgb(15, 23, 42);
        private static readonly Color ColMuted    = Color.FromArgb(100, 116, 139);
        private static readonly Color ColHeaderBg = Color.FromArgb(241, 245, 249);
        private static readonly Color ColBlue      = Color.FromArgb(37, 99, 235);
        private static readonly Color ColBlueDark = Color.FromArgb(29, 78, 188);
        private static readonly Color ColBlueSoft = Color.FromArgb(232, 240, 254);
        private static readonly Color ColGreen     = Color.FromArgb(22, 163, 74);
        private static readonly Color ColAmber     = Color.FromArgb(217, 119, 6);
        private static readonly Color ColRed       = Color.FromArgb(220, 38, 38);

        /// <summary>
        /// Izgara buton hücrelerinin yazı tipi.
        ///
        /// Eskiden CellFormatting içinde her seferinde "new Font(...)" ile üretiliyordu.
        /// CellFormatting her hücre için, her yeniden çizimde tetiklenir; listede
        /// gezinmek saniyede binlerce GDI font tanıtıcısı ayırıp hiçbirini bırakmıyordu.
        /// Uzun bir mesai sonunda GDI tanıtıcıları tükenip arayüz bozuluyordu.
        /// Tek statik örnek yeterli — hiç ayırma yapılmaz.
        /// </summary>
        private static readonly Font GridButonFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);

        private ProductService _productService;
        private CategoryService _categoryService;

        /// <summary>Arama kutusunu geciktiren zamanlayıcı — her harfte sorgu atılmaz.</summary>
        private System.Windows.Forms.Timer _searchTimer;

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
            this.BackColor = ColBg;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ═══════════════ ÜRÜNLER PANELİ (önce eklenir) ═══════════════
            _productPanel = new Panel { Dock = DockStyle.Fill, BackColor = ColBg };
            this.Controls.Add(_productPanel);

            Panel gridContainer = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Padding = new Padding(16, 12, 16, 16) };
            _productPanel.Controls.Add(gridContainer);

            _gridProducts = CreateProductGrid();
            gridContainer.Controls.Add(_gridProducts);

            _productPanel.Controls.Add(BuildFilterBar());   // Dock.Top

            // ═══════════════ KATEGORİLER PANELİ ═══════════════
            _categoryPanel = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Visible = false };
            this.Controls.Add(_categoryPanel);

            Panel catGridContainer = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Padding = new Padding(16, 12, 16, 16) };
            _categoryPanel.Controls.Add(catGridContainer);

            _gridCategories = CreateCategoryGrid();
            catGridContainer.Controls.Add(_gridCategories);

            // ═══════════════ HEADER (en son eklenir → en üstte) ═══════════════
            this.Controls.Add(BuildHeader());

            _productPanel.BringToFront();
        }

        // Başlık + eylem butonu + sekmeler
        private Panel BuildHeader()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 116, BackColor = ColSurface };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            // Sekme satırı (altta)
            header.Controls.Add(BuildTabRow());
            // Başlık + eylem satırı (üstte)
            header.Controls.Add(BuildTitleRow());

            return header;
        }

        private TableLayoutPanel BuildTitleRow()
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 62,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ColSurface,
                Padding = new Padding(24, 14, 24, 0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Label title = new Label
            {
                Text = "Ürün Yönetimi",
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = ColStrong,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            row.Controls.Add(title, 0, 0);

            _btnAction = new Button
            {
                Text = "+  Yeni Ürün",
                Size = new Size(150, 38),
                BackColor = ColBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 2, 0, 0)
            };
            _btnAction.FlatAppearance.BorderSize = 0;
            _btnAction.FlatAppearance.MouseOverBackColor = ColBlueDark;
            _btnAction.Click += BtnAction_Click;
            row.Controls.Add(_btnAction, 1, 0);

            return row;
        }

        private FlowLayoutPanel BuildTabRow()
        {
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = ColSurface,
                Padding = new Padding(24, 6, 24, 0)
            };

            _tabProducts = MakeTab("Ürünler");
            _tabProducts.Click += (s, e) => SwitchTab(true);
            flow.Controls.Add(_tabProducts);

            _tabCategories = MakeTab("Kategoriler");
            _tabCategories.Click += (s, e) => SwitchTab(false);
            flow.Controls.Add(_tabCategories);

            ApplyTabStyle();
            return flow;
        }

        private Button MakeTab(string text) => new Button
        {
            Text = text,
            Size = new Size(130, 36),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };

        private void ApplyTabStyle()
        {
            StyleTab(_tabProducts, _isProductsTab);
            StyleTab(_tabCategories, !_isProductsTab);
        }

        private void StyleTab(Button tab, bool active)
        {
            tab.FlatAppearance.BorderSize = 0;
            if (active)
            {
                tab.BackColor = ColBlueSoft;
                tab.ForeColor = ColBlue;
            }
            else
            {
                tab.BackColor = ColSurface;
                tab.ForeColor = ColMuted;
                tab.FlatAppearance.MouseOverBackColor = ColHeaderBg;
            }
        }

        // Ürünler sekmesi filtre çubuğu
        private Panel BuildFilterBar()
        {
            Panel filterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 66,
                BackColor = ColSurface
            };
            filterBar.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(pen, 0, filterBar.Height - 1, filterBar.Width, filterBar.Height - 1);
            };

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = ColSurface,
                Padding = new Padding(24, 14, 24, 0)
            };
            filterBar.Controls.Add(flow);

            flow.Controls.Add(new Label
            {
                Text = "Ara:",
                Font = new Font("Segoe UI", 11),
                ForeColor = ColMuted,
                AutoSize = true,
                Margin = new Padding(0, 9, 10, 0)
            });

            _txtSearch = new TextBox
            {
                Width = 320,
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 4, 0, 0)
            };
            // Her tuş vuruşunda sorgu atmak yerine yazma bitince tek sorgu.
            // "peynir" yazmak eskiden 6 ayrı LIKE '%..%' taraması + 6 tam ızgara
            // yeniden inşası demekti; arayüz "Yanıt vermiyor" duruma düşüyordu.
            _searchTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); LoadProducts(); };
            _txtSearch.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };
            flow.Controls.Add(_txtSearch);

            flow.Controls.Add(new Label
            {
                Text = "Kategori:",
                Font = new Font("Segoe UI", 11),
                ForeColor = ColMuted,
                AutoSize = true,
                Margin = new Padding(20, 9, 10, 0)
            });

            _cmbCategory = new ComboBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 0)
            };
            _cmbCategory.SelectedIndexChanged += (s, e) => LoadProducts();
            flow.Controls.Add(_cmbCategory);

            _lblCount = new Label
            {
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = ColGreen,
                AutoSize = true,
                Margin = new Padding(20, 9, 0, 0)
            };
            flow.Controls.Add(_lblCount);

            return filterBar;
        }

        private DataGridView CreateProductGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ColSurface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(233, 238, 244)
            };
            grid.RowTemplate.Height = 46;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10.5f);
            grid.DefaultCellStyle.ForeColor = ColText;
            grid.DefaultCellStyle.BackColor = ColSurface;
            grid.DefaultCellStyle.SelectionBackColor = ColBlueSoft;
            grid.DefaultCellStyle.SelectionForeColor = ColStrong;
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);

            grid.ColumnHeadersHeight = 44;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ColMuted;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ColHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            grid.Columns.Add("Id", "ID"); grid.Columns["Id"].Visible = false;
            grid.Columns.Add("Barcode", "BARKOD"); grid.Columns["Barcode"].FillWeight = 90;
            grid.Columns.Add("Name", "ÜRÜN ADI"); grid.Columns["Name"].FillWeight = 170;
            grid.Columns.Add("Category", "KATEGORİ"); grid.Columns["Category"].FillWeight = 90;
            grid.Columns.Add("BuyPrice", "ALIŞ"); grid.Columns["BuyPrice"].FillWeight = 65;
            grid.Columns["BuyPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns.Add("SellPrice", "SATIŞ"); grid.Columns["SellPrice"].FillWeight = 65;
            grid.Columns["SellPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["SellPrice"].DefaultCellStyle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            grid.Columns.Add("Stock", "STOK"); grid.Columns["Stock"].FillWeight = 55;
            grid.Columns["Stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns["Stock"].DefaultCellStyle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);

            // DÜZENLE
            var btnEdit = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "",
                Text = "Düzenle",
                UseColumnTextForButtonValue = true,
                FillWeight = 70,
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(btnEdit);

            // SİL
            var btnDel = new DataGridViewButtonColumn
            {
                Name = "Del",
                HeaderText = "",
                Text = "Sil",
                UseColumnTextForButtonValue = true,
                FillWeight = 50,
                FlatStyle = FlatStyle.Flat
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
                BackgroundColor = ColSurface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(233, 238, 244)
            };
            grid.RowTemplate.Height = 52;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11.5f);
            grid.DefaultCellStyle.ForeColor = ColText;
            grid.DefaultCellStyle.BackColor = ColSurface;
            grid.DefaultCellStyle.SelectionBackColor = ColBlueSoft;
            grid.DefaultCellStyle.SelectionForeColor = ColStrong;
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);

            grid.ColumnHeadersHeight = 44;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ColMuted;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ColHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            grid.Columns.Add("Id", "ID"); grid.Columns["Id"].Visible = false;
            grid.Columns.Add("Name", "KATEGORİ ADI"); grid.Columns["Name"].FillWeight = 200;
            grid.Columns.Add("Count", "ÜRÜN SAYISI"); grid.Columns["Count"].FillWeight = 100;

            var btnDel = new DataGridViewButtonColumn
            {
                Name = "Del",
                HeaderText = "",
                Text = "Sil",
                UseColumnTextForButtonValue = true,
                FillWeight = 60,
                FlatStyle = FlatStyle.Flat
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

            ApplyTabStyle();

            if (showProducts)
            {
                _btnAction.Text = "+  Yeni Ürün";
                _productPanel.BringToFront();
            }
            else
            {
                _btnAction.Text = "+  Yeni Kategori";
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
                    inputForm.BackColor = ColSurface;

                    Label lbl = new Label { Text = "Kategori Adı:", Location = new Point(30, 35), AutoSize = true, Font = new Font("Segoe UI", 12) };
                    TextBox txt = new TextBox { Location = new Point(140, 30), Width = 220, Font = new Font("Segoe UI", 12), BorderStyle = BorderStyle.FixedSingle };
                    Button btnOk = new Button
                    {
                        Text = "Ekle",
                        Location = new Point(140, 80),
                        Size = new Size(100, 40),
                        BackColor = ColBlue,
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
                        ForeColor = ColText,
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

        /// <summary>Önbellekten tekrar açıldığında verileri tazeler (bkz. IEkranYenileme).</summary>
        public void EkraniYenile()
        {
            LoadCategoryDropdown();
            LoadProducts();
            LoadCategories();
        }

        private void LoadProducts()
        {
            // Kategori süzgeci veritabanında uygulanır. Eskiden tüm ürünler çekilip
            // bellekte süzülüyordu — 20 ürünlük bir kategori için tüm katalog taşınıyordu.
            string kategori = _cmbCategory.SelectedIndex > 0
                ? _cmbCategory.SelectedItem?.ToString() ?? ""
                : "";

            List<Product> list;
            try
            {
                list = _productService.GetProducts(_txtSearch.Text.Trim(), kategori);
            }
            catch (Exception ex)
            {
                AppLogger.Yaz("Ürün listesi yüklenemedi", ex);
                MessageBox.Show("Ürün listesi yüklenemedi. Ayrıntı hata kaydına yazıldı.",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Satırlar eklenirken her eklemede yeniden yerleşim/çizim yapılmasın.
            _gridProducts.SuspendLayout();
            _gridProducts.Rows.Clear();

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

            _gridProducts.ResumeLayout();
            _lblCount.Text = $"{list.Count} ürün";
        }

        private void LoadCategories()
        {
            _gridCategories.Rows.Clear();
            var catsWithCount = _categoryService.GetCategoriesWithCount();

            foreach (var item in catsWithCount)
            {
                _gridCategories.Rows.Add(item.Category.Id, item.Category.Name, $"{item.ProductCount} ürün");
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
                string name = _gridProducts.Rows[e.RowIndex].Cells["Name"].Value?.ToString() ?? "";
                if (MessageBox.Show($"'{name}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        _productService.Delete(id);
                        LoadProducts();
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Satış geçmişi olan ürün — silinmesi raporları bozar.
                        MessageBox.Show(ex.Message, "Silinemez", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Yaz("Ürün silinemedi", ex);
                        MessageBox.Show("Ürün silinemedi. Ayrıntı hata kaydına yazıldı.",
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void GridProducts_CellFormatting(object s, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // STOK RENK (kolon 6)
            if (e.ColumnIndex == 6)
            {
                string val = e.Value?.ToString()?.Replace(".", "").Replace(",", ".") ?? "0";
                if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double stk))
                {
                    if (stk <= 5) e.CellStyle.ForeColor = ColRed;
                    else if (stk <= 20) e.CellStyle.ForeColor = ColAmber;
                    else e.CellStyle.ForeColor = ColGreen;
                }
            }
            // DÜZENLE BUTON - MAVİ (kolon 7)
            else if (e.ColumnIndex == 7)
            {
                e.CellStyle.BackColor = ColBlue;
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.SelectionBackColor = ColBlueDark;
                e.CellStyle.Font = GridButonFont;
            }
            // SİL BUTON - KIRMIZI (kolon 8)
            else if (e.ColumnIndex == 8)
            {
                e.CellStyle.BackColor = ColRed;
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.SelectionBackColor = Color.FromArgb(185, 28, 28);
                e.CellStyle.Font = GridButonFont;
            }
        }

        private void GridCategories_CellClick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _gridCategories.Columns[e.ColumnIndex].Name != "Del") return;
            int id = Convert.ToInt32(_gridCategories.Rows[e.RowIndex].Cells["Id"].Value);
            string name = _gridCategories.Rows[e.RowIndex].Cells["Name"].Value.ToString();

            if (MessageBox.Show($"'{name}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    _categoryService.Delete(id);
                    LoadCategories();
                    LoadCategoryDropdown();
                }
                catch (InvalidOperationException ex)
                {
                    // İçinde ürün olan kategori — eskiden burada uygulama çöküyordu.
                    MessageBox.Show(ex.Message, "Silinemez", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    AppLogger.Yaz("Kategori silinemedi", ex);
                    MessageBox.Show("Kategori silinemedi. Ayrıntı hata kaydına yazıldı.",
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void GridCategories_CellFormatting(object s, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // SİL BUTON - KIRMIZI (kolon 3)
            if (e.ColumnIndex == 3)
            {
                e.CellStyle.BackColor = ColRed;
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.SelectionBackColor = Color.FromArgb(185, 28, 28);
                e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }
        }
    }
}
