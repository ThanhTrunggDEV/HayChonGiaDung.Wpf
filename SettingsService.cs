using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HayChonGiaDung.Wpf
{
    public static class SettingsService
    {
        private static string PathFile =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "settings.json");

        public static void Load()
        {
            try
            {
                if (!File.Exists(PathFile)) { Save(true); } // mặc định bật âm
                var json = File.ReadAllText(PathFile);
                var obj = JsonSerializer.Deserialize<SettingsObj>(json) ?? new SettingsObj { Sound = true };
                SoundManager.SoundOn = obj.Sound;
            }
            catch { SoundManager.SoundOn = true; }
        }

        public static void Save(bool soundOn)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathFile)!);
            var json = JsonSerializer.Serialize(new SettingsObj { Sound = soundOn }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PathFile, json);
            SoundManager.SoundOn = soundOn;
        }

        private class SettingsObj { public bool Sound { get; set; } }
    }
}
