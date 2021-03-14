using System;
using AutoMapper;
using identity.Entities.Identities;
using identity.Resources;

namespace identity.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Map the UserSignUpResource to our User Domain object
            // We are assigning the User.UserName to be the UserSignUpResource.Email in the code below 
            CreateMap<UserSignUpResource, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName));
        }
    }
}
