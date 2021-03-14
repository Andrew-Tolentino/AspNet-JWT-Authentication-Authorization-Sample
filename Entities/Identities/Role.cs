using System;
using Microsoft.AspNetCore.Identity;

namespace identity.Entities.Identities
{
    public class Role : IdentityRole<Guid>
    {
        public Role()
        {
        }
    }
}
