using System;
using System.IO;
using System.Text.Json;

namespace HayChonGiaDung.Wpf
{
    public static class SettingsService
    {
        private const double DefaultVolume = 0.8;

        public static bool CurrentSoundOn { get; private set; } = true;
        public static double CurrentVolume { get; private set; } = DefaultVolume;
        private static bool _hasLoaded;

        private static string PathFile =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "settings.json");

        public static void Load()
        {
            bool soundOn = true;
            double volume = DefaultVolume;

            try
            {
                EnsureFileExists();
                var json = File.ReadAllText(PathFile);
                var obj = JsonSerializer.Deserialize<SettingsObj>(json) ?? new SettingsObj();

                soundOn = obj.Sound;
                volume = ClampVolume(obj.Volume ?? DefaultVolume);
            }
            catch
            {
                soundOn = true;
                volume = DefaultVolume;
            }

            var hasChanges = !_hasLoaded || soundOn != CurrentSoundOn || Math.Abs(volume - CurrentVolume) > 0.0001;

            CurrentSoundOn = soundOn;
            CurrentVolume = volume;
            _hasLoaded = true;

            if (hasChanges)
            {
                SoundManager.ApplySettings(CurrentSoundOn, CurrentVolume);
            }
        }

        public static void Save(bool soundOn, double volume)
        {
            CurrentSoundOn = soundOn;
            CurrentVolume = ClampVolume(volume);
            _hasLoaded = true;

            var payload = new SettingsObj
            {
                Sound = CurrentSoundOn,
                Volume = CurrentVolume
            };

            WriteToDisk(payload);
            SoundManager.ApplySettings(CurrentSoundOn, CurrentVolume);
        }

        private static void EnsureFileExists()
        {
            if (File.Exists(PathFile)) return;
            WriteToDisk(new SettingsObj { Sound = true, Volume = DefaultVolume });
        }

        private static void WriteToDisk(SettingsObj obj)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathFile)!);
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PathFile, json);
        }

        private static double ClampVolume(double volume)
            => Math.Clamp(volume, 0.0, 1.0);

        private class SettingsObj
        {
            public bool Sound { get; set; } = true;
            public double? Volume { get; set; } = DefaultVolume;
        }
    }
}
