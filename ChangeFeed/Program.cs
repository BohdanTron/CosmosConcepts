// Link to the official sample: https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/Microsoft.Azure.Cosmos.Samples/Usage/ChangeFeed/Program.cs#L552

using ChangeFeed;
using Microsoft.Azure.Cosmos;

var connectionString = "";
var cosmosClient = new CosmosClient(connectionString);

var databaseName = "MyShop";
var sourceContainerName = "Orders";
var leaseContainerName = "OrdersLeases";

var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);

var sourceContainer = await database.Database.CreateContainerIfNotExistsAsync(sourceContainerName, "/user");
var leaseContainer = await database.Database.CreateContainerIfNotExistsAsync(leaseContainerName, "/id");


var changeFeedProcessor = sourceContainer.Container
    .GetChangeFeedProcessorBuilder<Order>(processorName: "Orders_001", onChangesDelegate: HandleChanges)
    .WithLeaseAcquireNotification(leaseToken =>
    {
        Console.WriteLine($"Lease {leaseToken} is acquired and will start processing");
        return Task.CompletedTask;
    })
    .WithLeaseReleaseNotification(leaseToken =>
    {
        Console.WriteLine($"Lease {leaseToken} is released and processing is stopped");
        return Task.CompletedTask;
    })
    .WithInstanceName(Environment.MachineName)
    .WithLeaseContainer(leaseContainer)
    .WithStartTime(DateTime.UtcNow.AddDays(-70))
    .Build();

var changeFeedEstimator = sourceContainer.Container
    .GetChangeFeedEstimatorBuilder(processorName: "Orders_001", estimationDelegate: HandleEstimation, TimeSpan.FromMilliseconds(30000))
    .WithLeaseContainer(leaseContainer)
    .Build();

await changeFeedProcessor.StartAsync();
await changeFeedEstimator.StartAsync();

static async Task HandleChanges(
    ChangeFeedProcessorContext context,
    IReadOnlyCollection<Order> changes,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"Started handling changes for lease {context.LeaseToken}...");
    Console.WriteLine($"Change Feed request consumed {context.Headers.RequestCharge} RU.");

    // We may want to track any operation's Diagnostics that took longer than some threshold
    if (context.Diagnostics.GetClientElapsedTime() > TimeSpan.FromSeconds(1))
    {
        Console.WriteLine($"Change Feed request took longer than expected. Diagnostics:" + context.Diagnostics);
    }

    foreach (var order in changes)
    {
        Console.WriteLine($"Detected operation for order with id `{order.Id}`, created at {order.Created}.");
        await Task.Delay(10);
    }

    Console.WriteLine("Finished handling changes.");
}

static async Task HandleEstimation(long estimation, CancellationToken cancellationToken)
{
    if (estimation > 0)
    {
        // Consider scaling up processing resources or sending an alert

        Console.WriteLine($"\tEstimator detected {estimation} items pending to be read by the Processor.");
    }

    await Task.Delay(0);
}


while (true)
{
    Console.ReadLine();

    var rand = new Random().Next(1, 500);
    var order = new Order
    {
        Id = Guid.NewGuid(),
        User = $"User {rand}",
        Number = rand.ToString(),
        Created = DateTime.Now
    };

    await sourceContainer.Container.CreateItemAsync(order, new PartitionKey(order.User));
}