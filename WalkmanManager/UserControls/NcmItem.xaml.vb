Imports System.ComponentModel
Imports libNcmDump

Public Class NcmItem
	Public _filePath = ""
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
		If Not My.Computer.FileSystem.FileExists(filePath) Then
			Throw New IO.FileNotFoundException(filePath & " Not found")
		End If
		_filePath = filePath
		LabelFilename.Content = My.Computer.FileSystem.GetFileInfo(filePath).name

		_crypto = New NeteaseCrypto(New IO.FileStream(filePath, IO.FileMode.Open))
	End Sub

	Public Sub SetStatus(s As Status)
		Select Case s
			Case Status.wait
				IconStatus.Kind = MaterialDesignThemes.Wpf.PackIconKind.Clock
			Case Status.processing
				IconStatus.Kind = MaterialDesignThemes.Wpf.PackIconKind.Cached
			Case Status.done
				IconStatus.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check
		End Select
	End Sub

	Public Async Function Dump(dir As String) As Task
		SetStatus(Status.processing)
		Await Task.Run(Sub()
						   _crypto.FileName = dir & "\" & My.Computer.FileSystem.GetFileInfo(_filePath).NameWithoutExtention()
						   Try
							   _crypto.Dump()
						   Catch ex As Exception

						   End Try
					   End Sub)
		SetStatus(Status.done)
	End Function

	Public sub Dispose()
		_crypto.CloseFile
		_crypto = nothing
	End sub
End Class
