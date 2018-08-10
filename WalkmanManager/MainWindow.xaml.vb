Imports MaterialDesignThemes.Wpf
Imports WalkmanManager.Database

Class MainWindow
	Private Sub czTitle_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles CzTitle.MouseLeftButtonDown
		DragMove()
	End Sub

	Private Sub btn_window_minimize_Click(sender As Object, e As RoutedEventArgs) Handles BtnWindowMinimize.Click
		Me.WindowState = WindowState.Minimized
	End Sub

	Private Sub btn_window_close_Click(sender As Object, e As RoutedEventArgs) Handles BtnWindowClose.Click
		Environment.Exit(0)
	End Sub

	Private Async Sub MainWindow_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
		If Not My.Computer.FileSystem.FileExists("data.db") Then
			CreateDatabase()
		End If
		Dim dlg As New dlg_progress
		DialogHost.Show(dlg, "window-root")
		Dim upd As New DbUpdater
		Dim songDir = GetSetting("song_dir")
		If IsNothing(songDir) Then
			My.Computer.FileSystem.CreateDirectory("SongLib")
			SaveSetting("song_dir", "SongLib")
		End If
		Dim songs As List(Of SongInfo)
		Try
			Dim newLost = Await Task.Run(Async Function()
											 Dim lstNew = Await upd.FindNew(GetSetting("song_dir"))
											 Dim lstLost = Await upd.FindLost()
											 songs = GetSongs()
											 Return New Object() {lstNew, lstLost}
										 End Function)
		Catch ex As TaskCanceledException
		Catch unknown As Exception
		End Try
		DatSongList.ItemsSource = songs
		'Console.WriteLine(lstNew)
		DlgWindowRoot.IsOpen = False
	End Sub

End Class
