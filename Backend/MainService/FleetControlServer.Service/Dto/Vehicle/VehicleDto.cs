namespace FleetControlServer.Service.DTO.Vehicle;

public class VehicleDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string ModelName { get; set; } = null!;
    
    public string LicensePlateNumber { get; set; }  = null!;
    
    public string IdentificationNumber { get; set; }  = null!;
    
    public Guid? VehicleDriverId { get; set; }
    
    
    public VehicleDto() {}

    public VehicleDto(
        string modelName,
        string licensePlateNumber)
    {
        ModelName = modelName;
        LicensePlateNumber = licensePlateNumber;
    }
}
