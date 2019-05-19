Public Class DlgConvertNcm
	Public Event Close
	Public Event Minimize
	Private _flgDrag As Boolean = False
	Private _oldPoint As Point
	Private SongLibPath As String

	Public Sub New(songPath As String)

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		SongLibPath = songPath
	End Sub

	Public Sub Close_Click() Handles ButtonClose.Click
		RaiseEvent Close()
	End Sub

	Public Async Sub Init()
		Dim files As List(Of String)
		files = Await Task.Run(AddressOf SearchNcm)
		For Each f In files
			Dim itm As New NcmItem(f)
			StackPanelFiles.Children.Add(itm)
		Next
		IconCheck.Visibility = Visibility.Visible
		ProgressBarWorking.Visibility = Visibility.Hidden
		ButtonDump.IsEnabled = True
		LabelStatus.Content = "找到 " & files.Count & " 个加密文件"
	End Sub

	Private Function SearchNcm() As List(Of String)
		Dim files As New List(Of String)
		For Each f In My.Computer.FileSystem.GetFiles(SongLibPath, FileIO.SearchOption.SearchAllSubDirectories, "*.ncm")
			files.Add(f)
		Next
		Return files
	End Function

	Private Sub ButtonMinimize_Click(sender As Object, e As RoutedEventArgs) Handles ButtonMinimize.Click
		RaiseEvent Minimize
	End Sub
	
	Public Sub Dispose()
		For Each itm As NcmItem In StackPanelFiles.Children
			itm.Dispose()
		Next
	End Sub

	Private Async Sub ButtonDump_Click(sender As Object, e As RoutedEventArgs) Handles ButtonDump.Click
		ButtonDump.IsEnabled = False
		LabelStatus.Content = "正在转换..."
		ProgressBarWorking.Visibility = Visibility.Visible
		IconCheck.Visibility = Visibility.Hidden
		For Each itm As NcmItem In StackPanelFiles.Children
			Await itm.Dump(SongLibPath)
			itm.Dispose()
			My.Computer.FileSystem.DeleteFile(itm._filePath)
		Next
	End Sub
End Class
