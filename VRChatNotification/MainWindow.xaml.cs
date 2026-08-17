using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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
            authUserNameTextBlock.Text = "ユーザーネーム不明";
            authUserIdTextBlock.Text = "ユーザーID不明";
        }
        //public InstanceType CurrentInstanceType { get; private set; }

        //public enum LogType
        //{
        //    // 全プレイヤージョイン
        //    OnPlayerJoined,
        //    // 全プレイヤーレフト
        //    OnPlayerLeft,
        //    // 自分が初回join
        //    Joining,
        //    // 自分がシャットダウン
        //    HandleApplicationQuit,
        //}

        /* 
         * 変数などをここに記載
         */

        // ↓取り消す必要があることを CancellationToken に通知します。
        private CancellationTokenSource? _cts;
        private MediaPlayer _player = new MediaPlayer();
        private string _joinSoundPath = Path.Combine(Directory.GetCurrentDirectory(), "sound", "joinSound.wav");
        private string _leftSoundPath = Path.Combine(Directory.GetCurrentDirectory(), "sound", "leftSound.wav");
        private string _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low", "VRChat", "VRChat");
        private bool _interrupt = true;
        private bool _isWorld = true;
        private string _userName = "unknown User";
        private string _authUserId = "noAuthUser";
        
        public InstanceType _currentInstance { get; private set; } = InstanceType.Unknown;

        /* 
         * 関数などをここに記載
         */

        // 音楽の再生準備
        private async Task PlayJoinSound()
        {
            Debug.WriteLine("ここ疎通確認。2");
            _player.Open(new Uri(_joinSoundPath));
            _player.Play();
        }

        private async Task PlayLeftSound()
        {
            Debug.WriteLine("ここ疎通確認。3");
            _player.Open(new Uri(_leftSoundPath));
            _player.Play();
        }

        //public class Log
        //{
        //    public string Judgement
        //}


        // ボタンを押した際に、上記の関数を再生
        private async void Ignition(object sender, RoutedEventArgs e)
        {
            try
            {
                await PlayJoinSound();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        // 中断再生ボタン
        private void boolButton_Click(object sender, RoutedEventArgs e)
        {
            if (_interrupt == true)
            {
                _cts?.Cancel();
                _interrupt = false;
                Debug.WriteLine("今中断しました");
            }
            else if(_interrupt == false)
            {
                Task.Run(() => ReadFile());
                _interrupt = true;
                Debug.WriteLine("今再開しました");
            }
            else
            {
                return;
            }
        }

        // スライダーを調整して、0~100を1/10にしています。
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine("現在の値" + e.NewValue / 100);
            _player.Volume = e.NewValue / 100;
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
            if (line.Contains("[Behaviour] OnPlayerJoined") && !line.Contains(_authUserId) && _isWorld == true)
            {
                Dispatcher.Invoke(() => PlayJoinSound());
            }
            else if (line.Contains("[Behaviour] OnPlayerLeft") && !line.Contains(_authUserId) && _isWorld == true)
            {
                Dispatcher.Invoke(() => PlayLeftSound());
            } else if (line.Contains("[Behaviour] OnLeftRoom"))
            {
                _isWorld = false;
                //} else if (line.Contains("[Behaviour] Successfully joined room")) 
                //{
                //    //Thread.Sleep(300);
                //    _isWorld = true;
            }
            else if (line.Contains("[Behaviour] OnPlayerJoined") && line.Contains(_userName))
            {
                _isWorld = true;
            } else if (line.Contains("VRCApplication: HandleApplicationQuit"))
            {
                _cts?.Cancel();
                   /** 
                    後で確認なのですが、中断せずvrc落としても続けたい人が居るのでは？
                    */
                //_interrupt = false;
                _isWorld = false;
                Dispatcher.Invoke(() => authUserNameTextBlock.Text = "オフライン");
                Dispatcher.Invoke(() => authUserIdTextBlock.Text = "");
            }
        }



        // AuthUser確認

        // 最後の位置確認(もしvrc起動していなかったらOffline表記)
        private void FirstLogDef(string fLine)
        {
            if (fLine.Contains("User Authenticated"))
            {
                Match regexName = Regex.Match(fLine, @"User Authenticated: (.+?) \(");
                Match regexId = Regex.Match(fLine, @"usr_[a-f0-9-]+");
                if (regexId.Success && regexName.Success)
                {
                    _userName = regexName.Groups[1].Value;
                    _authUserId = regexId.Value;
                    Dispatcher.Invoke(() => authUserNameTextBlock.Text =  _userName);
                    Dispatcher.Invoke(() => authUserIdTextBlock.Text = _authUserId);
                }
            }
        }
        //{

        //}

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


                // 初回全行読み込み
                while (sr.ReadLine() != null)
                {
                    string? firstLine;
                    firstLine = sr.ReadLine();
                    //Debug.WriteLine(sr.ReadLine());
                    if (firstLine != null)
                    {
                        FirstLogDef(firstLine);
                    }
                }

                // ここから情報が変わったら処理
                _cts = new CancellationTokenSource();

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
            catch (TaskCanceledException)
            {
                Debug.WriteLine("停止しました。");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("エラーでてます", ex);
                MessageBox.Show($"エラーが発生しました。{ex}");
            }
        }

    }
}