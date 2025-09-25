using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace HayChonGiaDung.Wpf
{
    public static class SoundManager
    {
        public static bool SoundOn { get;  set; } = true;

        // Giữ ref các SFX đang phát để tránh bị GC dọn sớm
        private static readonly List<MediaPlayer> _livePlayers = new();

        // Nhạc nền (mp3) – 1 instance, loop
        private static MediaPlayer? _bgPlayer;
        private static string SoundsDir =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");

        public static void SetSound(bool on)
        {
            SoundOn = on;
            if (!on)
            {
                StopBackground();
                StopAllSfx();
            }
            else
            {
                // tuỳ anh: tự phát lại nền khi bật lại
                // StartBackground("WinterFluteVersion-VA_4b4y5.mp3");
            }
        }

        // ===== One-shot SFX (WAV/MP3 ngắn) =====
        private static void PlayOneShot(string fileName, double volume = 1.0)
        {
            if (!SoundOn) return;

            var path = Path.Combine(SoundsDir, fileName);
            if (!File.Exists(path)) return;

            var mp = new MediaPlayer
            {
                Volume = volume
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

        private static void StopAllSfx()
        {
            foreach (var p in _livePlayers)
            {
                try { p.Stop(); p.Close(); } catch { /* ignore */ }
            }
            _livePlayers.Clear();
        }

        // ===== Nhạc nền (loop) =====
        public static void StartBackground(string fileName = "WinterFluteVersion-VA_4b4y5.mp3", double volume = 0.8)
        {
            if (!SoundOn) return;

            var path = Path.Combine(SoundsDir, fileName);
            if (!File.Exists(path)) return;

            if (_bgPlayer == null)
            {
                _bgPlayer = new MediaPlayer();
                _bgPlayer.Volume = volume;

                _bgPlayer.MediaEnded += (s, e) =>
                {
                    // Loop mượt
                    if (!SoundOn) return;
                    try
                    {
                        _bgPlayer.Position = TimeSpan.Zero;
                        _bgPlayer.Play();
                    }
                    catch { /* ignore */ }
                };

                _bgPlayer.MediaOpened += (s, e) =>
                {
                    if (!SoundOn) return;
                    try { _bgPlayer.Play(); } catch { /* ignore */ }
                };

                _bgPlayer.MediaFailed += (s, e) =>
                {
                    try { _bgPlayer.Close(); } catch { /* ignore */ }
                    _bgPlayer = null;
                };
            }

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
