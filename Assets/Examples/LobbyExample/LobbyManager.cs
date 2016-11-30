using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour {

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
	public GameObject lobbyGameObject;
	public Transform chatContextTransform;
	public Transform playerContextTransform;
	public GameObject chatEntryPrefab;
	public GameObject playerEntryPrefab;

	private void Awake ()
	{
		Assert.IsNotNull (connectLobbyGameObject, string.Format ("{0}: connectLobbyGameObject has not been assigned in the inspector", this.name));
		Assert.IsNotNull (lobbyNameInputField, string.Format ("{0}: lobbyNameInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (gameIdInputField, string.Format ("{0}: gameIdInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (findLobbyGameObject, string.Format ("{0}: findLobbyGameObject has not been assigned in the inspector", this.name));
		Assert.IsNotNull (noLobbyFoundGameObject, string.Format ("{0}: noLobbyFoundGameObject has not been assigned in the inspector", this.name));
		Assert.IsNotNull (findLobbyContextTransform, string.Format ("{0}: lobbyContextTransform has not been assigned in the inspector", this.name));
		Assert.IsNotNull (lobbyListingPrefab, string.Format ("{0}: lobbyListingPrefab has not been assigned in the inspector", this.name));
		Assert.IsNotNull (lobbyGameObject, string.Format ("{0}: lobbyGameObject has not been assigned in the inspector", this.name));
		Assert.IsNotNull (chatContextTransform, string.Format ("{0}: chatContextTransform has not been assigned in the inspector", this.name));
		Assert.IsNotNull (playerContextTransform, string.Format ("{0}: playerContextTransform has not been assigned in the inspector", this.name));
		Assert.IsNotNull (chatEntryPrefab, string.Format ("{0}: chatEntryPrefab has not been assigned in the inspector", this.name));
		Assert.IsNotNull (playerEntryPrefab, string.Format ("{0}: playerEntryPrefab has not been assigned in the inspector", this.name));
	}

#region Button Actions

	public void CreateLobby ()
	{
		_MoveToPool (lobbyNameInputField.text);
	}

	public void FindLobbies ()
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

			} else {

				// Populate lobbies
				foreach (object obj in pools) {

					// Monobehaviors must be run on the main thread
					NetworkManager.RunOnMainThread (() => {

						Dictionary<string, object> lobby = (Dictionary<string, object>)obj;
						GameObject go = (GameObject)Instantiate (lobbyListingPrefab, Vector3.zero, Quaternion.identity);
						go.transform.SetParent (findLobbyContextTransform);
						go.GetComponent<LobbyListing> ().Init (lobby["name"].ToString ());
						go.transform.localPosition = new Vector3 (250.0f, -50.0f, 0);
					});
				}
			}
		});
	}

	public void JoinLobby (string lobbyName)
	{
		_MoveToPool (lobbyName);
	}

#endregion

	private void _MoveToPool (string poolName)
	{
		// Set the gameId
		AtomicNet.gameId = gameIdInputField.text;

		AtomicNet.instance.MoveToPool (poolName, "lobby", (string error, object obj) => {
			if (!string.IsNullOrEmpty (error)) {
				Debug.LogError (error);
				return;
			}

			Debug.Log ("Connected to Lobby connection pool");

			// AtomicNet callbacks are run on a background thread
			// MonoBehaviors have to run on the main thread
			NetworkManager.RunOnMainThread (() => {

				// Hide The Create Lobby Panel
				connectLobbyGameObject.SetActive (false);

				// Hide the Find Lobby Panel
				findLobbyGameObject.SetActive (false);

				// Set the Lobby Panel Active
				lobbyGameObject.SetActive (true);
			});
		});
	}


}
