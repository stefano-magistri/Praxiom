using System.ComponentModel.DataAnnotations;
using DottoressaIorio.Models.Abstract;

namespace DottoressaIorio.Models;

public class TherapyTemplate : IDataHandler
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Il titolo é richiesto.")]
    public string Title { get; set; }
    [Required(ErrorMessage = "La descrizione é richiesta.")]
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? EditDate { get; set; }
    public bool Deleted { get; set; }
}