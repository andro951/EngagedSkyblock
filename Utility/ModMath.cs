using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngagedSkyblock.Utility {
	public static class ModMath {
		public static int CeilingDivide(this int num, int denom) => (num - 1) / denom + 1;
	}
}
