using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealBookExtractor {
    class PdfExtractor {

        public class ProgressEventArgs : EventArgs {
            public int Page { get; private set; }
            public int Pages { get; private set; }
            public ProgressEventArgs(int page, int pages) {
                Page = page;
                Pages = pages;
            }
        }
        public event EventHandler<ProgressEventArgs> Progress;

        public class ErrorEventArgs : EventArgs {
            public string Message { get; set; }
            public Exception Ex { get; set; }
        }
        public event EventHandler<ErrorEventArgs> Error;

        public Task<bool> ExtractAsync(string outFolder, string pdfName, CancellationToken cancelToken) {
            return Task.Factory.StartNew(() => extract(outFolder, pdfName, cancelToken), cancelToken);
        }

        private bool extract(string outFolder, string pdfName, CancellationToken cancelToken) {
            Directory.CreateDirectory(outFolder);
            System.Diagnostics.Process.Start(outFolder);
            using (var pdf = CompatiblePdfReader.Open(pdfName, PdfDocumentOpenMode.Import)) {
                int pageCount = pdf.PageCount;
                int pageCounter = 0;
                int imageCounter = 1;
                foreach (PdfPage page in pdf.Pages) {
                    pageCounter++;
                    Progress?.Invoke(this, new ProgressEventArgs(pageCounter, pageCount));
                    try {
                        foreach (var img in page.GetImages()) {
                            try {
                                if (cancelToken.IsCancellationRequested) return false;
                                if (img == null) {
                                    Error?.Invoke(this, new ErrorEventArgs { Message = $"Cannot decode image on page {pageCounter}" });
                                    continue;
                                }
                                if (img.PixelFormat == PixelFormat.Format1bppIndexed) {
                                    checkNegative(img);
                                }
                                if (cancelToken.IsCancellationRequested) return false;
                                img.Save(Path.Combine(outFolder, string.Format("{0:000}.png", imageCounter)), ImageFormat.Png);
                            } catch (Exception ex) {
                                Error?.Invoke(this, new ErrorEventArgs { Message = $"Couldn't process image on page {pageCounter}", Ex = ex });
                            } finally {
                                imageCounter++;
                            }
                        }
                    } catch (Exception ex) {
                        Error?.Invoke(this, new ErrorEventArgs { Message = $"Couldn't extract image on page {pageCounter}", Ex = ex });
                    }
                }
            }
            return true;
        }
        private void checkNegative(Image img) {
            // 1bbp images are sometimes coming out -ve.
            // I'm assuming that the images are black and white and just checking the top corners.
            var whiteCount = 0;
            using (var bmp = new Bitmap(img)) {
                for (var x = 0; x < Math.Min(bmp.Width, 50); x++) {
                    for (var y = 0; y < Math.Min(bmp.Height, 50); y++) {
                        if (bmp.GetPixel(x, y).ToArgb() == Color.White.ToArgb()) whiteCount++;
                    }
                }
                // There's more white than black
                if (whiteCount > Math.Min(bmp.Width, 50) * Math.Min(bmp.Height, 50) / 2) return;
                // Check the top right hand corner as well in case the page is a scan of a torn page.
                whiteCount = 0;
                if (bmp.Width > 50) {
                    for (var x = bmp.Width - 51; x < bmp.Width; x++) {
                        for (var y = 0; y < Math.Min(bmp.Height, 50); y++) {
                            if (bmp.GetPixel(x, y).ToArgb() == Color.White.ToArgb()) whiteCount++;
                        }
                    }
                    if (whiteCount > 50 * Math.Min(bmp.Height, 50) / 2) return;
                }
            }

            // Reverse the entries in the palette
            var pal = img.Palette;
            var entries = pal.Entries.ToArray();
            pal.Entries.SetValue(entries[1], 0);
            pal.Entries.SetValue(entries[0], 1);
            img.Palette = pal;
        }
    }
}
