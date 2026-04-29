using WebApplication2.DTO;

namespace WebApplication2.Services;

public interface IDBService
{

    Task<GetCustomer> GetCustomer(int customerId);
    Task CreateRentalWithMoviesAsync(int customerId, CreateRentals dto);

}