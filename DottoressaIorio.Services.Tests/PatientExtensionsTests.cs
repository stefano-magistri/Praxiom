using DottoressaIorio.Models;

namespace DottoressaIorio.Services.Tests;

/// <summary>
/// Contains unit tests for the PatientExtensions class.
/// </summary>
public class PatientExtensionsTests
{
    /// <summary>
    /// Tests that the GetHtmlNotes method correctly converts patient notes into HTML paragraphs,
    /// handling both cases where notes exist and where they are null.
    /// </summary>
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