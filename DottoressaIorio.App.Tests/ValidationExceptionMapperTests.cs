using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.App.Tests;

public class ValidationExceptionMapperTests
{
    [Fact]
    public void ToUserMessage_WhenValidationExceptionWithValidationResult_ReturnsValidationError()
    {
        var exception = new ValidationException(
            new ValidationResult("Campo non valido."),
            validatingAttribute: null,
            value: null);

        var result = ValidationExceptionMapper.ToUserMessage(exception);

        Assert.Equal("Campo non valido.", result);
    }

    [Fact]
    public void ToUserMessage_WhenValidationExceptionWithoutValidationResult_ReturnsExceptionMessage()
    {
        var exception = new ValidationException("Messaggio validazione.");

        var result = ValidationExceptionMapper.ToUserMessage(exception);

        Assert.Equal("Messaggio validazione.", result);
    }

    [Fact]
    public void ToUserMessage_WhenUniqueConstraintOnTemplateTitle_ReturnsDuplicateTitleMessage()
    {
        var sqliteException = new SqliteException(
            "UNIQUE constraint failed: TherapyTemplates.Title",
            19,
            19);
        var exception = new DbUpdateException("db failure", sqliteException);

        var result = ValidationExceptionMapper.ToUserMessage(exception);

        Assert.Equal("Il titolo giá esiste.", result);
    }

    [Fact]
    public void ToUserMessage_WhenUnknownException_ReturnsGenericMessage()
    {
        var exception = new Exception("boom");

        var result = ValidationExceptionMapper.ToUserMessage(exception);

        Assert.Equal("Impossibile salvare i dati. Controlla i campi inseriti.", result);
    }
}
