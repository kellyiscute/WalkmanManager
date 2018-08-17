Imports ATL

Public Class DlgSongDetail
	Private ReadOnly _songInfo As Track

	Public Property IsDeleted As Boolean = False
	Public Property IsChanged As Boolean = False

	Public Sub New(path As String)

		' 此调用是设计器所必需的。
		InitializeComponent()

		' 在 InitializeComponent() 调用之后添加任何初始化。
		_songInfo = New Track(path)
		TextBoxMusicTitle.Text = _songInfo.Title
		TextBoxMusicArtists.Text = _songInfo.Artist
		TextBoxMusicAlbum.Text = _songInfo.Album
		TextBoxMusicDuration.Text = ToTimeString(_songInfo.Duration)
		If _songInfo.Year = 0
			TextBoxMusicYear.Text = "未知"
		Else
			TextBoxMusicYear.Text = _songInfo.Year
		End If
		TextBoxMusicBitRate.Text = _songInfo.Bitrate & " kbps"
		If _songInfo.EmbeddedPictures.Count > 0 Then
			Dim img = New BitmapImage
			img.BeginInit()
			img.StreamSource = new IO.MemoryStream(_songInfo.EmbeddedPictures(0).PictureData)
			img.EndInit()
			ImageAlbum.Source = img
		End If
	End Sub

	private Function ToTimeString(seconds As Integer) As String
		Dim str As String
		str = seconds \ 60
		If str.Length = 1 Then
			str = 0 & str
		End If
		str += ":"
		Dim sec = (Seconds Mod 60).ToString()
		If sec.Length = 1 Then
			str += "0" & sec
		Else
			str += sec
		End If
		Return str
	End Function

	Private sub TextBox_Change(sender As TextBox, e As EventArgs) _
		Handles TextBoxMusicTitle.TextChanged, TextBoxMusicArtists.TextChanged, TextBoxMusicAlbum.TextChanged
		Select Case sender.Name
			Case "TextBoxMusicTitle"
				_songInfo.Title = sender.Text
			Case "TextBoxMusicArtists"
				_songInfo.Artist = sender.Text
			Case "TextBoxMusicAlbum"
				_songInfo.Album = sender.Text
		End Select
	End sub

	Private Sub ButtonSaveChanges_Click(sender As Object, e As RoutedEventArgs) Handles ButtonSaveChanges.Click
		IsChanged = True
		_songInfo.Save()
	End Sub

	Private Sub ButtonDelete_Click(sender As Object, e As RoutedEventArgs) Handles ButtonDelete.Click
		IsDeleted = True
	End Sub
End Class
