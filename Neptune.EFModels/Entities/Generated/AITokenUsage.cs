using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Table("AITokenUsage")]
public partial class AITokenUsage
{
    [Key]
    public int AITokenUsageID { get; set; }

    public int PersonID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string Model { get; set; } = null!;

    public int InputTokens { get; set; }

    public int CachedInputTokens { get; set; }

    public int OutputTokens { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime RequestDate { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? RequestContext { get; set; }

    [ForeignKey("PersonID")]
    [InverseProperty("AITokenUsages")]
    public virtual Person Person { get; set; } = null!;
}
