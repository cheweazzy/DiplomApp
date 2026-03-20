using DiplomApp.Data;
using DiplomApp.Models;
using DiplomApp.Services;
using DiplomApp.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomApp.Tests;

public class ReservationServiceTests
{
    [Fact]
    public async Task CreateAsync_Throws_When_LessThan30MinutesAhead()
    {
        await using var resources = await TestResources.CreateAsync();
        var service = resources.Provider.GetRequiredService<IReservationService>();
        await resources.SeedDoctorAsync(MedicalSpecialty.Kardiolog);

        var slot = TimeSlotHelperTestsHelpers.GetValidSlotWithinNextThirtyMinutes();

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            PhoneNumber = "123456789",
            ReservationDateTime = slot,
            MedicalSpecialty = MedicalSpecialty.Kardiolog,
            UserId = "user-1"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(reservation));
    }

    [Fact]
    public async Task DeleteAsync_Respects12HourLimit_ForCustomer()
    {
        await using var resources = await TestResources.CreateAsync();
        var service = resources.Provider.GetRequiredService<IReservationService>();
        await resources.SeedDoctorAsync(MedicalSpecialty.Dermatolog);
        var customer = await resources.SeedUserWithRoleAsync("customer@test.com", "Customer");

        var slot = TimeSlotHelperTestsHelpers.GetValidFutureSlot(TimeSpan.FromHours(6));
        var reservation = await resources.SeedReservationAsync(slot, MedicalSpecialty.Dermatolog, customer.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(reservation.Id, null));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Employee")]
    public async Task DeleteAsync_AllowsAdminAndEmployee_ToBypass12HourLimit(string role)
    {
        await using var resources = await TestResources.CreateAsync();
        var service = resources.Provider.GetRequiredService<IReservationService>();
        await resources.SeedDoctorAsync(MedicalSpecialty.Neurolog);
        var privilegedUser = await resources.SeedUserWithRoleAsync($"{role.ToLower()}@test.com", role);

        var slot = TimeSlotHelperTestsHelpers.GetValidFutureSlot(TimeSpan.FromHours(6));
        var reservation = await resources.SeedReservationAsync(slot, MedicalSpecialty.Neurolog, privilegedUser.Id);

        var result = await service.DeleteAsync(reservation.Id, privilegedUser.Id);
        Assert.True(result);
    }

    [Fact]
    public async Task CreateAsync_Throws_OnDoubleBookingSameSlotAndSpecialty()
    {
        await using var resources = await TestResources.CreateAsync();
        var service = resources.Provider.GetRequiredService<IReservationService>();
        await resources.SeedDoctorAsync(MedicalSpecialty.Kardiolog);

        var slot = TimeSlotHelperTestsHelpers.GetValidFutureSlot(TimeSpan.FromHours(2));

        var reservation1 = new Reservation
        {
            Id = Guid.NewGuid(),
            Name = "User One",
            Email = "one@example.com",
            PhoneNumber = "123456789",
            ReservationDateTime = slot,
            MedicalSpecialty = MedicalSpecialty.Kardiolog,
            UserId = "user-1"
        };

        var reservation2 = new Reservation
        {
            Id = Guid.NewGuid(),
            Name = "User Two",
            Email = "two@example.com",
            PhoneNumber = "987654321",
            ReservationDateTime = slot,
            MedicalSpecialty = MedicalSpecialty.Kardiolog,
            UserId = "user-2"
        };

        await service.CreateAsync(reservation1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(reservation2));
    }
}

internal sealed class TestResources : IAsyncDisposable
{
    public ServiceProvider Provider { get; }
    private SqliteConnection Connection { get; }

    private TestResources(ServiceProvider provider, SqliteConnection connection)
    {
        Provider = provider;
        Connection = connection;
    }

    public static async Task<TestResources> CreateAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddDataProtection();
        services.AddIdentityCore<User>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IEmailService, FakeEmailService>();
        services.AddScoped<IDoctorAvailabilityService, FakeDoctorAvailabilityService>();
        services.AddScoped<IReservationService, DatabaseReservationService>();

        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Employee", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        return new TestResources(provider, connection);
    }

    public async Task<User> SeedUserWithRoleAsync(string email, string role)
    {
        using var scope = Provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = new User
        {
            UserName = email,
            Email = email
        };

        var result = await userManager.CreateAsync(user, "Passw0rd!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    public async Task<User> SeedDoctorAsync(MedicalSpecialty specialty)
    {
        using var scope = Provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var doctor = new User
        {
            UserName = $"{specialty.ToString().ToLower()}@doctor.com",
            Email = $"{specialty.ToString().ToLower()}@doctor.com",
            MedicalSpecialty = specialty
        };

        var result = await userManager.CreateAsync(doctor, "Passw0rd!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(doctor, "Employee");
        return doctor;
    }

    public async Task<Reservation> SeedReservationAsync(DateTime slot, MedicalSpecialty specialty, string userId)
    {
        using var scope = Provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            PhoneNumber = "123456789",
            ReservationDateTime = slot,
            MedicalSpecialty = specialty,
            UserId = userId
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();
        return reservation;
    }

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();
        await Connection.DisposeAsync();
    }
}

internal sealed class FakeDoctorAvailabilityService : IDoctorAvailabilityService
{
    public Task<bool> DeleteAvailabilityAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<DoctorAvailability?> GetAvailabilityAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default)
        => Task.FromResult<DoctorAvailability?>(null);

    public Task<List<DoctorAvailability>> GetAvailabilitiesAsync(string doctorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<DoctorAvailability>());

    public Task<bool> IsSlotAvailableAsync(string doctorId, DateTime slot, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<DoctorAvailability> SaveAvailabilityAsync(string doctorId, DateTime date, List<int> availableSlots, CancellationToken cancellationToken = default)
        => Task.FromResult(new DoctorAvailability { Id = Guid.NewGuid(), DoctorId = doctorId, Date = date, AvailableSlots = string.Join(",", availableSlots) });

    public Task<List<DateTime>> GetAvailableSlotsForDateAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<DateTime>());
}

internal sealed class FakeEmailService : IEmailService
{
    public Task SendReservationConfirmationAsync(Reservation reservation, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal static class TimeSlotHelperTestsHelpers
{
    public static DateTime GetValidSlotWithinNextThirtyMinutes()
    {
        var now = DateTime.Now;
        // Bierzemy slot 10 minut do przodu i wyrównujemy W DÓŁ do 30-minutowego kroku
        var candidate = now.AddMinutes(10);
        candidate = AlignToValidSlotDown(candidate);
        // Jeśli nadal jest dalej niż 30 minut w przód, cofamy się o 30 minut
        if (candidate > now.AddMinutes(30))
        {
            candidate = candidate.AddMinutes(-30);
        }
        // I jeszcze raz upewniamy się, że to poprawny slot wg logiki aplikacji
        while (!TimeSlotHelper.IsValid30MinuteTimeSlot(candidate))
        {
            candidate = candidate.AddMinutes(-30);
        }
        return candidate;
    }
    public static DateTime GetValidFutureSlot(TimeSpan offset)
    {
        var candidate = DateTime.Now.Add(offset);
        candidate = AlignToValidSlotDown(candidate);
        while (!TimeSlotHelper.IsValid30MinuteTimeSlot(candidate))
        {
            candidate = candidate.AddMinutes(30);
        }
        return candidate;
    }
    private static DateTime AlignToValidSlotDown(DateTime value)
    {
        var minute = value.Minute >= 30 ? 30 : 0;
        // NIE idziemy w przyszłość godzinami — tylko ustawiamy na 00 lub 30 tej samej godziny
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, minute, 0);
    }
}