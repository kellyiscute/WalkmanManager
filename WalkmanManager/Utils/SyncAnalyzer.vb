Imports WalkmanManager.Database
Imports WalkmanManager.CloudMusic

Public Class SyncAnalyzer

	Public Shared Function SyncPlaylist(playlistName As String, musicTracks As List(Of CloudMusicTracks), Optional dlg As dlg_progress = Nothing) As List(Of CloudMusicTracks)

		Dim lstFailed As New List(Of CloudMusicTracks)

		If Not IsNothing(dlg) Then
			dlg.Text = "Loading"
		End If

		If CheckPlaylistNameAvailability(playlistName) Then
			'Playlist does not exist locally
			AddPlaylist(playlistName)
		Else
			'Clear
			ClearPlaylist(GetPlaylistIdByName(playlistName))
		End If

		Dim id = GetPlaylistIdByName(playlistName)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		Dim counter = 0
		Dim currentProgress = 0
		For Each track As CloudMusicTracks In musicTracks
			Dim path = SongExists(track.Title, track.Artists, cmd)
			If path.Length = 0 Then
				lstFailed.Add(track)
			Else
				Dim songId = GetSongId(path, cmd)
				AddSongToPlaylist(id, songId, cmd, counter)
				counter += 1
			End If
			currentProgress += 1
			If Not IsNothing(dlg) Then
				dlg.Text = String.Format("{0}/{1}", currentProgress, musicTracks.Count)
			End If
		Next
		conn.Close()

		Return lstFailed
	End Function

End Class