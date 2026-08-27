using System.Text;
using System.Xml.Linq;
using System.IO.Compression;
using CareerCopilot.Application.Common.Interfaces;
using UglyToad.PdfPig;

namespace CareerCopilot.Infrastructure.Services;

public sealed class ResumeParserService : IResumeParserService
{
    private static readonly XNamespace WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public ResumeExtractionResult TryExtractText(Stream stream, string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        try
        {
            switch (extension)
            {
                case ".txt":
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        return new ResumeExtractionResult(true, reader.ReadToEnd(), null);
                    }

                case ".pdf":
                    return new ResumeExtractionResult(true, ExtractPdf(stream), null);

                case ".docx":
                    return new ResumeExtractionResult(true, ExtractDocx(stream), null);

                default:
                    return new ResumeExtractionResult(false, string.Empty,
                        "Unsupported file type. Upload a PDF, DOCX or TXT file.");
            }
        }
        catch (Exception ex)
        {
            return new ResumeExtractionResult(false, string.Empty,
                $"Could not read the file contents. {ex.Message.Split('\n')[0]}");
        }
    }

    private static string ExtractPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            sb.Append(page.Text);
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static string ExtractDocx(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("document.xml was not found in the DOCX file.");

        using var entryStream = entry.Open();
        var document = XDocument.Load(entryStream);

        var paragraphs = document.Descendants(WordNamespace + "p")
            .Select(p => string.Concat(p.Descendants(WordNamespace + "t").Select(t => t.Value.Trim())))
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(Environment.NewLine, paragraphs);
    }
}