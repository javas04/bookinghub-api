using BookingHub.Api.Endpoints;
using BookingHub.Api.Infrastructure;
using BookingHub.Application;
using BookingHub.Infrastructure;
using BookingHub.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddApiServices()
    .AddJwtAuthentication()
    .AddAuthRateLimiting()
    .AddApiDocumentation();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.MapScalarApiReference(options => options
    .WithTitle("BookingHub API")
    .AddPreferredSecuritySchemes(ApiDocumentation.BearerScheme));
app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
app.MapHealthChecks("/health");

app.MapAuthEndpoints();
app.MapSpaceEndpoints();
app.MapBookingEndpoints();

await DatabaseInitializer.InitializeAsync(app.Services);
await app.RunAsync();
