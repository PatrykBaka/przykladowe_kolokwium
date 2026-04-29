namespace WebApplication2.DTO;

public class GetCustomer
{
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<GetRental> Rentals { get; set; } = [];

}