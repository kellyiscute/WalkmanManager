Imports System.Collections.ObjectModel
Imports WalkmanManager.CloudMusic
Imports LibVLCSharp.Shared

Public Class DlgChooseLyric

    Dim _songInfo As Database.SongInfo
    Dim libVlc As LibVLC
    Dim MediaPlayer As MediaPlayer

    Public Sub New(searchResults As IEnumerable(Of ThridPartyCloudMusicApi.SearchResult), songInfo As Database.SongInfo)

        ' 此调用是设计器所必需的。
        InitializeComponent()

        ' 在 InitializeComponent() 调用之后添加任何初始化。
        _songInfo = songInfo
        DataGridSearchResults.ItemsSource = New ObservableCollection(Of ThridPartyCloudMusicApi.SearchResult)(searchResults)
        TextBlockTitle.Text = $"正在为 {songInfo.Title} 选择歌词"
        ' Init VLC
        Core.Initialize()
        libVlc = New LibVLC()
        MediaPlayer = New MediaPlayer(libVlc)
        MediaPlayer.Media = New Media(libVlc, songInfo.Path, FromType.FromPath)
    End Sub

    Private Sub DlgChooseLyric_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        TextTotalPlayTime.Content = MediaPlayer.Length  'TODO: Render ms into form of 00:00.000
    End Sub
End Class
