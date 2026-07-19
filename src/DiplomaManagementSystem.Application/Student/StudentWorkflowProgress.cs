using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Student;

public enum WorkflowAudience
{
    Student,
    Secretary,
}

public enum StudentWorkflowStepState
{
    Completed,
    Current,
    Upcoming,
}

public sealed record WorkflowPersonLabels(
    string? SupervisorName = null,
    string? ReviewerName = null,
    TopicApprovalDisplay? TopicApproval = null);

public sealed record StudentWorkflowStepStatusDto(
    string BadgeText,
    string BadgeCssClass,
    string? CompletedByName,
    DateTimeOffset? CompletedAt,
    CheckpointOutcome? Outcome,
    string? Comment,
    bool IsSecretaryOverride);

public sealed record StudentWorkflowStepDto(
    int Order,
    string Title,
    StudentWorkflowStepState State,
    string? Detail,
    string? NextActionHint,
    StudentWorkflowStepStatusDto? Status = null);

public sealed record StudentWorkflowProgressDto(
    IReadOnlyList<StudentWorkflowStepDto> Steps,
    int CompletedCount,
    int TotalCount,
    int ProgressPercent,
    string? CurrentStepHint);

public static class StudentWorkflowProgressBuilder
{
    private const int StepCount = 8;

    public static StudentWorkflowProgressDto Build(
        Diploma diploma,
        bool sessionActive,
        WorkflowAudience audience = WorkflowAudience.Student,
        IReadOnlyDictionary<Guid, string>? completedByNames = null,
        WorkflowPersonLabels? people = null)
    {
        ArgumentNullException.ThrowIfNull(diploma);

        DiplomaTopicVersion? latestTopic = diploma.TopicVersions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        bool supervisorCompleted = diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed;
        bool topicCompleted = diploma.TopicVersions.Any(version => version.Status == TopicVersionStatus.Approved);
        bool workPhaseCompleted = diploma.AdmissionStepAttempts.Count > 0
                                  || diploma.CurrentAdmissionStep is not null;
        bool supervisorFeedbackCompleted = IsStepPassing(
            diploma.AdmissionStepAttempts,
            AdmissionStep.SupervisorFeedback);
        bool formattingCompleted = IsStepPassing(
            diploma.AdmissionStepAttempts,
            AdmissionStep.FormattingReview);
        bool antiPlagiarismCompleted = IsStepPassing(
            diploma.AdmissionStepAttempts,
            AdmissionStep.AntiPlagiarismClearance);
        bool reviewCompleted = IsReviewStepCompleted(diploma);
        bool admissionCompleted = diploma.AdmissionStatus == DiplomaAdmissionStatus.Admitted;

        bool[] stepCompleted =
        [
            supervisorCompleted,
            topicCompleted,
            workPhaseCompleted,
            supervisorFeedbackCompleted,
            formattingCompleted,
            antiPlagiarismCompleted,
            reviewCompleted,
            admissionCompleted,
        ];

        string[] titles =
        [
            "Вибір керівника",
            "Тема на розгляді",
            "Робота виконується",
            "Відгук керівника",
            "Нормоконтроль",
            "Антиплагіат",
            "Рецензія",
            "Допуск до захисту",
        ];

        bool allCompleted = stepCompleted.All(completed => completed);
        int currentIndex = allCompleted ? -1 : Array.FindIndex(stepCompleted, completed => !completed);
        int completedCount = stepCompleted.Count(completed => completed);
        int progressPercent = allCompleted ? 100 : completedCount * 100 / StepCount;

        List<StudentWorkflowStepDto> steps = [];
        string? currentHint = null;

        for (int index = 0; index < StepCount; index++)
        {
            StudentWorkflowStepState state = ResolveState(index, currentIndex, allCompleted);
            string? detail = BuildStepDetail(index, stepCompleted[index], diploma, latestTopic, people);
            string? hint = state == StudentWorkflowStepState.Current
                ? BuildNextActionHint(index, diploma, latestTopic, sessionActive, audience)
                : null;

            if (hint is not null)
            {
                currentHint = hint;
            }

            StudentWorkflowStepStatusDto? status = BuildCheckpointStatus(index, diploma, completedByNames);

            steps.Add(new StudentWorkflowStepDto(
                index + 1,
                titles[index],
                state,
                detail,
                hint,
                status));
        }

        if (allCompleted)
        {
            currentHint = diploma.DefenceDate.HasValue
                ? audience == WorkflowAudience.Secretary
                    ? $"Студента допущено до захисту. Дата захисту: {diploma.DefenceDate.Value:d}."
                    : $"Ви допущені до захисту. Дата захисту: {diploma.DefenceDate.Value:d}."
                : audience == WorkflowAudience.Secretary
                    ? "Студента допущено до захисту."
                    : "Ви допущені до захисту.";
        }

        return new StudentWorkflowProgressDto(
            steps,
            completedCount,
            StepCount,
            progressPercent,
            currentHint);
    }

    private static StudentWorkflowStepState ResolveState(int index, int currentIndex, bool allCompleted)
    {
        if (allCompleted)
        {
            return StudentWorkflowStepState.Completed;
        }

        if (index < currentIndex)
        {
            return StudentWorkflowStepState.Completed;
        }

        if (index == currentIndex)
        {
            return StudentWorkflowStepState.Current;
        }

        return StudentWorkflowStepState.Upcoming;
    }

    private static string? BuildStepDetail(
        int stepIndex,
        bool isCompleted,
        Diploma diploma,
        DiplomaTopicVersion? latestTopic,
        WorkflowPersonLabels? people)
    {
        return stepIndex switch
        {
            0 => BuildSupervisorDetail(diploma, people?.SupervisorName),
            1 => BuildTopicReviewDetail(latestTopic, isCompleted, people?.TopicApproval),
            6 => BuildReviewerDetail(diploma, people?.ReviewerName),
            7 when diploma.DefenceDate.HasValue => diploma.DefenceDate.Value.ToString("d"),
            7 when isCompleted => "Допущено",
            _ when isCompleted => "Виконано",
            _ => null,
        };
    }

    private static string? BuildTopicReviewDetail(
        DiplomaTopicVersion? latestTopic,
        bool isCompleted,
        TopicApprovalDisplay? topicApproval)
    {
        if (latestTopic is null)
        {
            return null;
        }

        List<string> lines = [latestTopic.Title];

        if (isCompleted && topicApproval is not null)
        {
            if (!string.IsNullOrWhiteSpace(topicApproval.SupervisorLine))
            {
                lines.Add(topicApproval.SupervisorLine);
            }

            if (!string.IsNullOrWhiteSpace(topicApproval.HeadLine))
            {
                lines.Add(topicApproval.HeadLine);
            }
        }

        return string.Join('\n', lines);
    }

    private static string? BuildSupervisorDetail(Diploma diploma, string? supervisorName)
    {
        if (string.IsNullOrWhiteSpace(supervisorName))
        {
            return diploma.SupervisorAssignmentStatus switch
            {
                SupervisorAssignmentStatus.Confirmed => "Підтверджено",
                _ => null,
            };
        }

        string status = diploma.SupervisorAssignmentStatus switch
        {
            SupervisorAssignmentStatus.Confirmed => "Підтверджено",
            SupervisorAssignmentStatus.Rejected => "Відхилено",
            _ when SupervisorAssignmentRules.HasPendingRequest(
                diploma.SupervisorAssignmentStatus,
                diploma.SupervisorId) => "Очікує підтвердження",
            _ => "Не обрано",
        };

        return $"{supervisorName} ({status})";
    }

    private static string? BuildReviewerDetail(Diploma diploma, string? reviewerName)
    {
        if (!string.IsNullOrWhiteSpace(reviewerName))
        {
            return reviewerName;
        }

        return diploma.ReviewAssignmentStatus switch
        {
            ReviewAssignmentStatus.NotAssigned => "Не призначено",
            ReviewAssignmentStatus.Assigned => "Очікує відгук",
            ReviewAssignmentStatus.Completed => "Завершено",
            _ => null,
        };
    }

    private static string? BuildNextActionHint(
        int stepIndex,
        Diploma diploma,
        DiplomaTopicVersion? latestTopic,
        bool sessionActive,
        WorkflowAudience audience)
    {
        string? hint = audience == WorkflowAudience.Secretary
            ? BuildSecretaryNextActionHint(stepIndex, diploma, latestTopic, sessionActive)
            : BuildStudentNextActionHint(stepIndex, diploma, latestTopic, sessionActive);

        if (!sessionActive && hint is not null && stepIndex is 0 or 1 or 2)
        {
            return hint + " Сесія заархівована — нові дії недоступні.";
        }

        return hint;
    }

    private static string? BuildStudentNextActionHint(
        int stepIndex,
        Diploma diploma,
        DiplomaTopicVersion? latestTopic,
        bool sessionActive)
    {
        return stepIndex switch
        {
            0 => SupervisorAssignmentRules.HasPendingRequest(
                diploma.SupervisorAssignmentStatus,
                diploma.SupervisorId)
                ? "Очікуйте підтвердження від обраного керівника."
                : diploma.SupervisorAssignmentStatus switch
                {
                    SupervisorAssignmentStatus.Rejected =>
                        "Попередній запит відхилено — оберіть іншого керівника нижче.",
                    _ => "Оберіть керівника зі списку нижче.",
                },
            1 => latestTopic?.Status switch
            {
                null => "Подайте тему роботи (форма нижче).",
                TopicVersionStatus.PendingSupervisor => "Керівник розглядає подану тему.",
                TopicVersionStatus.PendingHead => "Завідувач кафедри має затвердити тему.",
                TopicVersionStatus.Rejected => "Останню тему відхилено — подайте нову версію.",
                _ => "Подайте тему роботи (форма нижче).",
            },
            2 => diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.NotAssigned
                ? "Очікуйте призначення рецензента секретарем ДЕК."
                : "Виконуйте роботу. Коли будете готові до перевірок — натисніть кнопку нижче.",
            3 => "Керівник має зафіксувати готовність роботи.",
            4 => "Нормоконтролер перевіряє оформлення роботи.",
            5 => "Відповідальний за антиплагіат проводить перевірку.",
            6 => diploma.ReviewAssignmentStatus switch
            {
                ReviewAssignmentStatus.Assigned => "Рецензент готує відгук.",
                ReviewAssignmentStatus.Completed => null,
                _ => "Секретар ДЕК призначить рецензента після затвердження теми.",
            },
            7 => diploma.LifecycleStatus == DiplomaLifecycleStatus.ReadyForAdmission
                ? "Усі умови виконано — секретар ДЕК оформить допуск до захисту."
                : "Після виконання всіх перевірок секретар ДЕК допустить вас до захисту.",
            _ => null,
        };
    }

    private static string? BuildSecretaryNextActionHint(
        int stepIndex,
        Diploma diploma,
        DiplomaTopicVersion? latestTopic,
        bool sessionActive)
    {
        return stepIndex switch
        {
            0 => SupervisorAssignmentRules.HasPendingRequest(
                diploma.SupervisorAssignmentStatus,
                diploma.SupervisorId)
                ? "Очікується підтвердження керівником запиту студента."
                : diploma.SupervisorAssignmentStatus switch
                {
                    SupervisorAssignmentStatus.Rejected =>
                        "Студент має обрати іншого керівника.",
                    SupervisorAssignmentStatus.Confirmed => null,
                    _ => "Студент має обрати керівника.",
                },
            1 => latestTopic?.Status switch
            {
                null => "Студент має подати тему роботи.",
                TopicVersionStatus.PendingSupervisor => "Керівник розглядає тему студента.",
                TopicVersionStatus.PendingHead => "Завідувач кафедри має затвердити тему.",
                TopicVersionStatus.Rejected => "Студент має подати нову версію теми.",
                _ => "Студент має подати тему роботи.",
            },
            2 => diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.NotAssigned
                ? sessionActive
                    ? "Призначте рецензента (форма нижче)."
                    : "Призначте рецензента після затвердження теми."
                : "Студент виконує роботу. Очікується повідомлення про готовність до перевірок.",
            3 => "Керівник має зафіксувати готовність роботи.",
            4 => "Нормоконтролер перевіряє оформлення роботи.",
            5 => "Відповідальний за антиплагіат проводить перевірку.",
            6 => diploma.ReviewAssignmentStatus switch
            {
                ReviewAssignmentStatus.Assigned => "Рецензент готує відгук.",
                ReviewAssignmentStatus.Completed => null,
                _ when sessionActive => "Призначте рецензента після затвердження теми.",
                _ => "Призначте рецензента після затвердження теми.",
            },
            7 => diploma.LifecycleStatus == DiplomaLifecycleStatus.ReadyForAdmission
                ? "Оформіть допуск до захисту (форма нижче)."
                : "Після виконання всіх перевірок оформіть допуск студента.",
            _ => null,
        };
    }

    private static StudentWorkflowStepStatusDto? BuildCheckpointStatus(
        int stepIndex,
        Diploma diploma,
        IReadOnlyDictionary<Guid, string>? completedByNames)
    {
        if (diploma.AdmissionStepAttempts.Count == 0 && diploma.CurrentAdmissionStep is null)
        {
            return null;
        }

        AdmissionStep? step = stepIndex switch
        {
            3 => AdmissionStep.SupervisorFeedback,
            4 => AdmissionStep.FormattingReview,
            5 => AdmissionStep.AntiPlagiarismClearance,
            6 => AdmissionStep.ExternalReview,
            _ => null,
        };

        if (step is null)
        {
            return null;
        }

        var attempts = diploma.AdmissionStepAttempts.ToList();
        DiplomaAdmissionStepAttempt? lastPassing = AdmissionStepStatusResolver.GetLastPassingAttempt(step.Value, attempts);
        DiplomaAdmissionStepAttempt? lastAttempt = AdmissionStepStatusResolver.GetLastAttempt(step.Value, attempts);

        bool isPassing = lastPassing is not null;
        bool isRejected = lastAttempt is not null
                          && !CheckpointOutcomeRules.IsPassing(lastAttempt.Outcome);
        bool isLocked = !AdmissionStepSequence.ArePriorOutcomeStepsPassing(step.Value, attempts)
                        && diploma.CurrentAdmissionStep != step;
        bool isCurrent = diploma.CurrentAdmissionStep == step
                         || (step == AdmissionStep.ExternalReview
                             && diploma.CurrentAdmissionStep == AdmissionStep.ReviewerAssignment
                             && diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Assigned);

        string badgeText;
        string badgeCssClass;
        if (isPassing)
        {
            badgeText = "Виконано";
            badgeCssClass = "bg-success";
        }
        else if (isRejected)
        {
            badgeText = "Відхилено";
            badgeCssClass = "bg-danger";
        }
        else if (isLocked)
        {
            badgeText = "Очікує попередніх";
            badgeCssClass = "bg-secondary";
        }
        else if (isCurrent)
        {
            badgeText = "Поточний етап";
            badgeCssClass = "bg-warning text-dark";
        }
        else
        {
            badgeText = "Очікує";
            badgeCssClass = "bg-secondary";
        }

        string? completedByName = null;
        DiplomaAdmissionStepAttempt? statusAttempt = lastPassing ?? lastAttempt;
        if (statusAttempt?.RecordedById is { } recordedById)
        {
            completedByNames?.TryGetValue(recordedById, out completedByName);
        }

        return new StudentWorkflowStepStatusDto(
            badgeText,
            badgeCssClass,
            completedByName,
            statusAttempt?.RecordedAt,
            statusAttempt?.Outcome,
            statusAttempt?.Comment,
            statusAttempt?.IsSecretaryOverride ?? false);
    }

    private static bool IsStepPassing(
        IEnumerable<DiplomaAdmissionStepAttempt> attempts,
        AdmissionStep step) =>
        AdmissionStepStatusResolver.HasPassingAttempt(step, attempts);

    private static bool IsReviewStepCompleted(Diploma diploma)
    {
        if (diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Completed)
        {
            return IsStepPassing(diploma.AdmissionStepAttempts, AdmissionStep.ExternalReview);
        }

        return IsStepPassing(diploma.AdmissionStepAttempts, AdmissionStep.ExternalReview);
    }
}
