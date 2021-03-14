using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using identity.Data;
using identity.Entities.Identities;
using identity.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace identity
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Inject into DbContext object the AppDbContext subclass using the connection string to the sql server running in docker
            services.AddDbContext<AppDbContext>
                (
                    options => options.UseSqlServer(Configuration.GetConnectionString("Test"))
                );

            // Inject an Identity 
            services.AddIdentity<User, Role>(options =>
            {
                // Use arrow functions to configure Identity 
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
                .AddEntityFrameworkStores<AppDbContext>() // Tell the Identity APIs to use AppDbContext as the source for the Identity information
                .AddDefaultTokenProviders(); // Adds default providers to generate tokens for password reset, 2-factor authentication, change email, and change telephone

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "identity", Version = "v1" });

                // Enable testing jwt tokens for authorizes routes in Swagger
                // In Swagger, when clicking on "Authorize" set the Value to be "Bearer {JWT}" and then any authorize routes should be accessible
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT containing userid claim",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                var security =
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "Bearer",
                                    Type = ReferenceType.SecurityScheme
                                },
                                UnresolvedReference = true
                            },
                            new List<string>()
                        }
                    };

                c.AddSecurityRequirement(security);

            });

            // Inject the AutoMapper which is used to map information from the users to our domain data and vice versa 
            services.AddAutoMapper(typeof(Startup));

            // Inject the JwtSettings as a snapshot. The values will be mapped from the appsettings.json onto the object itself
            services.Configure<JwtSettings>(Configuration.GetSection("Jwt"));

            // Create an Authorization Policy that uses the Jwt Authentication scheme
            services.AddAuthorization()
                .AddAuthentication(config =>
                {
                    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(config => // Include into this authentication scheme the Jwt bearer authentication scheme as well
                {
                    // Get the JwtSettings object that was injected
                    var jwtSettings = Configuration.GetSection("Jwt").Get<JwtSettings>();

                    // Convert Secret Key into a byte array of UTF8 encoding
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));

                    // TokenValidationParameters will be used to check if the incoming JWT is valid
                    config.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Issuer,
                        IssuerSigningKey = key
                    };

                    config.RequireHttpsMetadata = false; // Set the requirement of an Https environment to false for development
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "identity v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
