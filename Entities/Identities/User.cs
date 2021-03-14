using System;
using Microsoft.AspNetCore.Identity;

namespace identity.Entities.Identities
{
    public class User : IdentityUser<Guid>
    {
        public User()
        {
        }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
