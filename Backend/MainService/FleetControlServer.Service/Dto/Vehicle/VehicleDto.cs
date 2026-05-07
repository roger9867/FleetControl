namespace FleetControlServer.Service.DTO.Vehicle;

public class VehicleDto
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    public string ModelName { get; private set; } = null!;
    
    public string LicensePlateNumber { get; private set; }  = null!;
    
    public VehicleDto() {}

    public VehicleDto(
        string modelName,
        string licensePlateNumber)
    {
        ModelName = modelName;
        LicensePlateNumber = licensePlateNumber;
    }
}
