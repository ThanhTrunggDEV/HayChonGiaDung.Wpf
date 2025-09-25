using System;
using System.Collections.Generic;

namespace HayChonGiaDung.Wpf
{
    public static class GameState
    {
        public static string PlayerName { get; set; } = "";
        public static int TotalPrize { get; set; } = 0;
        public static int Coins { get; private set; }
        public static Random Rnd { get; } = new Random();
        public static List<Product> Catalog { get; private set; } = ProductRepository.LoadProducts();

        private static readonly Dictionary<PowerCardType, int> _powerCards = new();
        private static bool _doubleRewardQueued;

        public static IReadOnlyDictionary<PowerCardType, int> PowerCards => _powerCards;

        public static void Reset()
        {
            TotalPrize = 0;
            Coins = 0;
            _powerCards.Clear();
            _doubleRewardQueued = false;
        }

        public static void ReloadCatalog()
        {
            Catalog = ProductRepository.LoadProducts();
        }

        public static void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
        }

        public static bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            return true;
        }

        public static int GetCardCount(PowerCardType type)
            => _powerCards.TryGetValue(type, out var count) ? count : 0;

        public static void AddPowerCard(PowerCardType type, int amount = 1)
        {
            if (amount <= 0) return;
            _powerCards[type] = GetCardCount(type) + amount;
        }

        public static bool TryUsePowerCard(PowerCardType type)
        {
            if (!_powerCards.TryGetValue(type, out var count) || count <= 0)
            {
                return false;
            }

            _powerCards[type] = count - 1;
            return true;
        }

        public static void QueueDoubleReward()
        {
            _doubleRewardQueued = true;
        }

        public static void AddPrize(int amount)
        {
            if (amount <= 0) return;
            if (_doubleRewardQueued)
            {
                amount *= 2;
                _doubleRewardQueued = false;
            }
            TotalPrize += amount;
        }

        public static void CancelQueuedDouble()
        {
            _doubleRewardQueued = false;
        }
    }

    public enum PowerCardType
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
