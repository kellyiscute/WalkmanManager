Imports MaterialDesignThemes.Wpf
Imports WalkmanManager.Database

Public Class DlgNewPlaylist

	Public Property PlaylistName As String

	Private Async Sub TextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
		ButtonDone.IsEnabled = False
		PlaylistName = sender.Text.Trim()
		Dim result = Await Task.Run(Function()
										Return CheckPlaylistNameAvailability(PlaylistName)
									End Function)
		If result Then
			LabelValidation.Visibility = Visibility.Hidden
		Else
			LabelValidation.Visibility = Visibility.Visible
		End If
		ButtonDone.IsEnabled = result
	End Sub

End Class
