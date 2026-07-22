using EarTrumpet.Extensibility;

namespace EarTrumpet.Extensions
{
    public static class EarTrumpetAddonExtensions
    {
        // DeviceTrumpet: no more built-in addons, so every addon now comes from
        // the external Addons\ directory and gets its own About page.
        public static bool IsInternal(this EarTrumpetAddon addon)
        {
            return false;
        }
    }
}
