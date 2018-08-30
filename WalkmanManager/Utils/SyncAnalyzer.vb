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
		Dim trans = conn.BeginTransaction()
		cmd.Transaction = trans
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
		trans.Commit()
		conn.Close()

		Return lstFailed
	End Function

	Public Shared Function SyncAllPlaylists(cloudMusicObject As CloudMusic, Optional dlg As dlg_progress = Nothing) As String
		Dim playlists = cloudMusicObject.GetPlaylists()
		If Not IsNothing(dlg) Then
			dlg.Text = "获取歌单"
		End If
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		Dim trans = conn.BeginTransaction()
		cmd.Transaction = trans

		Dim rString As String = ""
		Dim currentProgress = 0

		For i = 0 To playlists.Count - 1

			Dim playlistName = playlists(i)("name")
			Dim lstFailed As New List(Of CloudMusicTracks)
			Dim counter = 0

			Dim id
			If CheckPlaylistNameAvailability(playlistName, cmd) Then
				'Playlist does not exist locally
				AddPlaylist(playlistName, cmd)
				id = GetPlaylistIdByName(playlistName, cmd)
			Else
				'Clear
				id = GetPlaylistIdByName(playlistName, cmd)
				ClearPlaylist(id, cmd)
			End If
			trans.Commit()
			trans = conn.BeginTransaction
			cmd.Transaction = trans

			Dim plDetail = cloudMusicObject.GetPlaylistDetail(playlists(i)("id"))

			If Not IsNothing(dlg) Then
				dlg.Text = String.Format("{0}/{1}", currentProgress, playlists.Count)
			End If

			For Each d As CloudMusicTracks In plDetail("tracks")
				Dim path = SongExists(d.Title, d.Artists, cmd)
				If path.Length = 0 Then
					lstFailed.Add(d)
				Else
					Dim songId = GetSongId(path, cmd)
					Try
						AddSongToPlaylist(id, songId, cmd, counter, True)
					Catch
						Console.WriteLine(String.Format("{0}: {1}", id, songId))
					End Try
					counter += 1
				End If
			Next

			rString += "同步 """ & playlistName & """ 完成" & vbTab & "失败 " & lstFailed.Count & " 个"
			rString += vbNewLine & "============================================"
			For Each itmCloudMusicTracks As CloudMusicTracks In lstFailed
				rString += vbNewLine & itmCloudMusicTracks.Title & " - " & itmCloudMusicTracks.Artists
			Next
			rString += vbNewLine

			currentProgress += 1
		Next

		trans.Commit()
		conn.Close()

		Return rString
	End Function

End Class