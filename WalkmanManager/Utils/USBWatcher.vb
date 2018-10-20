Imports System.Threading

Public Class Watcher

	Dim _Olist As New List(Of String)
	Dim _thWatcher As Thread

	Public Event DeviceArrived(Letter As String, DriveInfo As IO.DriveInfo)

	Public Function IsRunning() As Boolean
		Try
			If Not IsNothing(_thWatcher) Then
				If _thWatcher.IsAlive = True Then
					Return True
				Else
					Return False
				End If
			Else
				Return False
			End If
		Catch ex As Exception
			Return False
		End Try
	End Function

	Public Sub StartWatcher()
		Try
			If IsNothing(_thWatcher) Then
				_thWatcher = New Thread(AddressOf Watcher)
				_thWatcher.IsBackground = True
				_thWatcher.Start()
			ElseIf Not _thWatcher.IsAlive Then
				_thWatcher = New Thread(AddressOf Watcher)
				_thWatcher.IsBackground = True
				_thWatcher.Start()
			End If
		Catch ex As Exception
			' stop operation when unhandled exception occured
		End Try
	End Sub

	Public Sub StopWatcher()
		Try
			If _thWatcher.IsAlive Then
				_thWatcher.Abort()
			End If
		Catch ex As Exception
			' stop operation when unhandled exception occured
		End Try
	End Sub


	Private Sub Watcher()
		Try
			Do

				Dim N_List As New List(Of String)
				For Each dev In My.Computer.FileSystem.Drives
					If dev.DriveType = IO.DriveType.Removable Then
						N_List.Add(dev.Name)
					End If
				Next

				For Each dev In N_List
					Dim found As Boolean
					For Each odev In _Olist
						If dev = odev Then
							found = True
							Exit For
						End If
					Next
					If Not found Then
						Dim driveinf As IO.DriveInfo = Nothing
						For Each d In My.Computer.FileSystem.Drives
							If d.Name = dev Then
								driveinf = d
							End If
						Next
						If Not IsNothing(driveinf) Then
							'prevent error when surprise unplug occur
							RaiseEvent DeviceArrived(dev, driveinf)
						End If
					Else
						found = False
					End If
				Next

				_Olist.Clear()
				For Each dev In N_List
					_Olist.Add(dev)
				Next
				Thread.Sleep(700)
			Loop
		Catch ex As Exception
			'Stop thread when unhandled exception occur
		End Try
	End Sub

End Class