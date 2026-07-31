using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    /// <summary>
    /// Disables rendering work without changing gameplay object activation.
    /// </summary>
    internal sealed class RuntimeRenderVisibility
    {
        private readonly Transform _root;
        private readonly Dictionary<Renderer, bool> _renderers =
            new Dictionary<Renderer, bool>();
        private readonly Dictionary<Light, bool> _lights =
            new Dictionary<Light, bool>();
        private readonly Dictionary<Animator, AnimatorCullingMode> _animators =
            new Dictionary<Animator, AnimatorCullingMode>();

        private bool _isVisible = true;

        public RuntimeRenderVisibility(Transform root)
        {
            _root = root;
            Refresh();
        }

        public void Refresh()
        {
            if (_root == null) { return; }

            Renderer[] renderers = _root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                // Minimap tiles and icons are SpriteRenderers and use a separate camera.
                if (renderer is SpriteRenderer || _renderers.ContainsKey(renderer))
                {
                    continue;
                }

                _renderers.Add(renderer, renderer.enabled);
            }

            Light[] lights = _root.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (!_lights.ContainsKey(light))
                {
                    _lights.Add(light, light.enabled);
                }
            }

            Animator[] animators = _root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (!_animators.ContainsKey(animator))
                {
                    _animators.Add(animator, animator.cullingMode);
                }
            }

            ApplyVisibility();
        }

        public void SetVisible(bool isVisible)
        {
            if (_isVisible == isVisible) { return; }

            _isVisible = isVisible;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            foreach (KeyValuePair<Renderer, bool> entry in _renderers)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = _isVisible && entry.Value;
                }
            }

            foreach (KeyValuePair<Light, bool> entry in _lights)
            {
                if (entry.Key != null)
                {
                    entry.Key.enabled = _isVisible && entry.Value;
                }
            }

            foreach (KeyValuePair<Animator, AnimatorCullingMode> entry in
                     _animators)
            {
                if (entry.Key != null)
                {
                    entry.Key.cullingMode = _isVisible
                        ? entry.Value
                        : AnimatorCullingMode.CullCompletely;
                }
            }
        }
    }
}
