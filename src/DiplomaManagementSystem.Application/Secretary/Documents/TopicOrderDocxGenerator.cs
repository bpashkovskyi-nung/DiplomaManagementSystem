using System.Reflection;

using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Secretary.Documents.Dtos;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Application.Secretary.Documents;

internal sealed class TopicOrderDocxGenerator(IOptions<OrganizationOptions> organizationOptions)
{
    private const string TemplateResourceName =
        "DiplomaManagementSystem.Application.Secretary.Documents.Templates.topic-order.docx";

    private readonly OrganizationOptions _organization = organizationOptions.Value;

    public byte[] Generate(TopicOrderDocumentDto document)
    {
        using MemoryStream outputStream = new();
        using (Stream templateStream = LoadTemplateStream())
        {
            templateStream.CopyTo(outputStream);
        }

        outputStream.Position = 0;

        using (var wordDocument = WordprocessingDocument.Open(outputStream, true))
        {
            Body body = wordDocument.MainDocumentPart!.Document!.Body!;
            FillHeader(body, document);
            FillStudentTable(body, document.Students);
            FillReviewers(body, document.Reviewers);
            FillFormattingReviewer(body, document.FormattingReviewerLine);
            FillDepartmentHead(body, document.DepartmentHeadLine, document.DepartmentInfo.DepartmentName);
            FillRector(body);
            wordDocument.MainDocumentPart.Document.Save();
        }

        return outputStream.ToArray();
    }

    private static Stream LoadTemplateStream()
    {
        Assembly assembly = typeof(TopicOrderDocxGenerator).Assembly;
        Stream? stream = assembly.GetManifestResourceStream(TemplateResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"DOCX template resource '{TemplateResourceName}' was not found.");
        }

        return stream;
    }

    private void FillHeader(Body body, TopicOrderDocumentDto document)
    {
        Table? table = body.Elements<Table>().FirstOrDefault();
        IEnumerable<Paragraph> headerParagraphs = table is null
            ? body.Elements<Paragraph>()
            : body.Elements().TakeWhile(element => element is not Table).OfType<Paragraph>();

        var paragraphs = headerParagraphs.ToList();

        ReplaceParagraph(paragraphs, "МІНІСТЕРСТВО", _organization.MinistryName);
        ReplaceParagraph(paragraphs, "НАЦІОНАЛЬНИЙ", _organization.UniversityName);
        ReplaceParagraph(paragraphs, "м. ", _organization.City);

        Paragraph? yearParagraph = paragraphs.FirstOrDefault(paragraph =>
            DocxTextHelper.GetParagraphText(paragraph).Contains(" р", StringComparison.Ordinal));
        if (yearParagraph is not null)
        {
            DocxTextHelper.SetParagraphTextWithRightTab(
                yearParagraph,
                $"{document.Year} р.",
                $"№ {document.OrderNumber}");
        }

        TopicOrderDepartmentInfoDto departmentInfo = document.DepartmentInfo;
        ReplaceParagraph(
            paragraphs,
            "спеціальності",
            $"спеціальності {departmentInfo.SpecialtyCode} - \"{departmentInfo.SpecialtyName}\"");
        ReplaceParagraph(paragraphs, "факультету", FormatFacultyTitleLine(departmentInfo.FacultyName));
        ReplaceParagraph(paragraphs, "студентів", $"студентів {departmentInfo.StudyForm}");
        ReplaceParagraph(
            paragraphs,
            "Нижчезазначених",
            BuildPreamble(document));
    }

    private string BuildPreamble(TopicOrderDocumentDto document)
    {
        TopicOrderDepartmentInfoDto departmentInfo = document.DepartmentInfo;
        return $"Нижчезазначених студентів {document.SessionLevelPhrase} " +
               $"спеціальності - \"{departmentInfo.SpecialtyName}\" груп {document.GroupsPhrase} " +
               $"{departmentInfo.FacultyName} {departmentInfo.StudyForm} {document.CoursePhrase},";
    }

    private static void FillStudentTable(Body body, IReadOnlyList<TopicOrderStudentRowDto> students)
    {
        Table table = body.Elements<Table>().First();
        var rows = table.Elements<TableRow>().ToList();
        if (rows.Count < 2)
        {
            return;
        }

        TableRow templateRow = rows[1];
        for (int index = rows.Count - 1; index >= 2; index--)
        {
            rows[index].Remove();
        }

        if (students.Count == 0)
        {
            templateRow.Remove();
            return;
        }

        FillStudentRow(templateRow, 1, students[0]);
        TableRow lastRow = templateRow;
        for (int studentIndex = 1; studentIndex < students.Count; studentIndex++)
        {
            var newRow = (TableRow)templateRow.CloneNode(true);
            FillStudentRow(newRow, studentIndex + 1, students[studentIndex]);
            lastRow.InsertAfterSelf(newRow);
            lastRow = newRow;
        }
    }

    private static void FillStudentRow(TableRow row, int index, TopicOrderStudentRowDto student)
    {
        TableCell[] cells = row.Elements<TableCell>().ToArray();
        if (cells.Length < 2)
        {
            return;
        }

        Paragraph indexParagraph = cells[0].Elements<Paragraph>().First();
        DocxTextHelper.SetParagraphText(indexParagraph, $"1.{index}");

        Paragraph[] contentParagraphs = cells[1].Elements<Paragraph>().ToArray();
        if (contentParagraphs.Length < 3)
        {
            return;
        }

        DocxTextHelper.SetParagraphText(contentParagraphs[0], student.StudentFullName);
        DocxTextHelper.SetParagraphText(contentParagraphs[1], $"\"{student.TopicTitle}\"");
        DocxTextHelper.SetParagraphText(contentParagraphs[2], $"Керівник – {student.SupervisorLine}");
    }

    private static void FillReviewers(Body body, IReadOnlyList<TopicOrderReviewerRowDto> reviewers)
    {
        Paragraph? sectionHeader = DocxTextHelper.FindParagraphContaining(body, "2. Призначити рецензентів");
        if (sectionHeader is null)
        {
            return;
        }

        List<Paragraph> reviewerParagraphs = [];
        OpenXmlElement? current = sectionHeader.NextSibling();
        while (current is not null)
        {
            if (current is Paragraph paragraph)
            {
                string text = DocxTextHelper.GetParagraphText(paragraph);
                if (text.Contains("3. Призначити", StringComparison.Ordinal))
                {
                    break;
                }

                if (text.TrimStart().StartsWith("2.", StringComparison.Ordinal))
                {
                    reviewerParagraphs.Add(paragraph);
                }
            }

            current = current.NextSibling();
        }

        if (reviewerParagraphs.Count == 0)
        {
            return;
        }

        Paragraph templateParagraph = reviewerParagraphs[0];
        foreach (Paragraph extraParagraph in reviewerParagraphs.Skip(1))
        {
            extraParagraph.Remove();
        }

        if (reviewers.Count == 0)
        {
            templateParagraph.Remove();
            return;
        }

        FillReviewerParagraph(templateParagraph, 1, reviewers[0]);
        Paragraph lastParagraph = templateParagraph;
        for (int index = 1; index < reviewers.Count; index++)
        {
            var newParagraph = (Paragraph)templateParagraph.CloneNode(true);
            FillReviewerParagraph(newParagraph, index + 1, reviewers[index]);
            lastParagraph.InsertAfterSelf(newParagraph);
            lastParagraph = newParagraph;
        }
    }

    private static void FillReviewerParagraph(Paragraph paragraph, int index, TopicOrderReviewerRowDto reviewer) =>
        DocxTextHelper.SetParagraphText(
            paragraph,
            $"2.{index}. {reviewer.ReviewerLine} – {reviewer.AssignmentCount} чол.");

    private static void FillFormattingReviewer(Body body, string? formattingReviewerLine)
    {
        Paragraph? sectionHeader = DocxTextHelper.FindParagraphContaining(body, "3. Призначити відповідального");
        if (sectionHeader is null)
        {
            return;
        }

        Paragraph? detailParagraph = sectionHeader.NextSibling<Paragraph>();
        if (detailParagraph is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(formattingReviewerLine))
        {
            detailParagraph.Remove();
            return;
        }

        DocxTextHelper.SetParagraphText(detailParagraph, $"3.1. {formattingReviewerLine}");
    }

    private void FillDepartmentHead(Body body, string? departmentHeadLine, string departmentName)
    {
        Paragraph? paragraph = DocxTextHelper.FindParagraphContaining(body, "4. Контроль за виконанням");
        if (paragraph is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(departmentHeadLine))
        {
            DocxTextHelper.SetParagraphText(
                paragraph,
                $"4. Контроль за виконанням наказу покласти на завідувача {departmentName}");
            return;
        }

        DocxTextHelper.SetParagraphText(
            paragraph,
            $"4. Контроль за виконанням наказу покласти на завідувача {departmentName} {departmentHeadLine}");
    }

    private void FillRector(Body body)
    {
        Paragraph? paragraph = DocxTextHelper.FindParagraphContaining(body, "Ректор");
        if (paragraph is null || string.IsNullOrWhiteSpace(_organization.RectorName))
        {
            return;
        }

        DocxTextHelper.SetParagraphTextWithRightTab(paragraph, "Ректор", _organization.RectorName);
    }

    private static void ReplaceParagraph(IEnumerable<Paragraph> paragraphs, string contains, string newText)
    {
        Paragraph? paragraph = paragraphs.FirstOrDefault(candidate =>
            DocxTextHelper.GetParagraphText(candidate).Contains(contains, StringComparison.Ordinal));
        if (paragraph is not null)
        {
            DocxTextHelper.SetParagraphText(paragraph, newText);
        }
    }

    private static string FormatFacultyTitleLine(string facultyName)
    {
        if (facultyName.StartsWith("факультет ", StringComparison.OrdinalIgnoreCase))
        {
            return "факультету " + facultyName["факультет ".Length..];
        }

        return facultyName;
    }
}
