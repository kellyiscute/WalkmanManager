Imports System.Collections.ObjectModel
Imports WalkmanManager.Database
Imports WalkmanManager.CloudMusic.CloudMusic

Public Class DlgCloudMusicPlaylistSyncResult

    Dim _lstSongs As ObservableCollection(Of SongInfo)
    Dim _lstFailed As List(Of CloudMusicTracks)
    Dim _searchResults As ObservableCollection(Of SongInfo)
    Dim _nowSelecting As CloudMusicPlaylistSyncFailedItem
    Dim _playlistId As Integer

    Public Sub New(lstSongs As ObservableCollection(Of SongInfo), lstFailed As List(Of CloudMusicTracks), playlistId As Integer)

        ' 此调用是设计器所必需的。
        InitializeComponent()

        ' 在 InitializeComponent() 调用之后添加任何初始化。
        _lstSongs = lstSongs
        _lstFailed = lstFailed
        _playlistId = playlistId
        DataGridSearchResult.ItemsSource = _searchResults
        For Each f In lstFailed
            Dim itm As New CloudMusicPlaylistSyncFailedItem(f)
            StackPanelItems.Children.Add(itm)
            AddHandler itm.ButtonClicked, AddressOf FailedItem_ManualMatch_Click
        Next
    End Sub

    Private Sub FailedItem_ManualMatch_Click(sender As Object, e As CloudMusicPlaylistSyncFailedItem.ManualMatchClickEventArgs)
        TextBoxSearch.Text = e.track.Title
        GridSelectSong.Visibility = Visibility.Visible
        _nowSelecting = sender
    End Sub

    Private Sub DataGridSearchResult_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles DataGridSearchResult.MouseDoubleClick
        If Not IsNothing(DataGridSearchResult.SelectedItem) Then
            GridSelectSong.Visibility = Visibility.Hidden
            BindSong(_nowSelecting._track.Id, DataGridSearchResult.SelectedItem.Id)
            AddSongToPlaylist(_playlistId, DataGridSearchResult.SelectedItem.Id)
            _nowSelecting.ButtonMatch.IsEnabled = False
            _nowSelecting.ButtonMatch.Content = "匹配完成"
            TextBoxSearch.Text = ""
        End If
    End Sub

    Private Sub TextBoxSearch_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextBoxSearch.TextChanged
        If TextBoxSearch.Text = "" Then
            _searchResults = Nothing
            DataGridSearchResult.ItemsSource = _searchResults
            ButtonSelectDone.IsEnabled = False
        Else
            _searchResults = Searching.SearchSongs(_lstSongs, TextBoxSearch.Text)
            DataGridSearchResult.ItemsSource = _searchResults
        End If
    End Sub

    Private Sub DataGridSearchResult_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DataGridSearchResult.SelectionChanged
        If Not IsNothing(DataGridSearchResult.SelectedItem) Then
            ButtonSelectDone.IsEnabled = True
        Else
            ButtonSelectDone.IsEnabled = False
        End If
    End Sub

    Private Sub ButtonSelectDone_Click(sender As Object, e As RoutedEventArgs) Handles ButtonSelectDone.Click
        DataGridSearchResult_MouseDoubleClick(Nothing, Nothing)
    End Sub

    Private Sub ButtonSelectCancel_Click(sender As Object, e As RoutedEventArgs) Handles ButtonSelectCancel.Click
        TextBoxSearch.Text = ""
        GridSelectSong.Visibility = Visibility.Hidden
    End Sub
End Class
