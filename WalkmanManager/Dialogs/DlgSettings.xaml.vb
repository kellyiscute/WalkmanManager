Imports System.Windows.Forms
Imports MaterialDesignThemes.Wpf.DialogHostEx

Public Class DlgSettings
	Public Property FlgForceRestart As Boolean = False

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
				FlgForceRestart = True
			End If
		End Using
	End Sub

	Private Sub showOKDialog()
		Dim dlg As New DlgMessageDialog("", "操作完成")
		Dialog.ShowDialog(dlg)
	End Sub

	Private Sub ButtonClearPlaylist_Click(sender As Object, e As RoutedEventArgs) Handles ButtonClearPlaylist.Click
		Database.InitPlaylists()
		showOKDialog()
	End Sub

	Private Sub ButtonClearSongs_Click(sender As Object, e As RoutedEventArgs) Handles ButtonClearSongs.Click
		Database.InitSongLib()
		showOKDialog()
	End Sub

	Private Sub ButtonRebuildDb_Click(sender As Object, e As RoutedEventArgs) Handles ButtonRebuildDb.Click
		FlgForceRestart = True
		Database.ClearDatabase()
		showOKDialog()
	End Sub
End Class
