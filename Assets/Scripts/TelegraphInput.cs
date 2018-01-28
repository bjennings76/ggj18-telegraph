using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Collections;
using UnityEngine.UI;
using Unspeakable.Utils;

namespace Telegraph {
	public class TelegraphInput : Singleton<TelegraphInput> {
		[SerializeField] private MorseCodePlayer m_MorseCodePlayer;
		[SerializeField] private Text m_CodeOutput;
		[SerializeField] private Text m_LetterOutput;
		[SerializeField] private float m_LetterEndDuration = 0.3f;
		[SerializeField] private float m_MaxDotDuration = 0.13f;

		[SerializeField, ReadOnly] private string m_Characters = "";
		[SerializeField, ReadOnly] private string m_Codes = "";

		public static event Action<char> OnLetter;

		private float m_DotTimer;
		private float m_LetterEndTimer;

		private string Codes {
			get {
				return m_Codes;
			}
			set {
				m_Codes = value;
				m_CodeOutput.text = m_Codes;
			}
		}

		private void Start() {
			m_LetterOutput.text = "";
			m_CodeOutput.text = "";
		}

		private void Update() {
			CheckInput();
			CheckLetterTimer();
			CheckDotTimer();
		}

		private void Clear() {
			m_LetterOutput.text = "";
			m_CodeOutput.text = "";
			m_DotTimer = 0;
			m_LetterEndTimer = 0;
			Codes = "";
		}

		private bool m_LastOn;

		private void CheckInput() {
			bool on = Input.GetButton("tap");

			if (on == m_LastOn) {
				return;
			}

			m_LastOn = on;

			if (on) {
				if (InkPlayer.Instance.HasChoices) { Down(); }
				else m_LastOn = false;
			} else {
				Up();
			}
		}

		private void CheckDotTimer() {
			if (m_DotTimer > 0) {
				m_DotTimer -= Time.deltaTime;
			}
		}

		private void CheckLetterTimer() {
			if (m_LetterEndTimer > 0) {
				m_LetterEndTimer -= Time.deltaTime;

				if (m_LetterEndTimer <= 0 && !Codes.IsNullOrEmpty()) {
					CompleteCharacter();
				}
			}
		}

		// Use this for initialization
		private void Down() {
			m_MorseCodePlayer.StartTap();
			m_LetterEndTimer = -1;
			m_DotTimer = m_MaxDotDuration;
		}

		private void Up() {
			m_MorseCodePlayer.StopTap();

			char newCode = m_DotTimer < 0 ? '-' : '.';

			m_DotTimer = -1;
			m_LetterEndTimer = m_LetterEndDuration;

			if (!MorseCodePlayer.CodeToChar.ContainsKey(Codes + newCode)) {
				CompleteCharacter();
			} else {
				Codes += newCode;
				m_LetterOutput.text = MorseCodePlayer.CodeToChar[Codes].ToString();
				m_LetterOutput.color = new Color(0, 0, 0, 0.5f);
				m_LetterOutput.DOKill();
				m_LetterOutput.DOColor(Color.clear, 2).SetDelay(1);
			}

			m_CodeOutput.text = Codes;
		}

		private void CompleteCharacter() {
			char c = MorseCodePlayer.CodeToChar[Codes];
			m_Characters = m_Characters + c;
			Codes = "";
			m_LetterOutput.text = c.ToString();
			m_LetterOutput.color = Color.black;
			m_LetterOutput.DOKill();
			m_LetterOutput.DOColor(Color.clear, 2).SetDelay(1);
			RaiseOnLetter(c);
		}

		private void RaiseOnLetter(char letter) {
			if (OnLetter != null) {
				OnLetter(letter);
			}
		}

		public static void Skip() { Instance.Clear(); }
	}
}