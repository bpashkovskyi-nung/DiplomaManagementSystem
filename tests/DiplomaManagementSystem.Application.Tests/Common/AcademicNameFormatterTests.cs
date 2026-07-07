using DiplomaManagementSystem.Application.Common;

namespace DiplomaManagementSystem.Application.Tests.Common;

public sealed class AcademicNameFormatterTests
{
    [Theory]
    [InlineData("Гарасимів Тарас Григорович", "Гарасимів Т.Г.")]
    [InlineData("Драган Ніколай", "Драган Н.")]
    [InlineData("Бабчук Сергій Миколайович", "Бабчук С.М.")]
    [InlineData("Іваненко", "Іваненко")]
    public void ToShortDisplayName_FormatsUkrainianNames(string fullName, string expected)
    {
        Assert.Equal(expected, AcademicNameFormatter.ToShortDisplayName(fullName));
    }
}
