Imports System.Windows.Forms

Public Class DlgSettings
	Public Property _flgForceRestart As Boolean = False

	Public Sub Init()
		TextBoxSongDir.Text = My.Computer.FileSystem.GetDirectoryInfo(Database.GetSetting("song_dir")).FullName
	End Sub

	Private Sub ButtonOpen_Click(sender As Object, e As RoutedEventArgs) Handles ButtonOpen.Click
		Using dlg As New FolderBrowserDialog
			Dim result = dlg.ShowDialog
			If result = DialogResult.OK Then
				Console.WriteLine(dlg.SelectedPath)
				Database.SaveSetting("song_dir", dlg.SelectedPath)
				TextBoxSongDir.Text = dlg.SelectedPath
				_flgForceRestart = True
			End If
		End Using
	End Sub

	Private Sub ButtonClearPlaylist_Click(sender As Object, e As RoutedEventArgs) Handles ButtonClearPlaylist.Click
		Database.InitPlaylists()
	End Sub

	Private Sub ButtonClearSongs_Click(sender As Object, e As RoutedEventArgs) Handles ButtonClearSongs.Click

	End Sub
End Class
