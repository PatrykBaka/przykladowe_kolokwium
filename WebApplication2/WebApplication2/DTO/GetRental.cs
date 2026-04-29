namespace WebApplication2.DTO;

public class GetRental
{
    
    public int RentalId { get; set; }
    public DateTime RentalDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<GetMovie> Movies { get; set; } = [];

}