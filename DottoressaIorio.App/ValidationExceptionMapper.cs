using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.App;

public static class ValidationExceptionMapper
{
    public static string ToUserMessage(Exception exception)
    {
        if (exception is ValidationException validationException)
        {
            return validationException.ValidationResult?.ErrorMessage
                   ?? validationException.Message
                   ?? "Dati non validi.";
        }

        if (exception is DbUpdateException dbUpdateException &&
            dbUpdateException.InnerException is SqliteException sqliteException &&
            sqliteException.SqliteErrorCode == 19 &&
            sqliteException.Message.Contains("UNIQUE constraint failed: TherapyTemplates.Title",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Il titolo gia esiste.";
        }

        return "Impossibile salvare i dati. Controlla i campi inseriti.";
    }
}
