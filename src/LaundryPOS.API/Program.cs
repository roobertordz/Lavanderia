using System.Text;
using LaundryPOS.API.Hubs;
using LaundryPOS.API.Middleware;
using LaundryPOS.Application;
using LaundryPOS.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ───
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/laundrypos-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Services ───
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ─── Authentication ───
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };

    // Allow SignalR to receive token from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Administrator"));
    options.AddPolicy("SupervisorOrAbove", p => p.RequireRole("Administrator", "Supervisor"));
    options.AddPolicy("EmployeeOrAbove", p => p.RequireRole("Administrator", "Supervisor", "Employee"));
    options.AddPolicy("TechnicianAccess", p => p.RequireRole("Administrator", "Supervisor", "Technician"));
    // Cashier: only allowed to charge (payments) and sell products at the counter.
    options.AddPolicy("CashierOrAbove", p => p.RequireRole("Administrator", "Supervisor", "Employee", "Cashier"));
    // Cashier is also allowed to fully manage the product catalog (create/edit/delete + import/export).
    options.AddPolicy("ProductManagementAccess", p => p.RequireRole("Administrator", "Supervisor", "Cashier"));
});

// ─── Controllers ───
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ─── Swagger ───
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LaundryPOS API",
        Version = "v1",
        Description = "API for Self-Service Laundry Management System",
        Contact = new OpenApiContact { Name = "LaundryPOS Team" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─── SignalR ───
builder.Services.AddSignalR();
builder.Services.AddScoped<LaundryPOS.Domain.Interfaces.Services.IRealTimeNotificationService, LaundryPOS.API.Hubs.SignalRNotificationService>();

// ─── CORS ───
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ─── Database Migration & Seed ───
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LaundryPOS.Infrastructure.Persistence.LaundryDbContext>();
    await context.Database.MigrateAsync();
    await LaundryPOS.Infrastructure.Persistence.DbSeeder.SeedAsync(scope.ServiceProvider);
}

// ─── Middleware Pipeline ───
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LaundryPOS API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MachineHub>("/hubs/machines");
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();
