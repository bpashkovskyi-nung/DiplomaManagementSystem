using System.Security.Claims;

using DiplomaManagementSystem.Application.Admin.AnnualRoles.Contracts;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Dtos;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.ReadModels;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Admin.Controllers;
using DiplomaManagementSystem.Web.Areas.Admin.Models;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DiplomaManagementSystem.Web.Tests.Admin;

public sealed class AnnualRolesControllerTests
{
    [Fact]
    public async Task Index_WhenSessionMissing_ReturnsNotFound()
    {
        AnnualRolesController controller = CreateController(new FakeAnnualRoleService(null));

        IActionResult result = await controller.Index(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_WhenSessionExists_ReturnsViewWithModel()
    {
        Guid sessionId = Guid.NewGuid();
        AnnualRolesPageDto page = new(
            sessionId,
            "2026 — Бакалавр (сем. 1)",
            [
                new AnnualRoleSlotDto(AnnualRoleType.DepartmentHead, null, null),
                new AnnualRoleSlotDto(AnnualRoleType.ExamCommissionSecretary, null, null),
            ],
            [new PersonOptionDto(Guid.NewGuid(), "Employee One", "employee@test.local")]);
        AnnualRolesController controller = CreateController(new FakeAnnualRoleService(page));

        ViewResult result = Assert.IsType<ViewResult>(await controller.Index(sessionId, CancellationToken.None));
        AnnualRolesViewModel model = Assert.IsType<AnnualRolesViewModel>(result.Model);

        Assert.Equal(sessionId, model.DefenceSessionId);
        Assert.Equal(2, model.Roles.Count);
        Assert.Single(model.Employees);
    }

    [Fact]
    public async Task Assign_WhenValidationFails_ReturnsIndexWithForm()
    {
        Guid sessionId = Guid.NewGuid();
        AnnualRolesPageDto page = new(
            sessionId,
            "2026 — Бакалавр (сем. 1)",
            [new AnnualRoleSlotDto(AnnualRoleType.DepartmentHead, null, null)],
            []);
        AnnualRolesController controller = CreateController(
            new FakeAnnualRoleService(page),
            new AlwaysInvalidValidator());

        ViewResult result = Assert.IsType<ViewResult>(
            await controller.Assign(
                new AssignAnnualRoleFormViewModel
                {
                    DefenceSessionId = sessionId,
                    RoleType = AnnualRoleType.DepartmentHead,
                    EmployeeId = Guid.NewGuid(),
                },
                CancellationToken.None));

        Assert.Equal("Index", result.ViewName);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Assign_WhenServiceSucceeds_ReturnsIndexWithSuccess()
    {
        Guid sessionId = Guid.NewGuid();
        AnnualRolesPageDto page = new(
            sessionId,
            "2026 — Бакалавр (сем. 1)",
            [new AnnualRoleSlotDto(AnnualRoleType.DepartmentHead, Guid.NewGuid(), "Employee One")],
            []);
        FakeAnnualRoleService annualRoleService = new(page);
        AnnualRolesController controller = CreateController(annualRoleService, new AlwaysValidValidator());

        ViewResult result = Assert.IsType<ViewResult>(
            await controller.Assign(
                new AssignAnnualRoleFormViewModel
                {
                    DefenceSessionId = sessionId,
                    RoleType = AnnualRoleType.DepartmentHead,
                    EmployeeId = Guid.NewGuid(),
                },
                CancellationToken.None));

        Assert.Equal("Index", result.ViewName);
        Assert.Equal("Роль призначено.", controller.ViewData["Success"]);
        Assert.True(annualRoleService.AssignCalled);
    }

    [Fact]
    public async Task Assign_WhenDomainException_ReturnsIndexWithError()
    {
        Guid sessionId = Guid.NewGuid();
        AnnualRolesPageDto page = new(
            sessionId,
            "2026 — Бакалавр (сем. 1)",
            [new AnnualRoleSlotDto(AnnualRoleType.DepartmentHead, null, null)],
            []);
        AnnualRolesController controller = CreateController(
            new ThrowingAnnualRoleService(page, new DomainException("Помилка призначення")),
            new AlwaysValidValidator());

        ViewResult result = Assert.IsType<ViewResult>(
            await controller.Assign(
                new AssignAnnualRoleFormViewModel
                {
                    DefenceSessionId = sessionId,
                    RoleType = AnnualRoleType.DepartmentHead,
                    EmployeeId = Guid.NewGuid(),
                },
                CancellationToken.None));

        Assert.Equal("Index", result.ViewName);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors, error => error.ErrorMessage == "Помилка призначення");
    }

    private static AnnualRolesController CreateController(
        IAnnualRoleService annualRoleService,
        IValidator<AssignAnnualRoleDto>? validator = null)
    {
        AnnualRolesController controller = new(
            annualRoleService,
            new FakeDefenceSessionService(),
            validator ?? new AlwaysValidValidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, RoleNames.Admin),
                    ],
                    authenticationType: "test")),
                },
            },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider()),
        };

        return controller;
    }

    private sealed class FakeAnnualRoleService(AnnualRolesPageDto? page) : IAnnualRoleService
    {
        public bool AssignCalled { get; private set; }

        public Task<AnnualRolesPageDto?> GetPageAsync(Guid defenceSessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(page);

        public Task AssignAsync(AssignAnnualRoleDto dto, CancellationToken cancellationToken = default)
        {
            AssignCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingAnnualRoleService(AnnualRolesPageDto page, DomainException exception) : IAnnualRoleService
    {
        public Task<AnnualRolesPageDto?> GetPageAsync(Guid defenceSessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualRolesPageDto?>(page);

        public Task AssignAsync(AssignAnnualRoleDto dto, CancellationToken cancellationToken = default) =>
            throw exception;
    }

    private sealed class FakeDefenceSessionService : IDefenceSessionService
    {
        public Task<IReadOnlyList<DefenceSessionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DefenceSessionListItemDto>>([]);

        public Task<DefenceSessionDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<DefenceSessionDetailsDto?>(null);

        public Task<DefenceSessionFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<DefenceSessionFormDto?>(null);

        public Task<Guid> CreateAsync(DefenceSessionFormDto form, CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.NewGuid());

        public Task UpdateAsync(Guid id, DefenceSessionFormDto form, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ArchiveAsync(Guid id, Guid performedById, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AlwaysValidValidator : AbstractValidator<AssignAnnualRoleDto>;

    private sealed class AlwaysInvalidValidator : AbstractValidator<AssignAnnualRoleDto>
    {
        public AlwaysInvalidValidator()
        {
            RuleFor(dto => dto.EmployeeId)
                .Must(_ => false)
                .WithMessage("Потрібно обрати викладача.");
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object?> _data = [];

        public IDictionary<string, object?> LoadTempData(HttpContext context) => _data;

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) =>
            _data = new Dictionary<string, object?>(values);
    }
}
