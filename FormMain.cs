﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Windows.Forms;

namespace RealBookExtractor {
    public partial class FormMain : Form {

        private string _pngFolder;
        private string _firstPage;
        private string _lastPage;
        private UndoInfo[] _undoInfo;

        public FormMain() {
            InitializeComponent();
            setButtonStates();
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            if (!string.IsNullOrWhiteSpace(Settings.FolderPath) && Directory.Exists(Settings.FolderPath)) {
                textFolder.Text = Settings.FolderPath;
                btnLoad.PerformClick();
            }
        }

        private void setButtonStates() {
            btnLoad.Enabled = Directory.Exists(textFolder.Text);
            btnNegative.Enabled = picStart.Image != null && picStart.Image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
            if (_firstPage == null || !getPages().Any()) {
                cboArtist.Enabled = textTitle.Enabled = btnBack.Enabled
                    = btnNext.Enabled = btnSave.Enabled = btnDelete.Enabled = btnDuplicate.Enabled = false;
                return;
            }
            cboArtist.Enabled = textTitle.Enabled = true;
            btnBack.Enabled = _firstPage != _lastPage;
            btnNext.Enabled = _lastPage != getPages().Last();
            btnSave.Enabled = cboArtist.Text != "" && textTitle.Text != "";
            btnDelete.Enabled = btnDuplicate.Enabled = true;
            if (btnSave.Enabled) AcceptButton = btnSave;
        }

        private void menuExtract_Click(object sender, EventArgs e) {
            if (dlgPdf.ShowDialog(this) != DialogResult.OK) return;
            string pdfName = dlgPdf.FileName;
            string outFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Path.GetFileNameWithoutExtension(pdfName));
            if (Directory.Exists(outFolder)) {
                MessageBox.Show(this, outFolder + "\r\nalready exists.");
                return;
            }
            using (var f = new FormExtractProgress(outFolder, pdfName)) {
                if (f.ShowDialog(this) == DialogResult.OK) {
                    textFolder.Text = outFolder;
                    btnLoad.PerformClick();
                }
            }
        }

        private void textFolder_TextChanged(object sender, EventArgs e) {
            setButtonStates();
        }

        private void btnLoad_Click(object sender, EventArgs e) {
            _pngFolder = textFolder.Text;
            Settings.FolderPath = _pngFolder;
            _undoInfo = null;
            displayPage();
        }

        private IEnumerable<string> getPages() {
            return Directory.GetFiles(_pngFolder).Select(f => Path.GetFileName(f))
                .Where(f => Path.GetExtension(f).ToLower() != ".pdf").OrderBy(f => f);
        }

        private void displayPage() {
            _firstPage = _lastPage = getPages().FirstOrDefault();
            if (_firstPage == null) {
                picStart.Image = picEnd.Image = null;
            } else {
                picStart.Image = loadImage(_firstPage);
                loadEndPage();
            }
            setButtonStates();
            loadArtists();
            if (cboArtist.Enabled) BeginInvoke(new Action(() => cboArtist.Focus()));
        }

        private void loadArtists() {
            cboArtist.Items.Clear();
            cboArtist.Items.AddRange(Directory.GetDirectories(_pngFolder).Select(a => Path.GetFileName(a)).ToArray());
        }

        private void loadEndPage() {
            // The end page displayed is the one after _lastPage.
            var page = getPages().SkipWhile(p => p != _lastPage).Skip(1).FirstOrDefault();
            picEnd.Image = loadImage(page);
        }

        private void btnBack_Click(object sender, EventArgs e) {
            _lastPage = getPages().Reverse().SkipWhile(f => f != _lastPage).Skip(1).First();
            loadEndPage();
            setButtonStates();
        }

        private void btnNext_Click(object sender, EventArgs e) {
            _lastPage = getPages().SkipWhile(f => f != _lastPage).Skip(1).First();
            loadEndPage();
            setButtonStates();
        }

        private void text_Changed(object sender, EventArgs e) {
            setButtonStates();
        }

        private void btnDelete_Click(object sender, EventArgs e) {
            File.Delete(Path.Combine(_pngFolder, _firstPage));
            displayPage();
        }

        private void btnSave_Click(object sender, EventArgs e) {
            var folder = Path.Combine(_pngFolder, cboArtist.Text);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var pages = getPages().TakeWhile(f => f != _lastPage).ToList();
            pages.Add(_lastPage);
            int count = 0;
            bool counter = pages.Count > 1;
            _undoInfo = pages.Select(p => new {OrigName = p, 
                NewName = (counter ? string.Format("{0} {1}", textTitle.Text, ++count) : textTitle.Text) + Path.GetExtension(p)
            }).Select(p => new UndoInfo(_pngFolder, p.OrigName, folder, p.NewName)).ToArray();
            foreach (var p in _undoInfo) p.Move();
            textTitle.Text = cboArtist.Text = "";
            displayPage();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (e.KeyCode != Keys.PageUp && e.KeyCode != Keys.PageDown) return;
            if (e.KeyCode == Keys.PageUp && btnBack.Enabled) btnBack.PerformClick();
            if (e.KeyCode == Keys.PageDown && btnNext.Enabled) btnNext.PerformClick();
            e.SuppressKeyPress = e.Handled = true;
            base.OnKeyDown(e);
        }
        
        private Image loadImage(string file) {
            if (file == null) return null;
            string path = Path.Combine(_pngFolder, file);
            if (!File.Exists(path)) return null;
            // Image.FromFile seems to lock the file until the image is disposed.
            using (var memStream = new MemoryStream()) {
                using (FileStream s = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    s.CopyTo(memStream, 10240);
                }
                memStream.Seek(0, SeekOrigin.Begin);
                return Image.FromStream(memStream);
            }
        }

        private void menuEdit_DropDownOpening(object sender, EventArgs e) {
            menuUndo.Enabled = _undoInfo != null && _undoInfo.Any();
        }

        private void menuUndo_Click(object sender, EventArgs e) {
            foreach (var p in _undoInfo) p.Undo();
            _undoInfo = null;
            displayPage();
        }

        private void menuAbout_Click(object sender, EventArgs e) {
            using (var f = new FormAbout()) {
                f.ShowDialog(this);
            }
        }

        private void btnDuplicate_Click(object sender, EventArgs e) {
            var dupeNum = 1;
            var fname = Path.GetFileName(_firstPage);
            var ext = Path.GetExtension(_firstPage);
            string newName;
            do {
                newName = Path.Combine(_pngFolder, string.Format("{0} ({1}){2}", fname, dupeNum++, ext));
            } while (File.Exists(newName));
            File.Copy(Path.Combine(_pngFolder, _firstPage), newName);
            displayPage();
        }

        private void textTitle_KeyPress(object sender, KeyPressEventArgs e) {
            e.Handled = e.KeyChar != '\b' && Path.GetInvalidFileNameChars().Contains(e.KeyChar);
        }

        private void cboArtist_KeyPress(object sender, KeyPressEventArgs e) {
            e.Handled = e.KeyChar != '\b' && Path.GetInvalidFileNameChars().Contains(e.KeyChar);
        }

        private void menuOnlineWiki_Click(object sender, EventArgs e) {
            System.Diagnostics.Process.Start("https://github.com/trevorprinn/RealBookExtractor/wiki");
        }

        private void btnBrowse_Click(object sender, EventArgs e) {
            if (dlgFolder.ShowDialog(this) == DialogResult.OK) {
                textFolder.Text = dlgFolder.SelectedPath;
                btnLoad.PerformClick();
            }
        }

        private void btnNegative_Click(object sender, EventArgs e) {
            var img = picStart.Image;
            // Reverse the entries in the palette
            var pal = img.Palette;
            var entries = pal.Entries.ToArray();
            pal.Entries.SetValue(entries[1], 0);
            pal.Entries.SetValue(entries[0], 1);
            img.Palette = pal;

            img.Save(Path.Combine(_pngFolder, _firstPage));
            displayPage();
        }
    }

    public class UndoInfo {
        public string OriginalName { get; private set; }
        public string MovedName { get; private set; }
        public UndoInfo(string origFolder, string origFilename, string artistFolder, string filename) {
            OriginalName = Path.Combine(origFolder, origFilename);
            MovedName = Path.Combine(artistFolder, filename);
        }
        public void Move() {
            if (!Directory.Exists(Path.GetDirectoryName(MovedName))) Directory.CreateDirectory(Path.GetDirectoryName(MovedName));
            File.Move(OriginalName, MovedName);
        }
        public void Undo() {
            File.Move(MovedName, OriginalName);
        }
    }

    static class Settings {
        private static string _filename;
        private static XDocument _doc;

        static Settings() {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Babbacombe Computers Ltd", "RealBookExtractor");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _filename = Path.Combine(folder, "RealBookExtractor.xml");
            if (File.Exists(_filename)) {
                _doc = XDocument.Load(_filename);
            } else {
                _doc = new XDocument(
                    new XElement("Settings",
                        new XElement("Folder", new XAttribute("Path", ""))));
            }
        }

        public static string FolderPath {
            get { return _doc.Root.Element("Folder").Attribute("Path").Value; }
            set {
                _doc.Root.Element("Folder").SetAttributeValue("Path", value);
                _doc.Save(_filename);
            }
        }
    }
}
