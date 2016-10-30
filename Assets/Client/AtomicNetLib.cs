using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;
using DisruptorUnity3d;

/// <summary>
/// This will be compiled into a DLL
/// </summary>

namespace NYSU {

	public class AtomicNetLib {

		// Very special messages
		public const string kConnect            = "connect";
		public const string kAddPool            = "addPool";
		public const string kChangePool         = "changePool";
		public const string kMoveToPool         = "moveToPool";
		public const string kLeavePool			= "leavePool";
		public const string kPoolMaster         = "poolMaster";
		public const string kPoolType           = "poolType";
		public const string kFromPool           = "fromPool";

		public const string kSendToAll          = "sendToAll";
		public const string kSendToPool         = "sendToPool";
		public const string kSendToOthers       = "sendToOthers";
		public const string kSendToConnId       = "sendToConnId";
		public const string kSendToPoolMaster   = "sendToPoolMaster";
		public const string kPing               = "ping";

		public const string kAll 				= "all";
		public const string kMain				= "main";

		public const int kMaxQueueLength 		= 64;

		private const int kInitialMessageSize	= 8;
		private const int kQueueInitialPriority = 8;
		private const int kPingInterval			= 5000;
		private const int kDisconnectInterval	= 3000;
		private const int kDisconnectTimeAdd	= 15;

		public class NYSUNetworkMessage {
			public bool isMsgLength;
			public string lengthMsg;
			public Dictionary<string, object> msg = new Dictionary<string, object> ();
			public TBUtils.GenericObjectCallbackType callback;
		}

		public class CallbackHandle {
			public string type;
			public TBUtils.GenericObjectCallbackType callback;
			public string error;
			public object obj;
		}

		/// <summary>
		/// Messages intended for the server only
		/// </summary>
		public Queue<Dictionary<string, object>> serverMessages = new Queue<Dictionary<string, object>> ();

		/// <summary>
		/// Messages intended for clients only
		/// </summary>
		public Queue<Dictionary<string, object>> clientMessages = new Queue<Dictionary<string, object>> ();

		/// <summary>
		/// Messages intended for this client only
		/// </summary>
		public Queue<Dictionary<string, object>> connMessages = new Queue<Dictionary<string, object>> ();

		// Connection info
		public int connId = 0;

		// Round Trip time
		public int rtt = 0;
		public bool isStarted = false;
		public bool isConnected = false;

		// TCP Sockets
		private TcpClient _readClient = null;
		private TcpClient _sendClient = null;

		// UDP Socket
		private UdpClient _udpClient = null;

		// TCP Network Streams
		private NetworkStream _readStream = default(NetworkStream);
		private NetworkStream _sendStream = default(NetworkStream);

		// Worker Threads
		private Thread _readThread;
		private Thread _sendThread;
		private Thread _udpThread;

		// Thread AR bits
		private AutoResetEvent readAutoResetEvent = new AutoResetEvent (false);
		private AutoResetEvent sendAutoResetEvent = new AutoResetEvent (false);

		// Locks
		private System.Object _readStreamLock = new System.Object();
		private System.Object _sendStreamLock = new System.Object();

		// Timers
		private System.Timers.Timer _pingTimer;
		private System.Timers.Timer _disconnectCheckTimer;

		// Helpers
		private bool _readyToSendMessages;
		private DateTime _lastPingReceived;

		// Mapping of requests this client has made
		private Dictionary<string, TBUtils.GenericObjectCallbackType> _callbackMappings = new Dictionary<string, TBUtils.GenericObjectCallbackType> ();

		// Callback Handles for completed tasks
		public Queue<CallbackHandle> callbackHandles = new Queue<CallbackHandle> ();

		// Channel Info
		public enum PriorityChannel {
			ALL_COST_CHANNEL = 0,
			STATE_UPDATE_CHANNEL = 1,
			RELIABLE_CHANNEL = 2,
			UNRELIABLE_CHANNEL = 3,
		}

		// Queues
		private static Dictionary<PriorityChannel, RingBuffer<NYSUNetworkMessage>> _sendMessageQueues = new Dictionary<PriorityChannel, RingBuffer<NYSUNetworkMessage>> ();

		// Message Frame
		private int _readMsgSize = kInitialMessageSize;

		// Concactinated Received Message
		private string _msg = string.Empty;

		// Queue Starvation prevention
		private int queuePriority = kQueueInitialPriority;

		// Pools
		private Dictionary<string, string> _poolMembership = new Dictionary<string, string> ();

		/// <summary>
		/// This Init method also doubles as a reset method
		/// </summary>
		public void Init ()
		{
			// Clear the message Queues
			_sendMessageQueues.Clear ();

			// Set up the message Queues
			_sendMessageQueues.Add (PriorityChannel.ALL_COST_CHANNEL, new RingBuffer<NYSUNetworkMessage> (kMaxQueueLength));
			_sendMessageQueues.Add (PriorityChannel.STATE_UPDATE_CHANNEL, new RingBuffer<NYSUNetworkMessage> (kMaxQueueLength));
			_sendMessageQueues.Add (PriorityChannel.RELIABLE_CHANNEL, new RingBuffer<NYSUNetworkMessage> (kMaxQueueLength));
			_sendMessageQueues.Add (PriorityChannel.UNRELIABLE_CHANNEL, new RingBuffer<NYSUNetworkMessage> (kMaxQueueLength));

			// Clear the pool membership
			_poolMembership.Clear ();

			// Add the first pools
			_poolMembership.Add (kAll, kAll);
			_poolMembership.Add (kMain, string.Empty);

			// Ensure all connections are reset
			ShutdownTCPClients ();
			Disconnect ();

			// Set up our sockets
			if (_readClient == null) {
				_readClient = new TcpClient ();
			}

			if (_sendClient == null) {
				_sendClient = new TcpClient ();
			}

			if (_udpClient == null) {
				_udpClient = new UdpClient ();
			}

			// Set up our threads
			_readThread = new Thread (new ThreadStart (_ReadNetworkMessage));
			_sendThread = new Thread (new ThreadStart (_SendNetworkMessage));
			_udpThread = new Thread (new ThreadStart (_ReadUDPMessage));

			// Set up our timers
			_pingTimer = new System.Timers.Timer (kPingInterval);
			_pingTimer.Elapsed += new System.Timers.ElapsedEventHandler (_pingElapsed);

			_disconnectCheckTimer = new System.Timers.Timer (kDisconnectInterval);
			_disconnectCheckTimer.Elapsed += new System.Timers.ElapsedEventHandler (_disconnectCheckElapsed);

			// Set started to true
			isStarted = true;
		}

		public string GetMainPool ()
		{
			return _poolMembership.ContainsKey (kMain) ? _poolMembership[kMain] : string.Empty;
		}

		public List<string> GetAllPools ()
		{
			List<string> retval = new List<string> ();
			foreach (KeyValuePair<string, string> entry in _poolMembership) {
				retval.Add (entry.Value);
			}

			return retval;
		}

		public void Connect (string ipAddress, int sendPort, int readPort, int udpPort, TBUtils.GenericObjectCallbackType callback)
		{
			try {
				// Connect the read socket
				_readClient.Connect (ipAddress, readPort);
				_readClient.Client.SetSocketOption (SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

				// Connect the write socket
				_sendClient.Connect (ipAddress, sendPort);
				_sendClient.Client.SetSocketOption (SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

				// Connect the UDP send socket
				_udpClient.Connect (new IPEndPoint (IPAddress.Parse (ipAddress), udpPort));
				_udpClient.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

				// Create our network streams
				_sendStream = _sendClient.GetStream ();
				_readStream = _readClient.GetStream ();

				// Start the processing threads
				_readThread.Start ();
				_sendThread.Start ();
				_udpThread.Start ();

				// Start the timers
				_pingTimer.Enabled = true;
//				_disconnectCheckTimer.Enabled = true;

				// Set the last ping
				_lastPingReceived = DateTime.Now;

				// Set our connection status
				isConnected = true;

				// Add a connection callback
				_callbackMappings.Add (kConnect, callback);

			} catch (System.Exception e) {
				callback (e.ToString (), null);
			}
		}

		public void Disconnect ()
		{
			isStarted = false;
			isConnected = false;

			if (_pingTimer != null) {
				_pingTimer.Enabled = false;
				_disconnectCheckTimer.Enabled = false;
			}

			_pingTimer = null;
			_disconnectCheckTimer = null;

			// Abort the threads
			if (_readThread != null) {
				_readThread.Abort();
			}

			if (_sendThread != null) {
				_sendThread.Abort(); 
			}

			if (_udpThread != null) {
				_udpThread.Abort();
			}

			if (_readClient != null) {
				// Leave our TCP clients open until we join a new game or exit the game.
				// Closing them immediately when leaving a match was causing issues with retreving data from www calls.
				if (_readClient.Client.Connected)
				{
					_readClient.Client.Shutdown(SocketShutdown.Both);
					_readClient.Client.Disconnect(true);
				}
			}

			if (_sendClient != null) {
				if (_sendClient.Client.Connected)
				{
					_sendClient.Client.Shutdown(SocketShutdown.Both);
					_sendClient.Client.Disconnect(true);
				}
			}

				
			if (_udpClient != null)
			{
				try
				{
					_udpClient.Close();
					_udpClient = null;
				}
				catch (System.Exception e)
				{
					Console.WriteLine (e.ToString ());
				}
			}

			// Clear out any unsent messages
			foreach (KeyValuePair<PriorityChannel, RingBuffer<NYSUNetworkMessage>> entry in _sendMessageQueues)
			{

				for (int i = 0; i < entry.Value.Count; i++)
				{
					NYSUNetworkMessage m;
					entry.Value.TryDequeue(out m);

					// Don't send it, just dequeue it
				}
			}
		}

		public void ShutdownTCPClients () 
		{
			if (_readClient != null) {

				if (_readClient.Connected) {
					_readClient.GetStream ().Close ();
					_readClient.Client.Shutdown (SocketShutdown.Both);
					_readClient.Close ();
				}

				_readClient = null;
			}

			if (_sendClient != null) {

				if (_sendClient.Connected) {
					_readClient.GetStream ().Close ();
					_sendClient.Client.Shutdown (SocketShutdown.Both);
					_sendClient.Close ();
				}
				_sendClient = null;
			}
		}

#region Message Receiving

		private void _ReadNetworkMessage ()
		{
			while (true) {

				if (!isStarted) {
					Thread.Sleep(500);
					return;
				}

				if (!_readStream.CanRead) {
					Console.WriteLine ("thread can not read");
					Thread.Sleep(1);
					return;
				}

				if (_readStream.DataAvailable) {

					// Read bytes
					_readStream = _readClient.GetStream ();
					byte[] message = new byte[_readMsgSize];
					int bytesRead = 0;
					int byteOffset = _readMsgSize;

					UTF8Encoding encoder = new UTF8Encoding ();

					lock (_readStreamLock) 
					{
						_readStream.BeginRead (message, 0, _readMsgSize, ((IAsyncResult ar) => {

							bytesRead += _readStream.EndRead (ar);

							if (bytesRead == _readMsgSize) {

								_msg = string.Format ("{0}{1}", _msg, encoder.GetString (message, 0, byteOffset));

								if (_msg.Substring (0, 3) == "MSG") {

									_readMsgSize = int.Parse (_msg.Substring (3, _msg.Length - 3));
								} else {

									_readMsgSize = kInitialMessageSize;

									if (string.IsNullOrEmpty (_msg)) {
										Console.WriteLine ("netMsg is null!");
									} else {
										_ProcessMsg (string.Copy (_msg));
										_msg = string.Empty;
									}
								}

								_msg = string.Empty;

							} else if (bytesRead < _readMsgSize) {

								_msg = string.Format ("{0}{1}", _msg, encoder.GetString (message, 0, bytesRead));

								byteOffset = _readMsgSize - bytesRead;
								_readMsgSize = byteOffset;

							} else {
								Console.WriteLine (string.Format ("OVERFLOW: {0}/{1} -- {2}", bytesRead, _readMsgSize, _msg));
							}

							readAutoResetEvent.Set ();

						}), _readStream);
					}
					readAutoResetEvent.WaitOne ();
				} else {
					Thread.Sleep (100);
				}
			}
		}
			
		private void _ReadUDPMessage ()
		{
			while (true) {

				if (!isStarted)
				{
					Thread.Sleep(500);
					return;
				}

				try {

					IPEndPoint anyIP = new IPEndPoint (IPAddress.Any, 0);
					byte[] data = _udpClient.Receive(ref anyIP);

					string msg = Encoding.UTF8.GetString (data);

					_ProcessMsg (msg);

				} catch (Exception e) {
					if (!(e is ThreadAbortException)) {

						// Force a disconnect
						Disconnect ();
					}
				}
			}
		}

		private void _ProcessMsg (string msg)
		{
			Dictionary<string, object> data = (Dictionary<string, object>)MiniJSON.Json.Deserialize (msg);

			_ReceiveMessage (data);
		}

#endregion

#region Message Sending

		private void _SendNetworkMessage ()
		{
			while (true) {

				if (!isStarted) {
					Thread.Sleep(500);
					return;
				}

				if (!_sendStream.CanWrite) {
					Console.WriteLine ("Socket can not write, sleeping");

					Thread.Sleep(1);
					return;
				}

				if (_readyToSendMessages) {

					// Process the queues in their priority
					if (_sendMessageQueues[PriorityChannel.ALL_COST_CHANNEL].Count > 0) {

						NYSUNetworkMessage msg;
						var dequeued = _sendMessageQueues[PriorityChannel.ALL_COST_CHANNEL].TryDequeue (out msg);

						if (dequeued)
							_sendDequedNetworkMessage (msg);

					} else if (_sendMessageQueues[PriorityChannel.STATE_UPDATE_CHANNEL].Count > 0 && queuePriority > 5) {

						NYSUNetworkMessage msg;
						var dequeued = _sendMessageQueues[PriorityChannel.STATE_UPDATE_CHANNEL].TryDequeue (out msg);

						if (dequeued)
							_sendDequedNetworkMessage (msg);

					} else if (_sendMessageQueues[PriorityChannel.RELIABLE_CHANNEL].Count > 0 && queuePriority > 3) {

						NYSUNetworkMessage msg;
						var dequeued = _sendMessageQueues[PriorityChannel.RELIABLE_CHANNEL].TryDequeue (out msg);

						if (dequeued)
							_sendDequedNetworkMessage (msg);

					} else if (_sendMessageQueues[PriorityChannel.UNRELIABLE_CHANNEL].Count > 0) {

						NYSUNetworkMessage msg;
						var dequeued = _sendMessageQueues[PriorityChannel.UNRELIABLE_CHANNEL].TryDequeue (out msg);

						if (dequeued)
							_sendDequedNetworkMessage (msg);

					} else {
						queuePriority = kQueueInitialPriority;
					}
				}
			}
		}

		private void _sendDequedNetworkMessage (NYSUNetworkMessage networkMessage)
		{
			if (networkMessage == null) {
				Console.WriteLine ("netMsg to SEND is null!");
				return;
			}

			lock (_sendStreamLock) {

				string msg;
				byte[] outStream;

				// Send the length message
				msg = networkMessage.lengthMsg;
				outStream = Encoding.UTF8.GetBytes (msg);
				_sendStream.Write (outStream, 0, outStream.Length);

				// Send the message
				msg = MiniJSON.Json.Serialize (networkMessage.msg);
				outStream = Encoding.UTF8.GetBytes (msg);
				_sendStream.BeginWrite (outStream, 0, outStream.Length, (IAsyncResult ar) => {

					if (ar.IsCompleted) {

						_sendStream.EndWrite (ar);

						sendAutoResetEvent.Set ();

						if (networkMessage.callback == null)
							return;

						// Queue this to be handled by AtomicNet.cs
						callbackHandles.Enqueue (new CallbackHandle () {
							type = "sendNetworkMessage",
							callback = networkMessage.callback,
							error = string.Empty,
							obj = null,
						});
					}

				}, _sendStream);
			}
			sendAutoResetEvent.WaitOne ();
		}

#endregion

#region Control Messages

		/// <summary>
		/// A control message that will enroll this client to receive messages from this connection pool
		/// </summary>
		/// <param name="pool">Pool.</param>
		/// <param name="poolType">Pool type.</param>
		/// <param name="callback">Callback.</param>
		public void AddToPoolMessage (string pool, string poolType, TBUtils.GenericObjectCallbackType callback)
		{
			Dictionary<string, object> netMsg = new Dictionary<string, object> () {
				{ kAddPool, pool },
				{ kPoolType, poolType },
			};

			if (!_callbackMappings.ContainsKey (kAddPool)) {
				_callbackMappings.Add (kAddPool, callback);
			} else {
				_callbackMappings [kAddPool] = callback;
			}

			SendTCPMessage (netMsg, PriorityChannel.ALL_COST_CHANNEL, (string error, object obj) => {
				if (!string.IsNullOrEmpty (error)) {
					callback (error, null);
					return;
				}
			});
		}

		/// <summary>
		/// A control message that will change this client's main pool to the new pool and remove it from the previous main pool
		/// </summary>
		/// <param name="pool">Pool.</param>
		/// <param name="poolType">Pool type.</param>
		/// <param name="callback">Callback.</param>
		public void MoveToPoolMessage (string pool, string poolType, TBUtils.GenericObjectCallbackType callback)
		{
			Console.WriteLine ("MoveToPoolMessage");

			if (pool == _poolMembership[kMain]) {
				Console.WriteLine ("Can not move from pool to same pool");
				callback (string.Empty, null);
				return;
			}

			Dictionary<string, object> netMsg = new Dictionary<string, object> () {
				{ kFromPool, _poolMembership[kMain] },
				{ kChangePool, pool },
				{ kPoolType, poolType },
			};

			if (!_callbackMappings.ContainsKey (kMoveToPool)) {
				_callbackMappings.Add (kMoveToPool, callback);
			} else {
				_callbackMappings [kMoveToPool] = callback;
			}

			SendTCPMessage (netMsg, PriorityChannel.ALL_COST_CHANNEL, (string error, object obj) => {
				if (!string.IsNullOrEmpty (error)) {
					callback (error, null);
					return;
				}
			});
		}

		/// <summary>
		/// A control message that will remove this client from a connection pool. This method can not be performed on the main pool
		/// </summary>
		/// <param name="pool">Pool.</param>
		/// <param name="poolType">Pool type.</param>
		/// <param name="callback">Callback.</param>
		public void LeavePoolMessage (string pool, string poolType, TBUtils.GenericObjectCallbackType callback)
		{
			if (pool == _poolMembership[kMain]) {
				callback ("Unable to leave pool: You can not use Leave Pool command for your main pool. Use MoveToPool instead", null);
				return;
			}

			Console.WriteLine (string.Format ("Current pool is: {0} LEAVING pool: {1}", _poolMembership[kMain], pool));

			Dictionary<string, object> netMsg = new Dictionary<string, object> () {
				{ kLeavePool, pool },
				{ kPoolType, poolType },
			};

			SendTCPMessage (netMsg, PriorityChannel.ALL_COST_CHANNEL, (string error, object obj) => {
				if (!string.IsNullOrEmpty (error)) {
					callback (error, null);
					return;
				}

				// Did we leave our current pool?
				if (_poolMembership[kMain] == pool) {
					_poolMembership[kMain] = string.Empty;
				}

				callback(string.Empty, null);
			});
		}

		/// <summary>
		/// A control message that will set this client as the poolMaster for the given pool 
		/// </summary>
		/// <param name="pool">Pool.</param>
		/// <param name="callback">Callback.</param>
		public void SetConnectionAsPoolMasterMessage (string pool, TBUtils.GenericObjectCallbackType callback)
		{
			Dictionary<string, object> netMsg = new Dictionary<string, object> () {
				{ kPoolMaster, pool },
			};

			if (!_callbackMappings.ContainsKey ("poolMaster")) {
				_callbackMappings.Add ("poolMaster", callback);
			} else {
				_callbackMappings ["poolMaster"] = callback;
			}

			SendTCPMessage (netMsg, PriorityChannel.ALL_COST_CHANNEL, (string error, object obj) => {
				if (!string.IsNullOrEmpty (error)) {
					callback (error, null);
					return;
				}
			});
		}

#endregion

#region Message Handling

		private void _ReceiveMessage (Dictionary<string, object> netMsg)
		{
			if (netMsg == null) {
				Console.WriteLine ("netMsg was null");
				return;
			}

			// Connection message
			if (netMsg.ContainsKey (kConnect)) {

				// We got the message back that we're connected
				Console.WriteLine (string.Format ("Connected with connId: {0}", netMsg ["connId"]));

				connId = int.Parse (netMsg ["connId"].ToString ());

				// Send a message up the send socket, letting them know which connId we're associated with
				_SendConnIdMessage ();

				if (!_callbackMappings.ContainsKey (kConnect)) {
					Console.WriteLine ("We don't have a callback mapping for kConnect");
					return;
				}

				// Queue this to be handled by AtomicNet.cs
				callbackHandles.Enqueue (new CallbackHandle () {
					type = kConnect,
					callback = _callbackMappings[kConnect],
					error = string.Empty,
					obj = netMsg,
				});

				_callbackMappings.Remove (kConnect);
			}

			// Check for Network Relay Commands
			if (netMsg.ContainsKey (kChangePool)) {

				// The server has changed us to this pool
				_poolMembership[kMain] = netMsg [kChangePool].ToString ();

				Console.WriteLine (string.Format ("AtomicNetLib -> main pool membership: {0}", _poolMembership[kMain]));

				if (!_callbackMappings.ContainsKey (kMoveToPool)) {
					Console.WriteLine ("We don't have a callback mapping for kMoveToPool");
					return;
				}

				// Queue this to be handled by AtomicNet.cs
				callbackHandles.Enqueue (new CallbackHandle () {
					type = kMoveToPool,
					callback = _callbackMappings[kMoveToPool],
					error = string.Empty,
					obj = netMsg,
				});

				_callbackMappings.Remove (kMoveToPool);
			}

			if (netMsg.ContainsKey (kAddPool)) {

				if (_poolMembership.ContainsKey (netMsg[kAddPool].ToString ())) {
					Console.WriteLine (string.Format ("Error: we have been added to a pool we already have a membership with: {0}", netMsg[kAddPool]));
				} else {
					_poolMembership.Add (netMsg[kAddPool].ToString (), netMsg[kAddPool].ToString ());
					Console.WriteLine (string.Format ("AtomicNetLib -> pool membership added: {0}", _poolMembership[netMsg[kAddPool].ToString ()]));
				}

				// The server has changed us to this pool
				if (!_callbackMappings.ContainsKey (kAddPool)) {
					Console.WriteLine ("We don't have a callback mapping for kAddPool");
					return;
				}

				// Queue this to be handled by AtomicNet.cs
				callbackHandles.Enqueue (new CallbackHandle () {
					type = kAddPool,
					callback = _callbackMappings[kAddPool],
					error = string.Empty,
					obj = netMsg,
				});

				_callbackMappings.Remove (kAddPool);
			}

			if (netMsg.ContainsKey (kPoolMaster)) {

				Console.WriteLine (string.Format ("We have been set as the poolmaster of: {0}", netMsg[kPoolMaster]));

				if (!_callbackMappings.ContainsKey (kPoolMaster)) {
					Console.WriteLine ("We don't have a callback mapping for kPoolMaster");
					return;
				}

				// Queue this to be handled by AtomicNet.cs
				callbackHandles.Enqueue (new CallbackHandle () {
					type = kPoolMaster,
					callback = _callbackMappings[kPoolMaster],
					error = string.Empty,
					obj = netMsg,
				});

				_callbackMappings.Remove (kPoolMaster);
			}

			// Ping message
			if (netMsg.ContainsKey (kPing)) {

				DateTime then = DateTime.Parse (netMsg["time"].ToString ());
				TimeSpan span = DateTime.Now - then;

				// Set the rtt
				rtt = (int)span.TotalMilliseconds;

				// Used to determine if we are disconnected
				_lastPingReceived = DateTime.Now;
			}

			// This message is meant for the server
			if (netMsg.ContainsKey (kSendToPoolMaster)) {
				
				serverMessages.Enqueue (netMsg);
				return;
			}

			// This message is meant for this client only
			if (netMsg.ContainsKey (kSendToConnId)) {

				connMessages.Enqueue (netMsg);
				return;
			}

			// Pass the message onto the requested pool
			if (netMsg.ContainsKey (kSendToPool) || netMsg.ContainsKey (kSendToOthers)) {

				clientMessages.Enqueue (netMsg);
				return;
			}
		}

#endregion

#region Utility Messages

		private void _SendConnIdMessage ()
		{
			Dictionary<string, object> netMsg = new Dictionary<string, object> () {
				{ kConnect, true },
				{ "connId", connId },
			};

			string msg = MiniJSON.Json.Serialize (netMsg);
			byte[] outStream = System.Text.Encoding.UTF8.GetBytes(msg);

			_sendDequedNetworkMessage (new NYSUNetworkMessage () {
				isMsgLength = true,
				lengthMsg = string.Format ("MSG{0}", formattedOutStreamLength (outStream.Length)),
				msg = netMsg,
				callback = ConnIdSentCallback,
			});
				
			SendUDPMessage (netMsg, null);
		}

		private void ConnIdSentCallback (string error, object obj)
		{
			if (!string.IsNullOrEmpty (error)) {
				Console.WriteLine (error);
				return;
			}

			_readyToSendMessages = true;
		}

		private void _SendPing ()
		{
			// Send this back to ourselves
			Dictionary<string, object> netMsg = new Dictionary<string, object> () {
				{ kPing, true },
				{ "time", DateTime.Now },
				{ "connId", connId },
			};

			SendUDPMessage (netMsg, (string error, object obj) => {
				if (!string.IsNullOrEmpty (error)) {
					Console.WriteLine (error);
					return;
				}
			});
		}

		// Called from a timer
		private void _pingElapsed (object sender, System.Timers.ElapsedEventArgs e)
		{
			// Only run the following if we're connected to the relay server
			if (!isConnected || !_readyToSendMessages)
				return;

			_SendPing ();
		}

		// Called from a timer
		private void _disconnectCheckElapsed (object sender, System.Timers.ElapsedEventArgs e)
		{
			DateTime cutOffTime = _lastPingReceived.AddSeconds (kDisconnectTimeAdd);

			if (DateTime.Now > cutOffTime) {
				isConnected = false;
			}
		}

#endregion

		public void SendTCPMessage (Dictionary<string, object> netMsg, AtomicNetLib.PriorityChannel priority, TBUtils.GenericObjectCallbackType callback)
		{       
			string msg = MiniJSON.Json.Serialize (netMsg);
			byte[] outStream = Encoding.UTF8.GetBytes(msg);

			if (!_sendMessageQueues.ContainsKey (priority)) {
				Console.WriteLine (string.Format ("priority missing: {0} for msg {1}", priority, msg));
				return;
			}

			// Enqueue the message
			_sendMessageQueues[priority].Enqueue (new NYSUNetworkMessage () {
				isMsgLength = true,
				lengthMsg = string.Format ("MSG{0}", formattedOutStreamLength (outStream.Length)),
				msg = netMsg,
				callback = callback,
			});
		}
			
		public void SendUDPMessage (Dictionary<string, object> netMsg, TBUtils.GenericObjectCallbackType callback)
		{
			try {
				string msg = MiniJSON.Json.Serialize (netMsg);
				byte[] outStream = Encoding.UTF8.GetBytes(msg);

				_udpClient.BeginSend (outStream, outStream.Length, (IAsyncResult ar) => {

					if (ar.IsCompleted) {

						_udpClient.EndSend (ar);

						if (callback != null)
							callback (string.Empty, null);
					}

				}, _udpClient);
			} catch (SocketException e) {

				Console.WriteLine (e);

				// Force a disconnect
				Disconnect ();
			}
		}

		private string formattedOutStreamLength (int length) {
			if (length < 10) {
				return string.Format ("0000{0}", length);
			} else if (length < 100) {
				return string.Format ("000{0}", length);
			} else if (length < 1000) {
				return string.Format ("00{0}", length);
			} else if (length < 10000) {
				return string.Format ("0{0}", length);
			} else {
				return length.ToString ();
			}
		}
	}
}
