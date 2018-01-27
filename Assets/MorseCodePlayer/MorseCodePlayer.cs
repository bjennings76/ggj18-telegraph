using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Collections;

public class MorseCodePlayer : MonoBehaviour {
	[SerializeField] private AudioSource m_AudioSource;
	[SerializeField] private AudioClip m_DashSound;
	[SerializeField] private AudioClip m_DotSound;
	[SerializeField] private AudioClip m_Tone;
	[SerializeField] private float m_SpaceDelay = 0.5f;
	[SerializeField] private float m_LetterDelay = 0.02f;

	[Header("Animation"), SerializeField] private float m_FrameLength = 0.06f;

	[SerializeField] private SpriteRenderer m_Renderer;

	[SerializeField] private Sprite m_OffFrame;
	[SerializeField] private Sprite m_OffToOnFrame;
	[SerializeField] private Sprite m_OnFrame;

	[SerializeField, ReadOnly] private string m_Text;

	private Coroutine m_TapCoroutine;
	private Tween m_TapSequence;

	// International Morse Code Alphabet
	private static readonly string[] s_Alphabet = {
		//A     B       C       D      E    F       G
		".-", "-...", "-.-.", "-..", ".", "..-.", "--.",
		//H       I     J       K      L       M     N
		"....", "..", ".---", "-.-", ".-..", "--", "-.",
		//O      P       Q       R      S      T    U
		"---", ".--.", "--.-", ".-.", "...", "-", "..-",
		//V       W      X       Y       Z
		"...-", ".--", "-..-", "-.--", "--..",
		//0        1        2        3        4
		"-----", ".----", "..---", "...--", "....-",
		//5        6        7        8        9
		".....", "-....", "--...", "---..", "----."
	};

	private static Dictionary<char, string> s_CharToCode;
	private static Dictionary<string, char> s_CodeToChar;

	public static Dictionary<char, string> CharToCode {
		get {
			if (s_CharToCode == null) { CreateLookupTables(); }
			return s_CharToCode;
		}
	}
	public static Dictionary<string, char> CodeToChar {
		get {
			if (s_CodeToChar == null) { CreateLookupTables(); }
			return s_CodeToChar;
		}
	}

	private static void CreateLookupTables() {
		s_CharToCode = new Dictionary<char, string>();
		s_CodeToChar = new Dictionary<string, char>();

		for (int i = 0; i < s_Alphabet.Length; i++) {
			char c = i < 26 ? (char) ('A' + i) : (char) ('0' + i - 26);
			s_CharToCode[c] = s_Alphabet[i];
			s_CodeToChar[s_Alphabet[i]] = c;
		}
	}

	// Use this for initialization
	private void OnEnable() { m_AudioSource = m_AudioSource ? m_AudioSource : GetComponent<AudioSource>(); }

	public void PlayMorseCodeMessage(string message) {
		if (m_TapCoroutine != null) { StopCoroutine(m_TapCoroutine); }
		// Remove all characters that are not supported by Morse code...
		m_Text = CleanText(message);
		m_TapCoroutine = StartCoroutine(PlayMessage());
	}

	public void StartTap() {
		m_AudioSource.clip = m_Tone;
		m_AudioSource.loop = true;
		m_AudioSource.Play();
		if (m_TapSequence != null) { m_TapSequence.Kill(); }
		OffToOn();
		m_TapSequence = DOTween.Sequence().AppendInterval(m_FrameLength).AppendCallback(On);
	}

	public void StopTap() {
		if (m_TapSequence != null) { m_TapSequence.Kill(); }
		m_AudioSource.Stop();
		Off();
	}

	private IEnumerator PlayMessage() {
		// Convert the message into Morse code audio... 
		foreach (char letter in m_Text) {
			if (letter == ' ') {
				yield return new WaitForSeconds(m_SpaceDelay);
			}
			else {
				string letterCode = CharToCode[letter];
				foreach (char bit in letterCode) {
					float soundDelay = bit == '-' ? PlayDash() : PlayDot();
					yield return new WaitForSeconds(soundDelay + m_LetterDelay);
				}
			}
		}
	}

	private float PlayDot() {
		m_AudioSource.PlayOneShot(m_DotSound);
		PlayTap(m_DotSound.length);
		return m_DotSound.length;
	}

	private float PlayDash() {
		m_AudioSource.PlayOneShot(m_DashSound);
		PlayTap(m_DashSound.length);
		return m_DashSound.length;
	}

	private void PlayTap(float tapDelay) {
		if (m_TapSequence != null) { m_TapSequence.Kill(); }
		Off();
		m_TapSequence = DOTween.Sequence().SetTarget(this)
								.AppendInterval(m_FrameLength)
								.AppendCallback(OffToOn)
								.AppendInterval(m_FrameLength)
								.AppendCallback(On)
								.AppendInterval(tapDelay)
								.AppendCallback(Off);
	}

	private void Off() { m_Renderer.sprite = m_OffFrame; }
	private void OffToOn() { m_Renderer.sprite = m_OffToOnFrame; }
	private void On() { m_Renderer.sprite = m_OnFrame; }

	public static string CleanText(string text) {
		Regex regex = new Regex("[^A-z0-9 ]");
		return regex.Replace(text.ToUpper(), "");
	}
}