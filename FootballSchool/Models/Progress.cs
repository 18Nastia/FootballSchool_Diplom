using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Progress")]
public partial class Progress
{
    [Key]
    [Column("Progress_ID")]
    public int ProgressId { get; set; }

    [Column("Student_ID")]
    public int StudentId { get; set; }

    [Column("Date_progress")]
    public DateOnly DateProgress { get; set; }

    [Column("Tests_progress", TypeName = "text")]
    public string? TestsProgress { get; set; }

    [Column("Physical_progress", TypeName = "text")]
    public string? PhysicalProgress { get; set; }

    [Column("Plan_progress", TypeName = "text")]
    public string? PlanProgress { get; set; }

    [Column("Comment_progress", TypeName = "text")]
    public string? CommentProgress { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Progresses")]
    public virtual Student Student { get; set; } = null!;
}
