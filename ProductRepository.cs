using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace HayChonGiaDung.Wpf
{
    public static class ProductRepository
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static List<Product> LoadProducts()
        {
            foreach (var path in EnumerateCandidateFiles())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    if (!doc.RootElement.TryGetProperty("products", out var arr))
                    {
                        continue;
                    }

                    var list = new List<Product>();
                    foreach (var p in arr.EnumerateArray())
                    {
                        list.Add(new Product
                        {
                            Name = p.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                            Price = p.TryGetProperty("price", out var price) ? price.GetInt32() : 0,
                            ImageUrl = p.TryGetProperty("imageUrl", out var imageUrl) ? imageUrl.GetString() : null,
                            Image = p.TryGetProperty("image", out var image) ? image.GetString() : null,
                            Description = TryGetDescription(p)
                        });
                    }

                    return list;
                }
                catch
                {
                    // ignored
                }
            }

            return new List<Product>();
        }

        public static void SaveProducts(IEnumerable<Product> products)
        {
            var productArray = products
                .Select(p => new
                {
                    name = p.Name,
                    price = p.Price,
                    image = p.Image,
                    imageUrl = p.ImageUrl,
                    description = p.Description
                })
                .ToArray();

            var json = JsonSerializer.Serialize(new { products = productArray }, SerializerOptions);

            var written = false;
            foreach (var path in EnumerateCandidateFiles())
            {
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(path, json);
                    written = true;
                }
                catch
                {
                    // ignored
                }
            }

            if (!written)
            {
                var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "products.json");
                var directory = Path.GetDirectoryName(fallback);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fallback, json);
            }
        }

        private static string? TryGetDescription(JsonElement element)
        {
            if (element.TryGetProperty("description", out var description))
            {
                return description.GetString();
            }

            if (element.TryGetProperty("desc", out var desc))
            {
                return desc.GetString();
            }

            return null;
        }

        private static IEnumerable<string> EnumerateCandidateFiles()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory, AppContext.BaseDirectory })
            {
                if (string.IsNullOrEmpty(start))
                {
                    continue;
                }

                var current = Path.GetFullPath(start);
                for (var i = 0; i < 6 && !string.IsNullOrEmpty(current); i++)
                {
                    var candidate = Path.Combine(current, "Data", "products.json");
                    if (seen.Add(candidate))
                    {
                        yield return candidate;
                    }

                    var parent = Directory.GetParent(current);
                    if (parent == null)
                    {
                        break;
                    }

                    current = parent.FullName;
                }
            }
        }
    }
}
