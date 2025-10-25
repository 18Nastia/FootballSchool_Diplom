using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Attendance")]
public partial class Attendance
{
    [Key]
    [Column("Attendance_ID")]
    public int AttendanceId { get; set; }

    [Column("Training_ID")]
    public int TrainingId { get; set; }

    [Column("Student_ID")]
    public int StudentId { get; set; }

    [Column("Status_attendance")]
    [StringLength(50)]
    [Unicode(false)]
    public string StatusAttendance { get; set; } = null!;

    [Column("Note_attendance", TypeName = "text")]
    public string? NoteAttendance { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Attendances")]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey("TrainingId")]
    [InverseProperty("Attendances")]
    public virtual Training Training { get; set; } = null!;
}
