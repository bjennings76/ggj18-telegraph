using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Telegraph {
	[CreateAssetMenu(menuName = "Line Lookup Config")]
	public class LineLookup : ScriptableObject {
		[SerializeField] private Binding[] m_Bindings;
		[SerializeField] private Line m_DefaultLine;
		[SerializeField] private ChoiceLine m_DefaultChoiceLine;
		[SerializeField] private HeaderLine m_DefaultHeaderLine;

		private Dictionary<string, Line> m_LineDictionary;

		private Dictionary<string, Line> LineDictionary {
			get {
				return m_LineDictionary ?? (m_LineDictionary = m_Bindings.ToDictionary(b => b.Tag, b => b.Line));
			}
		}

		public Line DefaultLine { get { return m_DefaultLine; } }
		public ChoiceLine DefaultChoice { get { return m_DefaultChoiceLine; } }
		public HeaderLine DefaultHeader { get { return m_DefaultHeaderLine; } }

		public Line this[string key] {
			get {
				if (LineDictionary.ContainsKey(key)) { return LineDictionary[key]; }
				Debug.LogWarning("Couldn't find " + key + " in line directory.", this);
				return null;
			}
		}

		[Serializable]
		private class Binding {
			[UsedImplicitly] public Line Line;
			[UsedImplicitly] public string Tag;
		}

		public bool ContainsKey(string key) { return LineDictionary.ContainsKey(key); }
	}
}