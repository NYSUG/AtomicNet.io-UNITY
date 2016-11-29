using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace NYSU {

    public class AtomicNetRequest
    {
        private const string kBaseURL = "http://localhost:8080";
        private const string kAuthenticationEndpoint = "/api/authenticate";
        private const string kCreatePoolEndpoint = "/api/createPool";
        private const string kJoinPoolEndpoint = "/api/joinPool";
        private const string kRemovePoolEndpoint = "/api/removePool";
        private const string kGetPoolsEndpoint = "/api/getPools";

        public static void Connect ()
        {

            Debug.Log ("Trying this crazy thing");

            /*
            GetPools ((string error, Dictionary<string, object> data) => {
                if (!string.IsNullOrEmpty (error)) {
                    Debug.LogError (error);
                    return;
                }

                if (data.ContainsKey ("success")) {
                    Debug.Log (string.Format ("success: {0}", data["success"].ToString ()));
                }
            }); */

            CreatePool ("Tim's super fun pool of terror", "12345", (string error, Dictionary<string, object> data) => {
                if (!string.IsNullOrEmpty (error)) {
                    Debug.LogError (error);
                    return;
                }

                if (data.ContainsKey ("success")) {
                    Debug.LogWarning (string.Format ("The operation was successful: {0}", data["success"].ToString ()));
                }
            });
        }

        public static void GetPools (TBUtils.DictionaryCallbackType callback)
        {
            _GetData (string.Format ("{0}{1}", kBaseURL, kGetPoolsEndpoint), callback);
        }

        public static void CreatePool (string poolName, string gameId, TBUtils.DictionaryCallbackType callback)
        {
            Dictionary<string, object> body = new Dictionary<string, object> () {
                { "poolName", poolName },
                { "gameId", gameId },
            };

            _PostData (string.Format ("{0}{1}", kBaseURL, kCreatePoolEndpoint), body, callback);
        }

        private static void _GetData (string endpoint, TBUtils.DictionaryCallbackType callback)
        {
            try
            {
                var webRequest = System.Net.WebRequest.Create(endpoint);

                webRequest.Method = "GET";
                webRequest.Timeout = 20000;
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add ("token", AtomicNet.kApiKey);
                webRequest.Headers.Add ("projectid", AtomicNet.kProjectId);

                using (System.IO.Stream s = webRequest.GetResponse().GetResponseStream())
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                    {
                        var jsonResponse = sr.ReadToEnd();

                        Debug.Log (jsonResponse);

                        Dictionary<string, object> result = (Dictionary<string, object>)MiniJSON.Json.Deserialize (jsonResponse);

                        callback (string.Empty, result);
                    }
                }
            }
            catch (Exception ex) {
                callback (ex.ToString (), null);
            }
        }

        private static void _PostData (string endpoint, Dictionary<string, object> data, TBUtils.DictionaryCallbackType callback)
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
