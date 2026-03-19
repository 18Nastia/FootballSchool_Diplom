using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // <-- Подключаем ValidateNever
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Team")]
public partial class Team
{
    [Key]
    [Column("Team_ID")]
    public int TeamId { get; set; }

    // Внешний ключ для связи с филиалом
    [Column("Branch_ID")]
    [Required(ErrorMessage = "Необходимо выбрать филиал")]
    public int BranchId { get; set; }

    [Column("Category_team")]
    [StringLength(50)]
    [Unicode(false)]
    [Required(ErrorMessage = "Название группы обязательно")]
    public string CategoryTeam { get; set; } = null!;

    [Column("Status_team")]
    [StringLength(50)]
    [Unicode(false)]
    public string StatusTeam { get; set; } = null!;

    // Навигационное свойство филиала
    [ForeignKey("BranchId")]
    [InverseProperty("Teams")]
    [ValidateNever] // Игнорируем при валидации формы
    public virtual Branch Branch { get; set; } = null!;

    [InverseProperty("Team")]
    [ValidateNever] // Игнорируем при валидации формы
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    [InverseProperty("Team")]
    [ValidateNever] // Игнорируем при валидации формы
    public virtual ICollection<Training> Training { get; set; } = new List<Training>();
}