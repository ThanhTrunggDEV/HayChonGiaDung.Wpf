using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HayChonGiaDung.Wpf
{
    public static class GameState
    {
        public static string PlayerName { get; set; } = "";
        public static int TotalPrize { get; set; } = 0;
        public static Random Rnd { get; } = new Random();
        public static List<Product> Catalog { get; } = LoadProducts();

        public static void Reset()
        {
            TotalPrize = 0;
        }

        private static List<Product> LoadProducts()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "products.json");
            var list = new List<Product>();
            if (File.Exists(path))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    if (doc.RootElement.TryGetProperty("products", out var arr))
                    {
                        foreach (var p in arr.EnumerateArray())
                        {
                            list.Add(new Product
                            {
                                Name = p.GetProperty("name").GetString() ?? "",
                                Price = p.TryGetProperty("price", out var pr) ? pr.GetInt32() : 0,
                                ImageUrl = p.TryGetProperty("imageUrl", out var iu) ? iu.GetString() : null,
                                Image = p.TryGetProperty("image", out var im) ? im.GetString() : null,
                                 Description = p.TryGetProperty("description", out var ds) ? ds.GetString()
                  : (p.TryGetProperty("desc", out var ds2) ? ds2.GetString() : null)
                            });
                        }
                    }
                } catch {}
            }
            return list;
        }
    }

    public class Product
    {
        public string Name { get; set; } = "";
        public int Price { get; set; } // VND
        public string? Image { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}
