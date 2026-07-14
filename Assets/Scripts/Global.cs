using QFramework;

namespace SoulKnight3D
{
    public class Global : Architecture<Global>
    {
        protected override void Init()
        {
            RegisterSystem(new SaveSystem());
            RegisterSystem(new AudioSystem());
            RegisterSystem(new ControlSystem());
            RegisterSystem(new LanguageSystem());
        }
    }

}
