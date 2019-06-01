Public Class dlgDragImport
	Property Progress
		Get
			Return ProgressBar.Value
		End Get
		Set(value)
			ProgressBar.Value = value
			TextBlockPrompt.Text = "正在导入(" & value & "/" & ProgressBar.Maximum & ")"
		End Set
	End Property

	Property Max
		Get
			Return ProgressBar.Maximum
		End Get
		Set(value)
			ProgressBar.Maximum = value
			TextBlockPrompt.Text = "正在导入(" & ProgressBar.Value & "/" & value & ")"
		End Set
	End Property
End Class
