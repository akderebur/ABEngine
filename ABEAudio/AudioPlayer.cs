using SDL2;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static SDL2.SDL_mixer;

namespace ABEngine.ABEAudio
{
    internal class AudioPlayer : IDisposable
    {
        private IntPtr music;
        private int channel;
        internal bool IsCompleted;
        bool disposed = false;

        internal int PlayAudio(string filePath)
        {
            music = Mix_LoadWAV(filePath);
            if (music == IntPtr.Zero)
            {
                throw new Exception($"Failed to load audio file:");
            }

            channel = Mix_PlayChannel(-1, music, 0);
            return channel;
        }

        public void StopAudio()
        {
            if (music != IntPtr.Zero)
            {
                Mix_FreeChunk(music);
                music = IntPtr.Zero;
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
