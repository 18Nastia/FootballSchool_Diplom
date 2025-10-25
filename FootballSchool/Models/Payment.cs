using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Models;

[Table("Payment")]
public partial class Payment
{
    [Key]
    [Column("Payment_ID")]
    public int PaymentId { get; set; }

    [Column("Subscription_ID")]
    public int SubscriptionId { get; set; }

    [Column("Amount_payment", TypeName = "decimal(10, 2)")]
    public decimal AmountPayment { get; set; }

    [Column("Date_payment")]
    public DateOnly DatePayment { get; set; }

    [Column("Method_payment")]
    [StringLength(50)]
    [Unicode(false)]
    public string MethodPayment { get; set; } = null!;

    [Column("Status_payment")]
    [StringLength(50)]
    [Unicode(false)]
    public string? StatusPayment { get; set; }

    [ForeignKey("SubscriptionId")]
    [InverseProperty("Payments")]
    public virtual Subscription Subscription { get; set; } = null!;
}
