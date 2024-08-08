Public Class Form4
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim correctUsernames() As String = {"Talaba", "Mateo", "Hernandez"}
        Dim correctPassword As String = "123"

        Dim inputUsername As String = TextBox3.Text
        Dim inputPassword As String = TextBox1.Text

        If correctUsernames.Contains(inputUsername) AndAlso inputPassword = correctPassword Then
            Dim form5 As New Form5()
            form5.Show()
            Me.Hide()
        ElseIf Not correctUsernames.Contains(inputUsername) Then
            MessageBox.Show("Wrong name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        ElseIf inputPassword <> correctPassword Then
            MessageBox.Show("Wrong pass", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    Private Sub Form4_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class
