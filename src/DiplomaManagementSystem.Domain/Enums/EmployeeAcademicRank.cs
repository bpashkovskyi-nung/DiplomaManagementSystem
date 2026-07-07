using System.ComponentModel.DataAnnotations;

namespace DiplomaManagementSystem.Domain.Enums;

public enum EmployeeAcademicRank
{
    [Display(Name = "Асистент")]
    Assistant = 0,

    [Display(Name = "Викладач")]
    Lecturer = 1,

    [Display(Name = "Старший викладач")]
    SeniorLecturer = 2,

    [Display(Name = "Доцент")]
    AssociateProfessor = 3,

    [Display(Name = "Професор")]
    Professor = 4,
}
