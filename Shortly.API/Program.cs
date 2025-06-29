using FluentValidation;
using FluentValidation.AspNetCore;
using Shortly.Core;
using Shortly.Core.DTOs;
using Shortly.Core.Mappers;
using Shortly.Core.Validators;
using Shortly.Infrastructure;

namespace Shortly.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Infrastructure Services
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddCore();
        
        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddAutoMapper(typeof(ShortUrlMappingProfile).Assembly);
        
        // FluentValidations
        builder.Services.AddFluentValidationAutoValidation();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();

        app.Run();
    }
}