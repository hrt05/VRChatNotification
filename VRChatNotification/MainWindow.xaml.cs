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
            Task.Run(() => ReadFile());
        }

        public enum LogType
        {
            Unknown,
            PlayerJoin,
            PlayerLeft,
        }

        /* 
         * 変数などをここに記載
         */

        //private CancellationToken? _ct;
        private CancellationTokenSource? _cts;
        private MediaPlayer _player = new MediaPlayer();
        private string _soundPath = Path.Combine(Directory.GetCurrentDirectory(), "sound", "joinSound.wav");
        private string _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low", "VRChat", "VRChat");

        /* 
         * 関数などをここに記載
         */


        // 音楽の再生準備
        private async Task PlaySound()
        {
            _player.Open(new Uri(_soundPath));
            _player.Play();
        }

        // ボタンを押した際に、上記の関数を再生
        private async void Ignition(object sender, RoutedEventArgs e)
        {
            try
            {
                await PlaySound();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        // スライダーを調整して、0~100を1/10にしています。
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine("現在の値" + e.NewValue / 100);
            _player.Volume = e.NewValue / 100;
        }

        // テストボタンです。　後で消してください。
        //private void logCheck_Click(object sender, RoutedEventArgs e)
        //{
        //    ReadFile();
        //}

        private LogType AnalysisDef(string log)
        {
            Debug.WriteLine("logの中身", log);

            return LogType.Unknown;
        }

        private FileInfo? LatestRogFile()
        {
            var latestF = Directory.GetFiles(_logDir, "output_log_*.txt")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

                return latestF;
        }

        private void AnalysisSound(string line)
        {
            if (line.Contains("[Behaviour] OnPlayerJoined"))
            {
                Dispatcher.Invoke(() => PlaySound());
            }
        }

        private async Task ReadFile()
        {
            try
            {
                var latestF = LatestRogFile();

                if (latestF == default || latestF == null)
                {
                    Debug.WriteLine("ログファイルが見つかりません");
                    MessageBox.Show("ログファイルが見つかりません");
                    return;
                }

                // FileStream: Fileを開く担当
                // Fileの読み込みから！ Openして、Readのみ。　書き込み中でも読めるように
                // IDisposable だからusingを使用
                using var rf = new FileStream(latestF.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // StreamReader: バイトを「文字として読む」担当
                using var sr = new StreamReader(rf);



                // 全行
                //string? line;
                //line = sr.ReadLine();
                while (sr.ReadLine() != null)
                {
                    //AnalysisDef(line);
                }

                _cts = new CancellationTokenSource();
                //_ct = new CancellationTokenSource().Token;

                //while (_ct is CancellationToken token && !token.IsCancellationRequested)
                while (!_cts.Token.IsCancellationRequested)
                    {
                    string? line;
                    line = sr.ReadLine();

                    if (line != null)
                    {
                        AnalysisSound(line);
                    }
                    else
                    {
                        await Task.Delay(500, _cts.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("エラーでてます", ex);
                MessageBox.Show($"エラーが発生しました。{ex}");
            }
        }

        private void boolButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }
    }
}