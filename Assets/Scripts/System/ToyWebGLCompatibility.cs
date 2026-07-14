using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SoulKnight3D
{
    public static class ToyWebGLCompatibility
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void ConfigureAddressablePaths()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var previousTransform = Addressables.InternalIdTransformFunc;
            Addressables.InternalIdTransformFunc = location =>
            {
                string internalId = previousTransform?.Invoke(location) ?? location.InternalId;
                return internalId.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase)
                    ? internalId + ".unityweb"
                    : internalId;
            };
#endif
        }
    }
}
