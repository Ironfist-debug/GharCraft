namespace GharCraft.Domain.ValueObjects;

public record Address(
    string FullName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country = "India"
);
