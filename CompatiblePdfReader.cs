using System;
using System.IO;

// Modified from code found at http://forum.pdfsharp.net/viewtopic.php?f=2&t=693

namespace PdfSharp.Pdf.IO {
    /// <summary>
    /// uses itextsharp 4.1.6 to convert any pdf to 1.4 compatible pdf, called instead of PdfReader.open
    /// </summary>

    static public class CompatiblePdfReader {
        /// <summary>
        /// uses itextsharp 4.1.6 to convert any pdf to 1.4 compatible pdf, called instead of PdfReader.open
        /// </summary>
        static public PdfDocument Open(string pdfPath, PdfDocumentOpenMode openmode) {
            PdfDocument outDoc = null;

            try {
                outDoc = PdfReader.Open(pdfPath, openmode);
            } catch (PdfSharp.Pdf.IO.PdfReaderException) {
                //workaround if pdfsharp doesn't support this pdf
                MemoryStream outputStream = new MemoryStream();
                iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(pdfPath);
                iTextSharp.text.pdf.PdfStamper pdfStamper = new iTextSharp.text.pdf.PdfStamper(reader, outputStream);
                pdfStamper.FormFlattening = true;
                pdfStamper.Writer.SetPdfVersion(iTextSharp.text.pdf.PdfWriter.PDF_VERSION_1_4);
                pdfStamper.Writer.CloseStream = false;
                pdfStamper.Close();

                outDoc = PdfReader.Open(outputStream, openmode);
            }

            return outDoc;
        }
    }
}
