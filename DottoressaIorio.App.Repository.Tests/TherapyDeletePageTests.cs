using Bunit;
using DottoressaIorio.App.Data;
using DottoressaIorio.App.Pages.TherapyTemplates;
using DottoressaIorio.App.Services.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DottoressaIorio.App.Repository.Tests;

public class TherapyDeletePageTests : TestContext, IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PatientTherapyRepository _repository;

    public TherapyDeletePageTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _repository = new PatientTherapyRepository(_context);
        _context.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task DeleteButton_ShouldDeleteTherapyAndNavigateToPatientTherapiesPage()
    {
        // Arrange
        var therapyId = 1;
        var patientId = 1;
        var navigationManagerMock = new Mock<NavigationManager>();
        var expectedNavigationUri = $"/patient/{patientId}/therapies";

        // Arrange
        var ctx = new TestContext();

        ctx.Services.AddSingleton(_repository);
        ctx.Services.AddSingleton(navigationManagerMock.Object);


        var cut = RenderComponent<Delete>(parameters => parameters
            .Add(p => p.Id, therapyId));

        // Act
        await cut.Find("button.btn-danger").ClickAsync(new MouseEventArgs());

        // Assert
        navigationManagerMock.Verify(nav => nav.NavigateTo(expectedNavigationUri, It.IsAny<bool>()), Times.Once);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _context.DisposeAsync();
        Dispose();
    }
}