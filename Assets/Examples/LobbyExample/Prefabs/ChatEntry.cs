using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Collections;

public class ChatEntry : MonoBehaviour {

	public Text playerNameText;
	public Text chatText;

	private void Awake ()
	{
		Assert.IsNotNull (playerNameText, string.Format ("{0}: playerNameText has not been assigned in the inspector", this.name));
		Assert.IsNotNull (chatText, string.Format ("{0}: chatText has not been assigned in the inspector", this.name));
	}

	public void Init (string playerName, string message)
	{
		playerNameText.text = playerName;
		chatText.text = message;
	}
}
