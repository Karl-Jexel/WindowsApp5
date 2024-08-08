Imports MySql.Data.MySqlClient

Public Class Form5
    Dim connectionString As String = "data source=localhost;user id=root;database=db_talaba"

    Private Sub Form5_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadData()
        LoadAllOrderItems()
    End Sub

    Private Sub LoadData()
        Dim dt As New DataTable()

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()
                Dim query As String = "SELECT * FROM Customer"
                Dim cmd As New MySqlCommand(query, connection)
                Dim adapter As New MySqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            DataGridView1.DataSource = dt
        Catch ex As Exception
            MessageBox.Show("An error occurred while loading customer data: " & ex.Message)
        End Try
    End Sub

    Private Sub LoadAllOrderItems()
        Dim dt As New DataTable()

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()
                Dim query As String = "SELECT * FROM OrderItem"
                Dim cmd As New MySqlCommand(query, connection)
                Dim adapter As New MySqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            DataGridView2.DataSource = dt
        Catch ex As Exception
            MessageBox.Show("An error occurred while loading order items: " & ex.Message)
        End Try
    End Sub

    Private Sub DataGridView1_SelectionChanged(sender As Object, e As EventArgs) Handles DataGridView1.SelectionChanged
        If DataGridView1.SelectedRows.Count > 0 Then
            Dim selectedRow As DataGridViewRow = DataGridView1.SelectedRows(0)

            TextBox1.Text = selectedRow.Cells("Name").Value.ToString()
            TextBox2.Text = selectedRow.Cells("Email").Value.ToString()
            TextBox3.Text = selectedRow.Cells("Phone").Value.ToString()
            TextBox4.Text = selectedRow.Cells("Address").Value.ToString()

            Dim customerId As Integer = Convert.ToInt32(selectedRow.Cells("CustomerID").Value)
            LoadOrderItems(customerId)
        End If
    End Sub

    Private Sub LoadOrderItems(customerId As Integer)
        Dim dt As New DataTable()

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()
                Dim query As String = "SELECT * FROM OrderItem WHERE OrderID IN (SELECT OrderID FROM CustomerOrder WHERE CustomerID = @CustomerID)"
                Dim cmd As New MySqlCommand(query, connection)
                cmd.Parameters.AddWithValue("@CustomerID", customerId)
                Dim adapter As New MySqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            DataGridView2.DataSource = dt
        Catch ex As Exception
            MessageBox.Show("An error occurred while loading order items: " & ex.Message)
        End Try
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If DataGridView2.SelectedRows.Count > 0 Then
            Dim selectedRow As DataGridViewRow = DataGridView2.SelectedRows(0)
            Dim orderItemId As Integer = Convert.ToInt32(selectedRow.Cells("OrderItemID").Value)

            Dim confirmResult As DialogResult = MessageBox.Show("Are you sure you want to delete this order item?", "Confirm Delete", MessageBoxButtons.YesNo)
            If confirmResult = DialogResult.Yes Then
                DeleteOrderItem(orderItemId)
                LoadAllOrderItems()
            End If
        Else
            MessageBox.Show("Please select a row to delete.")
        End If
    End Sub

    Private Sub DeleteOrderItem(orderItemId As Integer)
        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim deleteSQL As String = "DELETE FROM OrderItem WHERE OrderItemID = @OrderItemID"
                Dim deleteCmd As New MySqlCommand(deleteSQL, connection)
                deleteCmd.Parameters.AddWithValue("@OrderItemID", orderItemId)
                deleteCmd.ExecuteNonQuery()

                MessageBox.Show("Order item deleted successfully.")
            End Using
        Catch ex As MySqlException
            MessageBox.Show("MySQL Error: " & ex.Message)
        Catch ex As Exception
            MessageBox.Show("General Error: " & ex.Message)
        End Try
    End Sub
End Class
