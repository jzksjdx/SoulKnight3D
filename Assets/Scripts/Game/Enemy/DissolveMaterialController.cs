using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    internal sealed class DissolveMaterialController
    {
        private readonly List<RendererMaterialTarget> _targets =
            new List<RendererMaterialTarget>();
        private MaterialPropertyBlock _propertyBlock;
        private int _dissolveProperty;

        public void Cache(Renderer[] renderers)
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
                _dissolveProperty = Shader.PropertyToID("_Dissolve");
            }

            _targets.Clear();

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null) { continue; }

                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material != null && material.HasProperty(_dissolveProperty))
                    {
                        _targets.Add(new RendererMaterialTarget(renderer, materialIndex));
                    }
                }
            }
        }

        public void SetValue(float value)
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                RendererMaterialTarget target = _targets[i];
                if (target.Renderer == null) { continue; }

                _propertyBlock.Clear();
                target.Renderer.GetPropertyBlock(_propertyBlock, target.MaterialIndex);
                _propertyBlock.SetFloat(_dissolveProperty, value);
                target.Renderer.SetPropertyBlock(_propertyBlock, target.MaterialIndex);
            }
        }

        private readonly struct RendererMaterialTarget
        {
            public readonly Renderer Renderer;
            public readonly int MaterialIndex;

            public RendererMaterialTarget(Renderer renderer, int materialIndex)
            {
                Renderer = renderer;
                MaterialIndex = materialIndex;
            }
        }
    }
}
