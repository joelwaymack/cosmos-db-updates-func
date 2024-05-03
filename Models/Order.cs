namespace CosmosDbUpdates.Models;

public record Order(Guid Id, Guid CustomerId, string CustomerFirstName, string CustomerLastName, decimal Subtotal);