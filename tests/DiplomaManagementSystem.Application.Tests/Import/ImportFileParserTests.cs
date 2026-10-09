using System.Text;

using ClosedXML.Excel;

using DiplomaManagementSystem.Application.Import;
using DiplomaManagementSystem.Application.Import.Models;

namespace DiplomaManagementSystem.Application.Tests.Import;

public sealed class CsvLineTokenizerTests
{
    [Fact]
    public void Split_UnquotedFields_ReturnsTrimmedValues()
    {
        string[] fields = CsvLineTokenizer.Split("Іваненко Іван,ivan@university.edu.ua,KN-41");

        Assert.Equal(["Іваненко Іван", "ivan@university.edu.ua", "KN-41"], fields);
    }

    [Fact]
    public void Split_QuotedFieldWithComma_PreservesCommaInsideField()
    {
        string[] fields = CsvLineTokenizer.Split("\"Іваненко, Іван\",ivan@university.edu.ua,KN-41");

        Assert.Equal(["Іваненко, Іван", "ivan@university.edu.ua", "KN-41"], fields);
    }

    [Fact]
    public void Split_EscapedQuotesInsideQuotedField_DecodesQuotes()
    {
        string[] fields = CsvLineTokenizer.Split("\"Іван \"\"Іво\"\" Іваненко\",ivan@university.edu.ua,KN-41");

        Assert.Equal(["Іван \"Іво\" Іваненко", "ivan@university.edu.ua", "KN-41"], fields);
    }

    [Fact]
    public void Split_EmptyField_PreservesEmptyColumn()
    {
        string[] fields = CsvLineTokenizer.Split("Іваненко Іван,,KN-41");

        Assert.Equal(3, fields.Length);
        Assert.Equal(string.Empty, fields[1]);
    }
}

public sealed class ImportFileParserTests
{
    private readonly ImportFileParser _parser = new();

    [Fact]
    public async Task ParseStudentsFromCsv_InsufficientColumns_AddsParseError()
    {
        await using MemoryStream stream = ToStream("ПІБ,email,group\nІваненко Іван,ivan@university.edu.ua\n");

        ImportParseResult<StudentImportRow> result = await _parser.ParseStudentsAsync(stream, "students.csv", CancellationToken.None);

        Assert.Empty(result.Rows);
        Assert.Single(result.ParseErrors);
        Assert.Contains("недостатньо стовпців", result.ParseErrors[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseStudentsFromCsv_QuotedNameWithComma_ParsesRow()
    {
        await using MemoryStream stream = ToStream(
            "ПІБ,email,group\n\"Іваненко, Іван\",ivan@university.edu.ua,KN-41\n");

        ImportParseResult<StudentImportRow> result = await _parser.ParseStudentsAsync(stream, "students.csv", CancellationToken.None);

        Assert.Single(result.Rows);
        Assert.Equal("Іваненко, Іван", result.Rows[0].FullName);
        Assert.Empty(result.ParseErrors);
    }

    [Fact]
    public async Task ParseEmployeesFromCsv_ValidRows_ParsesAll()
    {
        await using MemoryStream stream = ToStream(
            "ПІБ,email\nПетренко Петро,petro@university.edu.ua\nШевченко Олена,olena@university.edu.ua\n");

        ImportParseResult<EmployeeImportRow> result = await _parser.ParseEmployeesAsync(stream, "employees.csv", CancellationToken.None);

        Assert.Equal(2, result.Rows.Count);
        Assert.Empty(result.ParseErrors);
    }

    [Theory]
    [InlineData("students.csv", true)]
    [InlineData("students.CSV", true)]
    [InlineData("students.xlsx", true)]
    [InlineData("students.txt", false)]
    [InlineData("students", false)]
    public void CanParse_ReturnsExpected(string fileName, bool expected)
    {
        Assert.Equal(expected, _parser.CanParse(fileName));
    }

    [Fact]
    public async Task ParseStudentsFromCsv_Utf8Bom_SkipsHeaderAndParsesRow()
    {
        byte[] bom = Encoding.UTF8.GetPreamble();
        byte[] body = Encoding.UTF8.GetBytes("ПІБ,email,group\nІваненко Іван,ivan@university.edu.ua,KN-41\n");
        byte[] content = new byte[bom.Length + body.Length];
        bom.CopyTo(content, 0);
        body.CopyTo(content, bom.Length);

        await using MemoryStream stream = new(content);
        ImportParseResult<StudentImportRow> result = await _parser.ParseStudentsAsync(stream, "students.csv", CancellationToken.None);

        Assert.Single(result.Rows);
        Assert.Equal("Іваненко Іван", result.Rows[0].FullName);
        Assert.Empty(result.ParseErrors);
    }

    [Fact]
    public async Task ParseStudentsFromCsv_EmptyLines_SkipsBlankRows()
    {
        await using MemoryStream stream = ToStream(
            "ПІБ,email,group\n\nІваненко Іван,ivan@university.edu.ua,KN-41\n   \n");

        ImportParseResult<StudentImportRow> result = await _parser.ParseStudentsAsync(stream, "students.csv", CancellationToken.None);

        Assert.Single(result.Rows);
        Assert.Empty(result.ParseErrors);
    }

    [Fact]
    public async Task ParseEmployeesFromCsv_InsufficientColumns_AddsParseError()
    {
        await using MemoryStream stream = ToStream("ПІБ,email\nПетренко Петро\n");

        ImportParseResult<EmployeeImportRow> result = await _parser.ParseEmployeesAsync(stream, "employees.csv", CancellationToken.None);

        Assert.Empty(result.Rows);
        Assert.Single(result.ParseErrors);
        Assert.Contains("недостатньо стовпців", result.ParseErrors[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseStudentsFromXlsx_WithHeader_SkipsHeaderAndParsesRows()
    {
        await using MemoryStream stream = CreateStudentXlsx(
            includeHeader: true,
            headerFirstCell: "ПІБ",
            rows:
            [
                ("Іваненко Іван", "ivan@university.edu.ua", "KN-41"),
                ("Петренко Петро", "petro@university.edu.ua", "KN-42"),
            ]);

        ImportParseResult<StudentImportRow> result = await _parser.ParseStudentsAsync(stream, "students.xlsx", CancellationToken.None);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("ivan@university.edu.ua", result.Rows[0].Email);
        Assert.Equal("KN-42", result.Rows[1].GroupName);
        Assert.Empty(result.ParseErrors);
    }

    [Fact]
    public async Task ParseStudentsFromXlsx_WithEnglishNameHeader_SkipsHeader()
    {
        await using MemoryStream stream = CreateStudentXlsx(
            includeHeader: true,
            headerFirstCell: "Full name",
            rows: [("Іваненко Іван", "ivan@university.edu.ua", "KN-41")]);

        ImportParseResult<StudentImportRow> result = await _parser.ParseStudentsAsync(stream, "students.xlsx", CancellationToken.None);

        Assert.Single(result.Rows);
        Assert.Equal("Іваненко Іван", result.Rows[0].FullName);
    }

    [Fact]
    public async Task ParseStudentsFromXlsx_WithoutRecognizedHeader_ParsesFirstRowAsData()
    {
        await using MemoryStream stream = CreateStudentXlsx(
            includeHeader: false,
            rows: [("Іваненко Іван", "ivan@university.edu.ua", "KN-41")]);

        ImportParseResult<StudentImportRow> result = await _parser.ParseStudentsAsync(stream, "students.xlsx", CancellationToken.None);

        Assert.Single(result.Rows);
        Assert.Equal("Іваненко Іван", result.Rows[0].FullName);
    }

    [Fact]
    public async Task ParseStudentsFromXlsx_MissingRequiredFields_AddsParseError()
    {
        await using MemoryStream stream = CreateStudentXlsx(
            includeHeader: true,
            headerFirstCell: "ПІБ",
            rows: [("", "missing@university.edu.ua", "KN-41")]);

        ImportParseResult<StudentImportRow> result = await _parser.ParseStudentsAsync(stream, "students.xlsx", CancellationToken.None);

        Assert.Empty(result.Rows);
        Assert.Single(result.ParseErrors);
        Assert.Contains("обов'язкові поля", result.ParseErrors[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseEmployeesFromXlsx_ValidRows_ParsesAll()
    {
        await using MemoryStream stream = CreateEmployeeXlsx(
            includeHeader: true,
            headerFirstCell: "ПІБ",
            rows:
            [
                ("Петренко Петро", "petro@university.edu.ua"),
                ("Шевченко Олена", "olena@university.edu.ua"),
            ]);

        ImportParseResult<EmployeeImportRow> result = await _parser.ParseEmployeesAsync(stream, "employees.xlsx", CancellationToken.None);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("petro@university.edu.ua", result.Rows[0].Email);
        Assert.Empty(result.ParseErrors);
    }

    [Fact]
    public async Task ParseEmployeesFromXlsx_MissingRequiredFields_AddsParseError()
    {
        await using MemoryStream stream = CreateEmployeeXlsx(
            includeHeader: true,
            headerFirstCell: "name",
            rows: [("Петренко Петро", " ")]);

        ImportParseResult<EmployeeImportRow> result = await _parser.ParseEmployeesAsync(stream, "employees.xlsx", CancellationToken.None);

        Assert.Empty(result.Rows);
        Assert.Single(result.ParseErrors);
        Assert.Contains("обов'язкові поля", result.ParseErrors[0], StringComparison.Ordinal);
    }

    private static MemoryStream CreateStudentXlsx(
        bool includeHeader,
        string headerFirstCell = "ПІБ",
        (string FullName, string Email, string Group)[]? rows = null)
    {
        rows ??= [];
        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Students");
        int rowIndex = 1;

        if (includeHeader)
        {
            sheet.Cell(rowIndex, 1).Value = headerFirstCell;
            sheet.Cell(rowIndex, 2).Value = "email";
            sheet.Cell(rowIndex, 3).Value = "group";
            rowIndex++;
        }

        foreach ((string fullName, string email, string group) in rows)
        {
            sheet.Cell(rowIndex, 1).Value = fullName;
            sheet.Cell(rowIndex, 2).Value = email;
            sheet.Cell(rowIndex, 3).Value = group;
            rowIndex++;
        }

        return SaveWorkbook(workbook);
    }

    private static MemoryStream CreateEmployeeXlsx(
        bool includeHeader,
        string headerFirstCell = "ПІБ",
        (string FullName, string Email)[]? rows = null)
    {
        rows ??= [];
        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Employees");
        int rowIndex = 1;

        if (includeHeader)
        {
            sheet.Cell(rowIndex, 1).Value = headerFirstCell;
            sheet.Cell(rowIndex, 2).Value = "email";
            rowIndex++;
        }

        foreach ((string fullName, string email) in rows)
        {
            sheet.Cell(rowIndex, 1).Value = fullName;
            sheet.Cell(rowIndex, 2).Value = email;
            rowIndex++;
        }

        return SaveWorkbook(workbook);
    }

    private static MemoryStream SaveWorkbook(XLWorkbook workbook)
    {
        MemoryStream stream = new();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream ToStream(string content)
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes(content));
        stream.Position = 0;
        return stream;
    }
}
