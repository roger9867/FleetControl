using Moq;
using FleetControlApi.Persistence.Repos;
using Microsoft.EntityFrameworkCore;
using FleetControlApi.Domain;
using System.Runtime.CompilerServices;

namespace FleetControlApi.Persistence.Tests;



public class PersonRepositoryTests
{
    private readonly AppDbContext _context;

    private readonly Mock<DbSet<VehicleDriver>> _mockDrivers;

    private readonly VehicleDriverRepository _repo;


    public PersonRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
              _mockDrivers = new Mock<DbSet<VehicleDriver>>();

        
        _context = new AppDbContext(options);
        

        _repo = new VehicleDriverRepository(_context);
    }

        public void Dispose()
    {
        _context.Dispose();
    }
    

    [Fact]
    public async Task PersistingPerson_ShouldReturnSamePerson()
    {
        // Arrange
        VehicleDriver driver = new()
        {
            FirstName = "Max",
            LastName = "Mustermann"
        };

        // Act
        var result = await _repo.UpsertAsync(driver);

        // Assert

        var savedDriver = await _context.VehicleDrivers
            .FindAsync(driver.Id);

        Assert.True(result.Success);
        Assert.Null(result.Error);

        Assert.NotNull(savedDriver);
        Assert.Equal("Max", savedDriver.FirstName);
        Assert.Equal("Mustermann", savedDriver.LastName);
    }



}
