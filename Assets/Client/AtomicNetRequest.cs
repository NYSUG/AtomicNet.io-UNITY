#define DEV_MODE

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace NYSU {

    public class AtomicNetRequest
    {
        private const string kBaseDevURL = "http://localhost:8080";
		private const string kBaseProdURL = "https://atomicnet.io";
        private const string kAuthenticationEndpoint = "/api/authenticate";
        private const string kCreatePoolEndpoint = "/api/createPool";
        private const string kJoinPoolEndpoint = "/api/joinPool";
        private const string kRemovePoolEndpoint = "/api/removePool";
        private const string kGetPoolsEndpoint = "/api/getPools";

        public static void GetPools (AtomicUtils.DictionaryCallbackType callback)
        {
#if DEV_MODE
			_GetData (string.Format ("{0}{1}", kBaseDevURL, kGetPoolsEndpoint), callback);
#else 
			_GetData (string.Format ("{0}{1}", kBaseProdURL, kGetPoolsEndpoint), callback);
#endif
        }

		public static void CreatePool (string poolName, string poolType, string gameId, AtomicUtils.DictionaryCallbackType callback)
        {
            Dictionary<string, object> body = new Dictionary<string, object> () {
                { "poolName", poolName },
				{ "poolType", poolType },
                { "gameId", gameId },
            };

#if DEV_MODE
			_PostData (string.Format ("{0}{1}", kBaseDevURL, kCreatePoolEndpoint), body, callback);
#else
			_PostData (string.Format ("{0}{1}", kBaseProdURL, kCreatePoolEndpoint), body, callback);
#endif
        }

        private static void _GetData (string endpoint, AtomicUtils.DictionaryCallbackType callback)
        {
            try
            {
                var webRequest = System.Net.WebRequest.Create(endpoint);

                webRequest.Method = "GET";
                webRequest.Timeout = 20000;
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add ("token", AtomicNet.kApiKey);
                webRequest.Headers.Add ("projectid", AtomicNet.kProjectId);

				var httpResponse = (HttpWebResponse)webRequest.GetResponse ();
				using (var streamReader = new StreamReader (httpResponse.GetResponseStream ())) {
					var responseText = streamReader.ReadToEnd ();

					Debug.Log (responseText);

					Dictionary<string, object> result = (Dictionary<string, object>)MiniJSON.Json.Deserialize (responseText);

					callback (string.Empty, result); 
				}
            }
            catch (Exception ex) {
                callback (ex.ToString (), null);
            }
        }

        private static void _PostData (string endpoint, Dictionary<string, object> data, AtomicUtils.DictionaryCallbackType callback)
        {
            try {

                var webRequest = (HttpWebRequest)WebRequest.Create (endpoint);
                webRequest.ContentType = "application/json";
                webRequest.Method = "POST";
                webRequest.Headers.Add ("token", AtomicNet.kApiKey);
                webRequest.Headers.Add ("projectid", AtomicNet.kProjectId);

                using (var streamWriter = new StreamWriter (webRequest.GetRequestStream ())) {

                    streamWriter.Write (MiniJSON.Json.Serialize (data));
                    streamWriter.Flush ();
                }

                var httpResponse = (HttpWebResponse)webRequest.GetResponse ();
                using (var streamReader = new StreamReader (httpResponse.GetResponseStream ())) {
                    var responseText = streamReader.ReadToEnd ();

                    Debug.Log (responseText);

                    Dictionary<string, object> result = (Dictionary<string, object>)MiniJSON.Json.Deserialize (responseText);

                    callback (string.Empty, result); 
                }
            } catch (WebException ex) {
                callback (ex.Message, null);
            }
        }
    }
}
