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
            using (var pdf = PdfReader.Open(pdfName, PdfDocumentOpenMode.Import)) {
                foreach (PdfPage page in pdf.Pages) {
                    try {
                        foreach (var img in page.GetImages()) {
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

        private void textPdf_TextChanged(object sender, EventArgs e) {
            setButtonStates();
        }
    }
}
