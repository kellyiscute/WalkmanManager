Imports System.Windows.Forms
Imports MaterialDesignThemes.Wpf.DialogHostEx

Public Class DlgSettings
	Public Property FlgForceRestart As Boolean = False

	Public Sub Init()
		TextBoxSongDir.Text = My.Computer.FileSystem.GetDirectoryInfo(Database.GetSetting("song_dir")).FullName
		SliderChunkSize.Value = Database.GetSetting("chunkSize", 1024) \ 1024
		SliderSearchOnType.Value = Database.GetSetting("searchOnType", 1300)
		TextBoxLyricPreLoad.Text = Database.GetSetting("lyricPreLoad", 3)
	End Sub

	Private Sub ButtonOpen_Click(sender As Object, e As RoutedEventArgs) Handles ButtonOpen.Click
		Using dlg As New FolderBrowserDialog
			Dim result = dlg.ShowDialog
			If result = DialogResult.OK Then
				Console.WriteLine(dlg.SelectedPath)
				Database.SaveSetting("song_dir", dlg.SelectedPath)
				TextBoxSongDir.Text = dlg.SelectedPath
				FlgForceRestart = True
			End If
		End Using
	End Sub

	Private Sub showOKDialog()
		Dim dlg As New DlgMessageDialog("", "操作完成")
		Dialog.ShowDialog(dlg)
	End Sub

	Private Sub GotoLink(sender As Object, e As RoutedEventArgs)
		Process.Start(sender.tag)
	End Sub

	Private Sub RadioButton_Click(sender As Object, e As RoutedEventArgs)
		If RadioButtonLicenses.IsChecked Then
			ColorZoneLicenses.Visibility = Visibility.Visible
		Else
			ColorZoneLicenses.Visibility = Visibility.Hidden
		End If
	End Sub

	Private Sub ButtonSave_Click(sender As Object, e As RoutedEventArgs) Handles ButtonSave.Click
		Database.SaveSetting("searchOnType", SliderSearchOnType.Value)
		Database.SaveSetting("chunkSize", SliderChunkSize.Value * 1024)
		Database.SaveSetting("lyricPreLoad", Integer.Parse(TextBoxLyricPreLoad.Text))
	End Sub

	Private Sub TextBoxLyricPreLoad_PreviewTextInput(sender As Object, e As TextCompositionEventArgs) Handles TextBoxLyricPreLoad.PreviewTextInput
		If Not IsNumeric(sender.text) Then
			e.Handled = True
		End If
	End Sub
End Class
