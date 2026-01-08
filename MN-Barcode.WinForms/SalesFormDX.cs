using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using MN_Barcode.Business;
using MN_Barcode.Entities;
using Microsoft.VisualBasic;

namespace MN_Barcode.WinForms
{
    public class SalesFormDX : XtraForm
    {
        private ProductService _productService;
        private SaleService _saleService;

        // UI
        private GridControl _gridSepet;
        private GridView _gridViewSepet;
        private LabelControl _lblTotal;
        private TextEdit _txtBarcode;
        private PanelControl _modePanel;
        private LabelControl _lblMode;
        private TableLayoutPanel _quickButtonGrid;

        // Veri
        private decimal _grandTotal = 0;
        private List<SaleDetail> _cartDetails;
        private List<CartItem> _cartItems;
        private bool _isReturnMode = false;

        // Cart Item için yardımcı sınıf
        private class CartItem
        {
            public int ProductId { get; set; }
            public string Name { get; set; }
            public int Qty { get; set; }
            public string Price { get; set; }
            public string Total { get; set; }
        }

        public SalesFormDX()
        {
            _productService = new ProductService();
            _saleService = new SaleService();
            _cartDetails = new List<SaleDetail>();
            _cartItems = new List<CartItem>();

            SetupSalesUI();
            LoadQuickButtons();
        }

        private void SetupSalesUI()
        {
            this.Text = "Satış";
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.LookAndFeel.SkinName = "Office 2019 Black";

            // --- ANA İSKELET (3 SÜTUNLU TABLO) ---
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.Controls.Add(mainLayout);

            // ═══════════════════════════════════════════════════════════
            // 1. SOL PANEL (SEPET)
            // ═══════════════════════════════════════════════════════════
            PanelControl leftPanel = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(10)
            };
            mainLayout.Controls.Add(leftPanel, 0, 0);

            // Mod Göstergesi
            _modePanel = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 45,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            _modePanel.Appearance.BackColor = Color.FromArgb(34, 197, 94);
            leftPanel.Controls.Add(_modePanel);

            _lblMode = new LabelControl
            {
                Text = "💰 SATIŞ MODU",
                Dock = DockStyle.Fill,
                AutoSizeMode = LabelAutoSizeMode.None
            };
            _lblMode.Appearance.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            _lblMode.Appearance.ForeColor = Color.White;
            _lblMode.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            _modePanel.Controls.Add(_lblMode);

            // Barkod Alanı
            PanelControl barcodePanel = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 70,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(10)
            };
            barcodePanel.Appearance.BackColor = Color.White;
            leftPanel.Controls.Add(barcodePanel);

            _txtBarcode = new TextEdit
            {
                Dock = DockStyle.Fill
            };
            _txtBarcode.Properties.Appearance.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            _txtBarcode.Properties.NullValuePrompt = "📱 Barkod okutun veya yazın...";
            _txtBarcode.Properties.NullValuePromptShowForEmptyValue = true;
            _txtBarcode.KeyDown += Barcode_KeyDown;
            barcodePanel.Controls.Add(_txtBarcode);

            // Spacer
            leftPanel.Controls.Add(new PanelControl { Dock = DockStyle.Top, Height = 10, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder });

            // Grid Container
            PanelControl gridContainer = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            gridContainer.Appearance.BackColor = Color.White;
            leftPanel.Controls.Add(gridContainer);
            gridContainer.BringToFront();

            // Sepet Grid
            _gridSepet = new GridControl { Dock = DockStyle.Fill };
            _gridViewSepet = new GridView(_gridSepet);
            _gridSepet.MainView = _gridViewSepet;

            _gridViewSepet.OptionsView.ShowGroupPanel = false;
            _gridViewSepet.OptionsView.ShowIndicator = false;
            _gridViewSepet.OptionsBehavior.Editable = true;
            _gridViewSepet.RowHeight = 55;

            _gridViewSepet.Columns.Add(new GridColumn { FieldName = "ProductId", Caption = "ID", Visible = false });
            _gridViewSepet.Columns.Add(new GridColumn { FieldName = "Name", Caption = "ÜRÜN ADI", VisibleIndex = 0, Width = 200 });
            _gridViewSepet.Columns.Add(new GridColumn { FieldName = "Qty", Caption = "ADET", VisibleIndex = 1, Width = 70 });
            _gridViewSepet.Columns.Add(new GridColumn { FieldName = "Price", Caption = "FİYAT", VisibleIndex = 2, Width = 100 });
            _gridViewSepet.Columns.Add(new GridColumn { FieldName = "Total", Caption = "TUTAR", VisibleIndex = 3, Width = 100 });

            // Sil butonu
            var btnDelCol = new GridColumn
            {
                FieldName = "Delete",
                Caption = "",
                VisibleIndex = 4,
                Width = 60,
                UnboundType = DevExpress.Data.UnboundColumnType.Object
            };
            btnDelCol.OptionsColumn.AllowEdit = true;
            _gridViewSepet.Columns.Add(btnDelCol);

            var deleteRepo = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            deleteRepo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            deleteRepo.Buttons.Clear();
            var delBtn = new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            delBtn.Caption = "✕";
            delBtn.Appearance.BackColor = Color.FromArgb(239, 68, 68);
            delBtn.Appearance.ForeColor = Color.White;
            delBtn.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            delBtn.Appearance.Options.UseBackColor = true;
            delBtn.Appearance.Options.UseForeColor = true;
            delBtn.Appearance.Options.UseFont = true;
            delBtn.Width = 50;
            deleteRepo.Buttons.Add(delBtn);
            deleteRepo.ButtonClick += DeleteCartItem_Click;
            btnDelCol.ColumnEdit = deleteRepo;
            _gridSepet.RepositoryItems.Add(deleteRepo);

            // Diğer kolonları readonly yap
            foreach (GridColumn col in _gridViewSepet.Columns)
            {
                if (col.FieldName != "Delete")
                    col.OptionsColumn.AllowEdit = false;
            }

            gridContainer.Controls.Add(_gridSepet);

            // ═══════════════════════════════════════════════════════════
            // 2. ORTA PANEL (HIZLI TUŞLAR)
            // ═══════════════════════════════════════════════════════════
            PanelControl centerPanel = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(5)
            };
            mainLayout.Controls.Add(centerPanel, 1, 0);

            LabelControl lblQuick = new LabelControl
            {
                Text = "⚡ HIZLI ÜRÜNLER",
                Dock = DockStyle.Top,
                AutoSizeMode = LabelAutoSizeMode.None,
                Height = 35
            };
            lblQuick.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblQuick.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
            lblQuick.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            centerPanel.Controls.Add(lblQuick);

            _quickButtonGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(5, 0, 0, 0)
            };
            _quickButtonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _quickButtonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 5; i++) _quickButtonGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            centerPanel.Controls.Add(_quickButtonGrid);
            _quickButtonGrid.BringToFront();

            // ═══════════════════════════════════════════════════════════
            // 3. SAĞ PANEL (ÖDEME)
            // ═══════════════════════════════════════════════════════════
            PanelControl rightPanel = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(10)
            };
            rightPanel.Appearance.BackColor = Color.White;
            mainLayout.Controls.Add(rightPanel, 2, 0);

            // Toplam Panel
            PanelControl totalPanel = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 130,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            totalPanel.Appearance.BackColor = Color.FromArgb(30, 41, 59);
            rightPanel.Controls.Add(totalPanel);

            LabelControl lblTotalText = new LabelControl
            {
                Text = "💳 TOPLAM",
                Location = new Point(15, 15)
            };
            lblTotalText.Appearance.Font = new Font("Segoe UI", 12);
            lblTotalText.Appearance.ForeColor = Color.FromArgb(148, 163, 184);
            totalPanel.Controls.Add(lblTotalText);

            _lblTotal = new LabelControl
            {
                Text = "₺ 0.00",
                Dock = DockStyle.Bottom,
                AutoSizeMode = LabelAutoSizeMode.None,
                Height = 80
            };
            _lblTotal.Appearance.Font = new Font("Consolas", 36, FontStyle.Bold);
            _lblTotal.Appearance.ForeColor = Color.FromArgb(34, 197, 94);
            _lblTotal.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            totalPanel.Controls.Add(_lblTotal);

            // İşlem Butonları
            TableLayoutPanel actionGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(0, 15, 0, 0)
            };
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            rightPanel.Controls.Add(actionGrid);
            actionGrid.BringToFront();

            actionGrid.Controls.Add(CreateActionButton("💵 NAKİT\n(F2)", Color.FromArgb(34, 197, 94), () => CompleteSale("Nakit")), 0, 0);
            actionGrid.Controls.Add(CreateActionButton("💳 KART\n(F3)", Color.FromArgb(59, 130, 246), () => CompleteSale("Kredi Kartı")), 1, 0);
            actionGrid.Controls.Add(CreateActionButton("⏸️ BEKLET\n(F4)", Color.FromArgb(107, 114, 128), () => XtraMessageBox.Show("Fiş beklemeye alındı.", "Bilgi")), 0, 1);
            actionGrid.Controls.Add(CreateActionButton("↩️ İADE\n(F6)", Color.FromArgb(245, 158, 11), () => ToggleReturnMode()), 1, 1);

            SimpleButton btnCancel = CreateActionButton("🗑️ SATIŞI İPTAL ET (F5)", Color.FromArgb(239, 68, 68), () => ClearCart());
            actionGrid.Controls.Add(btnCancel, 0, 2);
            actionGrid.SetColumnSpan(btnCancel, 2);
        }

        private void LoadQuickButtons()
        {
            _quickButtonGrid.Controls.Clear();

            Color btnColor = Color.FromArgb(249, 250, 251);

            AddQuickButton("🍞 EKMEK\n₺8", 8.00m, btnColor);
            AddQuickButton("💧 SU\n₺5", 5.00m, btnColor);
            AddQuickButton("🍵 ÇAY\n₺10", 10.00m, btnColor);
            AddQuickButton("🥯 SİMİT\n₺12.50", 12.50m, btnColor);
            AddQuickButton("🛍️ POŞET\n₺0.25", 0.25m, btnColor);
            AddQuickButton("💰 10 TL", 10.00m, btnColor);
            AddQuickButton("💰 20 TL", 20.00m, btnColor);
            AddQuickButton("💰 50 TL", 50.00m, btnColor);

            // MANUEL TUTAR
            SimpleButton btnManual = new SimpleButton
            {
                Text = "⌨️ TUTAR GİR",
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Cursor = Cursors.Hand
            };
            btnManual.Appearance.BackColor = Color.FromArgb(30, 41, 59);
            btnManual.Appearance.ForeColor = Color.White;
            btnManual.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnManual.Appearance.Options.UseBackColor = true;
            btnManual.Appearance.Options.UseForeColor = true;
            btnManual.Appearance.Options.UseFont = true;
            btnManual.Click += (s, e) => AskManualPrice();
            _quickButtonGrid.Controls.Add(btnManual, 0, 4);
            _quickButtonGrid.SetColumnSpan(btnManual, 2);
        }

        private void AddQuickButton(string text, decimal price, Color color)
        {
            SimpleButton btn = new SimpleButton
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Cursor = Cursors.Hand
            };
            btn.Appearance.BackColor = color;
            btn.Appearance.ForeColor = Color.FromArgb(30, 41, 59);
            btn.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btn.Appearance.Options.UseBackColor = true;
            btn.Appearance.Options.UseForeColor = true;
            btn.Appearance.Options.UseFont = true;
            btn.Appearance.BorderColor = Color.FromArgb(209, 213, 219);

            btn.Click += (s, e) =>
            {
                string cleanName = text.Split('\n')[0].Replace("🍞", "").Replace("💧", "").Replace("🍵", "").Replace("🥯", "").Replace("🛍️", "").Replace("💰", "").Trim();
                string barcode = "QUICK-" + cleanName.ToUpper().Replace(" ", "").Replace("₺", "");
                var realProduct = _productService.GetOrCreateQuickProduct(cleanName, barcode, price);
                AddToCart(realProduct);
            };

            _quickButtonGrid.Controls.Add(btn);
        }

        private SimpleButton CreateActionButton(string text, Color color, Action onClick)
        {
            SimpleButton btn = new SimpleButton
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Cursor = Cursors.Hand
            };
            btn.Appearance.BackColor = color;
            btn.Appearance.ForeColor = Color.White;
            btn.Appearance.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btn.Appearance.Options.UseBackColor = true;
            btn.Appearance.Options.UseForeColor = true;
            btn.Appearance.Options.UseFont = true;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void AskManualPrice()
        {
            string input = Interaction.InputBox("Tutarı Giriniz:", "Serbest Tutar", "0");
            if (decimal.TryParse(input, out decimal price) && price > 0)
            {
                var genericProduct = _productService.GetOrCreateQuickProduct("Serbest Ürün", "MANUAL", 0);
                genericProduct.SellingPrice = price;
                AddToCart(genericProduct);
            }
        }

        private void DeleteCartItem_Click(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            int rowHandle = _gridViewSepet.FocusedRowHandle;
            if (rowHandle >= 0 && rowHandle < _cartItems.Count)
            {
                var removedItem = _cartDetails[rowHandle];
                _grandTotal -= removedItem.TotalPrice;
                _cartDetails.RemoveAt(rowHandle);
                _cartItems.RemoveAt(rowHandle);
                RefreshGrid();
                _txtBarcode.Focus();
            }
        }

        private void ToggleReturnMode()
        {
            _isReturnMode = !_isReturnMode;
            if (_isReturnMode)
            {
                _modePanel.Appearance.BackColor = Color.FromArgb(245, 158, 11);
                _lblMode.Text = "⚠️ İADE MODU";
            }
            else
            {
                _modePanel.Appearance.BackColor = Color.FromArgb(34, 197, 94);
                _lblMode.Text = "💰 SATIŞ MODU";
            }
            ClearCart();
        }

        private void Barcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string code = _txtBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    var product = _productService.GetByBarcode(code);
                    if (product != null)
                    {
                        AddToCart(product);
                        _txtBarcode.Text = "";
                    }
                    else
                    {
                        XtraMessageBox.Show("Ürün Bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _txtBarcode.SelectAll();
                    }
                    e.SuppressKeyPress = true;
                }
            }
            if (e.KeyCode == Keys.F2) CompleteSale("Nakit");
            if (e.KeyCode == Keys.F3) CompleteSale("Kredi Kartı");
            if (e.KeyCode == Keys.F5) ClearCart();
            if (e.KeyCode == Keys.F6) ToggleReturnMode();
        }

        private void AddToCart(Product p)
        {
            int qty = _isReturnMode ? -1 : 1;
            decimal price = _isReturnMode ? p.SellingPrice * -1 : p.SellingPrice;

            _cartItems.Add(new CartItem
            {
                ProductId = p.Id,
                Name = p.Name,
                Qty = qty,
                Price = p.SellingPrice.ToString("₺#,##0.00"),
                Total = price.ToString("₺#,##0.00")
            });

            _cartDetails.Add(new SaleDetail { ProductId = p.Id, Quantity = qty, SellingPrice = p.SellingPrice, TotalPrice = price });
            _grandTotal += price;

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _gridSepet.DataSource = null;
            _gridSepet.DataSource = _cartItems;
            _gridSepet.RefreshDataSource();
            _lblTotal.Text = _grandTotal.ToString("₺ #,##0.00");
        }

        private void CompleteSale(string type)
        {
            if (_grandTotal == 0 && !_isReturnMode) return;
            try
            {
                Sale newSale = new Sale { TransactionCode = DateTime.Now.ToString("yyyyMMddHHmmss"), PaymentType = type, TotalAmount = _grandTotal };
                _saleService.CompleteSale(newSale, _cartDetails);
                XtraMessageBox.Show($"İşlem Tamamlandı.\nTutar: {_grandTotal:₺#,##0.00}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearCart();
            }
            catch (Exception ex)
            {
                string err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                XtraMessageBox.Show("Hata: " + err, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearCart()
        {
            _cartItems.Clear();
            _cartDetails.Clear();
            _grandTotal = 0;
            RefreshGrid();
            _txtBarcode.Focus();
        }
    }
}
