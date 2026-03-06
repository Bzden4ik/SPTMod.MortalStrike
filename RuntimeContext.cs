using BepInEx.Bootstrap;

namespace MortalStrike
{
    public static class RuntimeContext
    {
        private static bool? _isHeadless;
        private static bool? _isFikaLoaded;

        /// <summary>
        /// True если мод запущен внутри Fika Headless клиента.
        /// На headless нет аудио и нет локального игрока.
        /// </summary>
        public static bool IsHeadless
        {
            get
            {
                if (_isHeadless == null)
                    _isHeadless = Chainloader.PluginInfos.ContainsKey("com.fika.headless");
                return _isHeadless.Value;
            }
        }

        /// <summary>
        /// True если Fika вообще загружена (мультиплеер).
        /// False = соло SPT без Fika.
        /// </summary>
        public static bool IsFikaLoaded
        {
            get
            {
                if (_isFikaLoaded == null)
                    _isFikaLoaded = Chainloader.PluginInfos.ContainsKey("com.fika.core");
                return _isFikaLoaded.Value;
            }
        }
    }
}
