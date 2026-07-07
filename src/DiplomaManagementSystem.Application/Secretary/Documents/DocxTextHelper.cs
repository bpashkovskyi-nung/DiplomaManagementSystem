using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DiplomaManagementSystem.Application.Secretary.Documents;

internal static class DocxTextHelper
{
    public static string GetParagraphText(Paragraph paragraph) =>
        string.Concat(paragraph.Descendants<Text>().Select(text => text.Text));

    public static Paragraph? FindParagraphContaining(OpenXmlElement root, string value) =>
        root.Descendants<Paragraph>()
            .FirstOrDefault(paragraph => GetParagraphText(paragraph).Contains(value, StringComparison.Ordinal));

    public static void SetParagraphText(Paragraph paragraph, string text)
    {
        Run? templateRun = paragraph.Descendants<Run>().FirstOrDefault();
        paragraph.RemoveAllChildren<Run>();

        Run run = templateRun is not null
            ? (Run)templateRun.CloneNode(true)
            : new Run();

        run.RemoveAllChildren<Text>();
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        paragraph.AppendChild(run);
    }

    public static void SetParagraphTextWithRightTab(Paragraph paragraph, string leftText, string rightText)
    {
        Run? templateRun = paragraph.Descendants<Run>().FirstOrDefault();
        paragraph.RemoveAllChildren<Run>();

        RunProperties? properties = templateRun?.RunProperties?.CloneNode(true) as RunProperties;

        Run leftRun = CreateRun(leftText, properties);
        Run tabRun = new();
        if (properties is not null)
        {
            tabRun.AppendChild((RunProperties)properties.CloneNode(true));
        }

        tabRun.AppendChild(new TabChar());

        Run rightRun = CreateRun(rightText, properties);

        paragraph.Append(leftRun, tabRun, rightRun);
    }

    private static Run CreateRun(string text, RunProperties? properties)
    {
        Run run = new();
        if (properties is not null)
        {
            run.AppendChild((RunProperties)properties.CloneNode(true));
        }

        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return run;
    }
}
