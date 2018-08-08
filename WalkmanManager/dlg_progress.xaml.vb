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

End Class
