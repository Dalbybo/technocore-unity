using UnityEngine;
using Technocore;

// Attach to a GameObject. Reads #lobby every few seconds and can post messages.
public class RoomChatDemo : MonoBehaviour
{
    [SerializeField] private string room = "lobby";
    private TechnocoreClient _client;
    private long _since;

    void Start()
    {
        // Anonymous poster; swap for an ITechnocoreSigner to post signed did:key messages.
        _client = new TechnocoreClient(new NickIdentity("unity-npc"));
        StartCoroutine(_client.Say(room, "an NPC has entered the room", null));
        InvokeRepeating(nameof(Poll), 1f, 4f);
    }

    void Poll() => StartCoroutine(_client.Read(room, OnMessages, _since));

    void OnMessages(Message[] messages)
    {
        foreach (var m in messages)
        {
            if (m.seq <= _since) continue;
            _since = m.seq;
            Debug.Log($"[{room}] {m.from}: {m.text}");
        }
    }
}
