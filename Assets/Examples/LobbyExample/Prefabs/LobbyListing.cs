using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Collections;

public class LobbyListing : MonoBehaviour {

	public Text lobbyNameText;

	private void Awake ()
	{
		Assert.IsNotNull (lobbyNameText, string.Format ("{0}: lobbyNameText has not been assigned in the inspector", this.name));
	}

	public void Init (string lobbyName)
	{
		lobbyNameText.text = lobbyName;
	}

#region Button Actions

	public void JoinButtonPressed ()
	{
		GameObject.FindObjectOfType<LobbyManager> ().JoinLobby (lobbyNameText.text);
	}

#endregion
}
