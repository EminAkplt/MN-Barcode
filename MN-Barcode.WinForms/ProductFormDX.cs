using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class ProductFormDX : XtraForm
    {
        private ProductService _productService;
        private CategoryService _categoryService;

        private PanelControl _productPanel;
        private PanelControl _categoryPanel;
        private SimpleButton _tabProducts;
        private SimpleButton _tabCategories;
        private SimpleButton _btnAction;

        private GridControl _gridProducts;
        private GridView _gridViewProducts;
        private TextEdit _txtSearch;
        private ComboBoxEdit _cmbCategory;
        private LabelControl _lblCount;

        private GridControl _gridCategories;
        private GridView _gridViewCategories;

        private bool _isProductsTab = true;

        public ProductFormDX()
        {
            _productService = new ProductService();
            _categoryService = new CategoryService();
            InitUI();

            this.Shown += (s, e) =>
            {
                LoadCategoryDropdown();
                LoadProducts();
                LoadCategories();
            };
        }

        private void InitUI()
        {
            this.Text = "Ürün Yönetimi";
            this.BackColor = Color.White;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.LookAndFeel.SkinName = "Office 2019 Black";

            // ═══════════════════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════════════════
            PanelControl header = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 70,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            header.Appearance.BackColor = Color.FromArgb(45, 55, 72);
            this.Controls.Add(header);

            // ÜRÜNLER TAB
            _tabProducts = new SimpleButton
            {
                Text = "📦 ÜRÜNLER",
                Size = new Size(160, 50),
                Location = new Point(30, 10),
                Cursor = Cursors.Hand
            };
            _tabProducts.Appearance.BackColor = Color.FromArgb(72, 187, 120);
            _tabProducts.Appearance.ForeColor = Color.White;
            _tabProducts.Appearance.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            _tabProducts.Appearance.Options.UseBackColor = true;
            _tabProducts.Appearance.Options.UseForeColor = true;
            _tabProducts.Appearance.Options.UseFont = true;
            _tabProducts.Click += (s, e) => SwitchTab(true);
            header.Controls.Add(_tabProducts);

            // KATEGORİLER TAB
            _tabCategories = new SimpleButton
            {
                Text = "📂 KATEGORİLER",
                Size = new Size(180, 50),
                Location = new Point(200, 10),
                Cursor = Cursors.Hand
            };
            _tabCategories.Appearance.BackColor = Color.FromArgb(113, 128, 150);
            _tabCategories.Appearance.ForeColor = Color.White;
            _tabCategories.Appearance.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            _tabCategories.Appearance.Options.UseBackColor = true;
            _tabCategories.Appearance.Options.UseForeColor = true;
            _tabCategories.Appearance.Options.UseFont = true;
            _tabCategories.Click += (s, e) => SwitchTab(false);
            header.Controls.Add(_tabCategories);

            // YENİ EKLE BUTONU
            _btnAction = new SimpleButton
            {
                Text = "+ YENİ ÜRÜN",
                Size = new Size(160, 45),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Cursor = Cursors.Hand
            };
            _btnAction.Location = new Point(header.Width - 200, 12);
            _btnAction.Appearance.BackColor = Color.FromArgb(56, 161, 105);
            _btnAction.Appearance.ForeColor = Color.White;
            _btnAction.Appearance.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            _btnAction.Appearance.Options.UseBackColor = true;
            _btnAction.Appearance.Options.UseForeColor = true;
            _btnAction.Appearance.Options.UseFont = true;
            _btnAction.Click += BtnAction_Click;
            header.Controls.Add(_btnAction);

            // ═══════════════════════════════════════════════════════════
            // ÜRÜNLER PANELİ
            // ═══════════════════════════════════════════════════════════
            _productPanel = new PanelControl { Dock = DockStyle.Fill, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            _productPanel.Appearance.BackColor = Color.FromArgb(247, 250, 252);
            this.Controls.Add(_productPanel);

            // FİLTRE ALANI
            PanelControl filterBar = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 80,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(30, 20, 30, 20)
            };
            filterBar.Appearance.BackColor = Color.White;
            _productPanel.Controls.Add(filterBar);

            // ARAMA LABEL
            LabelControl lblSearch = new LabelControl
            {
                Text = "🔍 Ara:",
                Location = new Point(30, 28),
                AutoSizeMode = LabelAutoSizeMode.Default
            };
            lblSearch.Appearance.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblSearch.Appearance.ForeColor = Color.FromArgb(45, 55, 72);
            filterBar.Controls.Add(lblSearch);

            // ARAMA KUTUSU
            _txtSearch = new TextEdit
            {
                Location = new Point(110, 22),
                Size = new Size(350, 38)
            };
            _txtSearch.Properties.Appearance.Font = new Font("Segoe UI", 13);
            _txtSearch.TextChanged += (s, e) => LoadProducts();
            filterBar.Controls.Add(_txtSearch);

            // KATEGORİ LABEL
            LabelControl lblCat = new LabelControl
            {
                Text = "Kategori:",
                Location = new Point(490, 28),
                AutoSizeMode = LabelAutoSizeMode.Default
            };
            lblCat.Appearance.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblCat.Appearance.ForeColor = Color.FromArgb(45, 55, 72);
            filterBar.Controls.Add(lblCat);

            // KATEGORİ DROPDOWN
            _cmbCategory = new ComboBoxEdit
            {
                Location = new Point(580, 22),
                Size = new Size(220, 38)
            };
            _cmbCategory.Properties.Appearance.Font = new Font("Segoe UI", 12);
            _cmbCategory.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            _cmbCategory.SelectedIndexChanged += (s, e) => LoadProducts();
            filterBar.Controls.Add(_cmbCategory);

            // SAYAÇ
            _lblCount = new LabelControl
            {
                Location = new Point(830, 28),
                AutoSizeMode = LabelAutoSizeMode.Default
            };
            _lblCount.Appearance.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            _lblCount.Appearance.ForeColor = Color.FromArgb(56, 161, 105);
            filterBar.Controls.Add(_lblCount);

            // ÜRÜN GRİD
            PanelControl gridContainer = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(20)
            };
            gridContainer.Appearance.BackColor = Color.White;
            _productPanel.Controls.Add(gridContainer);
            gridContainer.BringToFront();

            CreateProductGrid();
            gridContainer.Controls.Add(_gridProducts);

            // ═══════════════════════════════════════════════════════════
            // KATEGORİLER PANELİ
            // ═══════════════════════════════════════════════════════════
            _categoryPanel = new PanelControl { Dock = DockStyle.Fill, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, Visible = false };
            _categoryPanel.Appearance.BackColor = Color.FromArgb(247, 250, 252);
            this.Controls.Add(_categoryPanel);

            PanelControl catGridContainer = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(20)
            };
            catGridContainer.Appearance.BackColor = Color.White;
            _categoryPanel.Controls.Add(catGridContainer);

            CreateCategoryGrid();
            catGridContainer.Controls.Add(_gridCategories);

            _productPanel.BringToFront();
        }

        private void CreateProductGrid()
        {
            _gridProducts = new GridControl { Dock = DockStyle.Fill };
            _gridViewProducts = new GridView(_gridProducts);
            _gridProducts.MainView = _gridViewProducts;

            // Grid Ayarları
            _gridViewProducts.OptionsView.ShowGroupPanel = false;
            _gridViewProducts.OptionsView.ShowIndicator = false;
            _gridViewProducts.OptionsSelection.EnableAppearanceFocusedCell = false;
            _gridViewProducts.OptionsBehavior.Editable = true; // Butonlar için gerekli
            _gridViewProducts.RowHeight = 50;

            // Kolonlar
            _gridViewProducts.Columns.Add(new GridColumn { FieldName = "Id", Caption = "ID", Visible = false });
            _gridViewProducts.Columns.Add(new GridColumn { FieldName = "Barcode", Caption = "BARKOD", VisibleIndex = 0, Width = 120 });
            _gridViewProducts.Columns.Add(new GridColumn { FieldName = "Name", Caption = "ÜRÜN ADI", VisibleIndex = 1, Width = 200 });
            _gridViewProducts.Columns.Add(new GridColumn { FieldName = "Category", Caption = "KATEGORİ", VisibleIndex = 2, Width = 120 });
            _gridViewProducts.Columns.Add(new GridColumn { FieldName = "BuyPrice", Caption = "ALIŞ FİYATI", VisibleIndex = 3, Width = 100 });
            _gridViewProducts.Columns.Add(new GridColumn { FieldName = "SellPrice", Caption = "SATIŞ FİYATI", VisibleIndex = 4, Width = 100 });
            _gridViewProducts.Columns.Add(new GridColumn { FieldName = "Stock", Caption = "STOK", VisibleIndex = 5, Width = 80 });

            // ════════════════════════════════════════════════════════════════
            // MAVİ DÜZENLE BUTONU
            // ════════════════════════════════════════════════════════════════
            var btnEditCol = new GridColumn
            {
                FieldName = "Edit",
                Caption = "",
                VisibleIndex = 6,
                Width = 100,
                UnboundType = DevExpress.Data.UnboundColumnType.Object
            };
            btnEditCol.OptionsColumn.AllowEdit = true;
            _gridViewProducts.Columns.Add(btnEditCol);

            var editButtonRepo = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            editButtonRepo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            editButtonRepo.Buttons.Clear();
            var editBtn = new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            editBtn.Caption = "✏️ Düzenle";
            editBtn.Appearance.BackColor = Color.FromArgb(66, 153, 225); // MAVİ
            editBtn.Appearance.ForeColor = Color.White;
            editBtn.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            editBtn.Appearance.Options.UseBackColor = true;
            editBtn.Appearance.Options.UseForeColor = true;
            editBtn.Appearance.Options.UseFont = true;
            editBtn.Width = 90;
            editButtonRepo.Buttons.Add(editBtn);
            editButtonRepo.ButtonClick += EditButton_Click;
            btnEditCol.ColumnEdit = editButtonRepo;
            _gridProducts.RepositoryItems.Add(editButtonRepo);

            // ════════════════════════════════════════════════════════════════
            // KIRMIZI SİL BUTONU
            // ════════════════════════════════════════════════════════════════
            var btnDeleteCol = new GridColumn
            {
                FieldName = "Delete",
                Caption = "",
                VisibleIndex = 7,
                Width = 80,
                UnboundType = DevExpress.Data.UnboundColumnType.Object
            };
            btnDeleteCol.OptionsColumn.AllowEdit = true;
            _gridViewProducts.Columns.Add(btnDeleteCol);

            var deleteButtonRepo = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            deleteButtonRepo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            deleteButtonRepo.Buttons.Clear();
            var delBtn = new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            delBtn.Caption = "🗑️ Sil";
            delBtn.Appearance.BackColor = Color.FromArgb(229, 62, 62); // KIRMIZI
            delBtn.Appearance.ForeColor = Color.White;
            delBtn.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            delBtn.Appearance.Options.UseBackColor = true;
            delBtn.Appearance.Options.UseForeColor = true;
            delBtn.Appearance.Options.UseFont = true;
            delBtn.Width = 70;
            deleteButtonRepo.Buttons.Add(delBtn);
            deleteButtonRepo.ButtonClick += DeleteButton_Click;
            btnDeleteCol.ColumnEdit = deleteButtonRepo;
            _gridProducts.RepositoryItems.Add(deleteButtonRepo);

            // Stok renkli gösterimi + satır renklendirme
            _gridViewProducts.RowCellStyle += (s, e) =>
            {
                if (e.Column.FieldName == "Stock")
                {
                    var val = e.CellValue?.ToString()?.Replace(".", "").Replace(",", ".") ?? "0";
                    if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double stk))
                    {
                        if (stk <= 5)
                        {
                            e.Appearance.ForeColor = Color.FromArgb(229, 62, 62);
                            e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                        }
                        else if (stk <= 20)
                        {
                            e.Appearance.ForeColor = Color.FromArgb(237, 137, 54);
                            e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                        }
                    }
                }
            };

            // Tüm kolonları ReadOnly yap (buton kolonları hariç)
            foreach (GridColumn col in _gridViewProducts.Columns)
            {
                if (col.FieldName != "Edit" && col.FieldName != "Delete")
                    col.OptionsColumn.AllowEdit = false;
            }
        }

        private void CreateCategoryGrid()
        {
            _gridCategories = new GridControl { Dock = DockStyle.Fill };
            _gridViewCategories = new GridView(_gridCategories);
            _gridCategories.MainView = _gridViewCategories;

            _gridViewCategories.OptionsView.ShowGroupPanel = false;
            _gridViewCategories.OptionsView.ShowIndicator = false;
            _gridViewCategories.OptionsSelection.EnableAppearanceFocusedCell = false;
            _gridViewCategories.OptionsBehavior.Editable = true; // Butonlar için
            _gridViewCategories.RowHeight = 60;

            _gridViewCategories.Columns.Add(new GridColumn { FieldName = "Id", Caption = "ID", Visible = false });
            _gridViewCategories.Columns.Add(new GridColumn { FieldName = "Name", Caption = "KATEGORİ ADI", VisibleIndex = 0, Width = 300 });
            _gridViewCategories.Columns.Add(new GridColumn { FieldName = "Count", Caption = "ÜRÜN SAYISI", VisibleIndex = 1, Width = 150 });

            // ════════════════════════════════════════════════════════════════
            // MAVİ DÜZENLE BUTONU (Kategori)
            // ════════════════════════════════════════════════════════════════
            var btnEditCatCol = new GridColumn
            {
                FieldName = "EditCat",
                Caption = "",
                VisibleIndex = 2,
                Width = 110,
                UnboundType = DevExpress.Data.UnboundColumnType.Object
            };
            btnEditCatCol.OptionsColumn.AllowEdit = true;
            _gridViewCategories.Columns.Add(btnEditCatCol);

            var editCatRepo = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            editCatRepo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            editCatRepo.Buttons.Clear();
            var editCatBtn = new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            editCatBtn.Caption = "✏️ Düzenle";
            editCatBtn.Appearance.BackColor = Color.FromArgb(66, 153, 225); // MAVİ
            editCatBtn.Appearance.ForeColor = Color.White;
            editCatBtn.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            editCatBtn.Appearance.Options.UseBackColor = true;
            editCatBtn.Appearance.Options.UseForeColor = true;
            editCatBtn.Appearance.Options.UseFont = true;
            editCatBtn.Width = 100;
            editCatRepo.Buttons.Add(editCatBtn);
            editCatRepo.ButtonClick += EditCategoryButton_Click;
            btnEditCatCol.ColumnEdit = editCatRepo;
            _gridCategories.RepositoryItems.Add(editCatRepo);

            // ════════════════════════════════════════════════════════════════
            // KIRMIZI SİL BUTONU (Kategori)
            // ════════════════════════════════════════════════════════════════
            var btnDeleteCatCol = new GridColumn
            {
                FieldName = "DeleteCat",
                Caption = "",
                VisibleIndex = 3,
                Width = 90,
                UnboundType = DevExpress.Data.UnboundColumnType.Object
            };
            btnDeleteCatCol.OptionsColumn.AllowEdit = true;
            _gridViewCategories.Columns.Add(btnDeleteCatCol);

            var deleteCatRepo = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            deleteCatRepo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            deleteCatRepo.Buttons.Clear();
            var delCatBtn = new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            delCatBtn.Caption = "🗑️ Sil";
            delCatBtn.Appearance.BackColor = Color.FromArgb(229, 62, 62); // KIRMIZI
            delCatBtn.Appearance.ForeColor = Color.White;
            delCatBtn.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            delCatBtn.Appearance.Options.UseBackColor = true;
            delCatBtn.Appearance.Options.UseForeColor = true;
            delCatBtn.Appearance.Options.UseFont = true;
            delCatBtn.Width = 80;
            deleteCatRepo.Buttons.Add(delCatBtn);
            deleteCatRepo.ButtonClick += DeleteCategoryButton_Click;
            btnDeleteCatCol.ColumnEdit = deleteCatRepo;
            _gridCategories.RepositoryItems.Add(deleteCatRepo);

            // Tüm kolonları ReadOnly yap (buton kolonları hariç)
            foreach (GridColumn col in _gridViewCategories.Columns)
            {
                if (col.FieldName != "EditCat" && col.FieldName != "DeleteCat")
                    col.OptionsColumn.AllowEdit = false;
            }
        }

        private void SwitchTab(bool showProducts)
        {
            _isProductsTab = showProducts;
            _productPanel.Visible = showProducts;
            _categoryPanel.Visible = !showProducts;

            if (showProducts)
            {
                _tabProducts.Appearance.BackColor = Color.FromArgb(72, 187, 120);
                _tabCategories.Appearance.BackColor = Color.FromArgb(113, 128, 150);
                _btnAction.Text = "+ YENİ ÜRÜN";
                _productPanel.BringToFront();
            }
            else
            {
                _tabCategories.Appearance.BackColor = Color.FromArgb(72, 187, 120);
                _tabProducts.Appearance.BackColor = Color.FromArgb(113, 128, 150);
                _btnAction.Text = "+ YENİ KATEGORİ";
                _categoryPanel.BringToFront();
                LoadCategories();
            }
        }

        private void BtnAction_Click(object sender, EventArgs e)
        {
            if (_isProductsTab)
            {
                using (var f = new ProductEditForm())
                {
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        LoadProducts();
                        LoadCategoryDropdown();
                    }
                }
            }
            else
            {
                ShowCategoryDialog(null); // Yeni kategori
            }
        }

        private void ShowCategoryDialog(Category existingCategory)
        {
            bool isEdit = existingCategory != null;
            using (var inputForm = new XtraForm())
            {
                inputForm.Text = isEdit ? "Kategori Düzenle" : "Yeni Kategori";
                inputForm.Size = new Size(400, 180);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;
                inputForm.LookAndFeel.SkinName = "Office 2019 Black";

                LabelControl lbl = new LabelControl { Text = "Kategori Adı:", Location = new Point(30, 35) };
                lbl.Appearance.Font = new Font("Segoe UI", 12);
                TextEdit txt = new TextEdit { Location = new Point(140, 30), Width = 220 };
                txt.Properties.Appearance.Font = new Font("Segoe UI", 12);
                if (isEdit) txt.Text = existingCategory.Name;

                SimpleButton btnOk = new SimpleButton
                {
                    Text = isEdit ? "Kaydet" : "Ekle",
                    Location = new Point(140, 80),
                    Size = new Size(100, 40),
                    DialogResult = DialogResult.OK
                };
                btnOk.Appearance.BackColor = Color.FromArgb(56, 161, 105);
                btnOk.Appearance.ForeColor = Color.White;
                btnOk.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                btnOk.Appearance.Options.UseBackColor = true;
                btnOk.Appearance.Options.UseForeColor = true;
                btnOk.Appearance.Options.UseFont = true;

                SimpleButton btnCancel = new SimpleButton
                {
                    Text = "İptal",
                    Location = new Point(250, 80),
                    Size = new Size(100, 40),
                    DialogResult = DialogResult.Cancel
                };
                btnCancel.Appearance.BackColor = Color.FromArgb(226, 232, 240);
                btnCancel.Appearance.ForeColor = Color.FromArgb(45, 55, 72);
                btnCancel.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                btnCancel.Appearance.Options.UseBackColor = true;
                btnCancel.Appearance.Options.UseForeColor = true;
                btnCancel.Appearance.Options.UseFont = true;

                inputForm.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                inputForm.AcceptButton = btnOk;
                inputForm.CancelButton = btnCancel;

                if (inputForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                {
                    if (isEdit)
                    {
                        existingCategory.Name = txt.Text.Trim();
                        _categoryService.Update(existingCategory);
                    }
                    else
                    {
                        _categoryService.Add(new Category { Name = txt.Text.Trim() });
                    }
                    LoadCategories();
                    LoadCategoryDropdown();
                }
            }
        }

        private void LoadCategoryDropdown()
        {
            _cmbCategory.Properties.Items.Clear();
            _cmbCategory.Properties.Items.Add("Tümü");
            foreach (var c in _categoryService.GetCategories())
                _cmbCategory.Properties.Items.Add(c.Name);
            _cmbCategory.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            var list = _productService.GetProducts(_txtSearch.Text.Trim());

            if (_cmbCategory.SelectedIndex > 0)
            {
                string cat = _cmbCategory.SelectedItem.ToString();
                list = list.Where(p => p.Category?.Name == cat).ToList();
            }

            var dataSource = list.Select(p => new
            {
                p.Id,
                Barcode = p.Barcode ?? "-",
                p.Name,
                Category = p.Category?.Name ?? "-",
                BuyPrice = p.BuyingPrice.ToString("₺#,##0.00"),
                SellPrice = p.SellingPrice.ToString("₺#,##0.00"),
                Stock = p.StockQuantity.ToString("N0")
            }).ToList();

            _gridProducts.DataSource = dataSource;
            _lblCount.Text = $"{list.Count} ürün";
        }

        private void LoadCategories()
        {
            var cats = _categoryService.GetCategories();
            var prods = _productService.GetProducts();

            var dataSource = cats.Select(c => new
            {
                c.Id,
                c.Name,
                Count = $"{prods.Count(p => p.CategoryId == c.Id)} ürün"
            }).ToList();

            _gridCategories.DataSource = dataSource;
        }

        private void EditButton_Click(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            int rowHandle = _gridViewProducts.FocusedRowHandle;
            if (rowHandle < 0) return;
            int id = Convert.ToInt32(_gridViewProducts.GetRowCellValue(rowHandle, "Id"));
            var p = _productService.GetById(id);
            using (var f = new ProductEditForm(p))
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                    LoadCategoryDropdown();
                }
            }
        }

        private void DeleteButton_Click(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            int rowHandle = _gridViewProducts.FocusedRowHandle;
            if (rowHandle < 0) return;
            int id = Convert.ToInt32(_gridViewProducts.GetRowCellValue(rowHandle, "Id"));
            string name = _gridViewProducts.GetRowCellValue(rowHandle, "Name").ToString();

            if (XtraMessageBox.Show($"'{name}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _productService.Delete(id);
                LoadProducts();
            }
        }

        private void EditCategoryButton_Click(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            int rowHandle = _gridViewCategories.FocusedRowHandle;
            if (rowHandle < 0) return;
            int id = Convert.ToInt32(_gridViewCategories.GetRowCellValue(rowHandle, "Id"));
            var cat = _categoryService.GetById(id);
            if (cat != null)
            {
                ShowCategoryDialog(cat);
            }
        }

        private void DeleteCategoryButton_Click(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            int rowHandle = _gridViewCategories.FocusedRowHandle;
            if (rowHandle < 0) return;
            int id = Convert.ToInt32(_gridViewCategories.GetRowCellValue(rowHandle, "Id"));
            string name = _gridViewCategories.GetRowCellValue(rowHandle, "Name").ToString();

            if (XtraMessageBox.Show($"'{name}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _categoryService.Delete(id);
                LoadCategories();
                LoadCategoryDropdown();
            }
        }
    }
}
