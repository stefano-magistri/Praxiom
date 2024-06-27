using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Tests;

public class PatientExtensionsTests
{
    [Fact]
    public void GetHtmlNotes_ShouldConvertToHtmlParagraphs()
    {
        // Arrange
        var patientWithNotes = new Patient { Notes = "Note 1\nNote 2" };
        var patientWithoutNotes = new Patient { Notes = null };

        // Act
        var htmlNotesWithNotes = patientWithNotes.GetHtmlNotes();
        var htmlNotesWithoutNotes = patientWithoutNotes.GetHtmlNotes();

        // Assert
        Assert.Contains("<p>Note 1</p>", htmlNotesWithNotes);
        Assert.Contains("<p>Note 2</p>", htmlNotesWithNotes);
        Assert.Equal("", htmlNotesWithoutNotes); // Assert that null notes return an empty string
    }
}