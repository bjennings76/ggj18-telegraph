using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Telegraph {
	public class Quitter : MonoBehaviour {
		private void Update() {
			if (!Input.GetButton("quit")) {
				return;
			}

#if UNITY_EDITOR
			if (Application.isEditor) {
				EditorApplication.isPlaying = false;
				return;
			}
#endif
			InkPlayer.Restart();
			//Application.Quit();
		}
	}
}