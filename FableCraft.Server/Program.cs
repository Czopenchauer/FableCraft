var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

// Configure GraphRAG client
builder.Services.AddHttpClient<FableCraft.Server.Clients.GraphRagClient>(client =>
{
    var graphRagBaseUrl = builder.Configuration.GetValue<string>("GraphRag:BaseUrl") ?? "http://127.0.0.1:8111";
    client.BaseAddress = new Uri(graphRagBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(10); // Long timeout for index operations
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
