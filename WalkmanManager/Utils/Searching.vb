Imports System.Collections.ObjectModel
Imports WalkmanManager.Database

Public Class Searching
	Public Shared Function SearchSongs(lst As ObservableCollection(Of SongInfo), str As String) As ObservableCollection(Of SongInfo)
		Dim result As New ObservableCollection(Of SongInfo)

		'Full Match
		'Song Title First
		Dim matches = From itm In lst Where itm.Title.ToLower = str.ToLower() Select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next
		'Artist Second(Full Match At least One)
		matches = From itm In lst Where itm.Artists.Split("/").Contains(str) Select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next
		'WordMatch
		matches = From itm In lst Where itm.Title.Split(" ").Contains(str) And Not result.Contains(itm) Select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next
		matches = From itm In lst Where itm.Artists.Split(" ").Contains(str) And Not result.Contains(itm) Select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next
		'Partial Matches
		matches = From itm In lst Where itm.Title.Contains(str) And Not result.Contains(itm) Select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next
		matches = From itm In lst Where itm.Artists.Contains(str) And Not result.Contains(itm) Select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next
		'Letter Matches
		matches = From itm In lst Where itm.Title.Contains(str.ToStringArray()) And Not result.Contains(itm) Select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next
		matches = From itm In lst Where itm.Artists.Contains(str.ToStringArray()) And Not result.Contains(itm) Select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next

		Return result
	End Function
End Class
