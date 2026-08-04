var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5000
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();
