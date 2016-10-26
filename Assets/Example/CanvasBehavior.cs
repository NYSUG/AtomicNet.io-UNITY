using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NYSU;

public class CanvasBehavior : MonoBehaviour {

	public InputField serverIPAddressInputField;
	public InputField sendPortInputField;
	public InputField readPortInputField;
	public InputField udpPortInputField;
	public InputField poolNameInputField;
	public InputField poolTypeInputField;
	public InputField numberOfMessagesInputField;
	public InputField sendToPoolInputField;
	public InputField connIdInputField;

	private bool _isConnected;

	private Thread _readThread;

	private void Awake ()
	{
		Assert.IsNotNull (serverIPAddressInputField, string.Format ("{0}: serverIPAddressInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (sendPortInputField, string.Format ("{0}: sendPortInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (readPortInputField, string.Format ("{0}: readPortInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (udpPortInputField, string.Format ("{0}: udpPortInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (poolNameInputField, string.Format ("{0}: poolMasterInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (poolTypeInputField, string.Format ("{0}: poolTypeInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (numberOfMessagesInputField, string.Format ("{0}: numberOfMessagesInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (sendToPoolInputField, string.Format ("{0}: sendToPoolInputField has not been assigned in the inspector", this.name));
		Assert.IsNotNull (connIdInputField, string.Format ("{0}: connIdInputField has not been assigned in the inspector", this.name));
	}

	private void Start ()
	{
		AtomicNet.instance.StartAtomicNetClient ();

		_readThread = new Thread (new ThreadStart (_ReadNetworkMessages));
		_readThread.Start ();
	}

	private void OnApplicationQuit ()
	{
		if (_readThread != null) {
			_readThread.Abort ();
		}
	}

	private void _ReadNetworkMessages ()
	{
		while (true) {
			if (_isConnected) {
			
				if (!AtomicNet.instance.IsConnected ()) {
					_isConnected = false;
					Debug.LogError ("We have been disconnected");
				}

				_CheckForMessages ();
			}
		}
	}

	private void _CheckForMessages ()
	{
		Dictionary<string, object> serverMessage = AtomicNet.instance.CheckForServerMessages ();

		if (serverMessage != null) {
			Debug.Log (string.Format ("Received server message: {0}", MiniJSON.Json.Serialize (serverMessage)));
		}

		Dictionary<string, object> clientMessage = AtomicNet.instance.CheckForClientMessages ();

		if (clientMessage != null) {
			Debug.Log (string.Format ("Received client message: {0}", MiniJSON.Json.Serialize (clientMessage)));
		}

		Dictionary<string, object> connMessage = AtomicNet.instance.CheckForConnMessages ();

		if (connMessage != null) {
			Debug.Log (string.Format ("Received conn message: {0}", MiniJSON.Json.Serialize (connMessage)));
		}
	}

#region Button Actions

	public void ConnectButtonPressed ()
	{
		AtomicNet.instance.Connect (serverIPAddressInputField.text, int.Parse (sendPortInputField.text), int.Parse (readPortInputField.text), int.Parse (udpPortInputField.text), (string error, object obj) => {
			if (!string.IsNullOrEmpty (error)) {
				Debug.LogError (error);
				return;
			}

			_isConnected = true;

			Debug.Log ("AtomicNet connected successfully");
		});
	}

	public void DisconnectButtonPressed ()
	{
		_isConnected = false;

		AtomicNet.instance.StopAtomicNetClient ();
	}

	public void SetMainPoolButtonPressed ()
	{
		AtomicNet.instance.MoveToPool (poolNameInputField.text, poolTypeInputField.text, (string error, object obj) => {
			if (!string.IsNullOrEmpty (error)) {
				Debug.LogError (error);
				return;
			}

			Debug.Log (string.Format ("This Connection's main pool is now: {0}", poolNameInputField.text));
		});
	}

	public void AddToPoolButtonPressed ()
	{
		AtomicNet.instance.AddToPool (poolNameInputField.text, poolTypeInputField.text, (string error, object obj) => {
			if (!string.IsNullOrEmpty (error)) {
				Debug.LogError (error);
				return;
			}

			Debug.Log (string.Format ("This Connection has been added to pool: {0}", poolNameInputField.text));
		});
	}

	public void RemoveFromPoolButtonPressed ()
	{
		AtomicNet.instance.LeavePool (poolNameInputField.text, poolTypeInputField.text, (string error, object obj) => {
			if (!string.IsNullOrEmpty (error)) {
				Debug.LogError (error);
				return;
			}

			Debug.Log (string.Format ("This Connection has been removed from pool: {0}", poolNameInputField.text));
		});
	}

	public void MakePoolMasterButtonPressed ()
	{
		AtomicNet.instance.SetConnectionAsPoolMaster (poolNameInputField.text, (string error, object obj) => {
			if (!string.IsNullOrEmpty (error)) {
				Debug.LogError (error);
				return;
			}

			Debug.Log (string.Format ("This Connection is now the pool master of: {0}", poolNameInputField.text));
		});
	}

	public void SendUDPMessagesPressed ()
	{
		int num = int.Parse (numberOfMessagesInputField.text);

		for (int i = 0; i < num; i++) {

			Dictionary<string, object> data = new Dictionary<string, object> () {
				{ "TestMessage", "I am the common model of a modern major general" },
				{ "Continued Message", "I've information vegetable, animal, and mineral" },
			};

			AtomicNet.instance.SendUDPMessageToPool (sendToPoolInputField.text, data, (string error) => {
				if (!string.IsNullOrEmpty (error)) {
					Debug.LogError (error);
					return;
				}
			});
		}
	}

	public void SendTCPMessagesPressed ()
	{
		int num = int.Parse (numberOfMessagesInputField.text);

		for (int i = 0; i < num; i++) {

			Dictionary<string, object> data = new Dictionary<string, object> () {
				{ "TestMessage", "I am the common model of a modern major general" },
				{ "Continued Message", "I've information vegetable, animal, and mineral" },
			};

			AtomicNet.instance.SendTCPMessageToPool (sendToPoolInputField.text, data, AtomicNetLib.PriorityChannel.ALL_COST_CHANNEL, (string error) => {
				if (!string.IsNullOrEmpty (error)) {
					Debug.LogError (error);
					return;
				}
			});
		}
	}

	public void SendUDPMessageToPoolMasterPressed ()
	{
		int num = int.Parse (numberOfMessagesInputField.text);

		for (int i = 0; i < num; i++) {

			Dictionary<string, object> data = new Dictionary<string, object> () {
				{ "TestMessage", "I am the common model of a modern major general" },
				{ "Continued Message", "I've information vegetable, animal, and mineral" },
			};

			AtomicNet.instance.SendUDPMessageToPoolMaster (sendToPoolInputField.text, data, (string error) => {
				if (!string.IsNullOrEmpty (error)) {
					Debug.LogError (error);
					return;
				}
			});
		}
	}

	public void SendTCPMessageToPoolMasterPressed ()
	{
		int num = int.Parse (numberOfMessagesInputField.text);

		for (int i = 0; i < num; i++) {

			Dictionary<string, object> data = new Dictionary<string, object> () {
				{ "TestMessage", "I am the common model of a modern major general" },
				{ "Continued Message", "I've information vegetable, animal, and mineral" },
			};

			AtomicNet.instance.SendTCPMessageToPoolMaster (sendToPoolInputField.text, data, AtomicNetLib.PriorityChannel.ALL_COST_CHANNEL, (string error) => {
				if (!string.IsNullOrEmpty (error)) {
					Debug.LogError (error);
					return;
				}
			});
		}
	}

	public void SendUDPMessageToConnIdPressed ()
	{
		int num = int.Parse (numberOfMessagesInputField.text);

		for (int i = 0; i < num; i++) {

			Dictionary<string, object> data = new Dictionary<string, object> () {
				{ "TestMessage", "I am the common model of a modern major general" },
				{ "Continued Message", "I've information vegetable, animal, and mineral" },
			};

			AtomicNet.instance.SendUDPMessageToConnId (int.Parse (connIdInputField.text), data, (string error) => {
				if (!string.IsNullOrEmpty (error)) {
					Debug.LogError (error);
					return;
				}
			});
		}
	}

	public void SendTCPMessageToConnIdPressed ()
	{
		int num = int.Parse (numberOfMessagesInputField.text);

		for (int i = 0; i < num; i++) {

			Dictionary<string, object> data = new Dictionary<string, object> () {
				{ "TestMessage", "I am the common model of a modern major general" },
				{ "Continued Message", "I've information vegetable, animal, and mineral" },
			};

			AtomicNet.instance.SendTCPMessageToConnId (int.Parse (connIdInputField.text), data, AtomicNetLib.PriorityChannel.ALL_COST_CHANNEL, (string error) => {
				if (!string.IsNullOrEmpty (error)) {
					Debug.LogError (error);
					return;
				}
			});
		}
	}

	public void SendUDPMessageToOthersPressed ()
	{
		int num = int.Parse (numberOfMessagesInputField.text);

		for (int i = 0; i < num; i++) {

			Dictionary<string, object> data = new Dictionary<string, object> () {
				{ "TestMessage", "I am the common model of a modern major general" },
				{ "Continued Message", "I've information vegetable, animal, and mineral" },
			};

			AtomicNet.instance.SendUDPMessageToOthersInPool (sendToPoolInputField.text, data, (string error) => {
				if (!string.IsNullOrEmpty (error)) {
					Debug.LogError (error);
					return;
				}
			});
		}
	}

	public void SendTCPMessageToOthersPressed ()
	{
		int num = int.Parse (numberOfMessagesInputField.text);

		for (int i = 0; i < num; i++) {

			Dictionary<string, object> data = new Dictionary<string, object> () {
				{ "TestMessage", "I am the common model of a modern major general" },
				{ "Continued Message", "I've information vegetable, animal, and mineral" },
			};

			AtomicNet.instance.SendTCPMessageToOthersInPool (sendToPoolInputField.text, data, AtomicNetLib.PriorityChannel.ALL_COST_CHANNEL, (string error) => {
				if (!string.IsNullOrEmpty (error)) {
					Debug.LogError (error);
					return;
				}
			});
		}
	}

#endregion
}
