using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Documents.Contracts;
using DiplomaManagementSystem.Application.Secretary.Documents.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Secretary.Documents;

internal sealed class TopicOrderDocumentService(
    IApplicationDbContext dbContext,
    IDefenceSessionQueries defenceSessionQueries,
    IStudyGroupQueries studyGroupQueries,
    IUserDisplayQueries userDisplayQueries,
    ITopicVersionQueries topicVersionQueries,
    IAnnualRoleQueries annualRoleQueries,
    TopicOrderDocxGenerator docxGenerator) : ITopicOrderDocumentService
{
    private const string MissingLabel = "—";

    public async Task<TopicOrderFormDto?> GetFormAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        DefenceSessionSummary? session = await defenceSessionQueries.FindSummaryAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        List<StudyGroupOption> groups = await studyGroupQueries.ListOptionsForSessionAsync(sessionId, cancellationToken);
        string sessionLabel = SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);

        return new TopicOrderFormDto(
            sessionId,
            sessionLabel,
            groups
                .Select(group => new TopicOrderStudyGroupOptionDto(group.Id, group.Name, group.Course))
                .ToList(),
            session.Year);
    }

    public async Task<TopicOrderPreviewDto?> BuildPreviewAsync(
        TopicOrderGenerateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        TopicOrderDocumentDto? document = await BuildDocumentAsync(request, cancellationToken);
        if (document is null)
        {
            return null;
        }

        bool canGenerate = document.Students.Count > 0;

        return new TopicOrderPreviewDto(document, canGenerate);
    }

    public async Task<byte[]?> ExportDocxAsync(
        TopicOrderGenerateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        TopicOrderPreviewDto? preview = await BuildPreviewAsync(request, cancellationToken);
        if (preview is null || !preview.CanGenerate)
        {
            return null;
        }

        return docxGenerator.Generate(preview.Document);
    }

    private async Task<TopicOrderDocumentDto?> BuildDocumentAsync(
        TopicOrderGenerateRequestDto request,
        CancellationToken cancellationToken)
    {
        DefenceSessionSummary? session = await defenceSessionQueries.FindSummaryAsync(request.SessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        List<string> warnings = [];
        HashSet<Guid> selectedGroupIds = request.StudyGroupIds.ToHashSet();

        List<StudyGroup> selectedGroups = await dbContext.StudyGroups
            .AsNoTracking()
            .Where(group => group.DefenceSessionId == request.SessionId && selectedGroupIds.Contains(group.Id))
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        if (selectedGroups.Count != selectedGroupIds.Count)
        {
            throw new DomainException("Обрано недійсну групу для цієї сесії.");
        }

        HashSet<int?> courses = selectedGroups.Select(group => group.Course).ToHashSet();
        if (courses.Count > 1)
        {
            throw new DomainException("Помилка: обрані групи мають різний курс. Згенеруйте окремі накази.");
        }

        HashSet<Guid> specialtyIds = selectedGroups.Select(group => group.SpecialtyId).ToHashSet();
        if (specialtyIds.Count > 1)
        {
            throw new DomainException("Помилка: обрані групи мають різні спеціальності. Згенеруйте окремі накази.");
        }

        HashSet<string> studyForms = selectedGroups.Select(group => group.StudyForm).ToHashSet(StringComparer.Ordinal);
        if (studyForms.Count > 1)
        {
            throw new DomainException("Помилка: обрані групи мають різну форму навчання. Згенеруйте окремі накази.");
        }

        int? course = courses.FirstOrDefault();
        if (course is null)
        {
            warnings.Add("У обраних груп не задано курс — у преамбулі буде пропуск.");
        }

        List<Diploma> diplomas = await ListEligibleDiplomasAsync(
            request.SessionId,
            selectedGroupIds,
            cancellationToken);

        if (diplomas.Count == 0)
        {
            warnings.Add("Немає студентів із затвердженою темою та підтвердженим керівником.");
        }

        HashSet<Guid> userIds = CollectUserIds(diplomas);
        Guid? formattingReviewerId = await annualRoleQueries.GetEmployeeIdForSessionRoleAsync(
            request.SessionId,
            AnnualRoleType.FormattingReviewer,
            cancellationToken);
        Guid? departmentHeadId = await annualRoleQueries.GetEmployeeIdForSessionRoleAsync(
            request.SessionId,
            AnnualRoleType.DepartmentHead,
            cancellationToken);

        if (formattingReviewerId.HasValue)
        {
            userIds.Add(formattingReviewerId.Value);
        }

        if (departmentHeadId.HasValue)
        {
            userIds.Add(departmentHeadId.Value);
        }

        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(userIds, cancellationToken);

        HashSet<Guid> diplomaIds = diplomas.Select(diploma => diploma.Id).ToHashSet();
        Dictionary<Guid, string> topicTitles = await topicVersionQueries.GetApprovedTitlesAsync(diplomaIds, cancellationToken);

        List<TopicOrderStudentRowDto> students = diplomas
            .Select(diploma => MapStudentRow(diploma, users, topicTitles, warnings))
            .OrderBy(row => PersonNameSort.SurnameKey(row.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.StudentFullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        List<TopicOrderReviewerRowDto> reviewers = diplomas
            .Where(diploma => diploma.ReviewerId.HasValue)
            .GroupBy(diploma => diploma.ReviewerId!.Value)
            .Select(group =>
            {
                users.TryGetValue(group.Key, out ApplicationUser? reviewer);
                string line = reviewer is null
                    ? MissingLabel
                    : EmployeeOrderNameFormatter.Format(reviewer);

                if (reviewer is not null && reviewer.AcademicRank is null)
                {
                    warnings.Add($"Рецензент {reviewer.FullName} без вченого звання — у наказі без префікса.");
                }

                return new TopicOrderReviewerRowDto(line, group.Count());
            })
            .OrderBy(row => row.ReviewerLine, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (reviewers.Count == 0)
        {
            warnings.Add("Рецензентів ще не призначено — §2 буде порожнім.");
        }

        string? formattingReviewerLine = null;
        if (formattingReviewerId is Guid formattingId)
        {
            if (users.TryGetValue(formattingId, out ApplicationUser? formattingReviewer))
            {
                formattingReviewerLine = EmployeeOrderNameFormatter.Format(formattingReviewer);
            }
        }
        else
        {
            warnings.Add("Не призначено нормоконтролера для сесії — §3 буде порожнім.");
        }

        string? departmentHeadLine = null;
        if (departmentHeadId is Guid headId)
        {
            if (users.TryGetValue(headId, out ApplicationUser? departmentHead))
            {
                departmentHeadLine = EmployeeOrderNameFormatter.Format(departmentHead);
            }
        }
        else
        {
            warnings.Add("Не призначено завідувача кафедри для сесії — §4 буде порожнім.");
        }

        string coursePhrase = course.HasValue
            ? TopicOrderPhrases.FormatCoursePhrase(course.Value)
            : MissingLabel;

        TopicOrderDepartmentInfoDto departmentInfo =
            await LoadAcademicInfoFromGroupsAsync(selectedGroups, cancellationToken);

        return new TopicOrderDocumentDto(
            request.OrderNumber.Trim(),
            request.Year,
            TopicOrderPhrases.FormatSessionLevelPhrase(session.Type),
            TopicOrderPhrases.FormatGroupsPhrase(selectedGroups.Select(group => group.Name).ToList()),
            coursePhrase,
            departmentInfo,
            students,
            reviewers,
            formattingReviewerLine,
            departmentHeadLine,
            warnings);
    }

    private async Task<TopicOrderDepartmentInfoDto> LoadAcademicInfoFromGroupsAsync(
        IReadOnlyList<StudyGroup> selectedGroups,
        CancellationToken cancellationToken)
    {
        StudyGroup group = selectedGroups[0];

        Specialty? specialty = await dbContext.Specialties
            .AsNoTracking()
            .Include(item => item.Department)
            .ThenInclude(department => department.Faculty)
            .FirstOrDefaultAsync(item => item.Id == group.SpecialtyId, cancellationToken);

        if (specialty is null)
        {
            throw new DomainException("Спеціальність групи не знайдена.");
        }

        return new TopicOrderDepartmentInfoDto(
            specialty.Code,
            specialty.Name,
            specialty.Department.Faculty?.Name ?? string.Empty,
            group.StudyForm,
            specialty.Department.Name);
    }

    private async Task<List<Diploma>> ListEligibleDiplomasAsync(
        Guid sessionId,
        HashSet<Guid> studyGroupIds,
        CancellationToken cancellationToken)
    {
        List<Diploma> diplomas = await dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.TopicVersions)
            .Where(diploma => diploma.DefenceSessionId == sessionId
                              && diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed
                              && diploma.SupervisorId != null)
            .ToListAsync(cancellationToken);

        if (diplomas.Count == 0)
        {
            return [];
        }

        HashSet<Guid> studentIds = diplomas.Select(diploma => diploma.StudentId).ToHashSet();
        Dictionary<Guid, Guid?> studentGroups = await dbContext.Users
            .AsNoTracking()
            .Where(user => studentIds.Contains(user.Id))
            .Select(user => new { user.Id, user.StudyGroupId })
            .ToDictionaryAsync(user => user.Id, user => user.StudyGroupId, cancellationToken);

        return diplomas
            .Where(diploma =>
            {
                if (!studentGroups.TryGetValue(diploma.StudentId, out Guid? groupId)
                    || groupId is null
                    || !studyGroupIds.Contains(groupId.Value))
                {
                    return false;
                }

                return diploma.TopicVersions.Any(version => version.Status == TopicVersionStatus.Approved);
            })
            .ToList();
    }

    private static HashSet<Guid> CollectUserIds(IReadOnlyList<Diploma> diplomas)
    {
        HashSet<Guid> userIds = diplomas.Select(diploma => diploma.StudentId).ToHashSet();
        foreach (Diploma diploma in diplomas)
        {
            if (diploma.SupervisorId.HasValue)
            {
                userIds.Add(diploma.SupervisorId.Value);
            }

            if (diploma.ReviewerId.HasValue)
            {
                userIds.Add(diploma.ReviewerId.Value);
            }
        }

        return userIds;
    }

    private static TopicOrderStudentRowDto MapStudentRow(
        Diploma diploma,
        IReadOnlyDictionary<Guid, ApplicationUser> users,
        IReadOnlyDictionary<Guid, string> topicTitles,
        ICollection<string> warnings)
    {
        users.TryGetValue(diploma.StudentId, out ApplicationUser? student);
        string studentName = student?.FullName ?? MissingLabel;

        string topic = topicTitles.GetValueOrDefault(diploma.Id, MissingLabel);
        string supervisorLine = MissingLabel;
        if (diploma.SupervisorId.HasValue && users.TryGetValue(diploma.SupervisorId.Value, out ApplicationUser? supervisor))
        {
            supervisorLine = EmployeeOrderNameFormatter.Format(supervisor);
            if (supervisor.AcademicRank is null)
            {
                    warnings.Add($"Керівник {supervisor.FullName} без вченого звання — у наказі без префікса.");
            }
        }

        return new TopicOrderStudentRowDto(studentName, topic, supervisorLine);
    }
}
