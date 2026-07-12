using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SoulKnight3D
{
    public class LanguageSystem : AbstractSystem
    {
        public enum Languages
        {
            Chinese,
            English
        }

        public Languages CurrentLanguage = Languages.Chinese;

        private SaveSystem _saveSystem;

        public EasyEvent<Languages> OnLanguageChanged = new EasyEvent<Languages>();

        protected override void OnInit()
        {
            _saveSystem = this.GetSystem<SaveSystem>();
            int saveLanguageIndex = _saveSystem.LoadInt("Language");
            saveLanguageIndex = Mathf.Clamp(saveLanguageIndex, 0, System.Enum.GetValues(typeof(Languages)).Length - 1);
            CurrentLanguage = (Languages)saveLanguageIndex;
            SetLanguage(CurrentLanguage);

            //LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;
        }

        //private void LocalizationSettings_SelectedLocaleChanged(Locale obj)
        //{
        //    OnLanguageChanged.Trigger(CurrentLanguage);
        //}

        public void SetLanguage(Languages language)
        {
            int languageIndex = Mathf.Clamp((int)language, 0, LocalizationSettings.AvailableLocales.Locales.Count - 1);
            language = (Languages)languageIndex;
            if (LocalizationSettings.SelectedLocale == LocalizationSettings.AvailableLocales.Locales[(int)language])
            {
                return;
            }
            else
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)language];
                CurrentLanguage = language;
                _saveSystem.SaveInt("Language", (int)language);
                OnLanguageChanged.Trigger(CurrentLanguage);
            }
        }
    }
}
