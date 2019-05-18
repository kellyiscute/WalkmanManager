Imports libNcmDump

Public Class NcmItem
	Dim _filePath = ""
	Dim _crypto As libNcmDump.NeteaseCrypto

	Public enum Status
		wait
		processing
		done
	End Enum

	Sub New(filePath As String)

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		If Not My.Computer.FileSystem.DirectoryExists(filePath) Then
			Throw New IO.FileNotFoundException(filePath & " Not found")
		End If
		_filePath = filePath
		LabelFilename.Content = My.Computer.FileSystem.GetFileInfo(filePath).name

		_crypto = New NeteaseCrypto(New IO.FileStream(filePath, IO.FileMode.Open))
	End Sub

	Public sub SetStatus(s As Status)
		Select Case s
			Case Status.wait
				IconStatus.Kind = MaterialDesignThemes.Wpf.PackIconKind.Clock
			Case Status.processing
				IconStatus.Kind = MaterialDesignThemes.Wpf.PackIconKind.Cached
			Case Status.done
				IconStatus.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check
		End Select
	End sub

	Public Sub Dump()
		_crypto.FileName = My.Computer.FileSystem.GetFileInfo(_filePath).NameWithoutExtention()
		_crypto.Dump()
	End Sub
End Class
