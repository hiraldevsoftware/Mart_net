namespace Mart.Domain.ValueObjects
{
    public record Location(double Latitude, double Longitude, string? AddressTag);
    public record Money(decimal Amount, string Currency = "INR");


}
