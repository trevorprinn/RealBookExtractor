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

using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace RealBookExtractor {
    public partial class FormExtract : Form {
        public FormExtract() {
            InitializeComponent();
            setButtonStates();
        }

        private void btnBrowse_Click(object sender, EventArgs e) {
            if (dlgOpenPdf.ShowDialog() != DialogResult.OK) return;
            textPdf.Text = dlgOpenPdf.FileName;
        }

        private void btnExtract_Click(object sender, EventArgs e) {
            var cursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try {
                if (extract(textPdf.Text)) DialogResult = DialogResult.OK;
            } finally {
                Cursor.Current = cursor;
            }
        }

        private void setButtonStates() {
            btnExtract.Enabled = File.Exists(textPdf.Text);
        }

        public string OutFolder { get; private set; }

        private bool extract(string pdfName) {
            OutFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Path.GetFileNameWithoutExtension(pdfName));
            if (Directory.Exists(OutFolder)) {
                MessageBox.Show(this, OutFolder + "\r\nalready exists.");
                return false;
            }
            Directory.CreateDirectory(OutFolder);
            System.Diagnostics.Process.Start(OutFolder);
            int count = 1;
            using (var pdf = CompatiblePdfReader.Open(pdfName, PdfDocumentOpenMode.Import)) {
                foreach (PdfPage page in pdf.Pages) {
                    try {
                        foreach (var img in page.GetImages()) {
                            if (img.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed) {
                                checkNegative(img);
                            }
                            img.Save(Path.Combine(OutFolder, string.Format("{0:000}.jpg", count++)));
                        }
                    } catch (Exception ex) {
                        string msg = string.Format("Couldn't extract page {0}\r\n{1}", count++, ex.Message);
                        if (MessageBox.Show(this, msg, "Extraction error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1) == DialogResult.Cancel) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void checkNegative(Image img) {
            // 1bbp images are sometimes coming out -ve.
            // I'm assuming that the images are black and white and just checking the top LH corner.
            var whiteCount = 0;
            using (var bmp = new Bitmap(img)) {
                for (var x = 0; x < Math.Min(bmp.Width, 50); x++) {
                    for (var y = 0; y < Math.Min(bmp.Height, 50); y++) {
                        if (bmp.GetPixel(x, y) == Color.White) whiteCount++;
                    }
                }
                // There's more white than black;
                if (whiteCount > bmp.Width * bmp.Height / 2) return;
            }

            // Reverse the entries in the palette
            var pal = img.Palette;
            var entries = pal.Entries.ToArray();
            pal.Entries.SetValue(entries[1], 0);
            pal.Entries.SetValue(entries[0], 1);
            img.Palette = pal;
        }

        private void textPdf_TextChanged(object sender, EventArgs e) {
            setButtonStates();
        }
    }
}
