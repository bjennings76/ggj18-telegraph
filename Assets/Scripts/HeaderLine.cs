using UnityEngine;
using UnityEngine.UI;

namespace Telegraph {
	public class HeaderLine : Line {
		[SerializeField] private Text m_Date;
		[SerializeField] private Text m_To;
		[SerializeField] private Text m_From;

		public string Date { set { m_Date.text = value; } }
		public string To { set { m_To.text = value; } }
		public string From { set { m_From.text = value; } }
	}
}