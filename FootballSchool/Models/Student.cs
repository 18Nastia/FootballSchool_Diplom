using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Student")]
public partial class Student
{
    [Key]
    [Column("Student_ID")]
    public int StudentId { get; set; }

    [Column("Team_ID")]
    public int? TeamId { get; set; }

    [Column("Surname_student")]
    [StringLength(50)]
    [Unicode(false)]
    public string SurnameStudent { get; set; } = null!;

    [Column("Name_student")]
    [StringLength(50)]
    [Unicode(false)]
    public string NameStudent { get; set; } = null!;

    [Column("Middle_student")]
    [StringLength(50)]
    [Unicode(false)]
    public string? MiddleStudent { get; set; }

    [Column("Birth_student")]
    public DateOnly BirthStudent { get; set; }

    [Column("Gender_student")]
    [StringLength(10)]
    [Unicode(false)]
    public string GenderStudent { get; set; } = null!;

    [Column("Phone_student")]
    [StringLength(20)]
    [Unicode(false)]
    public string? PhoneStudent { get; set; }

    [Column("Email_student")]
    [StringLength(50)]
    [Unicode(false)]
    public string? EmailStudent { get; set; }

    [Column("Medical_student", TypeName = "text")]
    public string? MedicalStudent { get; set; }

    [Column("Level_student")]
    [StringLength(50)]
    [Unicode(false)]
    public string LevelStudent { get; set; } = null!;

    [Column("Photo_student")]
    [StringLength(50)]
    [Unicode(false)]
    public string? PhotoStudent { get; set; }

    [Column("Parent_number")]
    [StringLength(20)]
    [Unicode(false)]
    public string ParentNumber { get; set; } = null!;

    [Column("Surname_parent")]
    [StringLength(50)]
    [Unicode(false)]
    public string SurnameParent { get; set; } = null!;

    [Column("Name_parent")]
    [StringLength(50)]
    [Unicode(false)]
    public string NameParent { get; set; } = null!;

    [Column("Middle_parent")]
    [StringLength(50)]
    [Unicode(false)]
    public string? MiddleParent { get; set; }

    [Column("City_student")]
    [StringLength(50)]
    [Unicode(false)]
    public string CityStudent { get; set; } = null!;

    [Column("Street_student")]
    [StringLength(50)]
    [Unicode(false)]
    public string StreetStudent { get; set; } = null!;

    [Column("House_student")]
    [StringLength(20)]
    [Unicode(false)]
    public string HouseStudent { get; set; } = null!;

    [Column("Apartment_student")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ApartmentStudent { get; set; }

    [InverseProperty("Student")]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [InverseProperty("Student")]
    public virtual ICollection<Progress> Progresses { get; set; } = new List<Progress>();

    [InverseProperty("Student")]
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    [ForeignKey("TeamId")]
    [InverseProperty("Students")]
    public virtual Team? Team { get; set; }
}
