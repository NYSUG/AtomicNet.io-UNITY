using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyListing : MonoBehaviour {

	public Text lobbyNameText;

    private string _gameId;

	private void Awake ()
	{
		Assert.IsNotNull (lobbyNameText, string.Format ("{0}: lobbyNameText has not been assigned in the inspector", this.name));
	}

    public void Init (Dictionary<string, object> lobbyData)
	{
        lobbyNameText.text = lobbyData["name"].ToString ();
        _gameId = lobbyData["gameId"].ToString ();
	}

#region Button Actions

	public void JoinButtonPressed ()
	{
        GameObject.FindObjectOfType<LobbyManager> ().JoinLobby (lobbyNameText.text, _gameId);
	}

#endregion
}
