using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Telegraph {
	public static class StageAssets {
		private static Dictionary<string, GameObject> m_BackdropLookup;
		private static Dictionary<string, GameObject> m_CharacterLookup;

		public static Dictionary<string, GameObject> Backdrops {
			get {
				if (m_BackdropLookup == null) CheckInit();
				return m_BackdropLookup;
			}
		}

		public static Dictionary<string, GameObject> Characters {
			get {
				if (m_CharacterLookup == null) CheckInit();
				return m_CharacterLookup;
			}
		}

		private static void CheckInit() {
			m_BackdropLookup = Resources.LoadAll<GameObject>("Backdrops").ToDictionary(go => go.name);
			m_CharacterLookup = Resources.LoadAll<GameObject>("Characters").ToDictionary(go => go.name);
		}
	}
}