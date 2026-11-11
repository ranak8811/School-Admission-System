using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection string configuration
// Reads from appsettings.json or environment variable 'ConnectionStrings__DefaultConnection'
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres";

// 2. Register Entity Framework Core with PostgreSQL (Npgsql)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// 3. Configure Admission & Hold Lifecycle Options
builder.Services.Configure<SchoolAdmission.Api.Configuration.AdmissionOptions>(
    builder.Configuration.GetSection(SchoolAdmission.Api.Configuration.AdmissionOptions.SectionName));

// Support top-level environment variable HOLD_DURATION_SECONDS override
if (int.TryParse(builder.Configuration["HOLD_DURATION_SECONDS"], out var holdDurationEnv))
{
    builder.Services.PostConfigure<SchoolAdmission.Api.Configuration.AdmissionOptions>(options =>
    {
        options.HoldDurationSeconds = holdDurationEnv;
    });
}

// 4. Register Domain Services & Background Sweeper Worker
builder.Services.AddScoped<SchoolAdmission.Api.Services.ISeatAllocationService, SchoolAdmission.Api.Services.SeatAllocationService>();
builder.Services.AddHostedService<SchoolAdmission.Api.Services.Background.ExpiredHoldSweeper>();

// 5. Register Controllers, CORS, ProblemDetails, and API Explorer for Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// 6. Configure Cross-Origin Resource Sharing (CORS)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 7. Configure HTTP Request Pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseExceptionHandler();

app.UseAuthorization();
app.MapControllers();

// Automatically apply database migrations and seed initial data on startup
await DatabaseSeeder.MigrateAndSeedAsync(app.Services, app.Logger);

app.Run();

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
