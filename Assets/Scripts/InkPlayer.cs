using System;
using System.Collections.Generic;
using System.Linq;
using Unspeakable.Utils;
using DG.Tweening;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Telegraph {
	public class InkPlayer : MonoBehaviour {
		[SerializeField] private TextAsset m_InkJsonAsset;

		[Header("Settings")]

		[SerializeField] private float m_NextDelay = 1;
		[SerializeField] private MorseCodePlayer m_MorseCode;

		[Header("UI")]

		[SerializeField] private Transform m_Lines;
		[SerializeField] private Line m_TitleLinePrefab;
		[SerializeField] private Line m_SubtitleLinePrefab;
		[SerializeField] private Line m_DefaultLinePrefab;
		[SerializeField] private Line m_NPCLinePrefab;
		[SerializeField] private Line m_PlayerLinePrefab;
		[SerializeField] private ChoiceLine m_PlayerChoicePrefab;

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
		private int m_ResponseIndex;
		private string m_LastBack;
		private string m_LastRight;
		private string m_LastLeft;
		private bool m_OnFirstLine;

		private readonly List<ChoiceLine> m_CurrentChoiceLines = new List<ChoiceLine>();

		private void OnEnable() { StartStory(); }

		private void Update() {
			// Keyboard cheat.
			foreach (ChoiceLine choice in m_CurrentChoiceLines) {
				if (Input.GetKeyDown(choice.Letter.ToLower())) {
					choice.OnClick.Invoke();
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
			foreach (Line line in m_Lines.GetComponentsInChildren<Line>().Where(l => l)) {
				line.FadeOut().SetDelay(delay);
				delay += 0.1f;
			}

			m_OnFirstLine = true;
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
				Line nextLine = CreateContentView(text, m_ResponseIndex);
				m_ResponseIndex = -1;

				string back;
				string right;
				string left;

				// Check possible commands.
				if (GetTagValue("back", out back) && back != m_LastBack) {
					m_LastBack = back;
					m_Backdrop.DestroyAllChildren();
					if (StageAssets.Backdrops.ContainsKey(back)) { Instantiate(StageAssets.Backdrops[back], m_Backdrop, false); }
				}

				if (GetTagValue("right", out right) && right != m_LastRight) {
					m_LastRight = right;
					m_OnStageRight.DestroyAllChildren();
					if (StageAssets.Characters.ContainsKey(right)) {
						GameObject go = Instantiate(StageAssets.Characters[right], m_OnStageRight, false);
						go.transform.position = m_OffStageRight.position;
						go.transform.DOLocalMove(Vector3.zero, m_SlideDuration);
					}
				}

				if (GetTagValue("left", out left) && left != m_LastLeft) {
					m_LastLeft = left;
					m_OnStageLeft.DestroyAllChildren();
					if (StageAssets.Characters.ContainsKey(left)) {
						GameObject go = Instantiate(StageAssets.Characters[left], m_OnStageLeft, false);
						go.transform.position = m_OffStageLeft.position;
						go.transform.DOLocalMove(Vector3.zero, m_SlideDuration);
					}
				}

				if (m_Story.currentTags.Contains("reveal")) {
					m_Vignette.DOColor(Color.clear, 2);
				} 

				if (m_OnFirstLine && nextLine.LineType == Line.Type.Player) {
					m_OnFirstLine = false;
					Next();
					return;
				}

				if (m_OnFirstLine) {
					m_OnFirstLine = false;
				}

				if (m_MorseCode != null && (nextLine.LineType == Line.Type.NPC || nextLine.LineType == Line.Type.Player)) {
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
			//GetComponentsInChildren<Line>().Where(l => l.LineType == Line.Type.Player).ForEach(l => l.FadeOut());
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

		private void OnClickChoiceButton(ChoiceLine sender, Choice choice) {
			m_ResponseIndex = sender.transform.GetSiblingIndex();
			//UnityUtils.DestroyObject(sender);
			m_Story.ChooseChoiceIndex(choice.index);
			Refresh();
		}

		private Line CreateContentView(string text, int insertIndex = -1) {
			Line linePrefab = GetLinePrefab(ref text);
			Line storyLine = Instantiate(linePrefab, m_Lines, false);
			storyLine.SetText(text, insertIndex >= 0);

			if (insertIndex >= 0 && linePrefab == m_PlayerLinePrefab) {
				storyLine.transform.SetSiblingIndex(insertIndex);
			}

			return storyLine;
		}

		private Line GetLinePrefab(ref string text) {
			if (text.Contains("<h4")) { return m_SubtitleLinePrefab; }
			if (text.Contains("<h")) { return m_TitleLinePrefab; }

			if (text.StartsWith("(") || m_Story.currentTags.Contains("desc") || m_Story.currentTags.Contains("writing")) {
				text = text.Trim('(', ')');
				return m_DefaultLinePrefab;
			}

			return text.Contains("<i>") ? m_PlayerLinePrefab : m_NPCLinePrefab;
		}

		private ChoiceLine CreateChoiceView(string text, List<char> usedChars) {
			ChoiceLine choice = Instantiate(m_PlayerChoicePrefab, m_Lines, false);
			choice.SetText(text, usedChars);
			return choice;
		}
	}
}