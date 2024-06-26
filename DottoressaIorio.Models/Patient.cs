using System.ComponentModel.DataAnnotations;
using DottoressaIorio.Models.Abstract;

namespace DottoressaIorio.Models
{
    public class Patient : IDataHandler
    {
        public int PatientId { get; set; }

        public string? Title { get; set; }

        [Required(ErrorMessage = "Il Nome é obbligatorio.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Il Cognome é obbligatorio.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "La Data di Nascita é obbligatoria.")]
        [DataType(DataType.Date)]
        [Range(typeof(DateTime), "1900-01-01", "2099-12-31", ErrorMessage = "La data di nascita deve essere compresa tra il 1900/01/01 e il 2099/12/31.")]
        public DateTime? DateOfBirth { get; set; }

        public string? PlaceOfBirth { get; set; }

        public string? Gender { get; set; }

        [RegularExpression(@"^\+?[\d\s-().]+$", 
            ErrorMessage = "Il formato del numero di telefono non é valido.")]
        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? Notes { get; set; }

        public List<Therapy> Therapies { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? EditDate { get; set; }

        public bool Deleted { get; set; }
    }
}