using AnalyticOpinion.API.Security;
using AnalyticOpinion.Data;

var builder = WebApplication.CreateBuilder(args);

// Toda acción de la API exige el header X-Api-Key.
builder.Services.AddControllers(options => options.Filters.Add<ApiKeyAuthorizationFilter>());
builder.Services.AddOpenApi();

builder.Services.AddSourceRepositories(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
