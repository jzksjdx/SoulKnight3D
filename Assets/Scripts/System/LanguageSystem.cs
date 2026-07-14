using QFramework;
using UnityEngine;
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
        private bool _waitingForLocalization;

        public EasyEvent<Languages> OnLanguageChanged = new EasyEvent<Languages>();

        protected override void OnInit()
        {
            _saveSystem = this.GetSystem<SaveSystem>();
            int saveLanguageIndex = _saveSystem.LoadInt("Language");
            saveLanguageIndex = Mathf.Clamp(saveLanguageIndex, 0, System.Enum.GetValues(typeof(Languages)).Length - 1);
            CurrentLanguage = (Languages)saveLanguageIndex;
            SetLanguage(CurrentLanguage);
        }

        public void SetLanguage(Languages language)
        {
            int languageIndex = Mathf.Clamp(
                (int)language,
                0,
                System.Enum.GetValues(typeof(Languages)).Length - 1);
            CurrentLanguage = (Languages)languageIndex;
            _saveSystem.SaveInt("Language", languageIndex);

            if (!TryApplyLanguage(CurrentLanguage) && !_waitingForLocalization)
            {
                _waitingForLocalization = true;
                LocalizationSettings.InitializationOperation.Completed += _ =>
                {
                    _waitingForLocalization = false;
                    TryApplyLanguage(CurrentLanguage);
                };
            }
        }

        private bool TryApplyLanguage(Languages language)
        {
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null || locales.Count == 0)
            {
                return false;
            }

            int languageIndex = Mathf.Clamp((int)language, 0, locales.Count - 1);
            var selectedLocale = locales[languageIndex];
            if (LocalizationSettings.SelectedLocale != selectedLocale)
            {
                LocalizationSettings.SelectedLocale = selectedLocale;
            }

            CurrentLanguage = (Languages)languageIndex;
            OnLanguageChanged.Trigger(CurrentLanguage);
            return true;
        }
    }
}
