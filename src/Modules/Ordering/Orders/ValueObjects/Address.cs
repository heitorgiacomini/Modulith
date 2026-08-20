namespace Ordering.Orders.ValueObjects;
public record Address
{
    public string FirstName { get; } = default!;
    public string LastName { get; } = default!;
    public string? EmailAddress { get; } = default!;
    public string Phone { get; } = default!;
    public string AddressLine1 { get; } = default!;
    public string? AddressLine2 { get; } = default!;
    public string City { get; } = default!;
    public string State { get; } = default!;
    public string PostalCode { get; } = default!;
    public string CountryCode { get; } = default!;
    protected Address()
    {
    }

    private Address(string firstName, string lastName, string emailAddress, string phone, string addressLine1, string? addressLine2, string city, string state, string postalCode, string countryCode)
    {
        FirstName = firstName;
        LastName = lastName;
        EmailAddress = emailAddress;
        Phone = phone;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        PostalCode = postalCode;
        CountryCode = countryCode;
    }

    public static Address Of(string firstName, string lastName, string emailAddress, string phone, string addressLine1, string? addressLine2, string city, string state, string postalCode, string countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(postalCode.Length, 20);

        return new Address(firstName, lastName, emailAddress, phone, addressLine1, addressLine2, city, state, postalCode, countryCode);
    }
}
