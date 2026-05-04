using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

public class SystemUser
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    [Required]
    public string FirstName { get; private set; } = null!;  // i know it wont be null

    [Required]
    public string LastName { get; private set; } = null!;
    
    [Required]
    // Email should be unique
    public string Email { get; private set; } = null!;
    
    [Required]
    public string PasswordHash { get; private set; } = null!;
    
    public SystemUser() {}
    
    public SystemUser(
        string firstName, 
        string lastName, 
        string email, 
        string passwordHash)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
    }
}
