using Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Models;
using ReliefApi;
using ReliefApi.Contracts;
using ReliefApi.Services;
using Services;
using System;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var appBuilder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();


builder.Services.Configure<WhatsAppApiSettings>(builder.Configuration.GetSection("WhatsAppApiSettings"));

builder.Services.AddAuthentication().AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, cfg =>
{
    cfg.RequireHttpsMetadata = false;
    cfg.SaveToken = true;
    cfg.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidIssuer = "me",
        ValidAudience = "you",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SupersecretKey@9846760609")) //Secret
    };
});



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});
builder.Services.AddControllers();
builder.Services.AddSingleton<DapperContext>(new DapperContext(builder.Configuration.GetConnectionString("Relief")));
builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("Relief")));
builder.Services.AddScoped<IBranches, Branches>();
builder.Services.AddScoped<IFAreport, FAreports>();
builder.Services.AddScoped<IACmaster, Acmasters>();
builder.Services.AddScoped<IDbcodes, Dbcodes>();
builder.Services.AddScoped<ISale, Sales>();
builder.Services.AddScoped<IStockReport, Stockreports>();


//add cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(options => options.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
    //options.AddPolicy("AllowOrigin",
    //    builder =>
    //    {
    //        //builder.WithOrigins("http://localhost:4200",
    //        //                    "http://localhost:4200")
    //        //        .WithMethods("PUT", "DELETE", "GET");
    //        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    //    });
});

builder.Services.Configure<UpiDetails>(builder.Configuration.GetSection("UpiDetails"));

//new code

//old code
var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{    
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

//app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();









