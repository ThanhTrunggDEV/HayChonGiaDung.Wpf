using System;
using System.Collections.Generic;
using System.Linq;

namespace HayChonGiaDung.Wpf
{
    public static class GameState
    {
        public static string PlayerName { get; set; } = "";
        public static int TotalPrize { get; set; } = 0;
        public static Random Rnd { get; } = new Random();
        public static List<Product> Catalog { get; private set; } = ProductRepository.LoadProducts();

        public static int QuickCoins { get; private set; }
            = 0; // xu thưởng từ vòng khởi động

        private static readonly Dictionary<HelpCardType, int> _helpCards = new()
        {
            { HelpCardType.Hint, 0 },
            { HelpCardType.SwapProduct, 0 },
            { HelpCardType.DoubleReward, 0 }
        };

        public static int GetHelpCount(HelpCardType type) => _helpCards[type];

        public static void AddHelpCard(HelpCardType type, int amount = 1)
        {
            if (amount <= 0) return;
            _helpCards[type] = Math.Max(0, _helpCards[type] + amount);
        }

        public static bool UseHelpCard(HelpCardType type)
        {
            if (_helpCards[type] <= 0) return false;
            _helpCards[type]--;
            return true;
        }

        public static void AddCoins(int amount)
        {
            if (amount == 0) return;
            QuickCoins = Math.Max(0, QuickCoins + amount);
        }

        public static bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (QuickCoins < amount) return false;
            QuickCoins -= amount;
            return true;
        }

        public static void Reset()
        {
            TotalPrize = 0;
            QuickCoins = 0;
            foreach (var key in _helpCards.Keys.ToList())
            {
                _helpCards[key] = 0;
            }
        }

        public static void ReloadCatalog()
        {
            Catalog = ProductRepository.LoadProducts();
        }

        public static IReadOnlyDictionary<HelpCardType, int> GetInventory()
            => _helpCards;
    }

    public enum HelpCardType
    {
        Hint,
        SwapProduct,
        DoubleReward
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
