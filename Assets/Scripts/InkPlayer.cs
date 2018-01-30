﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unspeakable.Utils;
using DG.Tweening;
using Ink.Runtime;
using UnityEngine;

namespace Telegraph {
	public class InkPlayer : Singleton<InkPlayer> {
		[SerializeField] private TextAsset m_InkJsonAsset;

		[Header("Settings")]

		[SerializeField] private float m_NextDelay = 1;
		[SerializeField] private MorseCodePlayer m_MorseCode;

		[Header("UI")]

		[SerializeField] private Transform m_Lines;
		[SerializeField] private LineLookup m_LineLookup;

		[Header("Stage")]

		[SerializeField] private SpriteRenderer m_Vignette;
		[SerializeField] private Transform m_Backdrop;
		[SerializeField] private Transform m_OnStageLeft;
		[SerializeField] private Transform m_OnStageRight;
		[SerializeField] private Transform m_OffStageLeft;
		[SerializeField] private Transform m_OffStageRight;
		[SerializeField] private float m_SlideDuration = 1;
		[SerializeField] private Color m_DimColor = Color.gray;

		private Story m_Story;
		private string m_LastBack;
		private string m_LastRight;
		private string m_LastLeft;

		private readonly List<ChoiceLine> m_CurrentChoiceLines = new List<ChoiceLine>();
		private Line m_LastChoice;
		
		private HeaderInfo m_HeaderInfo;
		private string m_CurrentStyle;
		private Line m_SelectedLinePrefab;
		private ChoiceLine m_SelectedChoicePrefab;
		private HeaderLine m_SelectedHeaderPrefab;

		private HeaderInfo HeaderInfo { get { return m_HeaderInfo ?? (m_HeaderInfo = new HeaderInfo()); } }

		public bool HasChoices { get { return m_CurrentChoiceLines.Count > 0 && m_Story.currentChoices != null && m_Story.currentChoices.Count > 0; } }

		private void OnEnable() { StartStory(); }

		private void Update() {
			// Keyboard cheat.
			foreach (ChoiceLine choice in m_CurrentChoiceLines) {
				if (Input.GetKeyDown(choice.Letter.ToLower()) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
					choice.OnClick.Invoke();
					m_MorseCode.StopMessage();
					TelegraphInput.Skip();
				}
			}
		}

		public static void Restart() {
			FindObjectOfType<InkPlayer>().StartStory();
		}

		private void StartStory() {
			m_Story = new Story(m_InkJsonAsset.text);
			m_LastBack = null;
			m_LastRight = null;
			m_LastLeft = null;
			m_Vignette.color = Color.white;
			m_Lines.DestroyAllChildren();
			m_Backdrop.DestroyAllChildren();
			m_OnStageLeft.DestroyAllChildren();
			m_OnStageRight.DestroyAllChildren();
			m_OffStageLeft.DestroyAllChildren();
			m_OffStageRight.DestroyAllChildren();
			Refresh();
		}

		private void Refresh() {
			float delay = 0;

			foreach (Line line in m_Lines.GetComponentsInChildren<Line>().Where(l => l && l != m_LastChoice)) {
				line.FadeOut().SetDelay(delay);
				line.enabled = false;
				delay += 0.1f;
			}

			if (m_LastChoice) {
				m_LastChoice.FadeOut().SetDelay(10);
				m_LastChoice.enabled = false;
			}

			Next();
		}

		private bool GetTagValue(string tagKey, out string tagValue) {
			string tagString = m_Story.currentTags.FirstOrDefault(t => t.StartsWith(tagKey + ":", StringComparison.OrdinalIgnoreCase));
			if (tagString.IsNullOrEmpty()) {
				tagValue = null;
				return false;
			}
			tagValue = tagString.Split(':').ElementAtOrDefault(1);
			return true;
		}

		private void Next() {
			if (m_Story.canContinue) {
				string text = m_Story.Continue().Trim();

				if (text.IsNullOrEmpty()) {
					RunCommands();
					Next();
					return;
				}

				Line nextLine = CreateContentView(text);

				if (m_MorseCode != null && nextLine.PlayMorseCode) {
					m_MorseCode.PlayMorseCodeMessage(text);
				}

				SetDimmers(nextLine.LineType);

				TextFade tf = nextLine.GetComponentInChildren<TextFade>();

				if (tf) { tf.OnComplete.AddListener(Next); }
				else { DelayTracker.DelayAction(m_NextDelay, Next); }

				return;
			}

			m_MorseCode.StopMessage();

			List<char> usedChars = new List<char>();

			// Fade old choices.
			m_CurrentChoiceLines.Clear();
			if (m_Story.currentChoices.Count > 0) {
				for (int i = 0; i < m_Story.currentChoices.Count; i++) {
					Choice choice = m_Story.currentChoices[i];
					ChoiceLine choiceLine = CreateChoiceView(choice.text.Trim(), usedChars);
					m_CurrentChoiceLines.Add(choiceLine);
					choiceLine.OnClick.AddListener(() => OnClickChoiceButton(choiceLine, choice));
				}
			} else {
				ChoiceLine choiceLine = CreateChoiceView("End of story.\nRestart?", usedChars);
				m_CurrentChoiceLines.Add(choiceLine);
				choiceLine.OnClick.AddListener(StartStory);
			}
		}

		private void SetDimmers(Line.Type type) {
			SetDimmer(m_OnStageLeft, type == Line.Type.NPC);
			SetDimmer(m_OnStageRight, type == Line.Type.Player);
		}

		private void SetDimmer(Component side, bool dim) {
			side.GetComponentsInChildren<SpriteRenderer>().ForEach(r => {
				r.DOKill();
				r.DOColor(r.color = dim ? m_DimColor : Color.white, m_SlideDuration);
			});
		}

		private void OnClickChoiceButton(ChoiceLine choiceLine, Choice choice) {
			m_CurrentChoiceLines.Clear();
			m_Story.ChooseChoiceIndex(choice.index);
			m_LastChoice = choiceLine;
			Refresh();
		}

		private Line CreateContentView(string text) {
			RunCommands(text);

			Line linePrefab = m_SelectedLinePrefab ? m_SelectedLinePrefab : m_LineLookup.DefaultLine;
			Line storyLine = Instantiate(linePrefab, m_Lines, false);
			storyLine.SetText(text);

			return storyLine;
		}

		private ChoiceLine CreateChoiceView(string text, List<char> usedChars) {
			RunCommands(text, true);

			ChoiceLine prefab = m_SelectedChoicePrefab ? m_SelectedChoicePrefab : m_LineLookup.DefaultChoice;
			ChoiceLine choice = Instantiate(prefab, m_Lines, false);
			choice.SetText(text, usedChars);
			return choice;
		}

		private void RunCommands(string text = null, bool isChoice = false) {
			List<string> commands = GetCommands(text);
			foreach (string command in commands) { RunCommand(command, isChoice); }

			if (m_HeaderInfo != null) {
				m_SelectedHeaderPrefab = GetLine(m_CurrentStyle + "-header", m_LineLookup.DefaultHeader);
			}
		}

		private List<string> GetCommands(string text) {
			List<string> commandList = new List<string>(m_Story.currentTags);

			if (text.IsNullOrEmpty()) { return commandList; }

			MatchCollection matches = Regex.Matches(text, @"\$\w+(\:\w+)?", RegexOptions.IgnoreCase);
			foreach (Match match in matches) { commandList.Add(match.Value); }
			return commandList;
		}

		private void RunCommand(string commandString, bool isChoice) {
			if (commandString == "reveal") {
				m_Vignette.DOColor(Color.clear, 2);
				return;
			}

			if (isChoice) {
				ChoiceLine choiceLine = GetLine<ChoiceLine>(commandString + "-choice");

				if (choiceLine) {
					m_SelectedChoicePrefab = choiceLine;
					return;
				}
			}

			Line line = GetLine<Line>(commandString + "-choice");

			if (line) {
				m_SelectedLinePrefab = line;
				return;
			}

			if (commandString.Contains(":")) {
				string[] pieces = commandString.Split(':');

				string commandKey = pieces.FirstOrDefault();
				string value = pieces.ElementAtOrDefault(1);

				switch (commandKey) {
					case "to":
						HeaderInfo.To = value;
						return;

					case "from":
						HeaderInfo.From = value;
						return;

					case "date":
						HeaderInfo.Date = value;
						return;

					case "style":
						m_CurrentStyle = value;
						if (m_CurrentStyle.IsNullOrEmpty()) { return; }
						GetLine(isChoice ? value + "-choice" : value);
						return;

					case "back":
						if (value != m_LastBack) {
							m_LastBack = value;
							m_Backdrop.DestroyAllChildren();
							if (value != null && StageAssets.Backdrops.ContainsKey(value)) { Instantiate(StageAssets.Backdrops[value], m_Backdrop, false); }
						}
						return;

					case "right":
						if (value != m_LastRight) {
							m_LastRight = value;
							m_OnStageRight.DestroyAllChildren();
							if (value != null && StageAssets.Characters.ContainsKey(value)) {
								GameObject go = Instantiate(StageAssets.Characters[value], m_OnStageRight, false);
								go.transform.position = m_OffStageRight.position;
								go.transform.DOLocalMove(Vector3.zero, m_SlideDuration);
							}
						}

						return;
					case "left":
						if (value != m_LastLeft) {
							m_LastLeft = value;
							m_OnStageLeft.DestroyAllChildren();
							if (value != null && StageAssets.Characters.ContainsKey(value)) {
								GameObject go = Instantiate(StageAssets.Characters[value], m_OnStageLeft, false);
								go.transform.position = m_OffStageLeft.position;
								go.transform.DOLocalMove(Vector3.zero, m_SlideDuration);
							}
						}

						return;

					default:
						break;
				}
			}

			Debug.LogWarning("Nothing here to handle '" + commandString + "'.", this);
		}

		private T GetLine<T>(string command, T defaultLine = null) where T : Line {
			if (!m_LineLookup.ContainsKey(command)) {
				return defaultLine;
			}

			T prefab = m_LineLookup[command] as T;
			return prefab ? prefab : defaultLine;
		}
	}

	internal class HeaderInfo {
		public string To;
		public string From;
		public string Date;
	}
}