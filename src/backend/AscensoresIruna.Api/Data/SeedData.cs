using Microsoft.EntityFrameworkCore;
using AscensoresIruna.Api.Models;

namespace AscensoresIruna.Api.Data;

public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var context = new AppDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

        if (context.Elevators.Any())
            return;

        context.Elevators.AddRange(
            new Elevator
            {
                Name = "Ascensor Plaza del Castillo",
                Location = "Conexión con parking",
                Latitude = 42.81675,
                Longitude = -1.64460
            },
            new Elevator
            {
                Name = "Ascensor Azucarera",
                Location = "Conexión calle Baja/Baja ikastola",
                Latitude = 42.81750,
                Longitude = -1.64580
            },
            new Elevator
            {
                Name = "Ascensor Lindach",
                Location = "Conexión con Rochapea",
                Latitude = 42.82250,
                Longitude = -1.64150
            },
            new Elevator
            {
                Name = "Ascensor Conde Oliveto",
                Location = "Conexión con Ensanche",
                Latitude = 42.81600,
                Longitude = -1.64800
            },
            new Elevator
            {
                Name = "Ascensor Dominicales",
                Location = "Conexión zonamundi",
                Latitude = 42.81850,
                Longitude = -1.64200
            },
            new Elevator
            {
                Name = "Ascensor Labrit",
                Location = "Conexión con barrio",
                Latitude = 42.81820,
                Longitude = -1.63950
            },
            new Elevator
            {
                Name = "Ascensor San Valentín",
                Location = "Conexión con Iturrama",
                Latitude = 42.80350,
                Longitude = -1.65300
            },
            new Elevator
            {
                Name = "Ascensor Yamaguchi",
                Location = "Conexión con San Juan",
                Latitude = 42.79450,
                Longitude = -1.66450
            },
            new Elevator
            {
                Name = "Ascensor Ermitaña",
                Location = "Conexión con Ermitagaña",
                Latitude = 42.79950,
                Longitude = -1.65850
            },
            new Elevator
            {
                Name = "Ascensor Mgica",
                Location = "Conexión con Segundo Ensanche",
                Latitude = 42.80950,
                Longitude = -1.65000
            }
        );

        context.SaveChanges();
    }
}