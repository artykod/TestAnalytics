using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

public class EventService : MonoBehaviour
{
    [SerializeField]
    private string _serverUrl = "http://127.0.0.1:8080";

    [SerializeField]
    private float _cooldownBeforeSend = 2f;

    private class EventData
    {
        public string Type;
        public string Data;
    }

    private const string PERSISTENT_CACHE_NAME = "analytics_cache";

    private readonly HashSet<EventData> _pendingEvents = new HashSet<EventData>();

    public void TrackEvent(string type, string data)
    {
        AddEventData(new EventData
        {
            Type = type,
            Data = data,
        });

        SaveEventsToPersistentCache(_pendingEvents);
    }

    private void Awake()
    {
        var eventsFromCache = RestoreEventsFromPersistentCache();

        if (eventsFromCache != null && eventsFromCache.Count > 0)
        {
            foreach (var e in eventsFromCache)
            {
                AddEventData(e);
            }
        }
    }

    private IEnumerator Start()
    {
        var batchedEvents = new List<EventData>(8);
        var cooldownBeforeSendWaiter = new WaitForSecondsRealtime(_cooldownBeforeSend);

        while (true)
        {
            if (_pendingEvents.Count == 0)
            {
                yield return null;

                continue;
            }

            yield return cooldownBeforeSendWaiter;

            foreach (var e in _pendingEvents)
            {
                batchedEvents.Add(e);
            }

            using (var request = new UnityWebRequest(_serverUrl, "POST"))
            {
                var requestJson = GetJsonFromPendingEvents(_pendingEvents);
                var bytesToUpload = System.Text.UTF8Encoding.UTF8.GetBytes(requestJson);

                Debug.Log($"{request.method} to {_serverUrl}: {requestJson}");

                request.SetRequestHeader("Content-Type", "application/json");

                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bytesToUpload);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                request.disposeUploadHandlerOnDispose = true;
                request.disposeDownloadHandlerOnDispose = true;

                yield return request.SendWebRequest();

                if (request.responseCode == 200)
                {
                    Debug.Log($"Response: {request.downloadHandler.text}");

                    foreach (var e in batchedEvents)
                    {
                        _pendingEvents.Remove(e);
                    }

                    SaveEventsToPersistentCache(_pendingEvents);
                }
                else
                {
                    Debug.LogError($"Request error. Code: {request.responseCode} Error: {request.error}");
                }
            }

            batchedEvents.Clear();
        }
    }

    private void AddEventData(EventData eventData)
    {
        _pendingEvents.Add(eventData);
    }

    private string GetJsonFromPendingEvents(HashSet<EventData> events)
    {
        var json = new JSONObject();

        if (events.Count > 0)
        {
            var jsonEventsArray = new JSONArray();

            foreach (var eventData in events)
            {
                var jsonEvent = new JSONObject();

                jsonEvent["type"] = eventData.Type;
                jsonEvent["data"] = eventData.Data;

                jsonEventsArray.Add(jsonEvent);
            }

            json["events"] = jsonEventsArray;
        }

        return json.ToString();
    }

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SyncWebFiles();

    private void SaveEventsToPersistentCache(HashSet<EventData> events)
    {
        var persistentPath = Path.Combine(Application.persistentDataPath, PERSISTENT_CACHE_NAME);
        var cacheData = GetJsonFromPendingEvents(events);

        try
        {
            Debug.Log($"Save to persistent: {cacheData}");

            File.WriteAllText(persistentPath, cacheData);

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                SyncWebFiles();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Save events to persistent failed");
            Debug.LogException(e);
        }
    }

    private List<EventData> RestoreEventsFromPersistentCache()
    {
        var persistentPath = Path.Combine(Application.persistentDataPath, PERSISTENT_CACHE_NAME);
        var result = new List<EventData>(8);

        if (!File.Exists(persistentPath))
        {
            return result;
        }

        try
        {
            var cacheData = File.ReadAllText(persistentPath);
            var json = JSON.Parse(cacheData);
            var eventsNode = json.HasKey("events") ? json["events"] : null;

            if (eventsNode != null && eventsNode.IsArray)
            {
                var events = eventsNode.AsArray;

                foreach (var e in events.Values)
                {
                    var type = e["type"].Value;
                    var data = e["data"].Value;

                    result.Add(new EventData
                    {
                        Type = type,
                        Data = data,
                    });
                }
            }

            Debug.Log($"Restore from persistent: {json}");
        }
        catch (Exception e)
        {
            Debug.LogError("Restore events from persistent failed");
            Debug.LogException(e);
        }

        return result;
    }
}
