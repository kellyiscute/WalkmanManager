Imports System.Collections.ObjectModel
Imports WalkmanManager.Database
Imports System.Linq

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
		matches = from itm In lst where itm.Title.Split(" ").Contains(str) select itm
		For Each songInfo As SongInfo In matches
			result.Add(songInfo)
		Next

	End Function
End Class
