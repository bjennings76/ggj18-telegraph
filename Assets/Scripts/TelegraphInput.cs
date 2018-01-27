using UnityEngine;
using UnityEngine.Collections;
using Unspeakable.Utils;

namespace Dialog {
	public class TelegraphInput : MonoBehaviour {
		[SerializeField] private InkPlayer m_InkPlayer;
		[SerializeField] private MorseCodePlayer m_MorseCodePlayer;
		[SerializeField] private float m_LetterEndDuration = 0.3f;
		[SerializeField] private float m_MaxDotDuration = 0.13f;

		[SerializeField, ReadOnly] private string m_Characters = "";
		[SerializeField, ReadOnly] private string m_Codes = "";
		
		private float m_DotTimer;
		private float m_LetterEndTimer;

		private void Update() {
			CheckLetterTimer();
			CheckDotTimer();
		}

		private void CheckDotTimer() {
			if (m_DotTimer > 0) {
				m_DotTimer -= Time.deltaTime;
			}
		}

		private void CheckLetterTimer() {
			if (m_LetterEndTimer > 0) {
				m_LetterEndTimer -= Time.deltaTime;

				if (m_LetterEndTimer <= 0 && !m_Codes.IsNullOrEmpty()) {
					m_Characters += MorseCodePlayer.CodeToChar[m_Codes];
					m_Codes = "";
				}
			}
		}

		// Use this for initialization
		private void OnMouseDown() {
			m_MorseCodePlayer.StartTap();
			m_LetterEndTimer = -1;
			m_DotTimer = m_MaxDotDuration;
		}

		private void OnMouseUp() {
			m_MorseCodePlayer.StopTap();

			char newCode = m_DotTimer < 0 ? '-' : '.';

			m_DotTimer = -1;
			m_LetterEndTimer = m_LetterEndDuration;

			if (!MorseCodePlayer.CodeToChar.ContainsKey(m_Codes + newCode)) {
				m_Characters += MorseCodePlayer.CodeToChar[m_Codes];
				m_Codes = newCode + "";
			}
			else {
				m_Codes += newCode;
			}
		}
	}
}