using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public partial class ProductForm : Form
    {
        private ProductService _productService;
        private DataGridView _grid;
        private TextBox _txtSearch;

        public ProductForm()
        {
            _productService = new ProductService();
            SetupUI();
            LoadData(); // Verileri Çek
        }

        private void SetupUI()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // --- 1. ÜST PANEL (ARAMA VE BUTONLAR) ---
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(20), BackColor = Color.White };
            this.Controls.Add(topPanel);

            // Başlık
            Label lblTitle = new Label { Text = "ÜRÜN LİSTESİ", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(20, 25) };
            topPanel.Controls.Add(lblTitle);

            // Yeni Ekle Butonu (Sağa Yaslı)
            Button btnAdd = new Button { Text = "+ YENİ ÜRÜN", Height = 40, Width = 150, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand, Dock = DockStyle.Right };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) => MessageBox.Show("Ekleme Formu Yakında!"); // Buraya AddForm gelecek
            topPanel.Controls.Add(btnAdd);

            // Arama Kutusu (Ortada)
            _txtSearch = new TextBox { Width = 300, Font = new Font("Segoe UI", 12), PlaceholderText = "Barkod veya Ürün Adı Ara...", Location = new Point(200, 25) };
            _txtSearch.TextChanged += (s, e) => LoadData(_txtSearch.Text); // Yazdıkça Filtrele
            topPanel.Controls.Add(_txtSearch);


            // --- 2. GRID PANEL (LİSTE) ---
            Panel gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            this.Controls.Add(gridPanel);

            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.None;
            _grid.RowHeadersVisible = false;
            _grid.AllowUserToAddRows = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Grid Tasarımı (Satış Ekranıyla Aynı Kalite)
            _grid.RowTemplate.Height = 45;
            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            _grid.ColumnHeadersHeight = 45;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(236, 240, 241);
            _grid.EnableHeadersVisualStyles = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Kolonlar
            _grid.Columns.Add("Id", "#");
            _grid.Columns["Id"].Visible = false; // ID gizli kalsın
            _grid.Columns.Add("Barcode", "Barkod");
            _grid.Columns.Add("Name", "Ürün Adı");
            _grid.Columns.Add("Category", "Kategori");
            _grid.Columns.Add("Price", "Satış Fiyatı");
            _grid.Columns.Add("Stock", "Stok");

            // İşlem Butonları (Düzenle / Sil)
            DataGridViewButtonColumn btnEdit = new DataGridViewButtonColumn { HeaderText = "", Text = "✏️", UseColumnTextForButtonValue = true, Width = 40, FlatStyle = FlatStyle.Flat };
            DataGridViewButtonColumn btnDel = new DataGridViewButtonColumn { HeaderText = "", Text = "🗑️", UseColumnTextForButtonValue = true, Width = 40, FlatStyle = FlatStyle.Flat };
            btnDel.DefaultCellStyle.ForeColor = Color.Red;

            _grid.Columns.Add(btnEdit);
            _grid.Columns.Add(btnDel);

            _grid.CellClick += Grid_CellClick;

            gridPanel.Controls.Add(_grid);
        }

        // --- VERİ YÜKLEME ---
        private void LoadData(string search = "")
        {
            _grid.Rows.Clear();
            var products = _productService.GetProducts(search);

            foreach (var item in products)
            {
                string catName = item.Category != null ? item.Category.Name : "-";
                _grid.Rows.Add(item.Id, item.Barcode, item.Name, catName, item.SellingPrice.ToString("C2"), item.StockQuantity);
            }
        }

        // --- İŞLEMLER ---
        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int id = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["Id"].Value);

            // DÜZENLEME BUTONU (Index 6) - (0:Id, 1:Bar, 2:Name, 3:Cat, 4:Price, 5:Stock, 6:Edit, 7:Del)
            // Kolon indexlerini isme göre almak daha güvenlidir ama şimdilik manuel sayalım veya FindColumn yapalım.
            // Bizim ekleme sıramıza göre: 
            // Id=0, Barcode=1, Name=2, Category=3, Price=4, Stock=5, Edit=6, Del=7

            if (e.ColumnIndex == 6) // Edit
            {
                MessageBox.Show("Düzenleme Formu Açılacak ID: " + id);
            }
            else if (e.ColumnIndex == 7) // Delete
            {
                if (MessageBox.Show("Bu ürünü silmek istediğinize emin misiniz?", "Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _productService.Delete(id);
                    LoadData(); // Listeyi yenile
                }
            }
        }
    }
}