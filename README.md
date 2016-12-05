# AtomicNet.io-UNITY
This repo contains the library files and example projects for the AtomicNet.io client for Unity3D.
_Please note:_ AtomicNet.io is in early alpha. If you've made it here, you're one of the first to read this.

# What is this?
AtomicNet.io is a new networking as a service system provided by No, You Shut Up Inc. In short, AtomicNet.io provides a simple flexible way to network games and applications together seamlessly over the world. You can send TCP or UDP traffic and keep your existing paradigms. We do not want you to write your server and client code in our language, we just want to get your traffic from one device to another. This is the Unity3D client connector repo. More client connectors will be available soon.

## Getting Started
In order to use AtomicNet.io you must first create a project at http://atomicnet.io 

Once your project has been created and your options selected, open AtomicNet.cs and edit the following values:

```csharp
public const string kApiKey = "YOUR_API_KEY_HERE";
public const string kProjectId = "YOUR_PROJECT_ID_HERE";
```

## Starting the Client
The first thing that is required is to initialize the client. This will set up the TCP and UDP sockets as well as other configuration events. In this example project AtomicNet.cs has been created as a singleton and can be access via: 

```csharp
AtomicNet.instance.StartAtomicNetClient ();
```

_Please note:_ If you Stop the AtomicNet client you *must* call the StartAtomicNetClient method again

## Anatomy of a Network Message
Network Messages are a Dictionary type object with a string for the key and an object for the value:

```csharp
Dictionary<string, object> netMsg = new Dictionary<string, object> ();
```

Adding data to the dictionary can be done when the object is created or later on before being sent:
```csharp
Dictionary<string, object> netMsg = new Dictionary<string, object> () {
  { "foo", "bar" },
};

netMsg.add("foo", "bar");
```

## Check for Messages
There are three message queues available to the client:
* Server Messages
* Client Messages
* ConnId Message (These are messages that have been directly sent to this client only)

If a message is available it can be dequeued and handled by your code. 

```csharp
Dictionary<string, object> serverMessage = AtomicNet.instance.CheckForServerMessages ();
if (serverMessage != null) {
  ProcessNetworkServerMessage (serverMessage);
}
```

## Threading and the Main Thread
AtomicNet handles messages on background threads, which is fine for most functionality where data is being mutated. However, Unity MonoBehaviors must be called on the main thread. Calling any MonoBehavior off the main thread causes many issues and unexpected behavior. The example code has a method called RunOnMainThread in the NetworkManager.cs that can be used whenever a MonoBehavior action is required. (i.e. Instantiating objects, using CoRoutines, moving transforms, etc)

## AtomicNet Connection Pools
AtomicNet works by putting your clients into connection pools with each other. Clients can communicate with any other client in their connection pool. AtomicNet provides you with the following control methods:

* MoveToPool
This command is used for your 'main' pool. All clients can be in a number of different connection pools at a single time, but there can only ever be a single main pool. Use MoveToPool to enter your main pool.

* AddToPool
This command is used to add this client to additional connection pools outside of their main pool.

* LeavePool
This command is used to leave connection pools

* SetConnectionAsPoolMaster
This will set this connection as the poolMaster of the pool.

## Pool Masters
A Pool Master is the leader of the pool. You game server should set itself as the pool master in order to get all of the pool master messages.
