using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyBackgroundService.Response
{
    public class ResponseModel
    {
        public bool Sucess { get; set; } = false;
        public byte[]? FpImage { get; set; }
    }
}