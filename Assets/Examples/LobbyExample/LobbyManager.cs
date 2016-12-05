using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour {

    // Info Objects
    public Text isServerValueText;
    public InputField usernameInputField;

	// Create Lobby Objects
	public GameObject connectLobbyGameObject;
	public InputField lobbyNameInputField;
	public InputField gameIdInputField;

	// Find Lobby Objects
	public GameObject findLobbyGameObject;
	public GameObject noLobbyFoundGameObject;
	public Transform findLobbyContextTransform;
	public GameObject lobbyListingPrefab;

	// Chat Objects
	public Text lobbyNameText;
	public GameObject lobbyGameObject;
	public Transform chatContextTransform;
	public Transform playerContextTransform;
	public GameObject chatEntryPrefab;
	public GameObject playerEntryPrefab;
    public InputField chatInputField;

	private void Awake ()
	{
        Assert.IsNotNull (isServerValueText, string.Format ("{0}: isServerValueText has not been assigned in the inspector", this.name));
        Assert.IsNotNull (usernameInputField, string.Format ("{0}: usernameInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (connectLobbyGameObject, string.Format ("{0}: connectLobbyGameObject has not been assigned in the inspector", this.name));
		Assert.IsNotNull (lobbyNameInputField, string.Format ("{0}: lobbyNameInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (gameIdInputField, string.Format ("{0}: gameIdInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (findLobbyGameObject, string.Format ("{0}: findLobbyGameObject has not been assigned in the inspector", this.name));
		Assert.IsNotNull (noLobbyFoundGameObject, string.Format ("{0}: noLobbyFoundGameObject has not been assigned in the inspector", this.name));
		Assert.IsNotNull (findLobbyContextTransform, string.Format ("{0}: lobbyContextTransform has not been assigned in the inspector", this.name));
		Assert.IsNotNull (lobbyListingPrefab, string.Format ("{0}: lobbyListingPrefab has not been assigned in the inspector", this.name));
		Assert.IsNotNull (lobbyNameText, string.Format ("{0}: lobbyNameText has not been assigned in the inspector", this.name));
		Assert.IsNotNull (lobbyGameObject, string.Format ("{0}: lobbyGameObject has not been assigned in the inspector", this.name));
		Assert.IsNotNull (chatContextTransform, string.Format ("{0}: chatContextTransform has not been assigned in the inspector", this.name));
		Assert.IsNotNull (playerContextTransform, string.Format ("{0}: playerContextTransform has not been assigned in the inspector", this.name));
		Assert.IsNotNull (chatEntryPrefab, string.Format ("{0}: chatEntryPrefab has not been assigned in the inspector", this.name));
		Assert.IsNotNull (playerEntryPrefab, string.Format ("{0}: playerEntryPrefab has not been assigned in the inspector", this.name));
        Assert.IsNotNull (chatInputField, string.Format ("{0}: chatInputField has not been assigned in the inspector", this.name));
	}

    private void Update ()
    {
        isServerValueText.text = AtomicNet.instance.IsPoolMaster () ? "true" : "false";
    }

#region Button Actions

	public void CreateLobbyButtonPressed ()
	{
		if (string.IsNullOrEmpty (usernameInputField.text)) {
			Debug.LogError ("Please input a username before joining/creating a lobby");
			return;
		}

        _MoveToPool (lobbyNameInputField.text, gameIdInputField.text);
	}

	public void FindLobbiesButtonPressed ()
	{
		AtomicNet.instance.FindConnectionPools ((string error, Dictionary<string, object> data) => {
			if (!string.IsNullOrEmpty (error)) {
				Debug.LogError (error);
				return;
			}

			if (!data.ContainsKey ("pools")) {
				Debug.LogError ("No Pool data found in request");
				return;
			}

			List<object> pools = (List<object>)data["pools"];

			// Show the no result found message
			if (pools.Count == 0) {

				// Display the no lobbies found message
				NetworkManager.RunOnMainThread (() => {
					noLobbyFoundGameObject.SetActive (true);
				});

			} else {

				// Display the no lobbies found message
				NetworkManager.RunOnMainThread (() => {
					noLobbyFoundGameObject.SetActive (false);
				});

				// Populate lobbies
				foreach (object obj in pools) {

					// Monobehaviors must be run on the main thread
					NetworkManager.RunOnMainThread (() => {

						Dictionary<string, object> lobby = (Dictionary<string, object>)obj;
						GameObject go = (GameObject)Instantiate (lobbyListingPrefab, Vector3.zero, Quaternion.identity);
						go.transform.SetParent (findLobbyContextTransform);
						go.GetComponent<LobbyListing> ().Init (lobby);
						go.transform.localPosition = new Vector3 (250.0f, -50.0f, 0);
					});
				}
			}
		});
	}

    public void JoinLobby (string lobbyName, string gameId)
	{
		if (string.IsNullOrEmpty (usernameInputField.text)) {
			Debug.LogError ("Please input a username before joining/creating a lobby");
			return;
		}

        _MoveToPool (lobbyName, gameId);
	}

	public void LeaveLobbyButtonPressed ()
	{
		// Disconnect
		AtomicNet.instance.StopAtomicNetClient ();

		// Set the lobby name text
		lobbyNameText.text = string.Empty;

		// Hide The Create Lobby Panel
		connectLobbyGameObject.SetActive (true);

		// Hide the Find Lobby Panel
		findLobbyGameObject.SetActive (true);

		// Set the Lobby Panel Active
		lobbyGameObject.SetActive (false);
	}

    public void SendMessageButtonPressed ()
    {
        // Don't do anything if there is no message to send
        if (string.IsNullOrEmpty (chatInputField.text))
            return;

        // TODO: Create our chat entry locally first


        // Send a message to all the other connected devices
        Dictionary<string, object> netMsg = new Dictionary<string, object> ()
        {
            { "type", NetworkMessages.MessageTypes.ADD_CHAT },
            { "message", chatInputField.text },
        };

        AtomicNet.instance.SendTCPMessageToOthersInPool (netMsg, NYSU.AtomicNetLib.PriorityChannel.RELIABLE_CHANNEL, (string error, object obj) => {
            if (!string.IsNullOrEmpty (error)) {
                Debug.Log (error);
                return;
            }

            Debug.Log ("Chat message sent successfully");
        });
    }

#endregion

	public void ResetLobby ()
	{
		Debug.Log ("Resetting Lobby");

		NetworkManager.RunOnMainThread (() => {

			// Set the lobby name text
			lobbyNameText.text = string.Empty;

			// Hide The Create Lobby Panel
			connectLobbyGameObject.SetActive (true);

			// Hide the Find Lobby Panel
			findLobbyGameObject.SetActive (true);

			// Set the Lobby Panel Active
			lobbyGameObject.SetActive (false);
		});
	}

    /// <summary>
    /// Adds the user to chat. Since this direclty uses MonoBehavior methods this must be
    /// run on the main thread.
    /// </summary>
    /// <param name="username">Username.</param>
    public void AddUserToChat (string username)
    {
        GameObject go = (GameObject)Instantiate (playerEntryPrefab, Vector3.zero, Quaternion.identity);
        go.transform.SetParent (playerContextTransform);
        go.GetComponent<Text> ().text = username;
        go.transform.localPosition = new Vector3 (-150.0f, -25.0f, 0.0f);
    }

    private void _MoveToPool (string poolName, string gameId)
	{
		// Set the gameId
		AtomicNet.gameId = gameIdInputField.text;

        AtomicNet.instance.MoveToPool (poolName, "lobby", gameId, (string error, object obj) => {
			if (!string.IsNullOrEmpty (error)) {
				Debug.LogError (error);
				return;
			}

			Debug.Log ("Connected to Lobby connection pool");

			// AtomicNet callbacks are run on a background thread
			// MonoBehaviors have to run on the main thread
			NetworkManager.RunOnMainThread (() => {

				// Set the lobby name text
				lobbyNameText.text = poolName;

				// Hide The Create Lobby Panel
				connectLobbyGameObject.SetActive (false);

				// Hide the Find Lobby Panel
				findLobbyGameObject.SetActive (false);

				// Set the Lobby Panel Active
				lobbyGameObject.SetActive (true);
			});

            // Tell the server that you have joined the game
            Dictionary<string, object> netMsg = new Dictionary<string, object> () {
                { "type", NetworkMessages.MessageTypes.ADD_USERNAME },
                { "username", usernameInputField.text },
            };


            AtomicNet.instance.SendTCPMessageToPool (netMsg, NYSU.AtomicNetLib.PriorityChannel.STATE_UPDATE_CHANNEL, (string err, object o) => {
                if (!string.IsNullOrEmpty (err)) {
                    Debug.LogError (err);
                    return;
                }

                Debug.Log ("Add Username message sent successfully");
            });
		});
	}


}
