namespace DottoressaIorio.Models.Abstract;

/// <summary>
///     Interface that defines common properties for data entities in the system.
/// </summary>
public interface IDataHandler
{
    /// <summary>
    ///     Gets or sets the date when the record was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    ///     Gets or sets the date when the record was last edited.
    /// </summary>
    public DateTime? EditDate { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the record is marked as deleted.
    /// </summary>
    public bool Deleted { get; set; }
}