using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RealBookExtractor {
    public partial class FormExtractProgress : Form {
        private string _pdfPath;
        private string _outFolder;
        private CancellationTokenSource _tokenSource;
        private List<ErrorInfo> _errors = new List<ErrorInfo>();
        
        private class ErrorInfo {
            public string Message;
            public Exception Ex;
        }

        public FormExtractProgress(string outFolder, string pdfPath) {
            InitializeComponent();

            labelName.Text = Path.GetFileNameWithoutExtension(pdfPath);
            _outFolder = outFolder;
            _pdfPath = pdfPath;
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);

            var extractor = new PdfExtractor();
            extractor.Error += (s, ea) => this.BeginInvokeIfRequired(() => {
                listErrors.Items.Add(ea.Message);
                _errors.Add(new ErrorInfo { Message = ea.Message, Ex = ea.Ex });
            });
            extractor.Progress += (s, ea) => this.BeginInvokeIfRequired(() => {
                try {
                    progress.Maximum = ea.Pages;
                    progress.Value = ea.Page;
                } catch { }
            });
            _tokenSource = new CancellationTokenSource();
            var task = extractor.ExtractAsync(_outFolder, _pdfPath, _tokenSource.Token);
            task.ContinueWith(t => this.BeginInvokeIfRequired(() => {
                if (_errors.Any()) {
                    string msg = (_errors.Count == 1 ? "There was an error." : $"There were {_errors.Count} errors.") + "\r\nDo you want to copy the details to the clipboard?";
                    if (MessageBox.Show(this, msg, "Extract Errors", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes) {
                        Clipboard.SetText(string.Join("\r\n\r\n", _errors.Select(err => $"{err.Message}\r\n{err.Ex.ToString()}")));
                    }
                }
                DialogResult = task.Result ? DialogResult.OK : DialogResult.Cancel;
            }));
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            btnCancel.Enabled = false;
            _tokenSource.Cancel();
        }
    }
}
