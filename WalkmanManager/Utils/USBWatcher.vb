Imports System.Threading
Imports System.IO

Public Class UsbWatcher
	Property FlgStop As Boolean = False
	Property FlgPause As Boolean = False
	Dim _thWatcher As Thread

	Public Event Unplugged(sender As Object, d As DriveInfoMem)
	Public Event PluggedIn(sender As Object, d As DriveInfoMem)

	Public Sub Start()
		If Not IsNothing(_thWatcher) Then
			If _thWatcher.IsAlive Then
				_thWatcher.Abort()
			End If
			_thWatcher = Nothing
		End If

		_flgStop = False
		_thWatcher = New Thread(AddressOf Watch)
		_thWatcher.Start()
	End Sub

	Public Sub StopThread()
		_flgStop = True
	End Sub

	Private Sub Watch()
		Dim oldDeviceList As New List(Of DriveInfoMem)
		Dim newDeviceList As New List(Of DriveInfoMem)
		For Each d In My.Computer.FileSystem.Drives
			Dim dDbg = d.SaveToMem
			If d.DriveType = DriveType.Removable And d.IsReady Then
				oldDeviceList.Add(d.SaveToMem)
			End If
		Next
		Do Until FlgStop
			Thread.Sleep(500)
			If Not FlgPause Then
				newDeviceList.Clear()
				For Each d In My.Computer.FileSystem.Drives
					If d.DriveType = DriveType.Removable And d.IsReady Then
						newDeviceList.Add(d.SaveToMem)
					End If
				Next

				'Look for new drive
				Dim newDrives As New List(Of DriveInfoMem)
				For Each d In newDeviceList
					If Not oldDeviceList.ContainsDriveInfo(d) Then
						newDrives.Add(d)
					End If
				Next
				'Look for unplugged drive
				Dim unpluggedDrives As New List(Of DriveInfoMem)
				For Each d In oldDeviceList
					If Not newDeviceList.ContainsDriveInfo(d) Then
						unpluggedDrives.Add(d)
					End If
				Next
				'update old list
				oldDeviceList.Clear()
				For Each d In My.Computer.FileSystem.Drives
					If d.DriveType = DriveType.Removable And d.IsReady Then
						oldDeviceList.Add(d.SaveToMem)
					End If
				Next

				'Raise events
				If unpluggedDrives.Count <> 0 Then
					For Each d In unpluggedDrives
						RaiseEvent Unplugged(Me, d)
					Next
				End If
				If newDrives.Count <> 0 Then
					For Each d In newDrives
						RaiseEvent PluggedIn(Me, d)
					Next
				End If
			End If
		Loop
	End Sub

End Class
