# Cosmos DB Document Updates

This is an example of updating Cosmos DB denormalized data using the change feed and Durable Azure Functions. The accompanying blog post can be found [here](https://waymack.net/updating-denormalized-data).

## Tooling

To run this repo you'll need to install

- .NET 8
- CosmosDB Emulator
- Azurite Storage Emulator

## Settings

You'll also need to create a *local.settings.json* file in the directory root with the following data:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "Sales",
    "CustomerContainerName": "Customers",
    "OrderContainerName": "Orders"
  }
}
```

## Cosmos Setup

In the Cosmos emulator, create a new database named *Sales* with two containers:

- *Customers* with a partition key of */id*
- *Orders* with a partition key of */customerId*

Create a few customers and orders with the following format:

```json Customer
{
    "id": "e81031a2-d8ab-43d0-a8ce-00ca81fa357b",
    "firstName": "Jeff",
    "lastName": "Jefferson"
}
```

```json Order
{
    "id": "0548ab0e-3c2a-4d73-9e6c-261e32967b98",
    "customerId": "e81031a2-d8ab-43d0-a8ce-00ca81fa357b",
    "firstName": "Jeff",
    "lastName": "Jefferson",
    "subtotal": 82.31
}
```

## Running

Start the Azurite emulator and the Cosmos DB emulator. To run the function app, run *func start* in the top level directory.
