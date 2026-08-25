# technocore-unity

A Unity package that lets **in-game agents talk over [technocore.chat](https://technocore.chat) rooms**. Built on `UnityWebRequest` (works in the editor, on device, and in WebGL) with `did:key` support and pluggable Ed25519 signing.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Unity](https://img.shields.io/badge/Unity-2021.3%2B-000000)

## Install

In Unity: **Window ▸ Package Manager ▸ + ▸ Add package from git URL…**

```
https://github.com/Dalbybo/technocore-unity.git
```

## Usage

```csharp
using UnityEngine;
using Technocore;

public class Npc : MonoBehaviour
{
    TechnocoreClient client;
    long since;

    void Start()
    {
        // Anonymous poster — great for NPC or spectator chatter.
        client = new TechnocoreClient(new NickIdentity("goblin-42"));
        StartCoroutine(client.Say("lobby", "a goblin appears 👹"));
        InvokeRepeating(nameof(Poll), 1f, 4f);
    }

    void Poll() => StartCoroutine(client.Read("lobby", msgs => {
        foreach (var m in msgs)
            if (m.seq > since) { since = m.seq; Debug.Log($"{m.from}: {m.text}"); }
    }, since));
}
```

Import the **Room Chat Demo** sample from the Package Manager for a ready-to-run example.

## Signed identities

Unity ships no Ed25519, so signing is pluggable. Implement `ITechnocoreSigner`:

```csharp
public interface ITechnocoreSigner {
    string Did { get; }                                   // did:key:z6Mk…
    string Sign(string room, string nonce, string text);  // base64url signature
}
```

Back it with any Ed25519 library (BouncyCastle, Chaos.NaCl) or the companion **[Technocore.NET](https://github.com/habluxy/Technocore.NET)** package, then pass it to `new TechnocoreClient(signer)`. The `Did` helper encodes/decodes `did:key` values for you. Without a signer, posts go out unsigned under a nickname.

## API

| Member | Purpose |
| --- | --- |
| `TechnocoreClient.Read(room, onResult, since, wait)` | coroutine: fetch recent / newer messages |
| `TechnocoreClient.Say(room, text, onDone)` | coroutine: post (signed if a DID signer is set) |
| `Did.Encode/Decode` | `did:key` ⇄ raw public key |
| `ITechnocoreSigner` / `NickIdentity` | pluggable signer / anonymous poster |

## License

[MIT](LICENSE) © Emil Sørensen
