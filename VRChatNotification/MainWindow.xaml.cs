using System.Diagnostics;
using System.IO;
using System.Text.Json;
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
            checkJson();
            Task.Run(() => ReadFile());
            authUserNameTextBlock.Text = "ユーザーネーム不明";
            authUserIdTextBlock.Text = "ユーザーID不明";
        }

        /* 
         * 変数などをここに記載
         */

        // ↓取り消す必要があることを CancellationToken に通知します。
        private CancellationTokenSource? _cts;
        private MediaPlayer _player = new MediaPlayer();
        private SelectClass _selectClass = new SelectClass();
        private InstanceTypeClass _instanceTypeClass = new InstanceTypeClass();
        private string _joinSoundPath = Path.Combine(Directory.GetCurrentDirectory(), "sound", "joinSound.wav");
        private string _leftSoundPath = Path.Combine(Directory.GetCurrentDirectory(), "sound", "leftSound.wav");
        private string _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low", "VRChat", "VRChat");
        private bool _interrupt = true;
        private bool _isWorld = true;
        private string _userName = "unknown User";
        private string _authUserId = "noAuthUser";
        private string _wasThere = "noInstance";

        string fileName = "SelectingInstance.json";
        string _documentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VRCNotification");


        public InstanceType currentInstance { get; private set; } = InstanceType.Unknown;



        //public SelectType selectPublic { get; private set; } = SelectType.NoSound;
        //public SelectType selectGroupPublic { get; private set; } = SelectType.NoSound;
        //public SelectType selectGroupPlus { get; private set; } = SelectType.NoSound;
        //public SelectType selectGroup { get; private set; } = SelectType.NoSound;
        //public SelectType selectHidden { get; private set; } = SelectType.NoSound;
        //public SelectType selectFriends { get; private set; } = SelectType.NoSound;
        //public SelectType selectPrivatePlus { get; private set; } = SelectType.NoSound;
        //public SelectType selectPrivate { get; private set; } = SelectType.NoSound;
        //public SelectType selectUnknown { get; private set; } = SelectType.NoSound;

        //var noSoundColor = (Color)


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
            else if (_interrupt == false)
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
            }
            else if (line.Contains("[Behaviour] OnLeftRoom"))
            {
                _isWorld = false;
                currentInstance = InstanceType.Unknown;
                Dispatcher.Invoke(() => currentInstanceText.Text= "所在地無し");
                //} else if (line.Contains("[Behaviour] Successfully joined room")) 
                //{
                //    //Thread.Sleep(300);
                //    _isWorld = true;
            }
            else if (line.Contains("[Behaviour] OnPlayerJoined") && line.Contains(_userName))
            {
                _isWorld = true;
            }
            else if (line.Contains("VRCApplication: HandleApplicationQuit"))
            {
                _cts?.Cancel();
                /** 
                 後で確認なのですが、中断せずvrc落としても続けたい人が居るのでは？
                 */
                //_interrupt = false;
                _isWorld = false;
                currentInstance = InstanceType.Unknown;
                Dispatcher.Invoke(() => authUserNameTextBlock.Text = "オフライン");
                Dispatcher.Invoke(() => authUserIdTextBlock.Text = "");
                Dispatcher.Invoke(() => currentInstanceText.Text = "停止中");
            }


            if (line.Contains("[Behaviour] Joining wrld_"))
            {
                mutualInstanceType(line);
            }
        }

        private void mutualInstanceType(string line)
        {
            currentInstance = _instanceTypeClass.instanceTypeClassDef(line);

            Dispatcher.Invoke(() =>
            {
                currentInstanceText.Text = currentInstance switch
                {
                    InstanceType.Private => "インバイト",
                    InstanceType.PrivatePlus => "インバイト+",
                    InstanceType.Friends => "フレンド",
                    InstanceType.Hidden => "フレンド+",
                    InstanceType.Group => "グループ",
                    InstanceType.GroupPlus => "グループ+",
                    InstanceType.GroupPublic => "グループパブリック",
                    InstanceType.Public => "パブリック",
                    InstanceType.Unknown => "所在地無し",
                    _ => "不明",
                };
            });
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
                    Dispatcher.Invoke(() => authUserNameTextBlock.Text = _userName);
                    Dispatcher.Invoke(() => authUserIdTextBlock.Text = _authUserId);
                }
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


                // 初回全行読み込み
                string? firstLine;
                while ((firstLine = sr.ReadLine()) != null)
                {
                    Debug.WriteLine(firstLine);
                    FirstLogDef(firstLine);
                    
                    if (firstLine.Contains("[Behaviour] Joining wrld_"))
                    {
                        _wasThere = firstLine;
                    }
                }

                mutualInstanceType(_wasThere);

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

        public SolidColorBrush noSoundColor = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        public SolidColorBrush joinOnlyColor = new SolidColorBrush(Color.FromRgb(178, 210, 210));
        public SolidColorBrush joinLeftColor = new SolidColorBrush(Color.FromRgb(191, 178, 210));


        private void publicBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_selectClass.SelectPublic)
            {
                case SelectType.NoSound:
                    publicBtn.Background = joinOnlyColor;
                    _selectClass.SelectPublic = SelectType.JoinOnlySound;
                    changeJson();
                    break;
                case SelectType.JoinOnlySound:
                    publicBtn.Background = joinLeftColor;
                    _selectClass.SelectPublic = SelectType.JoinLeftSound;
                    changeJson();
                    break;
                case SelectType.JoinLeftSound:
                    publicBtn.Background = noSoundColor;
                    _selectClass.SelectPublic = SelectType.NoSound;
                    changeJson();
                    break;
            }
        }

        private void groupPublicBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_selectClass.SelectGroupPublic)
            {
                case SelectType.NoSound:
                    groupPublicBtn.Background = joinOnlyColor;
                    _selectClass.SelectGroupPublic = SelectType.JoinOnlySound;
                    changeJson();
                    break;
                case SelectType.JoinOnlySound:
                    groupPublicBtn.Background = joinLeftColor;
                    _selectClass.SelectGroupPublic = SelectType.JoinLeftSound;
                    changeJson();
                    break;
                case SelectType.JoinLeftSound:
                    groupPublicBtn.Background = noSoundColor;
                    _selectClass.SelectGroupPublic = SelectType.NoSound;
                    changeJson();
                    break;
            }
        }

        private void groupPlusBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_selectClass.SelectGroupPlus)
            {
                case SelectType.NoSound:
                    groupPlusBtn.Background = joinOnlyColor;
                    _selectClass.SelectGroupPlus = SelectType.JoinOnlySound;
                    changeJson();
                    break;
                case SelectType.JoinOnlySound:
                    groupPlusBtn.Background = joinLeftColor;
                    _selectClass.SelectGroupPlus = SelectType.JoinLeftSound;
                    changeJson();
                    break;
                case SelectType.JoinLeftSound:
                    groupPlusBtn.Background = noSoundColor;
                    _selectClass.SelectGroupPlus = SelectType.NoSound;
                    changeJson();
                    break;
            }
        }

        private void groupBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_selectClass.SelectGroup)
            {
                case SelectType.NoSound:
                    groupBtn.Background = joinOnlyColor;
                    _selectClass.SelectGroup = SelectType.JoinOnlySound;
                    changeJson();
                    break;
                case SelectType.JoinOnlySound:
                    groupBtn.Background = joinLeftColor;
                    _selectClass.SelectGroup = SelectType.JoinLeftSound;
                    changeJson();
                    break;
                case SelectType.JoinLeftSound:
                    groupBtn.Background = noSoundColor;
                    _selectClass.SelectGroup = SelectType.NoSound;
                    changeJson();
                    break;
            }
        }

        private void hiddenBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_selectClass.SelectHidden)
            {
                case SelectType.NoSound:
                    hiddenBtn.Background = joinOnlyColor;
                    _selectClass.SelectHidden = SelectType.JoinOnlySound;
                    changeJson();
                    break;
                case SelectType.JoinOnlySound:
                    hiddenBtn.Background = joinLeftColor;
                    _selectClass.SelectHidden = SelectType.JoinLeftSound;
                    changeJson();
                    break;
                case SelectType.JoinLeftSound:
                    hiddenBtn.Background = noSoundColor;
                    _selectClass.SelectHidden = SelectType.NoSound;
                    changeJson();
                    break;
            }
        }

        private void friendsBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_selectClass.SelectFriends)
            {
                case SelectType.NoSound:
                    friendsBtn.Background = joinOnlyColor;
                    _selectClass.SelectFriends = SelectType.JoinOnlySound;
                    changeJson();
                    break;
                case SelectType.JoinOnlySound:
                    friendsBtn.Background = joinLeftColor;
                    _selectClass.SelectFriends = SelectType.JoinLeftSound;
                    changeJson();
                    break;
                case SelectType.JoinLeftSound:
                    friendsBtn.Background = noSoundColor;
                    _selectClass.SelectFriends = SelectType.NoSound;
                    changeJson();
                    break;
            }
        }

        private void privatePlusBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_selectClass.SelectPrivatePlus)
            {
                case SelectType.NoSound:
                    privatePlusBtn.Background = joinOnlyColor;
                    _selectClass.SelectPrivatePlus = SelectType.JoinOnlySound;
                    changeJson();
                    break;
                case SelectType.JoinOnlySound:
                    privatePlusBtn.Background = joinLeftColor;
                    _selectClass.SelectPrivatePlus = SelectType.JoinLeftSound;
                    changeJson();
                    break;
                case SelectType.JoinLeftSound:
                    privatePlusBtn.Background = noSoundColor;
                    _selectClass.SelectPrivatePlus = SelectType.NoSound;
                    changeJson();
                    break;
            }
        }

        private void privateBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (_selectClass.SelectPrivate)
            {
                case SelectType.NoSound:
                    privateBtn.Background = joinOnlyColor;
                    _selectClass.SelectPrivate = SelectType.JoinOnlySound;
                    changeJson();
                    break;
                case SelectType.JoinOnlySound:
                    privateBtn.Background = joinLeftColor;
                    _selectClass.SelectPrivate = SelectType.JoinLeftSound;
                    changeJson();
                    break;
                case SelectType.JoinLeftSound:
                    privateBtn.Background = noSoundColor;
                    _selectClass.SelectPrivate = SelectType.NoSound;
                    changeJson();
                    break;
            }
        }

        private void changeJson()
        {
            string jsonString = JsonSerializer.Serialize(_selectClass, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            if (!Directory.Exists(_documentPath))
            {
                Directory.CreateDirectory(_documentPath);
            }
            File.WriteAllText(Path.Combine(_documentPath, fileName), jsonString);
        }

        private void checkJson()
        {
            string jsonPath = Path.Combine(_documentPath, fileName);
            if (!File.Exists(jsonPath))
            {
                return;
            }
            string jsonReadData = File.ReadAllText(jsonPath);

            //using var sr = new StreamReader(_documentPath);
            //var jsonReadData = sr.ReadToEnd();

            _selectClass = JsonSerializer.Deserialize<SelectClass>(jsonReadData) ?? new SelectClass();
        }
    }
}