using System.Collections.Generic;

public class AtomicUtils 
{
	
#region Generic Callback Definitions

	public delegate void AsynchronousProcedureCallbackType (string error);
	public delegate void IntCallbackType (string error,int data);
	public delegate void StringCallbackType (string error,string data);
	public delegate void GenericObjectsCallbackType (string error,List<object> objects);
	public delegate void GenericObjectCallbackType (string error,object obj);
	public delegate void DictionaryCallbackType (string error,Dictionary <string, object> data);
	public delegate void NetworkDataRequestCallbackType (string error, string guid, List<Dictionary<string, object>> objects);

#endregion

}
