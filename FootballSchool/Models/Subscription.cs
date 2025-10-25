using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Subscription")]
public partial class Subscription
{
    [Key]
    [Column("Subscription_ID")]
    public int SubscriptionId { get; set; }

    [Column("Student_ID")]
    public int StudentId { get; set; }

    [Column("Type_subscription")]
    [StringLength(50)]
    [Unicode(false)]
    public string TypeSubscription { get; set; } = null!;

    [Column("Terms_subscription")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TermsSubscription { get; set; }

    [Column("Days_subscription")]
    public int? DaysSubscription { get; set; }

    [Column("Cost_subscription", TypeName = "decimal(10, 2)")]
    public decimal? CostSubscription { get; set; }

    [InverseProperty("Subscription")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("StudentId")]
    [InverseProperty("Subscriptions")]
    public virtual Student Student { get; set; } = null!;
}
