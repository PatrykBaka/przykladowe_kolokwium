namespace WebApplication2.DTO;

public class CreateRentals
{
    public DateTime RentalDate { get; set; }
    public List<CreateMovies> Movies { get; set; } = [];
}