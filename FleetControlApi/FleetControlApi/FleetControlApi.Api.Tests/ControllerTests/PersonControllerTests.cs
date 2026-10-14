

using FleetControlApi.Api.Controllers;
using FleetControlApi.Domain;
using FleetControlApi.Service;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit.Sdk;

namespace FleetControlApi.Api.Tests;


public class PersonControllerTests
{

    private readonly PersonController _controller;

    private readonly Mock<IPersonService> _service = new();


    public PersonControllerTests()
    {
        _controller = new PersonController(_service.Object);
    }



    [Fact]
    async Task GetWithValidID_ShouldReturnOk()
    {
        // Given / Arrange
        VehicleDriver driver = new VehicleDriver()
        {
            FirstName = "Max",
            LastName = "Mustermann"
        };

        _service.Setup(s => s.GetByIdAsync(driver.Id)).ReturnsAsync((true, driver, null));

        // When / Act
        var result = await _controller.GetById(driver.Id);

        // Then / Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(driver, ok.Value);
    }

    [Fact]
    async Task GetWithInvalidId_ShouldReturnBadRequest()
    {

        // Act
        VehicleDriver driver = new VehicleDriver()
        {
            FirstName = "Max",
            LastName = "Mustermann"
        };

        _service.Setup(s => s.GetByIdAsync(driver.Id)).ReturnsAsync((false, driver, null));


        // Act
        var result = await _controller.GetById(driver.Id);


        Assert.IsType<NotFoundObjectResult>(result);
    }
}
