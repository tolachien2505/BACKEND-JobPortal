using System.Text;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace JobPortal.Services;

public class FileTextExtractor
{
    private readonly ILogger<FileTextExtractor> _logger;

    public FileTextExtractor(ILogger<FileTextExtractor> logger)
    {
        _logger = logger;
    }

    public string ExtractText(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => ExtractFromPdf(fileStream),
            ".docx" => ExtractFromDocx(fileStream),
            _ => throw new NotSupportedException($"File type '{extension}' is not supported. Please upload PDF or DOCX files.")
        };
    }

    private string ExtractFromPdf(Stream stream)
    {
        try
        {
            var sb = new StringBuilder();
            using var pdfReader = new PdfReader(stream);
            using var pdfDocument = new PdfDocument(pdfReader);

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var text = PdfTextExtractor.GetTextFromPage(page);
                sb.AppendLine(text);
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF");
            throw new InvalidOperationException("Không thể đọc file PDF. Vui lòng kiểm tra file không bị hỏng hoặc mã hóa.", ex);
        }
    }

    private string ExtractFromDocx(Stream stream)
    {
        try
        {
            var sb = new StringBuilder();
            using var wordDocument = WordprocessingDocument.Open(stream, false);
            var body = wordDocument.MainDocumentPart?.Document?.Body;

            if (body != null)
            {
                foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    sb.AppendLine(paragraph.InnerText);
                }
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from DOCX");
            throw new InvalidOperationException("Không thể đọc file DOCX. Vui lòng kiểm tra file không bị hỏng.", ex);
        }
    }
}
