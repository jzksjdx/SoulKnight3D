using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class AudioSystem : AbstractSystem
    {
        public BindableProperty<float> MusicVolume = new BindableProperty<float>(1);
        public BindableProperty<float> SoundVolume = new BindableProperty<float>(1);

        private SaveSystem _saveSystem;

        protected override void OnInit()
        {
            _saveSystem = this.GetSystem<SaveSystem>();
            float musicVolume = _saveSystem.LoadFloat("MusicVolume", 1f);
            float soundVolume = _saveSystem.LoadFloat("SoundVolume", 1f);
            ChangeMusicVolume(musicVolume);
            ChangeSoundVolume(soundVolume);
        }

        public void ChangeMusicVolume(float volume)
        {
            MusicVolume.Value = volume;
            AudioKit.Settings.MusicVolume.Value = volume;
            _saveSystem.SaveFloat("MusicVolume", MusicVolume.Value);
        }

        public void ChangeSoundVolume(float volume)
        {
            SoundVolume.Value = volume;
            AudioKit.Settings.SoundVolume.Value = volume;
            _saveSystem.SaveFloat("SoundVolume", SoundVolume.Value);
        }
    }

}
