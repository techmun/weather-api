using System.ComponentModel.DataAnnotations;

namespace WeatherAPI.Models.Dtos;

public sealed record TokenRequest(
    [Required] string Username,
    [Required] string Password);
