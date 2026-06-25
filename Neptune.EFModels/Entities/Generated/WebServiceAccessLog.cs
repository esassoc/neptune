using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Table("WebServiceAccessLog")]
[Index("PersonID", "RequestedDate", Name = "IX_WebServiceAccessLog_PersonID_RequestedDate", IsDescending = new[] { false, true })]
[Index("RequestedDate", Name = "IX_WebServiceAccessLog_RequestedDate", AllDescending = true)]
public partial class WebServiceAccessLog
{
    [Key]
    public int WebServiceAccessLogID { get; set; }

    public int PersonID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string Endpoint { get; set; } = null!;

    [StringLength(10)]
    [Unicode(false)]
    public string HttpMethod { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime RequestedDate { get; set; }

    public int ResponseStatusCode { get; set; }

    [ForeignKey("PersonID")]
    [InverseProperty("WebServiceAccessLogs")]
    public virtual Person Person { get; set; } = null!;
}
