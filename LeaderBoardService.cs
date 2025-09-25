using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HayChonGiaDung.Wpf
{
    public static class LeaderboardService
    {
        private static string PathFile =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "leaderboard.json");

        public class Entry
        {
            public string Name { get; set; } = "";
            public int Prize { get; set; }      // VND
            public DateTime At { get; set; }
        }

        public static List<Entry> LoadAll()
        {
            try
            {
                if (!File.Exists(PathFile)) return new();
                var json = File.ReadAllText(PathFile);
                return JsonSerializer.Deserialize<List<Entry>>(json) ?? new();
            }
            catch { return new(); }
        }

        public static void SaveAll(List<Entry> entries)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathFile)!);
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PathFile, json);
        }

        public static void AddScore(string name, int prize)
        {
            var list = LoadAll();
            list.Add(new Entry { Name = name, Prize = prize, At = DateTime.Now });
            SaveAll(list);
        }

        public static List<Entry> Top(int n = 100)
            => LoadAll().OrderByDescending(e => e.Prize).ThenBy(e => e.At).Take(n).ToList();
    }
}
