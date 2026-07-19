using DiplomaManagementSystem.Application.Common;

namespace DiplomaManagementSystem.Application.Tests;

public sealed class PersonNameSortTests
{
    [Theory]
    [InlineData("Богдан Пашковський", "Пашковський")]
    [InlineData("Євген Куцук", "Куцук")]
    [InlineData("Анастасія Мокрецька", "Мокрецька")]
    [InlineData("  Ілля   Гавриляк  ", "Гавриляк")]
    public void SurnameKey_WhenGivenNameFirst_ReturnsLastWord(string fullName, string expectedSurname)
    {
        Assert.Equal(expectedSurname, PersonNameSort.SurnameKey(fullName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SurnameKey_WhenMissing_ReturnsEmpty(string? fullName)
    {
        Assert.Equal(string.Empty, PersonNameSort.SurnameKey(fullName));
    }

    [Fact]
    public void CompareBySurname_WhenSameSurname_SortsByFullName()
    {
        int compare = PersonNameSort.CompareBySurname("Анна Куцук", "Євген Куцук");

        Assert.True(compare < 0);
    }

    [Fact]
    public void CompareBySurname_SortsByLastNameNotFirstName()
    {
        string[] names =
        [
            "Богдан Агафонкін",
            "Анастасія Мокрецька",
            "Євген Куцук",
        ];

        var sorted = names
            .OrderBy(name => name, Comparer<string>.Create(PersonNameSort.CompareBySurname))
            .ToList();

        Assert.Equal(
            ["Богдан Агафонкін", "Євген Куцук", "Анастасія Мокрецька"],
            sorted);
    }
}
