using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Telegraph {
	public class ChoiceLine : Line {
		[SerializeField] private Text m_Letter;
		[SerializeField] private Text m_MorseCode;

		public UnityEvent OnClick;

		public string Letter {
			get {
				return m_Letter.text;
			}
		}

		public void SetText(string text, List<char> usedChars) {
			string rawText = SetText(text);
			string morseText = MorseCodePlayer.CleanText(rawText);
			char letter = morseText.FirstOrDefault(c => c != ' ' && !usedChars.Contains(c));
			usedChars.Add(letter);

			m_Letter.text = letter.ToString();

			if (!MorseCodePlayer.CharToCode.ContainsKey(letter)) {
				Debug.LogError("Couldn't find '" + letter + "' in " + m_Letter.text);
				return;
			}

			m_MorseCode.text = MorseCodePlayer.CharToCode[letter];
			TelegraphInput.OnLetter += OnLetter;
		}

		private void OnLetter(char letter) {
			if (letter.ToString() == m_Letter.text) {
				OnClick.Invoke();
			}
		}

		private void OnDestroy() {
			TelegraphInput.OnLetter -= OnLetter;
		}
	}
}