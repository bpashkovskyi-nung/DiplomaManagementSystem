namespace DiplomaManagementSystem.Application;

internal static class WorkflowGuidanceMessages
{
    public const string NoEmployeesAdmin = "Немає викладачів у системі. Додайте викладачів в адмін-панелі.";

    public const string NoEmployeesStudent = "Немає викладачів у системі. Зверніться до адміністратора.";

    public const string ReviewerAlreadyAssigned = "Рецензента вже призначено.";

    public const string ReviewAlreadyCompleted = "Рецензію вже завершено.";

    public const string SessionArchivedActions = "Сесія захисту заархівована — дії недоступні.";

    public const string SessionArchivedUpload = "Сесія захисту заархівована — завантаження недоступне.";

    public const string SessionArchivedShort = "Сесія заархівована — зміни недоступні.";

    public const string SessionArchivedAdmit = "Сесія заархівована — допуск недоступний.";

    public const string SessionArchivedComments = "Сесія заархівована — додавання коментарів недоступне.";

    public const string SessionArchivedTopic = "Сесія захисту заархівована — подання теми недоступне.";

    public const string ChecksAlreadyStarted = "Перевірки вже розпочато або тема ще не затверджена.";

    public const string UploadWorkFirst = "Спочатку завантажте файл роботи (PDF, DOCX або ODT).";

    public const string UploadAfterTopicApproved = "Завантаження доступне після затвердження теми.";

    public const string UploadAfterReviewerAssigned = "Завантаження доступне після призначення рецензента.";

    public const string UploadAfterAdmitted = "Роботу вже допущено до захисту — нові версії недоступні.";

    public const string DeclareWorkReadyAwaitReviewer = "Спочатку секретар ДЕК має призначити рецензента.";

    public const string UploadWrongLifecycle = "Завантаження недоступне на поточному етапі.";

    public const string SupervisorChangeAfterTopic = "Після затвердження теми змінити керівника не можна.";

    public const string SupervisorNotConfirmed = "керівник не підтверджений";

    public const string TopicNotApproved = "тема не затверджена";

    public const string ChecksNotStarted = "перевірки ще не розпочато";

    public const string ReviewNotCompleted = "рецензія не завершена";

    public const string AdmitConditionsUpdating = "Умови допуску ще оновлюються — оновіть сторінку.";

    public const string AdmitNotReadyPrefix = "Ще не готово до допуску: ";

    public const string AdmitStepIncompletePrefix = "не виконано: ";

    public const string OverrideBeforeWorkReady = "Перевірки з'являться після повідомлення студента про готовність документів.";

    public const string OverrideWrongStepType = "Примусова зміна доступна лише для кроків перевірки, що очікують рішення.";

    public const string OverrideStepCompleted = "Поточний крок допуску вже пройдено — примусова зміна недоступна.";

    public const string OverrideReviewerNotAssigned = "Рецензента ще не призначено — примусова зміна рецензії недоступна.";

    public const string OverrideStepNotWaiting = "Примусова зміна доступна лише для поточного кроку, що очікує перевірки.";

    public const string CommentAfterAdmitted = "Роботу вже допущено до захисту — нові коментарі не додаються.";

    public const string SupervisorPending = "Запит надіслано керівнику — очікуйте підтвердження.";

    public const string SupervisorConfirmed = "Керівник уже підтверджений.";

    public const string TopicAwaitSupervisor = "Спочатку керівник має підтвердити ваш запит.";

    public const string TopicSelectSupervisor = "Спочатку оберіть і підтвердіть керівника.";

    public const string TopicAlreadyApproved = "Тему вже затверджено — нові версії не подаються.";

    public const string TopicPendingSupervisor = "Поточна тема на розгляді керівника.";

    public const string TopicPendingHead = "Поточна тема на розгляді завідувача кафедри.";

    public const string TopicRejectedResubmit = "Останню тему відхилено — подайте нову версію.";

    public const string StudentNoTopic = "Студент ще не подав тему роботи.";

    public const string TopicAwaitSupervisorLong =
        "Тема очікує схвалення керівника — після цього завідувач кафедри має її затвердити.";

    public const string TopicAwaitHeadLong =
        "Тема очікує схвалення завідувача кафедри (роль DepartmentHead на сесію).";

    public const string TopicRejectedStudent = "Остання тема відхилена — студент має подати нову версію.";

    public const string TopicApprovalRequired = "Потрібна затверджена тема роботи.";
}
