using System;
using System.Drawing;
using System.Windows.Forms;

namespace MN_Barcode.WinForms
{
    public class PasswordDialog : Form
    {
        private TextBox _txtPassword;
        private Button _btnOk;
        private Button _btnCancel;
        
        public string EnteredPassword => _txtPassword.Text;
        public bool IsConfirmed { get; private set; } = false;

        public PasswordDialog(string title = "Şifre Gerekli", string message = "Devam etmek için şifrenizi girin:")
        {
            InitUI(title, message);
        }

        private void InitUI(string title, string message)
        {
            this.Text = title;
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // İkon
            Label lblIcon = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI", 32),
                Location = new Point(20, 30),
                AutoSize = true
            };
            this.Controls.Add(lblIcon);

            // Mesaj
            Label lblMessage = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(90, 35),
                Size = new Size(280, 40)
            };
            this.Controls.Add(lblMessage);

            // Şifre TextBox
            _txtPassword = new TextBox
            {
                Location = new Point(90, 80),
                Size = new Size(280, 35),
                Font = new Font("Segoe UI", 12),
                PasswordChar = '●',
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    IsConfirmed = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };
            this.Controls.Add(_txtPassword);

            // OK Butonu
            _btnOk = new Button
            {
                Text = "Giriş",
                Size = new Size(100, 35),
                Location = new Point(170, 125),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (s, e) =>
            {
                IsConfirmed = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(_btnOk);

            // İptal Butonu
            _btnCancel = new Button
            {
                Text = "İptal",
                Size = new Size(100, 35),
                Location = new Point(275, 125),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) =>
            {
                IsConfirmed = false;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtPassword.Focus();
        }
    }
}
