using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Table("TrainingVideo")]
public partial class TrainingVideo
{
    [Key]
    public int TrainingVideoID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? VideoName { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? VideoDescription { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? VideoURL { get; set; }

    public int? NeptuneAreaID { get; set; }
}
