using Vasl.WebAPI;
using Vasl.WebAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureApplication(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.AddReadEndpoints();
app.AddWriteEndpoints();

app.Run();