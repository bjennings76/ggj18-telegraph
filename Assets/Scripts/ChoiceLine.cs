using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dialog {
	public class ChoiceLine : Line {
		[SerializeField] private Text m_Letter;
		[SerializeField] private Text m_MorseCode;

		public UnityEvent OnClick;

		[UsedImplicitly]
		public void OnPress() { OnClick.Invoke(); }

		public void SetText(string text, List<char> usedChars) {
			string rawText = SetText(text);
			string morseText = MorseCodePlayer.CleanText(rawText);
			char firstUnused = morseText.FirstOrDefault(c => !usedChars.Contains(c));
			usedChars.Add(firstUnused);
			AssignLetter(firstUnused);
		}

		public void AssignLetter(char letter) {
			m_Letter.text = letter.ToString();
			m_MorseCode.text = MorseCodePlayer.CharToCode[letter];
		}
	}
}