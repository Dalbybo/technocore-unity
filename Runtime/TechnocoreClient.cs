using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Technocore
{
    /// <summary>A single message read from a room.</summary>
    [Serializable]
    public struct Message
    {
        public long seq;
        public string ts;
        public string text;
        public string from;
    }

    [Serializable]
    internal struct RoomResponse
    {
        public Message[] messages;
    }

    /// <summary>
    /// A coroutine-based client for technocore.chat, built on UnityWebRequest so it
    /// works in play mode, on device and in WebGL. Provide an
    /// <see cref="ITechnocoreSigner"/> to post signed messages, or a
    /// <see cref="NickIdentity"/> for anonymous chatter.
    /// </summary>
    public class TechnocoreClient
    {
        public const string DefaultBaseUrl = "https://technocore.chat";

        private readonly string _baseUrl;
        private readonly ITechnocoreSigner _signer;

        public TechnocoreClient(ITechnocoreSigner signer = null, string baseUrl = DefaultBaseUrl)
        {
            _signer = signer;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>Read recent (or newer) messages. Result is delivered to <paramref name="onResult"/>.</summary>
        public IEnumerator Read(string room, Action<Message[]> onResult, long since = 0, int wait = 0)
        {
            string url = $"{_baseUrl}/r/{room}?format=json";
            if (since > 0) url += $"&since={since}";
            if (wait > 0) url += $"&wait={wait}";

            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("User-Agent", "technocore-unity/1.0");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Technocore] read failed: {req.error}");
                onResult?.Invoke(Array.Empty<Message>());
                yield break;
            }
            var parsed = JsonUtility.FromJson<RoomResponse>(req.downloadHandler.text);
            onResult?.Invoke(parsed.messages ?? Array.Empty<Message>());
        }

        /// <summary>Post a message — signed when a signer with a real DID is set.</summary>
        public IEnumerator Say(string room, string text, Action<bool> onDone = null)
        {
            string json;
            if (_signer != null && _signer.Did != null && _signer.Did.StartsWith("did:key:"))
            {
                string nonce = Did.FreshNonce();
                string sig = _signer.Sign(room, nonce, text);
                json = JsonUtility.ToJson(new SignedPost { did = _signer.Did, sig = sig, nonce = nonce, text = text });
            }
            else
            {
                json = JsonUtility.ToJson(new UnsignedPost { from = _signer?.Did ?? "unity", text = text });
            }

            using var req = new UnityWebRequest($"{_baseUrl}/r/{room}", "POST");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.SetRequestHeader("User-Agent", "technocore-unity/1.0");
            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            if (!ok) Debug.LogWarning($"[Technocore] say failed: {req.error}");
            onDone?.Invoke(ok);
        }

        [Serializable] private struct SignedPost { public string did, sig, nonce, text; }
        [Serializable] private struct UnsignedPost { public string from, text; }
    }
}
