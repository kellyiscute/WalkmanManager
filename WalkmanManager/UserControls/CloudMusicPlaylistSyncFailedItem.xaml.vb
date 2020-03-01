Imports WalkmanManager.CloudMusic.CloudMusic

Public Class CloudMusicPlaylistSyncFailedItem

    Public _track As CloudMusicTracks
    Public Event ButtonClicked(sender As Object, e As EventArgs)

    Public Class ManualMatchClickEventArgs
        Inherits EventArgs

        Sub New(t)
            track = t
        End Sub

        ReadOnly Property track As CloudMusicTracks
    End Class

    Public Sub New(track As CloudMusicTracks)

        ' 此调用是设计器所必需的。
        InitializeComponent()

        ' 在 InitializeComponent() 调用之后添加任何初始化。
        _track = track
        LabelInfo.Content = track.Artists & "-" & track.Title
    End Sub

    Private Sub ButtonMatch_Click(sender As Object, e As RoutedEventArgs) Handles ButtonMatch.Click
        RaiseEvent ButtonClicked(Me, New ManualMatchClickEventArgs(_track))
    End Sub
End Class
