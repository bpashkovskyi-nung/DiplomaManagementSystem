using System.Text;

using DiplomaManagementSystem.Application.Storage;

namespace DiplomaManagementSystem.Integration.Tests.Support;

internal static class IntegrationTestCsv
{
    public static MemoryStream Students(params (string FullName, string Email, string GroupName)[] rows)
    {
        StringBuilder builder = new();
        builder.AppendLine("FullName,Email,Group");
        foreach ((string fullName, string email, string groupName) in rows)
        {
            builder.AppendLine($"{fullName},{email},{groupName}");
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    public static MemoryStream Employees(params (string FullName, string Email)[] rows)
    {
        StringBuilder builder = new();
        builder.AppendLine("FullName,Email");
        foreach ((string fullName, string email) in rows)
        {
            builder.AppendLine($"{fullName},{email}");
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
    }
}

internal static class IntegrationTestDocuments
{
    public static UploadFileContent CreatePdf(string fileName)
    {
        byte[] bytes = "test document content"u8.ToArray();
        MemoryStream stream = new(bytes);
        return new UploadFileContent(stream, fileName, "application/pdf", bytes.Length);
    }

    public static UploadFileContent CreateInvalid(string fileName, string contentType)
    {
        byte[] bytes = "invalid content"u8.ToArray();
        MemoryStream stream = new(bytes);
        return new UploadFileContent(stream, fileName, contentType, bytes.Length);
    }

    public static UploadFileContent CreateEmptyPdf(string fileName)
    {
        MemoryStream stream = new([]);
        return new UploadFileContent(stream, fileName, "application/pdf", 0);
    }
}
