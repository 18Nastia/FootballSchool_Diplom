using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Coach")]
public partial class Coach
{
    [Key]
    [Column("Coach_ID")]
    public int CoachId { get; set; }

    [Column("Surname_coach")]
    [StringLength(50)]
    [Unicode(false)]
    public string SurnameCoach { get; set; } = null!;

    [Column("Name_coach")]
    [StringLength(50)]
    [Unicode(false)]
    public string NameCoach { get; set; } = null!;

    [Column("Middle_coach")]
    [StringLength(50)]
    [Unicode(false)]
    public string? MiddleCoach { get; set; }

    [Column("Qualification_coach")]
    [StringLength(100)]
    [Unicode(false)]
    public string QualificationCoach { get; set; } = null!;

    [Column("Specialty_coach")]
    [StringLength(100)]
    [Unicode(false)]
    public string SpecialtyCoach { get; set; } = null!;

    [Column("Schedule_coach", TypeName = "text")]
    public string? ScheduleCoach { get; set; }

    [Column("Salary_coach", TypeName = "decimal(10, 2)")]
    public decimal? SalaryCoach { get; set; }

    [InverseProperty("Coach")]
    public virtual ICollection<Training> Training { get; set; } = new List<Training>();
}
