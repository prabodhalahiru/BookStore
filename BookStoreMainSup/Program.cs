using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BookStoreMainSup.Data;
using BookStoreMainSup.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
));

// Register the TokenRevocationService
builder.Services.AddSingleton<ITokenRevocationService, TokenRevocationService>();

// Register the AuthService
builder.Services.AddScoped<AuthService>();

// Configure JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var tokenRevocationService = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();
            var token = context.SecurityToken as JwtSecurityToken;
            if (token != null && tokenRevocationService.IsTokenRevoked(token.RawData))
            {
                context.Fail("This token has been revoked.");
            }

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            string errorMessage = "User unauthorized";
            if (context.AuthenticateFailure != null)
            {
                if (context.AuthenticateFailure.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "text/plain";
                    errorMessage = "Token is expired";
                }
                else if (context.AuthenticateFailure.Message == "This token has been revoked.")
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "text/plain";
                    errorMessage = "Token has been revoked";
                }
                else
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "text/plain";
                }
            }

            return context.Response.WriteAsync(errorMessage);
        }
    };
});

builder.Services.AddAuthorization();

// Register controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();