using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Secretary.Documents;
using DiplomaManagementSystem.Application.Secretary.Documents.Dtos;

namespace DiplomaManagementSystem.Application.Tests.Secretary.Documents;

public sealed class TopicOrderDocxGeneratorTests
{
    [Fact]
    public void Generate_ProducesDocxWithStudentName()
    {
        TopicOrderDocxGenerator generator = new(Microsoft.Extensions.Options.Options.Create(new OrganizationOptions
        {
            UniversityName = "Тестовий університет",
            SpecialtyName = "Комп'ютерна інженерія",
            SpecialtyCode = "123",
            FacultyName = "ФІТ",
            DepartmentName = "кафедри КСМ",
            RectorName = "Тест Ректор",
        }));

        TopicOrderDocumentDto document = new(
            "42",
            2026,
            "першого бакалаврського рівня вищої освіти",
            "КІ-22-1",
            "четвертого курсу",
            new TopicOrderDepartmentInfoDto("123", "Комп'ютерна інженерія", "ФІТ", "очної форми навчання", "кафедри КСМ"),
            [new TopicOrderStudentRowDto("Студент Тест Тестович", "Тема роботи", "доц. Керівник К.К.")],
            [new TopicOrderReviewerRowDto("доц. Рецензент Р.Р.", 1)],
            "асист. Нормоконтроль Н.Н.",
            "проф. Завідувач З.З.",
            []);

        byte[] content = generator.Generate(document);

        Assert.NotEmpty(content);
        Assert.Equal(0x50, content[0]);
        Assert.Equal(0x4B, content[1]);

        string documentText = ExtractDocumentText(content);
        Assert.Contains("Студент Тест Тестович", documentText);
        Assert.Contains("Тема роботи", documentText);
        Assert.Contains("доц. Рецензент Р.Р.", documentText);
    }

    private static string ExtractDocumentText(byte[] content)
    {
        using MemoryStream stream = new(content);
        using WordprocessingDocument wordDocument = WordprocessingDocument.Open(stream, false);
        Body body = wordDocument.MainDocumentPart!.Document!.Body!;
        return string.Concat(body.Descendants<Text>().Select(text => text.Text));
    }
}
