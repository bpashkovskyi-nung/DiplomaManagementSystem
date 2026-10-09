using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests;

public sealed class DefenceWorkLabelTests
{
    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "бакалаврська робота")]
    [InlineData(DefenceSessionType.Master, "магістерська робота")]
    public void Singular_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.Singular(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "Бакалаврські роботи")]
    [InlineData(DefenceSessionType.Master, "Магістерські роботи")]
    public void PluralCapitalized_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.PluralCapitalized(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "Моя бакалаврська робота")]
    [InlineData(DefenceSessionType.Master, "Моя магістерська робота")]
    public void MyWork_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.MyWork(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "Тему бакалаврської роботи подано на розгляд.")]
    [InlineData(DefenceSessionType.Master, "Тему магістерської роботи подано на розгляд.")]
    public void TopicSubmitted_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.TopicSubmitted(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "Бакалаврська робота")]
    [InlineData(DefenceSessionType.Master, "Магістерська робота")]
    public void SingularCapitalized_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.SingularCapitalized(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor)]
    [InlineData(DefenceSessionType.Master)]
    public void NotCreatedYet_ContainsCapitalizedWorkType(DefenceSessionType type)
    {
        string label = DefenceWorkLabel.NotCreatedYet(type);

        Assert.Contains(DefenceWorkLabel.SingularCapitalized(type), label, StringComparison.Ordinal);
        Assert.Contains("адміністратора", label, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "бакалаврською роботою")]
    [InlineData(DefenceSessionType.Master, "магістерською роботою")]
    public void StudentLinkedToWork_ContainsInstrumentalForm(DefenceSessionType type, string expectedFragment)
    {
        Assert.Contains(expectedFragment, DefenceWorkLabel.StudentLinkedToWork(type), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "бакалаврські роботи")]
    [InlineData(DefenceSessionType.Master, "магістерські роботи")]
    public void Plural_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.Plural(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "бакалаврську роботу")]
    [InlineData(DefenceSessionType.Master, "магістерську роботу")]
    public void SingularAccusative_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.SingularAccusative(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "бакалаврською роботою")]
    [InlineData(DefenceSessionType.Master, "магістерською роботою")]
    public void SingularInstrumental_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.SingularInstrumental(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "бакалаврських робіт")]
    [InlineData(DefenceSessionType.Master, "магістерських робіт")]
    public void GenitivePlural_ReturnsExpectedLabel(DefenceSessionType type, string expected)
    {
        Assert.Equal(expected, DefenceWorkLabel.GenitivePlural(type));
    }

    [Theory]
    [InlineData(DefenceSessionType.Bachelor, "бакалаврську роботу")]
    [InlineData(DefenceSessionType.Master, "магістерську роботу")]
    public void StudentAlreadyHasWork_ContainsAccusativeForm(DefenceSessionType type, string expectedFragment)
    {
        Assert.Contains(expectedFragment, DefenceWorkLabel.StudentAlreadyHasWork(type), StringComparison.Ordinal);
    }

    [Fact]
    public void Singular_InvalidType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DefenceWorkLabel.Singular((DefenceSessionType)99));
    }

    [Theory]
    [InlineData(nameof(DefenceWorkLabel.Plural))]
    [InlineData(nameof(DefenceWorkLabel.SingularCapitalized))]
    [InlineData(nameof(DefenceWorkLabel.SingularAccusative))]
    [InlineData(nameof(DefenceWorkLabel.SingularInstrumental))]
    [InlineData(nameof(DefenceWorkLabel.GenitivePlural))]
    public void Methods_InvalidType_Throw(string methodName)
    {
        var invalid = (DefenceSessionType)99;

        Action action = methodName switch
        {
            nameof(DefenceWorkLabel.Plural) => () => DefenceWorkLabel.Plural(invalid),
            nameof(DefenceWorkLabel.SingularCapitalized) => () => DefenceWorkLabel.SingularCapitalized(invalid),
            nameof(DefenceWorkLabel.SingularAccusative) => () => DefenceWorkLabel.SingularAccusative(invalid),
            nameof(DefenceWorkLabel.SingularInstrumental) => () => DefenceWorkLabel.SingularInstrumental(invalid),
            nameof(DefenceWorkLabel.GenitivePlural) => () => DefenceWorkLabel.GenitivePlural(invalid),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName)),
        };

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}
