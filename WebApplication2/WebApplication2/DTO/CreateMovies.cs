using System.ComponentModel.DataAnnotations;

namespace WebApplication2.DTO;

public class CreateMovies
{
    [StringLength((200))]
    public string Title { get; set; } = string.Empty;
    public decimal RentalPrice { get; set; }
}