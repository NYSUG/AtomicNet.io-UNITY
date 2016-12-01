using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class NetworkManager : MonoBehaviour {

	/// <summary>
	/// AtomicNet network messages come in off of the main thread.
	/// This queue allows operations to be executed on the main thread. Any MonoBehavior methods or
	/// overrides must be run on the main thread.
	/// </summary>
	public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action> ();

	private Thread _readThread;

#region MonoBehaviors

	/// <summary>
	/// Awake this instance.
	/// </summary>
	private void Awake ()
	{
		// Init AtomicNet
		AtomicNet.instance.StartAtomicNetClient ();

		// Init our message reading thread
		_readThread = new Thread (new ThreadStart (_ReadNetworkMessages));
		_readThread.Start ();
	}

	/// <summary>
	/// The Update Loop is used to execute actions on the main thread.
	/// </summary>
	private void Update ()
	{
		lock (ExecuteOnMainThread) {
			while (ExecuteOnMainThread.Count > 0) {
				Action action = ExecuteOnMainThread.Dequeue ();
				if (action == null) {
					Debug.LogWarning (string.Format ("Action was null"));
					return;
				}

				action.Invoke ();
			}
		}
	}

#endregion

	/// <summary>
	/// Runs actions on main thread.
	/// </summary>
	/// <param name="action">Action.</param>
	public static void RunOnMainThread (Action action)
	{
		lock (ExecuteOnMainThread) {
			ExecuteOnMainThread.Enqueue (action);
		}
	}

#region Message Handling

	/// <summary>
	/// Loop run in a background thread to check for AtomicNet messages
	/// </summary>
	private void _ReadNetworkMessages ()
	{
		while (true) {

			// Do not check for messages if we are not connected
			if (AtomicNet.instance.IsConnected ()) {
				_CheckForMessages ();
			} else {
				Thread.Sleep (100);
			}
		}
	}

	/// <summary>
	/// Checks AtomicNet to see if there are any messages for pickup. AtomicNet messages
	/// are dictionaries of data.
	/// </summary>
	private void _CheckForMessages ()
	{
		if (AtomicNet.instance.HasServerMessages ()) {
			
			Dictionary<string, object> serverMessage = AtomicNet.instance.CheckForServerMessages ();
			if (serverMessage != null) {
				ProcessNetworkServerMessage (serverMessage);
			}
		}

		if (AtomicNet.instance.HasClientMessages ()) {

			Dictionary<string, object> clientMessage = AtomicNet.instance.CheckForClientMessages ();
			if (clientMessage != null) {
				ProcessNetworkClientMessage (clientMessage);
			}
		}

		if (AtomicNet.instance.HasConnMessages ()) {

			Dictionary<string, object> connMessage = AtomicNet.instance.CheckForConnMessages ();
			if (connMessage != null) {
				ProcessNetworkClientMessage (connMessage);
			}
		}
	}

	/// <summary>
	/// Processes the network server message.
	/// </summary>
	/// <param name="netMsg">Net message.</param>
	public void ProcessNetworkServerMessage (Dictionary<string, object> netMsg)
	{
		// Get the type from the object
		NetworkMessages.MessageTypes type = (NetworkMessages.MessageTypes)System.Enum.Parse (typeof (NetworkMessages.MessageTypes), netMsg ["type"].ToString ());

        Debug.Log (string.Format ("Server Message: {0}", type));

		switch (type) {
            case NetworkMessages.MessageTypes.JOIN_GAME:
                Debug.Log (string.Format ("connId: {0} has joined the game", netMsg["connId"].ToString ()));
			break;
			default:
				Debug.LogError (string.Format ("Unknown MessageType: {0}", type));
			break;
		}
	}

	/// <summary>
	/// Processes the network client or connId message.
	/// </summary>
	/// <param name="netMsg">Net message.</param>
	public void ProcessNetworkClientMessage (Dictionary<string, object> netMsg)	
	{
		// Get the type from the object
		NetworkMessages.MessageTypes type = (NetworkMessages.MessageTypes)System.Enum.Parse (typeof (NetworkMessages.MessageTypes), netMsg ["type"].ToString ());

        Debug.Log (string.Format ("Client Message: {0}", type));

		switch (type) {
			case NetworkMessages.MessageTypes.SERVER_DISCONNECT:
				Debug.LogError ("The Server has disconnected");
				GameObject.FindObjectOfType<LobbyManager> ().ResetLobby ();
			break;

            case NetworkMessages.MessageTypes.ADD_USERNAME:
                RunOnMainThread (() => {
                    GameObject.FindObjectOfType<LobbyManager> ().AddUserToChat (netMsg["username"].ToString ());
                });
            break;
			default:
				Debug.LogError (string.Format ("Unknown MessageType: {0}", type));
			break;
		}
	}

#endregion

}
