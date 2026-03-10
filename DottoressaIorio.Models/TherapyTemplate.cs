using System.ComponentModel.DataAnnotations;
using DottoressaIorio.Models.Abstract;

namespace DottoressaIorio.Models;

/// <summary>
///     Represents a template for creating therapies or treatments.
/// </summary>
public class TherapyTemplate : IDataHandler
{
    /// <summary>
    ///     Gets or sets the unique identifier for the therapy template.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the title of the therapy template.
    /// </summary>
    [Required(ErrorMessage = "Il titolo è richiesto.")]
    public string Title { get; set; }

    /// <summary>
    ///     Gets or sets the description of the therapy template.
    /// </summary>
    [Required(ErrorMessage = "La descrizione è richiesta.")]
    public string Description { get; set; }

    /// <summary>
    ///     Gets or sets the date when the therapy template was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    ///     Gets or sets the date when the therapy template was last edited.
    /// </summary>
    public DateTime? EditDate { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the therapy template is marked as deleted.
    /// </summary>
    public bool Deleted { get; set; }
}