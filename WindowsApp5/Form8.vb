Imports MySql.Data.MySqlClient

Public Class Form8
    Dim connectionString As String = "data source=localhost;user id=root;database=db_talaba"
    Public Property SelectedCarIDs As New List(Of Integer)
    Public Property SelectedCarPartIDs As New List(Of Integer)
    Public Property OrderID As Integer
    Private _customerID As Integer

    Public Sub New(customerID As Integer)
        InitializeComponent()
        _customerID = customerID
    End Sub

    Private Sub Form8_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSelectedItems()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try
            If _customerID > 0 Then

                InsertOrderData(_customerID)




                InsertPaymentData(OrderID)

                MsgBox("Payout successful. Receipt is now displayed.")
            Else
                MsgBox("Customer data is not available.")
            End If
        Catch ex As Exception
            MsgBox("An error occurred: " & ex.Message)
        End Try
    End Sub

    Private Sub InsertOrderData(customerID As Integer)
        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim orderSQL As String = "INSERT INTO CustomerOrder (CustomerID, OrderDate, Status) VALUES (@CustomerID, @OrderDate, @Status)"
                Dim orderCmd As New MySqlCommand(orderSQL, connection)
                orderCmd.Parameters.AddWithValue("@CustomerID", customerID)
                orderCmd.Parameters.AddWithValue("@OrderDate", DateTime.Now)
                orderCmd.Parameters.AddWithValue("@Status", "Pending")
                orderCmd.ExecuteNonQuery()

                Dim orderID As Integer = CType(orderCmd.LastInsertedId, Integer)

                For Each carID As Integer In SelectedCarIDs
                    Dim priceQuery As String = "SELECT Price FROM Car WHERE CarID = @CarID"
                    Dim priceCmd As New MySqlCommand(priceQuery, connection)
                    priceCmd.Parameters.AddWithValue("@CarID", carID)
                    Dim price As Decimal = Convert.ToDecimal(priceCmd.ExecuteScalar())

                    Dim carSQL As String = "INSERT INTO OrderItem (OrderID, ProductType, ProductID, Quantity, Price) VALUES (@OrderID, 'Car', @ProductID, 1, @Price)"
                    Dim carCmd As New MySqlCommand(carSQL, connection)
                    carCmd.Parameters.AddWithValue("@OrderID", orderID)
                    carCmd.Parameters.AddWithValue("@ProductID", carID)
                    carCmd.Parameters.AddWithValue("@Price", price)
                    carCmd.ExecuteNonQuery()

                    Dim updateCarSQL As String = "UPDATE Car SET Quantity = Quantity - 1 WHERE CarID = @CarID"
                    Dim updateCarCmd As New MySqlCommand(updateCarSQL, connection)
                    updateCarCmd.Parameters.AddWithValue("@CarID", carID)
                    updateCarCmd.ExecuteNonQuery()
                Next

                For Each partID As Integer In SelectedCarPartIDs
                    Dim partSQL As String = "INSERT INTO OrderItem (OrderID, ProductType, ProductID, Quantity, Price) " &
                        "VALUES (@OrderID, 'CarPart', @ProductID, 1, " &
                        "(SELECT Price FROM CarPart WHERE CarPartID = @ProductID LIMIT 1))"
                    Dim partCmd As New MySqlCommand(partSQL, connection)
                    partCmd.Parameters.AddWithValue("@OrderID", orderID)
                    partCmd.Parameters.AddWithValue("@ProductID", partID)

                    Dim rowsAffected As Integer = partCmd.ExecuteNonQuery()
                    If rowsAffected = 0 Then
                        Throw New Exception("Failed to insert order item for CarPartID: " & partID)
                    End If

                    Dim updatePartSQL As String = "UPDATE CarPart SET Quantity = Quantity - 1 WHERE CarPartID = @ProductID"
                    Dim updatePartCmd As New MySqlCommand(updatePartSQL, connection)
                    updatePartCmd.Parameters.AddWithValue("@ProductID", partID)
                    updatePartCmd.ExecuteNonQuery()
                Next

                Me.OrderID = orderID
            End Using
        Catch ex As Exception
            MsgBox("An error occurred while inserting order data: " & ex.Message)
        End Try
    End Sub

    Private Sub InsertPaymentData(orderID As Integer)
        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim amount As Decimal = CalculateTotalPrice(orderID)
                If amount <= 0 Then
                    Throw New Exception("Calculated amount is zero or negative. Check order items or calculations.")
                End If

                Dim paymentSQL As String = "INSERT INTO Payment (OrderID, Amount, PaymentDate, Status) VALUES (@OrderID, @Amount, @PaymentDate, @Status)"
                Dim paymentCmd As New MySqlCommand(paymentSQL, connection)
                paymentCmd.Parameters.AddWithValue("@OrderID", orderID)
                paymentCmd.Parameters.AddWithValue("@Amount", amount)
                paymentCmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now)
                paymentCmd.Parameters.AddWithValue("@Status", "Pending")

                Dim rowsAffected As Integer = paymentCmd.ExecuteNonQuery()
                If rowsAffected = 0 Then
                    Throw New Exception("Payment insertion failed, no rows affected.")
                End If

                MsgBox("Payment data inserted successfully with amount: " & amount.ToString("C"))
            End Using
        Catch ex As MySqlException
            MsgBox("MySQL Error: " & ex.Message)
        Catch ex As Exception
            MsgBox("An error occurred while inserting payment data: " & ex.Message)
        End Try
    End Sub

    Private Function CalculateTotalPrice(orderID As Integer) As Decimal
        Dim totalPrice As Decimal = 0

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim query As String = "SELECT SUM(Price) FROM OrderItem WHERE OrderID = @OrderID"
                Dim cmd As New MySqlCommand(query, connection)
                cmd.Parameters.AddWithValue("@OrderID", orderID)
                totalPrice = Convert.ToDecimal(cmd.ExecuteScalar())
            End Using
        Catch ex As Exception
            MsgBox("An error occurred while calculating total price: " & ex.Message)
        End Try

        Return totalPrice
    End Function

    Private Sub LoadSelectedItems()
        Dim dt As New DataTable()

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim query As String = "SELECT ProductType, ProductID, " &
                                       "CASE WHEN ProductType = 'Car' THEN (SELECT Make FROM Car WHERE CarID = ProductID) " &
                                       "WHEN ProductType = 'CarPart' THEN (SELECT Name FROM CarPart WHERE CarPartID = ProductID) " &
                                       "END AS ProductName, Quantity, Price " &
                                       "FROM OrderItem " &
                                       "WHERE OrderID = @OrderID"
                Dim cmd As New MySqlCommand(query, connection)
                cmd.Parameters.AddWithValue("@OrderID", OrderID)
                Dim adapter As New MySqlDataAdapter(cmd)

                adapter.Fill(dt)
            End Using

            DataGridView1.DataSource = dt

        Catch ex As Exception
            MsgBox("An error occurred while loading selected items: " & ex.Message)
        End Try
    End Sub

    Private Sub LoadOrderDetails()
        Dim dt As New DataTable()

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim query As String = "SELECT ProductType, ProductID, " &
                                       "CASE WHEN ProductType = 'Car' THEN (SELECT Make FROM Car WHERE CarID = ProductID) " &
                                       "WHEN ProductType = 'CarPart' THEN (SELECT Name FROM CarPart WHERE CarPartID = ProductID) " &
                                       "END AS ProductName, Quantity, Price " &
                                       "FROM OrderItem " &
                                       "WHERE OrderID = @OrderID"
                Dim cmd As New MySqlCommand(query, connection)
                cmd.Parameters.AddWithValue("@OrderID", OrderID)
                Dim adapter As New MySqlDataAdapter(cmd)

                adapter.Fill(dt)
            End Using

            DataGridView1.DataSource = dt

        Catch ex As Exception
            MsgBox("An error occurred while loading order details: " & ex.Message)
        End Try
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Form3.Show()
        Me.Close()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Form9.Show()
        Me.Close()
    End Sub
End Class
