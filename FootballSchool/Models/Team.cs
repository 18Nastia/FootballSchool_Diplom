using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Team")]
public partial class Team
{
    [Key]
    [Column("Team_ID")]
    public int TeamId { get; set; }

    [Column("Category_team")]
    [StringLength(50)]
    [Unicode(false)]
    public string CategoryTeam { get; set; } = null!;

    [Column("Status_team")]
    [StringLength(50)]
    [Unicode(false)]
    public string StatusTeam { get; set; } = null!;

    [InverseProperty("Team")]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    [InverseProperty("Team")]
    public virtual ICollection<Training> Training { get; set; } = new List<Training>();
}
