using System.Threading.Tasks;
using System.Windows;

namespace HayChonGiaDung.Wpf
{
    public static class RoundCelebrationHelper
    {
        public static async Task ShowWinAsync(Window owner, string message)
        {
            MessageBox.Show(owner, message, "Chúc mừng", MessageBoxButton.OK, MessageBoxImage.Information);
            var fireworks = new FireworksWindow(owner);
            fireworks.Show();
            await fireworks.Completion;
        }

        public static void ShowLose(Window owner, string message)
        {
            MessageBox.Show(owner, message, "Rất tiếc", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
