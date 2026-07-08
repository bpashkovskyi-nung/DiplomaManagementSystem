namespace DiplomaManagementSystem.Application;

public static class DepartmentMessages
{
    public const string AccessDenied = "Немає доступу до обраної кафедри.";

    public const string ContextRequired = "Оберіть кафедру для продовження.";

    public const string SessionNotInDepartment = "Сесія захисту не належить до обраної кафедри.";

    public const string FacultyNotFound = "Факультет не знайдено.";

    public const string DepartmentNotFound = "Кафедру не знайдено.";

    public const string DuplicateFacultyName = "Факультет з такою назвою вже існує.";

    public const string DuplicateDepartmentName = "Кафедра з такою назвою вже існує у цьому факультеті.";
}
