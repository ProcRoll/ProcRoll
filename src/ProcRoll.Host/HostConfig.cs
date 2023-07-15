using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcRoll;

/// <summary>
/// 
/// </summary>
public class HostConfig
{
    /// <summary>
    /// 
    /// </summary>
    [Required, MinLength(8), MaxLength(8)]
    public required string ID { get; set; }
}
