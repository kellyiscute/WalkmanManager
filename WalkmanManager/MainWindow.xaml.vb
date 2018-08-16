Imports System.Collections.ObjectModel
Imports System.Windows.Shell
Imports MaterialDesignThemes.Wpf
Imports WalkmanManager.Database
Imports ATL
Imports System.Linq
Imports GongSolutions.Wpf.DragDrop

Class MainWindow
	Dim _lstSongs As ObservableCollection(Of SongInfo)

	Private Sub czTitle_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) _
		Handles CzTitle.MouseLeftButtonDown
		Try
			DragMove()
		Catch ex As Exception

		End Try
	End Sub

	Private Sub btn_window_minimize_Click(sender As Object, e As RoutedEventArgs) Handles BtnWindowMinimize.Click
		WindowState = WindowState.Minimized
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

		Dim newLost = Await Task.Run(Async Function()
										 Dim lstNew = Await upd.FindNew(GetSetting("song_dir"))
										 Dim lstLost = Await upd.FindLost()
										 _lstSongs = GetSongs()
										 Dim syncResult As String = ""
										 If lstNew.Count > 0 Then
											 syncResult = "=========================发现以下新项目=========================" & vbNewLine
											 For Each NewItem In lstNew
												 syncResult += NewItem & vbNewLine
											 Next
										 End If
										 If lstLost.Count > 0 Then
											 syncResult += "=========================发现已删除项目=========================" & vbNewLine
											 For Each LostItem In lstLost
												 syncResult += LostItem & vbNewLine
											 Next
										 End If
										 Return syncResult
									 End Function)
		Dim lstPlaylist = Await Task.Run(Function()
											 Dim r = GetPlaylists()
											 Return r
										 End Function)
		For Each itm In lstPlaylist
			ListBoxPlaylist.Items.Insert(ListBoxPlaylist.Items.Count - 1, New ListBoxItem() With {.Content = itm})
		Next
		DatSongList.ItemsSource = _lstSongs
		DlgWindowRoot.IsOpen = False
		If newLost <> "" Then
			Dim dlgSyncResult = New dlgDirSyncResult(newLost)
			Await DialogHost.Show(dlgSyncResult, "window-root")
		End If
	End Sub

	Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
		WindowChrome.SetWindowChrome(Me,
									New WindowChrome() _
										With {.GlassFrameThickness = New Thickness(1),
										.UseAeroCaptionButtons = True, .ResizeBorderThickness = New Thickness(5),
										.CornerRadius = New CornerRadius(0),
										.CaptionHeight = 0})
	End Sub

	Private Async Sub DatSongList_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) _
		Handles DatSongList.MouseDoubleClick
		If DatSongList.SelectedIndex <> -1 Then
			DlgWindowRoot.CloseOnClickAway = True
			Dim detailDialog As New DlgSongDetail(DatSongList.SelectedItem.path)
			Await DlgWindowRoot.ShowDialog(detailDialog)

			If Not detailDialog.IsDeleted Then
				If detailDialog.IsChanged Then
					UpdateInfo(DatSongList.SelectedItem.path)
					Dim t As New Track(DatSongList.SelectedItem.path)

					Dim index = _lstSongs.IndexOf(DatSongList.SelectedItem)
					_lstSongs(index) = New SongInfo _
						With {.Id = DatSongList.SelectedItem.Id, .Title = t.Title, .Artists = t.Artist, .Path =
							DatSongList.SelectedItem.path}
				End If
			Else
				_lstSongs.RemoveAt(DatSongList.SelectedIndex)
			End If
		End If
	End Sub

	Private Sub ListBoxItem_Drop(sender As Object, e As DragEventArgs)
		Console.WriteLine(e.Data.GetFormats())
		Try
			Dim songInf As SongInfo = e.Data.GetData(DragDrop.DataFormat.Name)
		Catch ex As Exception
			Dim a = e.Data.GetData("GongSolutions.Wpf.DragDrop")
			Console.WriteLine(a.ToString)
			For Each itm As SongInfo In a
				Console.WriteLine(itm.Title)
			Next
		End Try
	End Sub

	Private Async Sub ListItem_SelectionChange(sender As ListBox, e As EventArgs)
		If sender.SelectedItem.Tag = "NewPlaylist" Then
			Dim dlg As New DlgNewPlaylist
			Dim result = Await DlgWindowRoot.ShowDialog(dlg)
			If result Then
				AddPlaylist(dlg.PlaylistName)
				ListBoxPlaylist.Items.Insert(ListBoxPlaylist.Items.Count - 1, New ListBoxItem() With {.Content = dlg.PlaylistName})
			End If
		End If
	End Sub

	Private Sub ButtonMusic_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles ButtonMusic.MouseLeftButtonUp
		DatSongList.ItemsSource = _lstSongs
	End Sub
End Class
