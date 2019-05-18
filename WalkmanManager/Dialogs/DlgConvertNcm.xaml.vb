Public Class DlgConvertNcm
	Public MustOverride Sub Close Handles ButtonClose.Click

	Private async Sub DlgConvertNcm_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
		Dim files As List(Of String)
		files = Await Task.Run(AddressOf SearchNcm)
		For Each f In files
			Dim itm As New NcmItem(f)
			StackPanelFiles.Children.Add(itm)
		Next
	End Sub

	Private Function SearchNcm() As List(Of String)
		Dim files As New List(Of String)
		For each f In My.Computer.FileSystem.GetFiles("", FileIO.SearchOption.SearchAllSubDirectories, "*.ncm")
			files.Add(f)
		Next
		Return files
	End Function
End Class
