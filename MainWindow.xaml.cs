using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace test
{
    public partial class MainWindow
    {
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer();
        private readonly List<string> _listeningHistory = [];
        private readonly DispatcherTimer _timer;
        private List<string> _musicFiles = [];
        private int _currentTrackIndex;
        private bool _isRepeating;
        private bool _isShuffling;
        private bool _isPlaying;

        public MainWindow()
        {
            InitializeComponent();
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
        }
        
        private void FilesButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
            var res = dlg.ShowDialog();
            if (res != CommonFileDialogResult.Ok) return;
            string[] audioExtensions = [".mp3", ".m4a", ".wav", ".ogg"];
            _musicFiles = Directory.GetFiles(dlg.FileName).Where(file => audioExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)).ToList();
            if (_musicFiles.Count <= 0) return;
            {
                MusicBox.Items.Clear();
                foreach (var file in _musicFiles) { MusicBox.Items.Add(Path.GetFileName(file)); }
                PlayTrack(0);
            }
            UpdateCurrentlyPlayingSong();
        }
        
        private void ShowListeningHistoryWindow()
        {
            var historyWindow = new History(_listeningHistory);
            var result = historyWindow.ShowDialog();
            if (result != true) return;
            var selectedSong = historyWindow.SelectedSong;
            if (selectedSong == null) return;
            var index = _listeningHistory.IndexOf(selectedSong);
            if (index == -1) return;
            var trackIndex = _musicFiles.FindIndex(s => Path.GetFileName(s) == selectedSong);
            if (trackIndex == -1) return;
            _currentTrackIndex = trackIndex;
            UpdateCurrentlyPlayingSong();
            PlayTrack(_currentTrackIndex);
        }
        
        private void PlayTrack(int trackIndex)
        {
            if (trackIndex < 0 || trackIndex >= _musicFiles.Count) return;
            _mediaPlayer.Open(new Uri(_musicFiles[trackIndex]));
            _mediaPlayer.Play();
            _isPlaying = true;
            UpdateCurrentlyPlayingSong();
            UpdatePlayPauseButton();
            UpdateCurrentTrackInfo();
            StartTimer();
            AddToListeningHistory(_musicFiles[trackIndex]);
        }
        
        private void AddToListeningHistory(string song)
        {
            var fileName = Path.GetFileName(song);
            if (!_listeningHistory.Contains(fileName))
                _listeningHistory.Add(fileName);
        }
        
        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            if (_isRepeating)
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
            }
            else if (_isShuffling)
            {
                var random = new Random();
                _currentTrackIndex = random.Next(0, _musicFiles.Count);
                PlayTrack(_currentTrackIndex);
            }
            else
            {
                _currentTrackIndex++;
                if (_currentTrackIndex >= _musicFiles.Count)
                    _currentTrackIndex = 0;
                PlayTrack(_currentTrackIndex);
            }
        }
        
        private void ListeningHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowListeningHistoryWindow();
        }
        
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                _mediaPlayer.Pause();
                _isPlaying = false;
            }
            else
            {
                _mediaPlayer.Play();
                _isPlaying = true;
            }
            UpdatePlayPauseButton();
            UpdateCurrentlyPlayingSong();
        }
        
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentTrackIndex++;
            if (_currentTrackIndex >= _musicFiles.Count)
                _currentTrackIndex = 0;
            PlayTrack(_currentTrackIndex);
            UpdateCurrentlyPlayingSong();
        }
        
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            _currentTrackIndex--;
            if (_currentTrackIndex < 0)
                _currentTrackIndex = _musicFiles.Count - 1;
            PlayTrack(_currentTrackIndex);
            UpdateCurrentlyPlayingSong();
        }
        
        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            _isRepeating = !_isRepeating;
            RepeatButton.Content = _isRepeating ? "Повтор Вкл" : "Повтор Выкл";
            UpdateCurrentlyPlayingSong();
        }
        
        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            _isShuffling = !_isShuffling;
            ShuffleButton.Content = _isShuffling ? "Shuffle Вкл" : "Shuffle Выкл";
            UpdateCurrentlyPlayingSong();
        }
        
        private void UpdatePlayPauseButton()
        {
            PlayPauseButton.Content = _isPlaying ? "Стоп" : "Старт";
            UpdateCurrentlyPlayingSong();
        }
        
        private void UpdateCurrentlyPlayingSong()
        {
            if (_currentTrackIndex < 0 || _currentTrackIndex >= MusicBox.Items.Count) return;
            MusicBox.SelectedIndex = _currentTrackIndex;
            MusicBox.ScrollIntoView(MusicBox.SelectedItem);
        }
        
        private void UpdateCurrentTrackInfo()
        {
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan) return;
            var total = _mediaPlayer.NaturalDuration.TimeSpan;
            var current = _mediaPlayer.Position;
            var remain = total - current;
            ProgressSlider.Minimum = 0;
            ProgressSlider.Maximum = total.TotalSeconds;
            ProgressSlider.Value = current.TotalSeconds;
            CurrentTimeTextBlock.Text = current.ToString(@"mm\:ss");
            RemainingTimeTextBlock.Text = remain.ToString(@"mm\:ss");
        }
        
        private void SliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Task.Run(UpdateCurrentTrackInfo);
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan) return;
            var newPosition = TimeSpan.FromSeconds(ProgressSlider.Value);
            _mediaPlayer.Position = newPosition;
        }
        
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mediaPlayer.Volume = VolumeSlider.Value;
        }
        
        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateCurrentTrackInfo();
        }
        
        private void StartTimer()
        {
            _timer.Start();
        }
    }
}
