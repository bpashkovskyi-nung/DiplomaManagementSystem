using System.Security.Claims;

using DiplomaManagementSystem.Application.Admin.AnnualRoles.Contracts;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Dtos;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Contracts;
using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Dtos;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.ReadModels;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Admin.Controllers;
using DiplomaManagementSystem.Web.Areas.Admin.Models;

using FluentValidation;

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
        var sessionId = Guid.NewGuid();
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
        Assert.Equal(3, model.Commission.Members.Count);
    }

    [Fact]
    public async Task Assign_WhenValidationFails_ReturnsIndexWithForm()
    {
        var sessionId = Guid.NewGuid();
        AnnualRolesPageDto page = new(
            sessionId,
            "2026 — Бакалавр (сем. 1)",
            [new AnnualRoleSlotDto(AnnualRoleType.DepartmentHead, null, null)],
            []);
        AnnualRolesController controller = CreateController(
            new FakeAnnualRoleService(page),
            assignValidator: new AlwaysInvalidAssignValidator());

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
        var sessionId = Guid.NewGuid();
        AnnualRolesPageDto page = new(
            sessionId,
            "2026 — Бакалавр (сем. 1)",
            [new AnnualRoleSlotDto(AnnualRoleType.DepartmentHead, Guid.NewGuid(), "Employee One")],
            []);
        FakeAnnualRoleService annualRoleService = new(page);
        AnnualRolesController controller = CreateController(annualRoleService, assignValidator: new AlwaysValidAssignValidator());

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
        var sessionId = Guid.NewGuid();
        AnnualRolesPageDto page = new(
            sessionId,
            "2026 — Бакалавр (сем. 1)",
            [new AnnualRoleSlotDto(AnnualRoleType.DepartmentHead, null, null)],
            []);
        AnnualRolesController controller = CreateController(
            new ThrowingAnnualRoleService(page, new DomainException("Помилка призначення")),
            assignValidator: new AlwaysValidAssignValidator());

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

    [Fact]
    public async Task SaveCommission_WhenServiceSucceeds_ReturnsIndexWithSuccess()
    {
        var sessionId = Guid.NewGuid();
        AnnualRolesPageDto page = new(
            sessionId,
            "2026 — Бакалавр (сем. 1)",
            [new AnnualRoleSlotDto(AnnualRoleType.DepartmentHead, null, null)],
            []);
        FakeExaminationCommissionService commissionService = new(
            new ExaminationCommissionEditorDto(
                sessionId,
                "2026 — Бакалавр (сем. 1)",
                new ExaminationCommissionDto(null, []),
                []));
        AnnualRolesController controller = CreateController(
            new FakeAnnualRoleService(page),
            commissionService,
            new AlwaysValidAssignValidator(),
            new AlwaysValidCommissionValidator());

        ViewResult result = Assert.IsType<ViewResult>(
            await controller.SaveCommission(
                new ExaminationCommissionFormViewModel
                {
                    DefenceSessionId = sessionId,
                    Chair = new ExaminationCommissionParticipantFormViewModel
                    {
                        IsExternal = true,
                        FullName = "Голова",
                        Position = "проф.",
                    },
                    Members =
                    [
                        new() { IsExternal = true, FullName = "Ч1", Position = "доц." },
                        new() { IsExternal = true, FullName = "Ч2", Position = "доц." },
                        new() { IsExternal = true, FullName = "Ч3", Position = "доц." },
                    ],
                },
                CancellationToken.None));

        Assert.Equal("Index", result.ViewName);
        Assert.Equal("Склад ЕК збережено.", controller.ViewData["Success"]);
        Assert.True(commissionService.SaveCalled);
    }

    private static AnnualRolesController CreateController(
        IAnnualRoleService annualRoleService,
        IExaminationCommissionService? commissionService = null,
        IValidator<AssignAnnualRoleDto>? assignValidator = null,
        IValidator<SaveExaminationCommissionDto>? commissionValidator = null)
    {
        AnnualRolesController controller = new(
            annualRoleService,
            commissionService ?? new FakeExaminationCommissionService(null),
            new FakeDefenceSessionService(),
            assignValidator ?? new AlwaysValidAssignValidator(),
            commissionValidator ?? new AlwaysValidCommissionValidator())
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

    private sealed class FakeExaminationCommissionService(ExaminationCommissionEditorDto? editor)
        : IExaminationCommissionService
    {
        public bool SaveCalled { get; private set; }

        public Task<ExaminationCommissionEditorDto?> GetEditorAsync(
            Guid defenceSessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(editor);

        public Task SaveAsync(SaveExaminationCommissionDto request, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
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

    private sealed class AlwaysValidAssignValidator : AbstractValidator<AssignAnnualRoleDto>;

    private sealed class AlwaysValidCommissionValidator : AbstractValidator<SaveExaminationCommissionDto>;

    private sealed class AlwaysInvalidAssignValidator : AbstractValidator<AssignAnnualRoleDto>
    {
        public AlwaysInvalidAssignValidator()
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
