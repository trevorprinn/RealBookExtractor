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

namespace RealBookExtracter {
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
            int count = 0;
            using (var pdf = PdfReader.Open(pdfName, PdfDocumentOpenMode.Import)) {
                foreach (PdfPage page in pdf.Pages) {
                    foreach (var img in page.GetImages()) {
                        img.Save(Path.Combine(OutFolder, string.Format("{0:000}.jpg", ++count)));
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
