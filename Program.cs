using BasisBank.Identity.Api.Data;
using BasisBank.Identity.Api.Entities;
using BasisBank.Identity.Api.Filters;
using BasisBank.Identity.Api.Interfaces;
using BasisBank.Identity.Api.Middlewares;
using BasisBank.Identity.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();

var columnOptions = new ColumnOptions();
columnOptions.Store.Clear();
// დამატებითი სვეტების სია
columnOptions.AdditionalColumns = new Collection<SqlColumn>
{
    new SqlColumn { ColumnName = "ApplicationId", DataType = SqlDbType.Int },
    new SqlColumn { ColumnName = "ApplicationName", DataType = SqlDbType.NVarChar, DataLength = 100 },
    new SqlColumn { ColumnName = "ServiceName", DataType = SqlDbType.NVarChar, DataLength = 250 },
    new SqlColumn { ColumnName = "MethodName", DataType = SqlDbType.NVarChar, DataLength = 100 },
    new SqlColumn { ColumnName = "LocalDate", DataType = SqlDbType.Date },
    new SqlColumn { ColumnName = "LocalTime", DataType = SqlDbType.Time },
    new SqlColumn { ColumnName = "IsRequest", DataType = SqlDbType.Bit },
    new SqlColumn { ColumnName = "IsResponse", DataType = SqlDbType.Bit },
    new SqlColumn { ColumnName = "Data", DataType = SqlDbType.NVarChar, DataLength = -1 },
    new SqlColumn { ColumnName = "GroupId", DataType = SqlDbType.UniqueIdentifier }
};

// Use configuration value if present (add "MonitoringConnection" to appsettings), fallback to local
var monitoringConn = builder.Configuration.GetConnectionString("MonitoringConnection")
                     ?? "Server=.;Database=Monitoring_DB;Trusted_Connection=True;TrustServerCertificate=True;";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ApplicationName", "BasisBank.Identity.Api")
    .Enrich.WithProperty("ServiceName", "Identity")
    // DB sink — only events where LogToDb = true
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(le => le.Properties.ContainsKey("LogToDb") && le.Properties["LogToDb"].ToString().Trim('"').Equals("True", StringComparison.OrdinalIgnoreCase))
        .WriteTo.MSSqlServer(
            connectionString: monitoringConn,
            sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions { TableName = "ActivityLogs", AutoCreateSqlTable = true },
            columnOptions: columnOptions))
    .WriteTo.Console()
    .CreateLogger();

SelfLog.Enable(msg => Console.Error.WriteLine($"Serilog SelfLog: {msg}"));

builder.Host.UseSerilog();
// --- 1. Database & Identity Services ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// --- 2. Auth Configuration (JWT & Policies) ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options => {
    options.AddPolicy("MfaRequired", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                (c.Type == "amr" || c.Type == "http://schemas.microsoft.com/claims/authnmethodsreferences")
                && c.Value == "mfa")));
});

// --- 3. Dependency Injection & Identity Options ---
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(5));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// --- 4. Controllers & API Behavior ---
builder.Services.AddControllers(options => {
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options => {
    // ეს არის ის "თარჯიმანი", რომელიც JSON-ის სტრინგს ენუმად აქცევს
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
})
.ConfigureApiBehaviorOptions(options => {
    options.InvalidModelStateResponseFactory = context => {
        return new BadRequestObjectResult(new {
            Result = (object?)null,
            ErrorCode = 400,
            Message = "BadRequest"
        });
    };
});

builder.Services.AddEndpointsApiExplorer();

// --- 5. Swagger Configuration ---
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BasisBank Identity API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "შეიყვანეთ ტოკენი这样: {your_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// 2. StatusCodePages (ჭერს "ცარიელ" პასუხებს: 401, 404, 500)
app.UseStatusCodePages(async context => {
    context.HttpContext.Response.ContentType = "application/json";
    var statusCode = context.HttpContext.Response.StatusCode;

    await context.HttpContext.Response.WriteAsJsonAsync(new {
        Result = (object?)null,
        ErrorCode = statusCode,
        Message = statusCode switch {
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "NotFound",
            500 => "Internal Server Error",
            _ => "Error"
        }
    });
});

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

// pipeline ordering (recommended)
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();

app.UseAuthentication();                         // ensure authentication runs
app.UseMiddleware<RequestResponseLoggingMiddleware>(); // logging after authentication
app.UseAuthorization();

app.MapControllers();

// Seed only roles in Development (no automatic dev-admin creation)
if (app.Environment.IsDevelopment()) {
    using (var scope = app.Services.CreateScope()) {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        string[] roles = new[] { "Admin", "User" };
        foreach (var role in roles) {
            if (!await roleManager.RoleExistsAsync(role)) {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole<int>(role));
                if (!createRoleResult.Succeeded) {
                    logger.LogError("Failed to create role {Role}: {Errors}", role,
                        string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                }
                else {
                    logger.LogInformation("Created role {Role}", role);
                }
            }
            else {
                logger.LogInformation("Role {Role} already exists", role);
            }
        }
    }
}

app.Run();
