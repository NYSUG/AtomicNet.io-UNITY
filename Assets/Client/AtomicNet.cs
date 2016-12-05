using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using NYSU;

public class AtomicNet : MonoBehaviour {

	public const string kAtomicNetPrefab = "NYSU/AtomicNet";

#region Singleton

	// singleton instance
	private static AtomicNet _instance = null;

	/// <summary>
	/// Gets the instance.
	/// </summary>
	/// <value>The instance.</value>
	public static AtomicNet instance {
		get {
			if (_instance == null) {

				// Main thread creation
				_instance = CreateSingleton ();
				return _instance;
			}

			return _instance;
		}
	}

	/// <summary>
	/// Creates the singleton.
	/// </summary>
	/// <returns>The singleton.</returns>
	private static AtomicNet CreateSingleton ()
	{
		GameObject go = Instantiate (Resources.Load (kAtomicNetPrefab), Vector3.zero, Quaternion.identity) as GameObject;
		go.name = "AtomicNet";

		DontDestroyOnLoad (go);

		return go.GetComponent<AtomicNet> ();
	}

	// Main Thread Queue
	public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action> ();

	// AtomicNet Library
	private AtomicNetLib _atomicNetLib = new AtomicNetLib ();

	/// <summary>
	/// Update this instance.
	/// </summary>
	public void Update ()
	{
		// dispatch stuff on main thread
		while (ExecuteOnMainThread.Count > 0) {
			Action action = ExecuteOnMainThread.Dequeue ();
			if (action == null) {
				return;
			}

			action.Invoke ();
		}

		// Listen for callbacks
		while (_atomicNetLib.callbackHandles.Count > 0) {
			AtomicNetLib.CallbackHandle handle = _atomicNetLib.callbackHandles.Dequeue ();
			handle.callback (handle.error, handle.obj);
		}
	}

#endregion

	/// <summary>
	/// The game identifier. This can be hardcoded or set dynamically based on
	/// your games needs.
	/// </summary>
	public static string gameId = string.Empty;

	/// <summary>
	/// Raises the application quit event.
	/// </summary>
	private void OnApplicationQuit ()
	{
		_atomicNetLib.Disconnect ();

		_atomicNetLib.ShutdownTCPClients ();
	}

#region Start / Stop / Connect Client

	/// <summary>
	/// Starts the atomic net client.
	/// </summary>
	public void StartAtomicNetClient ()
	{
		Debug.Log ("Starting AtomicNet Client");

		// Sanity Check
		if (_atomicNetLib.isStarted) {
			Debug.LogWarning ("Unable to start client: Client is already started");
			return;
		}

		// Init AtomicNetLib
		_atomicNetLib.Init (kApiKey, kProjectId);
	}

	/// <summary>
	/// Stops the atomic net client.
	/// </summary>
	public void StopAtomicNetClient ()
	{
		Debug.Log ("Stopping AtomicNet Client");

		// Sanity Check
		if (!_atomicNetLib.isStarted) {
			Debug.LogWarning ("Unable to stop AtomicNet: Client is not started");
			return;
		}

		// Disconnect the sockets
		ExecuteOnMainThread.Enqueue (() => {
			_atomicNetLib.Disconnect ();
		});
	}

#endregion

#region Info

	/// <summary>
	/// Gets the conn identifier.
	/// </summary>
	/// <returns>The conn identifier.</returns>
	public int GetConnId ()
	{
		return _atomicNetLib.connId;
	}

	/// <summary>
	/// Gets the Round-Trip-Time.
	/// </summary>
	/// <returns>The rtt.</returns>
	public int GetRtt ()
	{
		return _atomicNetLib.rtt;
	}

	/// <summary>
	/// Determines whether this instance is connected.
	/// </summary>
	/// <returns><c>true</c> if this instance is connected; otherwise, <c>false</c>.</returns>
	public bool IsConnected ()
	{
		return _atomicNetLib.isConnected;
	}

    /// <summary>
    /// Determines whether this instance is pool master.
    /// </summary>
    /// <returns><c>true</c> if this instance is pool master; otherwise, <c>false</c>.</returns>
    public bool IsPoolMaster ()
    {
        return _atomicNetLib.isPoolMaster;
    }

	/// <summary>
	/// Gets the main connection pool.
	/// </summary>
	/// <returns>The main pool.</returns>
	public string GetMainPool ()
	{
		return _atomicNetLib.GetMainPool ();
	}

	/// <summary>
	/// Gets all connection pools.
	/// </summary>
	/// <returns>all connection pools.</returns>
	public List<string> GetAllPools ()
	{
		return _atomicNetLib.GetAllPools ();
	}

#endregion

#region Check for Messages

	/// <summary>
	/// Determines whether this instance has server messages.
	/// </summary>
	/// <returns><c>true</c> if this instance has server messages; otherwise, <c>false</c>.</returns>
	public bool HasServerMessages () 
	{
		return _atomicNetLib.serverMessages.Count > 0;
	}

	/// <summary>
	/// Checks for server messages.
	/// </summary>
	/// <returns>The for server messages.</returns>
	public Dictionary<string, object> CheckForServerMessages ()
	{
		return _atomicNetLib.serverMessages.Count > 0 ? _atomicNetLib.serverMessages.Dequeue () : null;
	}

	/// <summary>
	/// Determines whether this instance has client messages.
	/// </summary>
	/// <returns><c>true</c> if this instance has client messages; otherwise, <c>false</c>.</returns>
	public bool HasClientMessages ()
	{
		return _atomicNetLib.clientMessages.Count > 0;
	}

	/// <summary>
	/// Checks for client messages.
	/// </summary>
	/// <returns>The for client messages.</returns>
	public Dictionary<string, object> CheckForClientMessages ()
	{
		return _atomicNetLib.clientMessages.Count > 0 ? _atomicNetLib.clientMessages.Dequeue () : null;
	}

	/// <summary>
	/// Determines whether this instance has conn messages.
	/// </summary>
	/// <returns><c>true</c> if this instance has conn message; otherwise, <c>false</c>.</returns>
	public bool HasConnMessages ()
	{
		return _atomicNetLib.connMessages.Count > 0;
	}

	/// <summary>
	/// Checks for messages sent directly to this connId.
	/// </summary>
	/// <returns>The for conn messages.</returns>
	public Dictionary<string, object> CheckForConnMessages ()
	{
		return _atomicNetLib.connMessages.Count > 0 ? _atomicNetLib.connMessages.Dequeue () : null;
	}

#endregion

#region Control Messages

	/// <summary>
	/// Adds to pool.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="poolType">Pool type.</param>
	/// <param name="callback">Callback.</param>
    public void AddToPool (string poolName, string poolType, string gameId, AtomicUtils.GenericObjectCallbackType callback)
	{
		_atomicNetLib.AddToPoolMessage (poolName, poolType, gameId, callback);
	}

	/// <summary>
	/// Moves to pool.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="poolType">Pool type.</param>
	/// <param name="callback">Callback.</param>
    public void MoveToPool (string poolName, string poolType, string gameId, AtomicUtils.GenericObjectCallbackType callback)
	{
        _atomicNetLib.MoveToPoolMessage (poolName, poolType, gameId, callback);
	}

	/// <summary>
	/// Leaves the pool.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="poolType">Pool type.</param>
	/// <param name="callback">Callback.</param>
	public void LeavePool (string poolName, string poolType, string gameId, AtomicUtils.GenericObjectCallbackType callback)
	{
		_atomicNetLib.LeavePoolMessage (poolName, poolType, gameId, callback);
	}

	/// <summary>
	/// Sets the connection as pool master.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="callback">Callback.</param>
    public void SetConnectionAsPoolMaster (string poolName, AtomicUtils.GenericObjectCallbackType callback)
	{
		_atomicNetLib.SetConnectionAsPoolMasterMessage (poolName, callback);
	}

#endregion

#region Request Information

	public void FindConnectionPools (AtomicUtils.DictionaryCallbackType callback)
	{
		AtomicNetRequest.GetPools (callback);
	}

#endregion

#region Send Messages

	/// <summary>
	/// Sends the TCP message to conn identifier.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="netMsg">Net message.</param>
	/// <param name="priority">Priority.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="requestReceipt">If set to <c>true</c> request receipt.</param>
	public void SendTCPMessageToConnId (int id, Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, AtomicUtils.GenericObjectCallbackType callback, bool requestReceipt = false)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToConnId, true);
		if (!netMsg.ContainsKey ("connId"))
			netMsg.Add ("connId", id);

		_atomicNetLib.SendTCPMessage (netMsg, priority, callback, requestReceipt);
	}

	/// <summary>
	/// Sends the UDP message to conn identifier.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="netMsg">Net message.</param>
	/// <param name="callback">Callback.</param>
	public void SendUDPMessageToConnId (int id, Dictionary<string, object> netMsg, AtomicUtils.GenericObjectCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToConnId, true);
		if (!netMsg.ContainsKey ("connId"))
			netMsg.Add ("connId", id);

		_atomicNetLib.SendUDPMessage (netMsg, callback);
	}

	/// <summary>
	/// Sends the TCP message to others in pool.
	/// </summary>
	/// <param name="netMsg">Net message.</param>
	/// <param name="priority">Priority.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="requestReceipt">If set to <c>true</c> request receipt.</param>
	public void SendTCPMessageToOthersInPool (Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, AtomicUtils.GenericObjectCallbackType callback, bool requestReceipt = false)
	{
		SendTCPMessageToOthersInPool (GetMainPool (), netMsg, priority, callback, requestReceipt);
	}

	/// <summary>
	/// Sends the TCP message to others in pool.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="netMsg">Net message.</param>
	/// <param name="priority">Priority.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="requestReceipt">If set to <c>true</c> request receipt.</param>
	public void SendTCPMessageToOthersInPool (string poolName, Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, AtomicUtils.GenericObjectCallbackType callback, bool requestReceipt = false)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToOthers, poolName);
		netMsg.Add ("connId", _atomicNetLib.connId);

		_atomicNetLib.SendTCPMessage (netMsg, priority, callback, requestReceipt);
	}

	/// <summary>
	/// Sends the UDP message to others in pool.
	/// </summary>
	/// <param name="netMsg">Net message.</param>
	/// <param name="callback">Callback.</param>
	public void SendUDPMessageToOthersInPool (Dictionary<string, object> netMsg, AtomicUtils.GenericObjectCallbackType callback)
	{
		SendUDPMessageToOthersInPool (GetMainPool (), netMsg, callback);
	}

	/// <summary>
	/// Sends the UDP message to others in pool.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="netMsg">Net message.</param>
	/// <param name="callback">Callback.</param>
	public void SendUDPMessageToOthersInPool (string poolName, Dictionary<string, object> netMsg, AtomicUtils.GenericObjectCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToOthers, poolName);
		netMsg.Add ("connId", _atomicNetLib.connId);

		_atomicNetLib.SendUDPMessage (netMsg, callback);
	}

	/// <summary>
	/// Sends the TCP message to pool master.
	/// </summary>
	/// <param name="netMsg">Net message.</param>
	/// <param name="priority">Priority.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="requestReceipt">If set to <c>true</c> request receipt.</param>
	public void SendTCPMessageToPoolMaster (Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, AtomicUtils.GenericObjectCallbackType callback, bool requestReceipt = false)
	{
		SendTCPMessageToPoolMaster (GetMainPool (), netMsg, priority, callback, requestReceipt);
	}

	/// <summary>
	/// Sends the TCP message to pool master.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="netMsg">Net message.</param>
	/// <param name="priority">Priority.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="requestReceipt">If set to <c>true</c> request receipt.</param>
	public void SendTCPMessageToPoolMaster (string poolName, Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, AtomicUtils.GenericObjectCallbackType callback, bool requestReceipt = false)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToPoolMaster, poolName);

		_atomicNetLib.SendTCPMessage (netMsg, priority, callback, requestReceipt);
	}

	/// <summary>
	/// Sends the UDP message to pool master.
	/// </summary>
	/// <param name="netMsg">Net message.</param>
	/// <param name="callback">Callback.</param>
	public void SendUDPMessageToPoolMaster (Dictionary<string, object> netMsg, AtomicUtils.GenericObjectCallbackType callback)
	{
		SendUDPMessageToPoolMaster (GetMainPool (), netMsg, callback);
	}

	/// <summary>
	/// Sends the UDP message to pool master.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="netMsg">Net message.</param>
	/// <param name="callback">Callback.</param>
	public void SendUDPMessageToPoolMaster (string poolName, Dictionary<string, object> netMsg, AtomicUtils.GenericObjectCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToPoolMaster, poolName);

		_atomicNetLib.SendUDPMessage (netMsg, callback);
	}

	/// <summary>
	/// Sends the TCP message to pool.
	/// </summary>
	/// <param name="netMsg">Net message.</param>
	/// <param name="priority">Priority.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="requestReceipt">If set to <c>true</c> request receipt.</param>
	public void SendTCPMessageToPool (Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, AtomicUtils.GenericObjectCallbackType callback, bool requestReceipt = false)
	{
		SendTCPMessageToPool (GetMainPool (), netMsg, priority, callback, requestReceipt);
	}

	/// <summary>
	/// Sends the TCP message to pool.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="netMsg">Net message.</param>
	/// <param name="priority">Priority.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="requestReceipt">If set to <c>true</c> request receipt.</param>
	public void SendTCPMessageToPool (string poolName, Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, AtomicUtils.GenericObjectCallbackType callback, bool requestReceipt = false)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToPool, poolName);

		_atomicNetLib.SendTCPMessage (netMsg, priority, callback, requestReceipt);
	}

	/// <summary>
	/// Sends the UDP message to pool.
	/// </summary>
	/// <param name="netMsg">Net message.</param>
	/// <param name="callback">Callback.</param>
	public void SendUDPMessageToPool (Dictionary<string, object> netMsg, AtomicUtils.GenericObjectCallbackType callback)
	{
		SendUDPMessageToPool (GetMainPool (), netMsg, callback);
	}

	/// <summary>
	/// Sends the UDP message to pool.
	/// </summary>
	/// <param name="poolName">Pool name.</param>
	/// <param name="netMsg">Net message.</param>
	/// <param name="callback">Callback.</param>
	public void SendUDPMessageToPool (string poolName, Dictionary<string, object> netMsg, AtomicUtils.GenericObjectCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToPool, poolName);

		_atomicNetLib.SendUDPMessage (netMsg, callback);
	}

#endregion

}
