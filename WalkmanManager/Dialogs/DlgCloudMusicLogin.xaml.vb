Public Class DlgCloudMusicLogin

	Public Property Phone As String = ""
	Public Property Password As String = ""

	Private Sub TextBoxPhone_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextBoxPhone.TextChanged
		Phone = TextBoxPhone.Text
		If Phone = "" Then
			ButtonLogin.IsEnabled = False
		ElseIf Password <> "" Then
			ButtonLogin.IsEnabled = True
		End If
	End Sub

	Private Sub TextBoxPassword_TextChanged(sender As Object, e As RoutedEventArgs) Handles TextBoxPassword.PasswordChanged
		Password = TextBoxPassword.Password
		If Password = "" Then
			ButtonLogin.IsEnabled = False
		ElseIf Phone <> "" Then
			ButtonLogin.IsEnabled = True
		End If
	End Sub

	Private Sub TextBoxPhone_PreviewTextInput(sender As Object, e As TextCompositionEventArgs) Handles TextBoxPhone.PreviewTextInput
		If Not (AscW(e.Text) > 47 And AscW(e.Text) < 58) Then
			e.Handled = True
		End If
	End Sub
End Class
