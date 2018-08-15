Imports WalkmanManager.Database

Public Class DlgNewPlaylist

	Public Property PlaylistName As String

	Private Async Sub TextBox_TextChanged(sender As TextBox, e As TextChangedEventArgs)
		ButtonDone.IsEnabled = False
		PlaylistName = sender.Text.Trim()
		Dim result = Await Task.Run(Function()
										Return CheckPlaylistNameAvailability(PlaylistName)
									End Function)
		If result Then
			LabelValidation.Visibility = Visibility.Visible
		Else
			LabelValidation.Visibility = Visibility.Hidden
		End If
		ButtonDone.IsEnabled = Not result
	End Sub

End Class
