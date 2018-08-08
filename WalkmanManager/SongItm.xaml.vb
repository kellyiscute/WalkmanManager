Public Class SongItm

	Public Event Checked(sender As SongItm, ischecked As Boolean)

	Public Sub New(songName As String, artists As String, img As String, lyric As Boolean)

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		ToggleLyric(lyric)
		lblTitle.Text = songName
		lblArtist.Text = artists
		If My.Computer.FileSystem.FileExists(img) Then
			Dim i As New BitmapImage(New Uri(img))
			imgAlbum.Source = i
		End If
	End Sub

	Private Sub ToggleLyric(lyric As Boolean)
		If Lyric Then
			flgLyric.Background = New SolidColorBrush(Colors.Red)
			flgLyric.Foreground = New SolidColorBrush(Colors.White)
		Else
			flgLyric.Background = New SolidColorBrush(Colors.White)
			flgLyric.Foreground = New SolidColorBrush(Colors.Red)
		End If
	End Sub

	Public Overrides Function ToString() As String
		Return lblTitle.Text
	End Function

	Public Property IsChecked As Boolean
		Get
			Return chkChecked.IsChecked
		End Get
		Set(value As Boolean)
			chkChecked.IsChecked = value
			RaiseEvent Checked(Me, chkChecked.IsChecked)
		End Set
	End Property

	Private Sub Border_MouseDown(sender As Object, e As MouseButtonEventArgs)
		border.Background = New SolidColorBrush(Color.FromRgb(&HDA, &HDA, &HDA))
		IsChecked = Not IsChecked
	End Sub

	Private Sub Border_MouseEnter(sender As Object, e As MouseEventArgs)
		border.Background = New SolidColorBrush(Colors.GhostWhite)
	End Sub

	Private Sub border_MouseLeave(sender As Object, e As MouseEventArgs) Handles border.MouseLeave
		border.Background = New SolidColorBrush(Colors.WhiteSmoke)
	End Sub

	Private Sub border_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles border.MouseUp
		border.Background = New SolidColorBrush(Colors.GhostWhite)
	End Sub
End Class
