using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly WeatherForecastService _service;

    public WeatherForecastController(WeatherForecastService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetAll()
    {
        return await _service.GetAllAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WeatherForecast>> GetById(int id)
    {
        var forecast = await _service.GetByIdAsync(id);
        if (forecast is null) return NotFound();
        return forecast;
    }

    [HttpPost]
    public async Task<ActionResult<WeatherForecast>> Create(WeatherForecast forecast)
    {
        var created = await _service.CreateAsync(forecast);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
