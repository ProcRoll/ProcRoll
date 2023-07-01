using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcRoll;

public class HostConfig
{
    [Required, MinLength(8), MaxLength(8)]
    public required string ID { get; set; }
}
