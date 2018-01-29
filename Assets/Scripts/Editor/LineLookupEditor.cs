using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Telegraph {
	[CustomEditor(typeof(LineLookup))]
	public class LineLookupEditor : Editor {
		private ReorderableList m_Bindings;

		private void OnEnable() {
			m_Bindings = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Bindings"));

			m_Bindings.drawHeaderCallback = rect => {
				using (new GUILayout.HorizontalScope()) {
					rect.y += 2;
					rect.x += 14;
					float arrowWidth = 18;
					float half = (rect.width - arrowWidth) / 2;
					GUI.Label(new Rect(rect.x, rect.y, half, EditorGUIUtility.singleLineHeight), "Tag", EditorStyles.boldLabel);
					GUI.Label(new Rect(rect.x + arrowWidth + half, rect.y, half, EditorGUIUtility.singleLineHeight), "Line", EditorStyles.boldLabel);
				}
			};

			m_Bindings.drawElementCallback = (rect, index, isActive, isFocused) => {
				rect.y += 2;
				float arrowWidth = 30;
				float half = (rect.width - arrowWidth) / 2;
				SerializedProperty element = m_Bindings.serializedProperty.GetArrayElementAtIndex(index);
				EditorGUI.PropertyField(new Rect(rect.x, rect.y, half, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Tag"), GUIContent.none);
				EditorGUI.LabelField(new Rect(rect.x + half, rect.y, arrowWidth, EditorGUIUtility.singleLineHeight), " --> ");
				EditorGUI.PropertyField(new Rect(rect.x + arrowWidth + half, rect.y, half, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Line"), GUIContent.none);
			};
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DefaultLine"));
			m_Bindings.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}