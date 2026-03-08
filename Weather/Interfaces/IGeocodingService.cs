using Weather.Models;

namespace Weather.Interfaces;

public interface IGeocodingService
{
    /// <summary>
    /// Получает координаты и информацию о городе по его названию.
    /// </summary>
    /// <param name="city">Название города</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>GeocodingResult или null, если город не найден</returns>
    Task<GeocodingResult?> GetCoordinatesAsync(string city, CancellationToken cancellationToken = default);
}