using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackgroundServiceResponse
{
		public class Response
		
		{
		public int status { get; set; } = 404;
		public bool sucess { get; set; }=false;
		
		public string massage { get; set; } = "";
		public  string? FpImage { get; set; } 	
				
		}
}