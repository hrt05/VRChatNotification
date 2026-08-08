using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;

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
        }

        private async Task PlaySound()
        {
            // C:\Users\kouki\source\repos\VRChatNotification\VRChatNotification\bin\Debug\net10.0-windows
            // C:\Users\kouki\source\repos\VRChatNotification\VRChatNotification\bin\Debug\net10.0-windows/sound/joinSound.wav

            var player = new SoundPlayer(Path.Combine(Directory.GetCurrentDirectory(), "sound", "joinSound.wav"));
            player.Play();
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
    }
}