using System;
using System.Collections.Generic;

namespace HayChonGiaDung.Wpf
{
    public static class GameState
    {
        public static string PlayerName { get; set; } = "";
        public static int TotalPrize { get; set; } = 0;
        public static Random Rnd { get; } = new Random();
        public static List<Product> Catalog { get; private set; } = ProductRepository.LoadProducts();

        public static void Reset()
        {
            TotalPrize = 0;
        }

        public static void ReloadCatalog()
        {
            Catalog = ProductRepository.LoadProducts();
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
