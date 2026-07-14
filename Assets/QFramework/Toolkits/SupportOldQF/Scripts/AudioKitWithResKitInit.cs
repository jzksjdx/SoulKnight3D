using System;
using UnityEngine;
#if UNITY_EDITOR && UNITY_WEBGL
using System.Collections.Generic;
using System.IO;
using UnityEditor;
#endif

namespace QFramework
{
    public class AudioKitWithResKitInit 
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            AudioKit.Config.AudioLoaderPool = new ResKitAudioLoaderPool();
        }
    }

    public class ResKitAudioLoaderPool : AbstractAudioLoaderPool
    {
        public class ResKitAudioLoader : IAudioLoader
        {
            private ResLoader mResLoader = null;
#if UNITY_EDITOR && UNITY_WEBGL
            private static readonly Dictionary<string, AudioClip> EditorAudioClipCache =
                new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
#endif

            public AudioClip Clip => mClip;
            private AudioClip mClip;

            public AudioClip LoadClip(AudioSearchKeys audioSearchKeys)
            {
#if UNITY_EDITOR && UNITY_WEBGL
                mClip = LoadEditorAudioClip(audioSearchKeys.AssetName);
                return mClip;
#else
                if (mResLoader == null)
                {
                    mResLoader = ResLoader.Allocate();
                }

                mClip = mResLoader.LoadSync<AudioClip>(audioSearchKeys.AssetName);

                if (!EnsureAudioDataLoaded(mClip))
                {
                    mClip = null;
                }

                return mClip;
#endif
            }

            public void LoadClipAsync(AudioSearchKeys audioSearchKeys, Action<bool, AudioClip> onLoad)
            {
#if UNITY_EDITOR && UNITY_WEBGL
                mClip = LoadEditorAudioClip(audioSearchKeys.AssetName);
                onLoad(mClip != null, mClip);
#else
                if (mResLoader == null)
                {
                    mResLoader = ResLoader.Allocate();
                }

                mResLoader.Add2Load<AudioClip>(audioSearchKeys.AssetName, (b, res) =>
                {
                    mClip = res.Asset as AudioClip;
                    bool audioDataReady = b && EnsureAudioDataLoaded(mClip);
                    onLoad(audioDataReady, audioDataReady ? mClip : null);
                });

                mResLoader.LoadAsync();
#endif
            }

#if UNITY_EDITOR && UNITY_WEBGL
            private static AudioClip LoadEditorAudioClip(string assetName)
            {
                if (EditorAudioClipCache.TryGetValue(assetName, out AudioClip cachedClip))
                {
                    return cachedClip;
                }

                string[] audioClipGuids = AssetDatabase.FindAssets(
                    $"{assetName} t:AudioClip",
                    new[] { "Assets/Art/Audio" });

                foreach (string guid in audioClipGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.Equals(
                            Path.GetFileNameWithoutExtension(assetPath),
                            assetName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                        EditorAudioClipCache[assetName] = clip;
                        return clip;
                    }
                }

                Debug.LogError($"AudioClip '{assetName}' was not found under Assets/Art/Audio.");
                return null;
            }
#endif

            private static bool EnsureAudioDataLoaded(AudioClip clip)
            {
                if (clip == null || clip.loadState == AudioDataLoadState.Failed)
                {
                    return false;
                }

                if (clip.loadState == AudioDataLoadState.Unloaded && !clip.LoadAudioData())
                {
                    Debug.LogError($"Failed to load audio data for clip '{clip.name}'.");
                    return false;
                }

                return clip.loadState != AudioDataLoadState.Failed;
            }

            public void Unload()
            {
                mClip = null;
                mResLoader?.Recycle2Cache();
                mResLoader = null;
            }
        }

        protected override IAudioLoader CreateLoader()
        {
            return new ResKitAudioLoader();
        }
    }
}
