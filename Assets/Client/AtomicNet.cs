#define TCPON
#define UDP

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AtomicNet : MonoBehaviour {

	public const string kAtomicNet = "NYSU/AtomicNet";

#region Singleton

	// singleton instance
	private static AtomicNet _instance = null;

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

	private static AtomicNet CreateSingleton ()
	{
		GameObject go = Instantiate (Resources.Load (kAtomicNet), Vector3.zero, Quaternion.identity) as GameObject;
		go.name = "AtomicNet";

		DontDestroyOnLoad (go);

		return go.GetComponent<AtomicNet> ();
	}

	public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action> ();

	private AtomicNetLib _atomicNetLib = new AtomicNetLib ();

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
	}

#endregion

	private void OnApplicationQuit ()
	{
		_atomicNetLib.Disconnect ();

		_atomicNetLib.ShutdownTCPClients ();
	}

#region Start / Stop / Connect Client

	public void StartAtomicNetClient ()
	{
		// Sanity Check
		if (_atomicNetLib.isStarted) {
			Debug.LogWarning ("Unable to start client: Client is already started");
			return;
		}

		// Init AtomicNetLib
		_atomicNetLib.Init ();
	}

	public void StopAtomicNetClient ()
	{
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

	public void Connect (string ipAddress, int sendPort, int readPort, int udpPort, TBUtils.GenericObjectCallbackType callback)
	{
		_atomicNetLib.Connect (ipAddress, sendPort, readPort, udpPort, callback);
	}

#endregion

#region Info

	public int GetConnId ()
	{
		return _atomicNetLib.connId;
	}

	public int GetRtt ()
	{
		return _atomicNetLib.rtt;
	}

#endregion

#region Check for Messages

	public Dictionary<string, object> CheckForServerMessages ()
	{
		return _atomicNetLib.serverMessages.Count > 0 ? _atomicNetLib.serverMessages.Dequeue () : null;
	}

	public Dictionary<string, object> CheckForClientMessages ()
	{
		return _atomicNetLib.clientMessages.Count > 0 ? _atomicNetLib.clientMessages.Dequeue () : null;
	}

	public Dictionary<string, object> CheckForConnMessages ()
	{
		return _atomicNetLib.connMessages.Count > 0 ? _atomicNetLib.connMessages.Dequeue () : null;
	}

#endregion

#region Control Messages

	public void AddToPool (string poolName, string poolType, TBUtils.GenericObjectCallbackType callback)
	{
		_atomicNetLib.AddToPoolMessage (poolName, poolType, callback);
	}

	public void MoveToPool (string poolName, string poolType, TBUtils.GenericObjectCallbackType callback)
	{
		_atomicNetLib.MoveToPoolMessage (poolName, poolType, callback);
	}

	public void LeavePool (string poolName, string poolType, TBUtils.GenericObjectCallbackType callback)
	{
		_atomicNetLib.LeavePoolMessage (poolName, poolType, callback);
	}

	public void SetConnectionAsPoolMaster (string poolName, TBUtils.GenericObjectCallbackType callback)
	{
		_atomicNetLib.SetConnectionAsPoolMasterMessage (poolName, callback);
	}

#endregion

#region Send Messages

	public void SendTCPMessageToConnId (int id, Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, TBUtils.AsynchronousProcedureCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToConnId, true);
		if (!netMsg.ContainsKey ("connId"))
			netMsg.Add ("connId", id);

		_atomicNetLib.SendTCPMessage (netMsg, priority, callback);
	}

	public void SendUDPMessageToConnId (int id, Dictionary<string, object> netMsg, TBUtils.AsynchronousProcedureCallbackType callback)
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

	public void SendTCPMessageToOthersInPool (string poolName, Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, TBUtils.AsynchronousProcedureCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToOthers, poolName);
		netMsg.Add ("connId", _atomicNetLib.connId);

		_atomicNetLib.SendTCPMessage (netMsg, priority, callback);
	}

	public void SendUDPMessageToOthersInPool (string poolName, Dictionary<string, object> netMsg, TBUtils.AsynchronousProcedureCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToOthers, poolName);
		netMsg.Add ("connId", _atomicNetLib.connId);

		_atomicNetLib.SendUDPMessage (netMsg, callback);
	}

	public void SendTCPMessageToPoolMaster (string poolName, Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, TBUtils.AsynchronousProcedureCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToPoolMaster, poolName);

		_atomicNetLib.SendTCPMessage (netMsg, priority, callback);
	}

	public void SendTCPMessageToPool (string poolName, Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, TBUtils.AsynchronousProcedureCallbackType callback)
	{
		if (!_atomicNetLib.isStarted) {
			Debug.LogError ("Can not send network message: Client is not connected");
			return;
		}

		netMsg.Add (AtomicNetLib.kSendToPool, poolName);

		_atomicNetLib.SendTCPMessage (netMsg, priority, callback);
	}

	public void SendUDPMessageToPool (string poolName, Dictionary<string, object> netMsg, TBUtils.AsynchronousProcedureCallbackType callback)
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
