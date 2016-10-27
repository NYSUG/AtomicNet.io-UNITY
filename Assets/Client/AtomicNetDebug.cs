using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Collections;

public class AtomicNetDebug : MonoBehaviour {

	public Text connId;
	public Text rtt;
	public Text mainPool;
	public Text pools;

	private AtomicNet _atomicNet;

	private void Awake ()
	{
		Assert.IsNotNull (connId, string.Format ("{0}: connId has not been set in the inspector", this.name));
		Assert.IsNotNull (rtt, string.Format ("{0}: rtt has not been set in the inspector", this.name));
		Assert.IsNotNull (mainPool, string.Format ("{0}: mainPool has not been set in the inspector", this.name));
		Assert.IsNotNull (pools, string.Format ("{0}: pools has not been set in the inspector", this.name));
		Assert.IsNotNull (this.GetComponent<AtomicNet> (), string.Format ("AtomicNet has not been attached to this gameObject", this.name));

		_atomicNet = this.GetComponent<AtomicNet> ();
	}

	private void Update ()
	{
		connId.text = _atomicNet.GetConnId ().ToString ();
		rtt.text = _atomicNet.GetRtt ().ToString ();
		mainPool.text = _atomicNet.GetMainPool ();

		string text = string.Empty;
		foreach (string s in _atomicNet.GetAllPools ()) {
			text = string.Format ("{0}, {1}", text, s);
		}

		pools.text = text;
	}
}
