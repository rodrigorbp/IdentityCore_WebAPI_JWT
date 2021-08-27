using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Identity
{
    public class TwoFactorModel
    {
        [Required]
        public string Token { get; set; }
    }
}
