using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

public partial class Training
{
    [Key]
    [Column("Training_ID")]
    public int TrainingId { get; set; }

    [Column("Facility_ID")]
    public int FacilityId { get; set; }

    [Column("Team_ID")]
    public int TeamId { get; set; }

    [Column("Coach_ID")]
    public int CoachId { get; set; }

    [Column("Date_training")]
    public DateOnly DateTraining { get; set; }

    [Column("Time_training")]
    public TimeOnly TimeTraining { get; set; }

    [Column("Plan_training", TypeName = "text")]
    public string? PlanTraining { get; set; }

    [InverseProperty("Training")]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [ForeignKey("CoachId")]
    [InverseProperty("Training")]
    public virtual Coach Coach { get; set; } = null!;

    [ForeignKey("FacilityId")]
    [InverseProperty("Training")]
    public virtual Facility Facility { get; set; } = null!;

    [ForeignKey("TeamId")]
    [InverseProperty("Training")]
    public virtual Team Team { get; set; } = null!;
}
