using System.ComponentModel.DataAnnotations;
using DottoressaIorio.Models.Abstract;

namespace DottoressaIorio.Models
{
    /// <summary>
    /// Represents a patient in the medical system.
    /// </summary>
    public class Patient : IDataHandler
    {
        /// <summary>
        /// Gets or sets the unique identifier for the patient.
        /// </summary>
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets the title of the patient (e.g., Mr., Mrs., Dr.).
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the first name of the patient.
        /// </summary>
        [Required(ErrorMessage = "Il Nome é obbligatorio.")]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the patient.
        /// </summary>
        [Required(ErrorMessage = "Il Cognome é obbligatorio.")]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the date of birth of the patient.
        /// </summary>
        [Required(ErrorMessage = "La Data di Nascita é obbligatoria.")]
        [DataType(DataType.Date)]
        [Range(typeof(DateTime), "1900-01-01", "2099-12-31", ErrorMessage = "La data di nascita deve essere compresa tra il 1900/01/01 e il 2099/12/31.")]
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Gets or sets the place of birth of the patient.
        /// </summary>
        public string? PlaceOfBirth { get; set; }

        /// <summary>
        /// Gets or sets the gender of the patient.
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// Gets or sets the phone number of the patient.
        /// </summary>
        [RegularExpression(@"^\+?[\d\s-().]+$", 
            ErrorMessage = "Il formato del numero di telefono non é valido.")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the email address of the patient.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets any additional notes about the patient.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the list of therapies associated with the patient.
        /// </summary>
        public List<Therapy> Therapies { get; set; }

        /// <summary>
        /// Gets or sets the date when the patient record was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the date when the patient record was last edited.
        /// </summary>
        public DateTime? EditDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the patient record is marked as deleted.
        /// </summary>
        public bool Deleted { get; set; }
    }
}