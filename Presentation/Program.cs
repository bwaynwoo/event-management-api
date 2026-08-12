using Application;
using Infrastructure;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Presentation;
using Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddApplication();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapOpenApi();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Open Api V1"));

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapAuthEndpoints();
app.MapEventEndpoints();
app.MapBookingEndpoints();

app.Run();