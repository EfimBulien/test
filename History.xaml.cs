using System.Windows.Controls;

namespace test;

public partial class History
{
    
    private readonly List<string> _listeningHistory; 
    public string? SelectedSong { get; private set; }
    
    public History(List<string> listeningHistory)
    {
        InitializeComponent();
        _listeningHistory = listeningHistory;
        PopulateListeningHistory();
    }
    
    private void PopulateListeningHistory()
    {
        foreach (var item in _listeningHistory.Select(song => new ListBoxItem { Content = song }))
        {
            HistoryListBox.Items.Add(item);
        }
    }
    
    private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryListBox.SelectedItem == null) return;
        SelectedSong = HistoryListBox.SelectedItem.ToString();
        DialogResult = true;
        Close();
    }
}
