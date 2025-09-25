using System;
using System.Windows;

namespace HayChonGiaDung.Wpf
{
    public partial class SettingsWindow : Window
    {
        private bool _soundOn;
        private double _volume;
        private bool _initializing = true;

        public SettingsWindow()
        {
            InitializeComponent();
            SettingsService.Load();

            _soundOn = SettingsService.CurrentSoundOn;
            _volume = SettingsService.CurrentVolume;

            RbOn.IsChecked = _soundOn;
            RbOff.IsChecked = !_soundOn;

            VolumeSlider.Value = Math.Round(_volume * 100, MidpointRounding.AwayFromZero);
            UpdateVolumeText();
            UpdateUiState();

            _initializing = false;
        }

        private void RbOn_Checked(object sender, RoutedEventArgs e)
        {
            _soundOn = true;
            UpdateUiState();
        }

        private void RbOff_Checked(object sender, RoutedEventArgs e)
        {
            _soundOn = false;
            UpdateUiState();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_initializing) return;

            _volume = VolumeSlider.Value / 100.0;
            UpdateVolumeText();
        }

        private void UpdateVolumeText()
        {
            if (VolumeValueText == null) return;
            VolumeValueText.Text = $"{Math.Round(VolumeSlider.Value)}%";
        }

        private void UpdateUiState()
        {
            if (VolumeSlider == null || PreviewButton == null || VolumeValueText == null) return;

            VolumeSlider.IsEnabled = _soundOn;
            PreviewButton.IsEnabled = _soundOn;
            VolumeValueText.Opacity = _soundOn ? 1.0 : 0.5;
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (!_soundOn) return;
            SoundManager.PlayPreview(_volume);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.Save(_soundOn, _volume);
            DialogResult = true;
            Close();
        }
    }
}
