using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Note: this version expects the bug list item prefab to contain a BugListItem_TMP component.
public class UIManager_uGUI_TMP : MonoBehaviour
{
    [Header("References")]
    public BugManager bugManager;
    public RectTransform bugListContent;          // content panel of ScrollView for list items
    public GameObject bugListItemPrefab;          // prefab containing BugListItem_TMP
    public BugLogFeed_TMP bugLogFeed;             // TMP-based log feed manager
    public VFXSpawner vfxSpawner;                 // optional VFX spawner
    public AudioCueManager audioCueManager;       // optional audio manager

    private Dictionary<string, BugListItem_TMP> activeItems = new Dictionary<string, BugListItem_TMP>();

    void Awake()
    {
        if (bugManager == null) bugManager = FindObjectOfType<BugManager>();
        if (bugLogFeed == null) bugLogFeed = FindObjectOfType<BugLogFeed_TMP>();
    }

    void OnEnable()
    {
        if (bugManager != null)
        {
            bugManager.OnBugSpawned += OnBugSpawned;
            bugManager.OnBugResolved += OnBugResolved;
            bugManager.OnBugExpired += OnBugExpired;
        }
    }

    void OnDisable()
    {
        if (bugManager != null)
        {
            bugManager.OnBugSpawned -= OnBugSpawned;
            bugManager.OnBugResolved -= OnBugResolved;
            bugManager.OnBugExpired -= OnBugExpired;
        }
    }

    void Update()
    {
        // Refresh timer visuals
        foreach (var kv in activeItems.ToList())
        {
            if (kv.Value != null)
                kv.Value.RefreshTimer();
        }
    }

    void OnBugSpawned(BugInstance b)
    {
        if (bugListItemPrefab == null || bugListContent == null)
        {
            Debug.LogWarning("[UIManager_TMP] Missing prefab/content for bug list item.");
            return;
        }

        var go = Instantiate(bugListItemPrefab, bugListContent);
        var item = go.GetComponent<BugListItem_TMP>();
        if (item == null)
        {
            Debug.LogWarning("[UIManager_TMP] bugListItemPrefab missing BugListItem_TMP component.");
            Destroy(go);
            return;
        }

        item.Initialize(b, this);
        activeItems[b.Id] = item;

        // Log and VFX/Audio
        bugLogFeed?.AddLog($"Spawned: {b.Definition.bugName} ({(b.target?b.target.name:"global")})", BugLogFeed_TMP.LogType.Spawn);
        if (vfxSpawner != null) vfxSpawner.PlaySpawnAt(b.target != null ? b.target.transform.position : Vector3.zero);
        audioCueManager?.PlaySpawn();
    }

    void OnBugResolved(BugInstance b)
    {
        if (activeItems.TryGetValue(b.Id, out var item))
        {
            item.MarkResolved();
            Destroy(item.gameObject, 0.6f);
            activeItems.Remove(b.Id);
        }

        bugLogFeed?.AddLog($"Resolved: {b.Definition.bugName}", BugLogFeed_TMP.LogType.Resolve);
        if (vfxSpawner != null) vfxSpawner.PlayResolveAt(b.target != null ? b.target.transform.position : Vector3.zero);
        audioCueManager?.PlayResolve();
    }

    void OnBugExpired(BugInstance b)
    {
        if (activeItems.TryGetValue(b.Id, out var item))
        {
            item.MarkExpired();
            Destroy(item.gameObject, 0.8f);
            activeItems.Remove(b.Id);
        }

        bugLogFeed?.AddLog($"Expired: {b.Definition.bugName}", BugLogFeed_TMP.LogType.Expired);
        if (vfxSpawner != null) vfxSpawner.PlayExpireAt(b.target != null ? b.target.transform.position : Vector3.zero);
        audioCueManager?.PlayExpire();
    }

    public void AssignBugToPlayer(string bugId, string playerName)
    {
        bugLogFeed?.AddLog($"Assigned bug {bugId} to {playerName}", BugLogFeed_TMP.LogType.Info);
        Debug.Log($"[UIManager_TMP] Assigned bug {bugId} to {playerName}");
        // TODO: locking / ticket logic
    }

    public void AttemptResolveFromUI(string bugId)
    {
        var inst = bugManager.ActiveBugs.FirstOrDefault(b => b.Id == bugId);
        if (inst == null) { bugLogFeed?.AddLog($"UI Resolve: bug {bugId} not found", BugLogFeed_TMP.LogType.Warning); return; }
        var ctx = new PlayerPatchContext { playerName = "UI", toolName = "UI_Resolve", toolPower = 0.9f, extra = null };
        bool ok = bugManager.TryResolveBug(bugId, ctx);
        bugLogFeed?.AddLog(ok ? $"UI resolved {inst.Definition.bugName}" : $"UI failed to resolve {inst.Definition.bugName}", ok ? BugLogFeed_TMP.LogType.Resolve : BugLogFeed_TMP.LogType.Warning);
    }
}
