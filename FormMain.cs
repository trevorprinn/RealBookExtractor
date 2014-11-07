using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RealBookExtracter {
    public partial class FormMain : Form {

        private string _jpgFolder;
        private string _firstPage;
        private string _lastPage;

        public FormMain() {
            InitializeComponent();
            setButtonStates();
        }

        private void setButtonStates() {
            btnLoad.Enabled = Directory.Exists(textFolder.Text);
            if (_firstPage == null) {
                textArtist.Enabled = textTitle.Enabled = btnBack.Enabled
                    = btnNext.Enabled = btnSave.Enabled = btnDelete.Enabled = false;
                return;
            }
            textArtist.Enabled = textTitle.Enabled = true;
            btnBack.Enabled = _firstPage != _lastPage;
            btnNext.Enabled = _lastPage != Path.GetFileName(Directory.GetFiles(_jpgFolder).Last());
            btnSave.Enabled = textArtist.Text != "" && textTitle.Text != "";
            btnDelete.Enabled = true;
            if (btnSave.Enabled) AcceptButton = btnSave;
        }

        private void menuExtract_Click(object sender, EventArgs e) {
            using (var f = new FormExtract()) {
                if (f.ShowDialog(this) == DialogResult.OK) {
                    textFolder.Text = f.OutFolder;
                    btnLoad.PerformClick();
                }
            }
        }

        private void textFolder_TextChanged(object sender, EventArgs e) {
            setButtonStates();
        }

        private void btnLoad_Click(object sender, EventArgs e) {
            _jpgFolder = textFolder.Text;
            displayPage();
        }

        private void displayPage() {
            var pages = Directory.GetFiles(_jpgFolder).Select(f => Path.GetFileName(f)).OrderBy(f => f);
            _firstPage = _lastPage = pages.FirstOrDefault();
            if (_firstPage == null) {
                picStart.Image = picEnd.Image = null;
            } else {
                picStart.Image = loadImage(Path.Combine(_jpgFolder, _firstPage));
                picEnd.Image = picStart.Image;
            }
            setButtonStates();
            if (textArtist.Enabled) textArtist.Focus();
        }

        private void btnBack_Click(object sender, EventArgs e) {
            _lastPage = Directory.GetFiles(_jpgFolder).Select(f => Path.GetFileName(f))
                .OrderByDescending(f => f).SkipWhile(f => f != _lastPage).Skip(1).First();
            picEnd.Image = loadImage(Path.Combine(_jpgFolder, _lastPage));
            setButtonStates();
        }

        private void btnNext_Click(object sender, EventArgs e) {
            _lastPage = Directory.GetFiles(_jpgFolder).Select(f => Path.GetFileName(f))
                .OrderBy(f => f).SkipWhile(f => f != _lastPage).Skip(1).First();
            picEnd.Image = loadImage(Path.Combine(_jpgFolder, _lastPage));
            setButtonStates();
        }

        private void text_Changed(object sender, EventArgs e) {
            setButtonStates();
        }

        private void btnDelete_Click(object sender, EventArgs e) {
            File.Delete(Path.Combine(_jpgFolder, _firstPage));
            displayPage();
        }

        private void btnSave_Click(object sender, EventArgs e) {
            var folder = Path.Combine(_jpgFolder, textArtist.Text);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var pages = Directory.GetFiles(_jpgFolder).Select(f => Path.GetFileName(f))
                .OrderBy(f => f).TakeWhile(f => f != _lastPage).ToList();
            pages.Add(_lastPage);
            int count = 0;
            bool counter = pages.Count > 1;
            foreach (var page in pages) {
                var name = counter 
                    ? string.Format("{0} {1}", textTitle.Text, ++count)
                    : textTitle.Text;
                name += Path.GetExtension(page);
                File.Move(Path.Combine(_jpgFolder, page), Path.Combine(folder, name));
            }
            textTitle.Text = textArtist.Text = "";
            displayPage();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (e.KeyCode == Keys.PageUp && btnBack.Enabled) btnBack.PerformClick();
            if (e.KeyCode == Keys.PageDown && btnNext.Enabled) btnNext.PerformClick();
            base.OnKeyDown(e);
        }

        
        private Image loadImage(string file) {
            // Image.FromFile seems to lock the file until the image is disposed.
            using (var memStream = new MemoryStream()) {
                using (FileStream s = new FileStream(file, FileMode.Open)) {
                    s.CopyTo(memStream, 10240);
                }
                memStream.Seek(0, SeekOrigin.Begin);
                return Image.FromStream(memStream);
            }
        }

    }
}
