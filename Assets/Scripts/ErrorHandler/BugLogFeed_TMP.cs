using System.Collections.Generic;
using UnityEngine;
using TMPro;

// TMP-based log feed. Prefab for entries must contain a TextMeshProUGUI component.
public class BugLogFeed_TMP : MonoBehaviour
{
    public enum LogType { Info, Spawn, Resolve, Expired, Warning }
    public RectTransform contentParent;
    public GameObject logEntryPrefab; // prefab with TextMeshProUGUI as root component
    public int maxEntries = 80;

    private readonly Queue<GameObject> entries = new Queue<GameObject>();

    public void AddLog(string message, LogType type = LogType.Info)
    {
        if (contentParent == null || logEntryPrefab == null)
        {
            Debug.Log($"[BugLogFeed_TMP] {message}");
            return;
        }

        var go = Instantiate(logEntryPrefab, contentParent);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"{System.DateTime.Now:HH:mm:ss} - {message}";
            switch (type)
            {
                case LogType.Spawn: tmp.color = Color.cyan; break;
                case LogType.Resolve: tmp.color = Color.green; break;
                case LogType.Expired: tmp.color = Color.red; break;
                case LogType.Warning: tmp.color = new Color(1f, 0.6f, 0.0f); break;
                default: tmp.color = Color.white; break;
            }
        }
        entries.Enqueue(go);
        if (entries.Count > maxEntries)
        {
            var old = entries.Dequeue();
            Destroy(old);
        }
    }
}
