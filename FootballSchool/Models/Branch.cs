using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Branch")]
public partial class Branch
{
    [Key]
    [Column("Branch_ID")]
    public int BranchId { get; set; }

    [Column("Name_branch")]
    [StringLength(50)]
    [Unicode(false)]
    public string NameBranch { get; set; } = null!;

    [Column("City_branch")]
    [StringLength(50)]
    [Unicode(false)]
    public string CityBranch { get; set; } = null!;

    [Column("Street_branch")]
    [StringLength(50)]
    [Unicode(false)]
    public string StreetBranch { get; set; } = null!;

    [Column("House_branch")]
    [StringLength(20)]
    [Unicode(false)]
    public string HouseBranch { get; set; } = null!;

    [Column("Phone_branch")]
    [StringLength(20)]
    [Unicode(false)]
    public string PhoneBranch { get; set; } = null!;

    [InverseProperty("Branch")]
    public virtual ICollection<Facility> Facilities { get; set; } = new List<Facility>();
}
