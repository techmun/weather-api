using System.ComponentModel.DataAnnotations;

namespace WeatherAPI.Models.Dtos;

public sealed record BootstrapUserRequest(
    [Required] string Username,
    [Required] string Password);
