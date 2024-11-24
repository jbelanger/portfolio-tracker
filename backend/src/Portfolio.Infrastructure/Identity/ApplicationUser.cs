using Microsoft.AspNetCore.Identity;

namespace Portfolio.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string GoogleId { get; set; }
}