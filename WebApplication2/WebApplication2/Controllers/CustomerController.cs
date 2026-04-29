using Microsoft.AspNetCore.Mvc;
using WebApplication2.DTO;
using WebApplication2.Exceptions;
using WebApplication2.Services;

namespace WebApplication2.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerController : ControllerBase
{
    
    private readonly IDBService _dBService;

    public CustomerController(IDBService dbService)
    {
        _dBService = dbService;
    }

    [HttpGet]
    [Route("{id:int}/rentals")]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        try
        {
            var result = await _dBService.GetCustomer(id);
            return Ok(result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [Route("{id}/rentals")]
    [HttpPost]
    public async Task<IActionResult> Post([FromRoute] int id, [FromBody] CreateRentals dto)
    {
        if (!dto.Movies.Any())
        {
            return BadRequest("At least one item is required.");
        }

        try
        {
            await _dBService.CreateRentalWithMoviesAsync(id, dto);
            return Created($"api/customers/{id}/rentals", dto);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

}