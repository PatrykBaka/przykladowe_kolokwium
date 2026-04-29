using WebApplication2.DTO;

using WebApplication2.Exceptions;
using Microsoft.Data.SqlClient;

namespace WebApplication2.Services;

public class DBService : IDBService
{

    private readonly string _connectionString;
    
    public DBService(IConfiguration config)
    {
	    _connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<GetCustomer> GetCustomer(int customerId)
    {

        var query = """
                    SELECT first_name AS FirstName,
                           last_name AS LastName,
                           R.rental_id AS RentalId,
                           R.rental_date AS RentalDate,
                           R.return_date AS ReturnDate, 
                           S.name AS Status,
                           M.title AS Title,
                           RI.price_at_rental AS PriceAtRental
                    FROM Customer C
                    JOIN Rental R ON R.customer_id = C.customer_id
                    JOIN Status S ON S.status_id = R.status_id
                    JOIN Rental_Item RI ON RI.rental_id = R.rental_id
                    JOIN Movie M ON M.movie_id = RI.movie_id
                    WHERE C.customer_id = @customerId;
                    """;

        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync();
        
        await using var comm = new SqlCommand();
        comm.Connection = con;
        comm.CommandText = query;
        comm.Parameters.AddWithValue("@customerId", customerId);

        await using var reader = await comm.ExecuteReaderAsync();
        
        GetCustomer? result = null;
        
        var ordFirstName = reader.GetOrdinal("FirstName");
        var ordLastName = reader.GetOrdinal("LastName");
        var ordRentalId = reader.GetOrdinal("RentalId");
        var ordRentalDate = reader.GetOrdinal("RentalDate");
        var ordReturnDate = reader.GetOrdinal("ReturnDate");
        var ordStatus = reader.GetOrdinal("Status");
        var ordMovieTitle = reader.GetOrdinal("Title");
        var ordPrice = reader.GetOrdinal("PriceAtRental");

        while (await reader.ReadAsync())
        {

            if (result is null)
            {
                result = new GetCustomer()
                {
                    FirstName = reader.GetString(ordFirstName),
                    LastName = reader.GetString(ordLastName),
                    Rentals = new List<GetRental>()
                };
            }
            
            var rentalId =  reader.GetInt32(ordRentalId);
            
            var rental = result.Rentals.FirstOrDefault(e => e.RentalId == rentalId);

            if (rental is null)
            {
                rental = new GetRental()
                {
                    RentalId = rentalId,
                    RentalDate = reader.GetDateTime(ordRentalDate),
                    ReturnDate = reader.IsDBNull(ordReturnDate) ? null : reader.GetDateTime(ordReturnDate),
                    Status = reader.GetString(ordStatus),
                    Movies = new List<GetMovie>()
                };
                result.Rentals.Add(rental);
            }

            rental.Movies.Add(new GetMovie
            {
                Title = reader.GetString(ordMovieTitle),
                PriceAtRental = reader.GetDecimal(ordPrice)
            });

        }
        return result ?? throw new NotFoundException("No rental found");

    }
    
    public async Task CreateRentalWithMoviesAsync(int customerId, CreateRentals dto)
    {
	    var createRentalQuery = """
	                            INSERT INTO Rental
	                            VALUES(@RentalDate, @ReturnDate, @CustomerId, @StatusId)
	                            SELECT @@IDENTITY;
	                            """;

	    var createRentalItemQuery = """
	                                INSERT INTO Rental_Item
	                                VALUES(@RentalId, @MovieId, @Price);
	                                """;

	    var getMovieIdQuery = """
	                          SELECT movie_id
	                          FROM Movie
	                          WHERE title = @MovieTitle;
	                          """;

	    var checkCustomerQuery = """
	                             SELECT 1 
	                             FROM Customer 
	                             WHERE customer_id = @IdCustomer;
	                             """;

	    await using var connection = new SqlConnection(_connectionString);
	    await connection.OpenAsync();
	    
	    await using var transaction = await connection.BeginTransactionAsync();
        
	    await using var command = new SqlCommand();
	    command.Connection = connection;
	    command.Transaction = transaction as SqlTransaction;

	    try
	    {
		    command.Parameters.Clear();
		    command.CommandText = checkCustomerQuery;
		    command.Parameters.AddWithValue("@IdCustomer", customerId);
                
		    var customerIdRes = await command.ExecuteScalarAsync();
		    if (customerIdRes == null)
		    {
			    throw new NotFoundException($"Customer with ID - {customerId} - not found.");
		    }
		    
			command.Parameters.Clear();
			command.CommandText = createRentalQuery;
			command.Parameters.AddWithValue("@RentalDate", dto.RentalDate);
			command.Parameters.AddWithValue("@ReturnDate", DBNull.Value);
			command.Parameters.AddWithValue("@CustomerId", customerId);
			command.Parameters.AddWithValue("@StatusId", 1);

			var rentalObject = await command.ExecuteScalarAsync();
			var rentalId = Convert.ToInt32(rentalObject);

			foreach (var movie in dto.Movies)
			{
				command.Parameters.Clear();
				command.CommandText = getMovieIdQuery;
				command.Parameters.AddWithValue("@MovieTitle", movie.Title);
				
				var movieObject = await command.ExecuteScalarAsync();
				if (movieObject == null)
				{
					throw new NotFoundException($"Movie - {movie.Title} - not found.");
				}
				
				var movieId = Convert.ToInt32(movieObject);
				
				command.Parameters.Clear();
				command.CommandText = createRentalItemQuery;
				command.Parameters.AddWithValue("@RentalId", rentalId);
				command.Parameters.AddWithValue("@MovieId", movieId);
				command.Parameters.AddWithValue("@Price", movie.RentalPrice);

				await command.ExecuteNonQueryAsync();
			}
		    
		    await transaction.CommitAsync();
	    }
	    catch (Exception e)
	    {
		    await transaction.RollbackAsync();
		    throw;
	    }
    }
    
}