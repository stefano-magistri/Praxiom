using System.ComponentModel.DataAnnotations;
using DottoressaIorio.Models.Abstract;

namespace DottoressaIorio.Models;

/// <summary>
///     Represents a therapy or treatment prescribed to a patient.
/// </summary>
public class Therapy : IDataHandler
{
    /// <summary>
    ///     Gets or sets the unique identifier for the therapy.
    /// </summary>
    public int TherapyId { get; set; }

    /// <summary>
    ///     Gets or sets the description of the therapy or treatment.
    /// </summary>
    [Required(ErrorMessage = "La descrizione è richiesta.")]
    public string Description { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the patient associated with this therapy.
    /// </summary>
    public int PatientId { get; set; }

    /// <summary>
    ///     Gets or sets the patient associated with this therapy.
    /// </summary>
    public Patient Patient { get; set; }

    /// <summary>
    ///     Gets or sets the date when the therapy record was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    ///     Gets or sets the date when the therapy record was last edited.
    /// </summary>
    public DateTime? EditDate { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the therapy record is marked as deleted.
    /// </summary>
    public bool Deleted { get; set; }
}