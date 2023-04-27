using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static SDL2.SDL_mixer;

namespace ABEngine.ABEAudio
{
    internal class AudioPlayer : IDisposable
    {
        internal static Dictionary<string, IntPtr> trackCache = new Dictionary<string, IntPtr>();

        private IntPtr track;
        private int channel;
        internal bool IsCompleted;
        bool disposed = false;

        internal int PlayAudio(string filePath)
        {
            if (!trackCache.TryGetValue(filePath, out track))
            {
                track = Mix_LoadWAV(filePath);
                trackCache.Add(filePath, track);
            }

            if (track == IntPtr.Zero)
            {
                throw new Exception($"Failed to load audio file:");
            }

            channel = Mix_PlayChannel(-1, track, 0);
            return channel;
        }

        public void StopAudio()
        {
            if (track != IntPtr.Zero)
            {
                //Mix_FreeChunk(track);
                track = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            StopAudio();
            disposed = true;
        }
    }
}
