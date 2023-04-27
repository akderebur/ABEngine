using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SDL2;
using static SDL2.SDL_mixer;
using ABEngine.ABERuntime;

namespace ABEngine.ABEAudio
{
    public class AudioManager : BaseSystem
    {
        private static Dictionary<int, AudioPlayer> _audioPlayers = new Dictionary<int, AudioPlayer>();
        public event Action MusicFinishedEvent;

        private static ChannelFinishedDelegate channelFinishedCallback;

        private AudioManager()
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) < 0)
                throw new Exception($"Unable to initialize SDL2 audio subsystem: {SDL.SDL_GetError()}");

            Mix_Init(MIX_InitFlags.MIX_INIT_MP3);
            if (Mix_OpenAudio(MIX_DEFAULT_FREQUENCY, MIX_DEFAULT_FORMAT, MIX_DEFAULT_CHANNELS, 4096) == -1)
            {
                throw new Exception($"Failed to open audio device:");
            }

            channelFinishedCallback = new ChannelFinishedDelegate(OnChannelFinished);
            Mix_ChannelFinished(channelFinishedCallback);
        }

        private static AudioManager instance = null;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new AudioManager();

                return instance;
            }
        }

        public static void PlayOneShot(string audioAssetPath)
        {
            var audio = new AudioPlayer();
            int channel = audio.PlayAudio(Game.AssetPath + audioAssetPath);

            // Shouldn't happen a ideally
            if (_audioPlayers.TryGetValue(channel, out AudioPlayer exAudio))
            {
                Console.WriteLine("Exists");
                exAudio.Dispose();
                _audioPlayers.Remove(channel);
            }

            _audioPlayers.Add(channel, audio);
        }

        private static void OnChannelFinished(int channel)
        {
            if(_audioPlayers.TryGetValue(channel, out AudioPlayer audio))
            {
                audio.Dispose();
                _audioPlayers.Remove(channel);
            }
        }

        public override void CleanUp(bool reload)
        {
            Mix_HaltMusic();

            foreach (var key in _audioPlayers.Keys)
            {
                _audioPlayers[key].Dispose();
                _audioPlayers.Remove(key);
            }

            foreach (var track in AudioPlayer.trackCache)
            {
                if (track.Value != IntPtr.Zero)
                {
                    Mix_FreeChunk(track.Value);
                }
            }
            AudioPlayer.trackCache.Clear();

            if (!reload)
            {
                Mix_CloseAudio();
                Mix_Quit();
                SDL2.SDL.SDL_AudioQuit();
            }
        }
    }
}
