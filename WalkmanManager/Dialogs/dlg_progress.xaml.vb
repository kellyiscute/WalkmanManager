Imports System.ComponentModel
Imports System.Threading

Public Class dlg_progress

	Property Progress() As Integer
		Get
			Return ProgProgress.Value
		End Get
		Set(value As Integer)
			ProgProgress.Value = value
		End Set
	End Property

	Property IsIndeterminate() As Boolean
		Get
			Return ProgProgress.IsIndeterminate
		End Get
		Set(value As Boolean)
			ProgProgress.IsIndeterminate = value
		End Set
	End Property

	Property Text As String
		Get
			Dim r = LabelStatus.Dispatcher.Invoke(Function()
													  Return LabelStatus.Content
												  End Function)
			Return r
		End Get
		Set
			LabelStatus.Dispatcher.Invoke(Sub()
											  LabelStatus.Content = Value
										  End Sub)

		End Set
	End Property

	Public Sub ChangeColorTheme(brush As SolidColorBrush)
		ProgProgress.Foreground = brush
	End Sub
End Class
