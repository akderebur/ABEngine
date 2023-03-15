using System;
using NetCoreAudio;

namespace ABEngine.ABEAudio
{
    public static class AudioManager
    {
        private static Player _player = null;
        private static string _assetPath;

        static AudioManager()
        {
           
        }

        public static void Initialize(string assetPath)
        {
            if (_player == null)
            {
                _player = new Player();
                _assetPath = assetPath;
            }
        }

        public static void PlayOneShot(string localMediaPath)
        {
            //var media = new Media(_libVLC, new Uri(_assetPath + localMediaPath));
            //_VLCPlayer.Play(media);

            _player.Play(_assetPath + localMediaPath);
        }
    }
}
