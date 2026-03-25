using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FootballSchool.Models;

[Table("User")]
public partial class User
{
    [Key]
    [Column("User_ID")]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Login { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = null!;

    [StringLength(100)]
    public string? Email { get; set; }

    // Связь 1 к 1 со студентом
    [InverseProperty("User")]
    public virtual Student? Student { get; set; }

    // Связь 1 к 1 с тренером
    [InverseProperty("User")]
    public virtual Coach? Coach { get; set; }
}