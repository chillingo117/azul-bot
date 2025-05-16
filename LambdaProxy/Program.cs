using System.Text.Json;
using System.Text.Json.Serialization;
using AzulLambda;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// Single endpoint to invoke the Lambda function
app.MapPost("/lambda/mcts", async (HttpContext context) =>
{
    try
    {
        // Read the request body
        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();

        // Deserialize the input into a GameState object
        var gameState = JsonSerializer.Deserialize<GameState>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        });

        if (gameState == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid game state");
            return;
        }

        // Call the Lambda function
        var lambdaResult = AzulMCTSFunction.Handle(requestBody);

        // Return the result
        context.Response.ContentType = "application/json";
        Console.WriteLine($"Lambda result: {lambdaResult}");
        await context.Response.WriteAsync(lambdaResult);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync($"Error executing Lambda: {ex.Message}");
    }
});

// heartbeat so the frontend can check if the server is available
app.MapGet("/heartbeat", () => "Ok");

app.MapGet("/", () => "Lambda Proxy Server is running. Send POST requests to /lambda to invoke the Lambda function.");

app.Run("http://localhost:3001");