using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Facility")]
public partial class Facility
{
    [Key]
    [Column("Facility_ID")]
    public int FacilityId { get; set; }

    [Column("Branch_ID")]
    public int BranchId { get; set; }

    [Column("Name_facility")]
    [StringLength(50)]
    [Unicode(false)]
    public string NameFacility { get; set; } = null!;

    [Column("Type_facility")]
    [StringLength(50)]
    [Unicode(false)]
    public string TypeFacility { get; set; } = null!;

    [Column("Capacity_facility")]
    public int CapacityFacility { get; set; }

    [Column("Status_facility")]
    [StringLength(50)]
    [Unicode(false)]
    public string StatusFacility { get; set; } = null!;

    [Column("Cost_facility", TypeName = "decimal(10, 2)")]
    public decimal? CostFacility { get; set; }

    [Column("Number_facility")]
    [StringLength(20)]
    [Unicode(false)]
    public string? NumberFacility { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("Facilities")]
    public virtual Branch Branch { get; set; } = null!;

    [InverseProperty("Facility")]
    public virtual ICollection<Training> Training { get; set; } = new List<Training>();
}
