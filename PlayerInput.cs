using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace EngagedSkyblock {
	public static class PlayerInput {
		private static Keys[] pressedKeys = null;
		public static void Update() {
            if (Debugger.IsAttached)
				pressedKeys = Main.keyState.GetPressedKeys();
		}

		public static bool Clicked(this Keys key) => pressedKeys?.Contains(key) == true;
	}
}
