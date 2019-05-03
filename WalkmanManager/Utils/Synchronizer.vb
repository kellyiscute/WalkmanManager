Imports System.IO

Public Class Synchronizer
	Dim _copiedLength As Long

	Public ReadOnly Property CopiedLength As Long
		Get
			Return _copiedLength
		End Get
	End Property

	Dim _totalLength As Long

	Public ReadOnly Property TotalLength As Long
		Get
			Return _totalLength
		End Get
	End Property

	Dim _chunkSize As Integer

	''' <summary>
	''' Size of the data chunk write each time
	''' in KB
	''' </summary>
	''' <returns></returns>
	Public ReadOnly Property ChunkSize As Integer
		Get
			Return _chunkSize / 1024
		End Get
	End Property

	Dim _readSpeed As Long

	''' <summary>
	''' Measured read speed in KB
	''' </summary>
	''' <returns></returns>
	Public ReadOnly Property ReadSpeed As Long
		Get
			Return _readSpeed / 1024
		End Get
	End Property

	Dim _writeSpeed As Long

	''' <summary>
	''' Measured write speed in KB
	''' </summary>
	''' <returns></returns>
	Public ReadOnly Property WriteSpeed As Long
		Get
			Return _writeSpeed
		End Get
	End Property

	Public Event Update(sender As Synchronizer)

	Sub New(Optional blockSize As Long = 512)

	End Sub

	Public Sub CopyFile(source As String, destination As String)
		_copiedLength = 0

		Dim sourceFile = New BinaryReader(New FileStream(source, FileMode.Open))
		_totalLength = sourceFile.BaseStream.Length
		_chunkSize = 1024 * 1024 'Initial Chunk Size = 1MB
		Dim destinationFile = New BinaryWriter(New FileStream(destination, FileMode.OpenOrCreate))
		'Prepare variables
		Dim rTime As Long
		Dim wTime As Long
		' read until EOF
		Do
			If _totalLength - _copiedLength > _chunkSize Then
				'If the there is still more than one full chunk, Read one chunk
				rTime = My.Computer.Clock.TickCount
				Dim data() = sourceFile.ReadBytes(_chunkSize)
				'Chunk_size / (time elapsed (ms)) * 1000 = avg speed (b/s)

				If My.Computer.Clock.TickCount - rTime = 0 Then
					_readSpeed = _chunkSize * 2
				Else
					_readSpeed = _chunkSize / ((My.Computer.Clock.TickCount - rTime) * 1000)
				End If

				wTime = My.Computer.Clock.TickCount
				destinationFile.Write(data)
				destinationFile.Flush()
				If My.Computer.Clock.TickCount - wTime = 0 Then
					_writeSpeed = _chunkSize * 2
				Else
					_writeSpeed = _chunkSize / ((My.Computer.Clock.TickCount - wTime) * 1000)
				End If
				'add to copied
				_copiedLength += _chunkSize
				data = Nothing
			Else
				Dim data() = sourceFile.ReadBytes(_totalLength - _copiedLength)
				destinationFile.Write(data)
				_copiedLength = _totalLength
				sourceFile.Close()
				sourceFile.Dispose()
				destinationFile.Flush()
				destinationFile.Close()
				destinationFile.Dispose()
				Exit Do
			End If
			RaiseEvent Update(Me)
		Loop
	End Sub

	''' <summary>
	''' Create m3u playlist
	''' </summary>
	''' <param name="lst">file list</param>
	''' <param name="path">remote path</param>
	Public Shared Sub CreatePlaylist(lst As IEnumerable(Of String), path As String)
		Dim m3u As New StreamWriter(New FileStream(path, FileMode.Create))
		For Each itm In lst
			m3u.WriteLine(itm)
		Next
		m3u.Flush()
		m3u.Close()
		m3u.Dispose()
	End Sub
End Class
