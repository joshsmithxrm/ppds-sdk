# PPDS.Dataverse

High-performance Dataverse connectivity with connection pooling, throttle-aware routing, and bulk operations.

## Installation

```bash
dotnet add package PPDS.Dataverse
```

## Quick Start

```csharp
// 1. Register services
services.AddDataverseConnectionPool(options =>
{
    options.Connections.Add(new DataverseConnection(
        "Primary",
        "AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=xxx;ClientSecret=xxx"));
});

// 2. Inject and use
public class AccountService
{
    private readonly IDataverseConnectionPool _pool;

    public AccountService(IDataverseConnectionPool pool) => _pool = pool;

    public async Task<Entity> GetAccountAsync(Guid id)
    {
        await using var client = await _pool.GetClientAsync();
        return await client.RetrieveAsync("account", id, new ColumnSet(true));
    }
}
```

## Features

### Connection Pooling

Reuse connections efficiently with automatic lifecycle management:

```csharp
options.Pool.MaxPoolSize = 50;        // Total connections
options.Pool.MinPoolSize = 5;         // Keep warm
options.Pool.MaxIdleTime = TimeSpan.FromMinutes(5);
options.Pool.MaxLifetime = TimeSpan.FromMinutes(30);
```

### Multi-Connection Load Distribution

Distribute load across multiple Application Users to multiply your API quota:

```csharp
options.Connections = new List<DataverseConnection>
{
    new("AppUser1", connectionString1),
    new("AppUser2", connectionString2),
    new("AppUser3", connectionString3),  // 3x the quota!
};
options.Pool.SelectionStrategy = ConnectionSelectionStrategy.ThrottleAware;
```

### Throttle-Aware Routing

Automatically routes requests away from throttled connections:

```csharp
options.Pool.SelectionStrategy = ConnectionSelectionStrategy.ThrottleAware;
options.Resilience.EnableThrottleTracking = true;
```

### Bulk Operations

High-throughput data operations using modern Dataverse APIs:

```csharp
var executor = serviceProvider.GetRequiredService<IBulkOperationExecutor>();

var result = await executor.UpsertMultipleAsync("account", entities,
    new BulkOperationOptions
    {
        BatchSize = 1000,
        ContinueOnError = true,
        BypassCustomPluginExecution = true
    });

Console.WriteLine($"Success: {result.SuccessCount}, Failed: {result.FailureCount}");
```

### Affinity Cookie Disabled by Default

The SDK's affinity cookie routes all requests to a single backend node. Disabling it provides 10x+ throughput improvement:

```csharp
options.Pool.DisableAffinityCookie = true; // Default
```

## Configuration

### Via Code

```csharp
services.AddDataverseConnectionPool(options =>
{
    options.Connections.Add(new DataverseConnection("Primary", connectionString));
    options.Pool.MaxPoolSize = 50;
    options.Pool.DisableAffinityCookie = true;
    options.Pool.SelectionStrategy = ConnectionSelectionStrategy.ThrottleAware;
});
```

### Via appsettings.json

```json
{
  "Dataverse": {
    "Connections": [
      {
        "Name": "Primary",
        "ConnectionString": "AuthType=ClientSecret;..."
      }
    ],
    "Pool": {
      "MaxPoolSize": 50,
      "DisableAffinityCookie": true,
      "SelectionStrategy": "ThrottleAware"
    }
  }
}
```

```csharp
services.AddDataverseConnectionPool(configuration);
```

## Impersonation

Execute operations on behalf of another user:

```csharp
var options = new DataverseClientOptions { CallerId = userId };
await using var client = await pool.GetClientAsync(options);
await client.CreateAsync(entity);  // Created as userId
```

## Pool Statistics

Monitor pool health:

```csharp
var stats = pool.Statistics;
Console.WriteLine($"Active: {stats.ActiveConnections}");
Console.WriteLine($"Idle: {stats.IdleConnections}");
Console.WriteLine($"Throttled: {stats.ThrottledConnections}");
Console.WriteLine($"Requests: {stats.RequestsServed}");
```

## Security

### Connection String Handling

Connection strings contain sensitive credentials. This library provides built-in protection:

**Automatic Redaction:** Connection strings are automatically redacted in logs and error messages:

```csharp
using PPDS.Dataverse.Security;

// Redacts ClientSecret, Password, and other sensitive values
var safe = ConnectionStringRedactor.Redact(connectionString);
// "AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=xxx;ClientSecret=***REDACTED***"
```

**Exception Safety:** Connection errors throw `DataverseConnectionException` with sanitized messages:

```csharp
try
{
    await using var client = await pool.GetClientAsync();
}
catch (DataverseConnectionException ex)
{
    // ex.Message is safe to log - credentials are redacted
    logger.LogError(ex, "Connection failed for {Connection}", ex.ConnectionName);
}
```

**Safe ToString:** `DataverseConnection.ToString()` excludes credentials:

```csharp
var connection = new DataverseConnection("Primary", connectionString);
Console.WriteLine(connection); // "DataverseConnection { Name = Primary, MaxPoolSize = 10 }"
```

### Best Practices

1. **Use Environment Variables** instead of hardcoding connection strings:

   ```csharp
   var connectionString = Environment.GetEnvironmentVariable("DATAVERSE_CONNECTION");
   ```

2. **Use Azure Key Vault** for production deployments:

   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri("https://your-vault.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

3. **Use Managed Identity** when running in Azure:

   ```
   AuthType=OAuth;Url=https://org.crm.dynamics.com;
   AppId=your-client-id;RedirectUri=http://localhost;
   TokenCacheStorePath=token.cache;LoginPrompt=Never
   ```

4. **Never log connection strings directly:**

   ```csharp
   // DON'T
   logger.LogInformation("Connecting with: {ConnectionString}", connectionString);

   // DO
   logger.LogInformation("Connecting to: {Name}", connection.Name);
   // Or if you need the URL:
   logger.LogInformation("Connecting with: {Redacted}", connection.GetRedactedConnectionString());
   ```

### Sensitive Data Attribute

Properties containing sensitive data are marked with `[SensitiveData]` for documentation and static analysis:

```csharp
public class DataverseConnection
{
    public string Name { get; set; }

    [SensitiveData(Reason = "Contains authentication credentials", DataType = "ConnectionString")]
    public string ConnectionString { get; set; }
}
```

## Target Frameworks

- `net8.0`
- `net10.0`

## License

MIT License
