using System;
using System.Collections.Generic;
using System.Text;

namespace BaseLibrary.DTOs
{
    public record CustomUserClaims
        (string Id = null!, string Name = null!, string Email = null!, string Role = null!);

}