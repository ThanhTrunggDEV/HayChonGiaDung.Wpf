using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace HayChonGiaDung.Wpf
{
    public static class SoundManager
    {
        private const double DefaultVolume = 0.8;

        public static bool SoundOn { get; private set; } = true;
        public static double MasterVolume { get; private set; } = DefaultVolume;

        // Giữ ref các SFX đang phát để tránh bị GC dọn sớm
        private static readonly List<MediaPlayer> _livePlayers = new();

        // Nhạc nền (mp3) – 1 instance, loop
        private static MediaPlayer? _bgPlayer;
        private static string SoundsDir =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");

        public static void SetSound(bool on)
            => ApplySettings(on, MasterVolume);

        public static void ApplySettings(bool on, double masterVolume)
        {
            MasterVolume = Math.Clamp(masterVolume, 0.0, 1.0);
            SoundOn = on;

            if (!SoundOn)
            {
                StopBackground();
                StopAllSfx();
                return;
            }

            StartBackground("WinterFluteVersion-VA_4b4y5.mp3");
        }

        // ===== One-shot SFX (WAV/MP3 ngắn) =====
        private static void PlayOneShot(string fileName, double volume = 1.0, bool bypassMute = false)
        {
            if (!SoundOn && !bypassMute) return;

            var path = Path.Combine(SoundsDir, fileName);
            if (!File.Exists(path)) return;

            var mp = new MediaPlayer
            {
                Volume = Math.Clamp(volume, 0.0, 1.0) * (bypassMute ? 1.0 : MasterVolume)
            };

            // Giữ tham chiếu cho tới khi phát xong
            _livePlayers.Add(mp);

            mp.MediaEnded += (s, e) =>
            {
                try { mp.Stop(); mp.Close(); } catch { /* ignore */ }
                _livePlayers.Remove(mp);
            };

            mp.MediaFailed += (s, e) =>
            {
                // lỗi đọc file -> dọn player
                _livePlayers.Remove(mp);
                try { mp.Close(); } catch { /* ignore */ }
            };

            // MỞ BẤT ĐỒNG BỘ → chỉ Play khi Open xong
            mp.MediaOpened += (s, e) =>
            {
                try
                {
                    mp.Position = TimeSpan.Zero; // cho chắc lần nào cũng phát từ đầu
                    mp.Play();
                }
                catch { /* ignore */ }
            };

            mp.Open(new Uri(path));
        }

        public static void Correct() => PlayOneShot("Correct.wav");
        public static void Wrong() => PlayOneShot("Sai.wav");
        public static void StartRound() => PlayOneShot("Choi.wav");
        public static void Win() => PlayOneShot("Win.wav");
        public static void End() => PlayOneShot("End.wav");
        public static void PlayPreview(double volume) =>
            PlayOneShot("Correct.wav", Math.Clamp(volume, 0.0, 1.0), bypassMute: true);

        private static void StopAllSfx()
        {
            foreach (var p in _livePlayers)
            {
                try { p.Stop(); p.Close(); } catch { /* ignore */ }
            }
            _livePlayers.Clear();
        }

        // ===== Nhạc nền (loop) =====
        public static void StartBackground(string fileName = "WinterFluteVersion-VA_4b4y5.mp3", double? volumeOverride = null)
        {
            if (!SoundOn) return;

            var path = Path.Combine(SoundsDir, fileName);
            if (!File.Exists(path)) return;

            if (_bgPlayer == null)
            {
                _bgPlayer = new MediaPlayer();

                _bgPlayer.MediaEnded += (s, e) =>
                {
                    // Loop mượt
                    if (!SoundOn) return;
                    try
                    {
                        _bgPlayer.Position = TimeSpan.Zero;
                        _bgPlayer.Volume = MasterVolume;
                        _bgPlayer.Play();
                    }
                    catch { /* ignore */ }
                };

                _bgPlayer.MediaOpened += (s, e) =>
                {
                    if (!SoundOn) return;
                    try
                    {
                        _bgPlayer.Volume = MasterVolume;
                        _bgPlayer.Play();
                    }
                    catch { /* ignore */ }
                };

                _bgPlayer.MediaFailed += (s, e) =>
                {
                    try { _bgPlayer.Close(); } catch { /* ignore */ }
                    _bgPlayer = null;
                };
            }

            var volume = volumeOverride.HasValue
                ? Math.Clamp(volumeOverride.Value, 0.0, 1.0)
                : MasterVolume;
            _bgPlayer.Volume = volume;
            // (Re)Open mỗi khi gọi để đảm bảo đã có nguồn
            try { _bgPlayer.Open(new Uri(path)); } catch { /* ignore */ }
        }

        public static void StopBackground()
        {
            if (_bgPlayer == null) return;
            try
            {
                _bgPlayer.Stop();
                _bgPlayer.Close();
            }
            catch { /* ignore */ }
            _bgPlayer = null;
        }
    }
}
