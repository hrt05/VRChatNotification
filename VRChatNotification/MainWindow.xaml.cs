using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace VRChatNotification
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //_player.Volume = 0.1;
        }

        private MediaPlayer _player = new MediaPlayer();

        private async Task PlaySound()
        {
            // C:\Users\kouki\source\repos\VRChatNotification\VRChatNotification\bin\Debug\net10.0-windows
            // C:\Users\kouki\source\repos\VRChatNotification\VRChatNotification\bin\Debug\net10.0-windows/sound/joinSound.wav

            string soundPath = Path.Combine(Directory.GetCurrentDirectory(), "sound", "joinSound.wav");

            _player.Open(new Uri(soundPath));

            _player.Play();
        }

        private async void Ignition(object sender, RoutedEventArgs e)
        {
            //var tetetest = Directory.GetCurrentDirectory();
            //Debug.WriteLine(tetetest);

            try
            {
                await PlaySound();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine("現在の値" + e.NewValue / 100);
            _player.Volume = e.NewValue / 100;
        }
    }
}