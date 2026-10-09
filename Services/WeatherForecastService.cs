using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

public class WeatherForecastService
{
    private readonly AppDbContext _db;

    public WeatherForecastService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<WeatherForecast>> GetAllAsync()
    {
        return await _db.WeatherForecasts.ToListAsync();
    }

    public async Task<WeatherForecast?> GetByIdAsync(int id)
    {
        return await _db.WeatherForecasts.FindAsync(id);
    }

    public async Task<WeatherForecast> CreateAsync(WeatherForecast forecast)
    {
        _db.WeatherForecasts.Add(forecast);
        await _db.SaveChangesAsync();
        return forecast;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var forecast = await _db.WeatherForecasts.FindAsync(id);
        if (forecast is null) return false;
        _db.WeatherForecasts.Remove(forecast);
        await _db.SaveChangesAsync();
        return true;
    }
}
