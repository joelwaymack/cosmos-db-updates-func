using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using CosmosDbUpdates.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace CosmosDbUpdates.Handlers;

public class CustomerHandler
{
    private readonly ILogger _logger;
    private readonly Container _orderContainer;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CustomerHandler(ILoggerFactory loggerFactory, [FromKeyedServices("OrderContainer")] Container orderContainer)
    {
        _logger = loggerFactory.CreateLogger<CustomerHandler>();
        _orderContainer = orderContainer;
    }

    [Function("ProcessCustomerChange")]
    public async Task ProcessCustomerChange([CosmosDBTrigger(
            databaseName: "%DatabaseName%",
            containerName: "%CustomerContainerName%",
            Connection = "CosmosConnectionString",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<Customer> customers,
            [DurableClient] DurableTaskClient durableClient)
    {
        if (customers != null && customers.Count > 0)
        {
            _logger.LogInformation("Documents modified: " + customers.Count);
            foreach (var customer in customers)
            {
                var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync("UpdateCustomerDenormalizedData", customer);
                _logger.LogInformation($"Started orchestration for customer '{customer.Id}' with orchestration of '{instanceId}'.");
            }
        }
    }

    [Function("UpdateCustomerDenormalizedData")]
    public async Task UpdateCustomerDenormalizedData([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var customer = context.GetInput<Customer>();

        if (customer == null)
        {
            _logger.LogWarning("Customer not found.");
            return;
        }

        var orders = await context.CallActivityAsync<List<Order>>("GetOrdersToUpdate", customer.Id);

        var updateTask = new List<Task>();
        foreach (var order in orders)
        {
            updateTask.Add(context.CallActivityAsync("UpdateOrderCustomer", Tuple.Create(order, customer)));
        }

        await Task.WhenAll(updateTask);
    }

    [Function("GetOrdersToUpdate")]
    public async Task<IList<Order>> GetOrdersToUpdate([ActivityTrigger] Guid customerId)
    {
        _logger.LogInformation($"Getting orders to update for customer {customerId}");
        var query = new QueryDefinition("SELECT * FROM o WHERE o.customerId = @customerId")
            .WithParameter("@customerId", customerId);
        var filteredFeed = _orderContainer.GetItemQueryIterator<Order>(query);

        var orders = new List<Order>();
        while (filteredFeed.HasMoreResults)
        {
            var response = await filteredFeed.ReadNextAsync();
            orders.AddRange(response.Select(o => o));
        }

        _logger.LogInformation($"Found {orders.Count} orders to update for customer {customerId}");

        return orders;
    }

    [Function("UpdateOrderCustomer")]
    public async Task UpdateOrderCustomer([ActivityTrigger] Tuple<Order, Customer> updateData)
    {
        _logger.LogInformation($"Updating order {updateData.Item1.Id} customer data");
        var order = updateData.Item1 with { CustomerFirstName = updateData.Item2.FirstName, CustomerLastName = updateData.Item2.LastName };
        await _orderContainer.ReplaceItemAsync(order, order.Id.ToString(), new PartitionKey(order.CustomerId.ToString()));
    }
}
