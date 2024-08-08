Imports MySql.Data.MySqlClient
Imports System.Linq

Public Class Form8
    Dim connectionString As String = "data source=localhost;user id=root;database=db_talaba"
    Public Property SelectedCarIDs As List(Of Integer)
    Public Property SelectedCarPartIDs As List(Of Integer)
    Public Property OrderID As Integer

    Private Sub Form5_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadCarData()
        LoadSelectedCarParts()
        If OrderID > 0 Then
            LoadOrderDetails()
        End If
    End Sub

    Private Sub LoadCarData()
        Dim dt As New DataTable()

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()
                If SelectedCarIDs IsNot Nothing AndAlso SelectedCarIDs.Count > 0 Then
                    Dim query As String = "SELECT * FROM Car WHERE CarID IN (" & String.Join(",", SelectedCarIDs) & ")"
                    Dim cmd As New MySqlCommand(query, connection)
                    Dim adapter As New MySqlDataAdapter(cmd)
                    adapter.Fill(dt)
                End If
            End Using
            If dt.Rows.Count > 0 Then
                DataGridView1.DataSource = dt
            End If
        Catch ex As Exception
            MsgBox("An error occurred while loading car data: " & ex.Message)
        End Try
    End Sub

    Private Sub LoadSelectedCarParts()
        Dim dt As New DataTable()

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()
                If SelectedCarPartIDs IsNot Nothing AndAlso SelectedCarPartIDs.Count > 0 Then
                    Dim query As String = "SELECT * FROM CarPart WHERE CarPartID IN (" & String.Join(",", SelectedCarPartIDs) & ")"
                    Dim cmd As New MySqlCommand(query, connection)
                    Dim adapter As New MySqlDataAdapter(cmd)
                    adapter.Fill(dt)
                End If
            End Using
            If dt.Rows.Count > 0 Then
                If DataGridView1.DataSource Is Nothing Then
                    DataGridView1.DataSource = dt
                Else
                    Dim existingDt As DataTable = CType(DataGridView1.DataSource, DataTable)
                    existingDt.Merge(dt)
                End If
            End If
        Catch ex As Exception
            MsgBox("An error occurred while loading car part data: " & ex.Message)
        End Try
    End Sub

    Private Sub Button1_Click_1(sender As Object, e As EventArgs) Handles Button1.Click
        Label2.Text = "To Be Shipped"
        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()
                Dim sql As String = "INSERT INTO Customer (Name, Email, Phone, Address) VALUES (@Name, @Email, @Phone, @Address)"
                Dim cmd As New MySqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@Name", TextBox1.Text)
                cmd.Parameters.AddWithValue("@Email", TextBox3.Text)
                cmd.Parameters.AddWithValue("@Phone", TextBox4.Text)
                cmd.Parameters.AddWithValue("@Address", TextBox2.Text)
                cmd.ExecuteNonQuery()
                MsgBox("Customer Ordered Successfully")

                Dim customerID As Integer = CType(cmd.LastInsertedId, Integer)
                InsertOrderData(customerID)
            End Using
        Catch ex As Exception
            MsgBox("An error occurred: " & ex.Message)
        End Try
    End Sub

    Private Sub InsertOrderData(customerID As Integer)
        Dim orderID As Integer
        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Using transaction As MySqlTransaction = connection.BeginTransaction()
                    Dim orderSQL As String = "INSERT INTO CustomerOrder (CustomerID, OrderDate, Status) VALUES (@CustomerID, @OrderDate, @Status)"
                    Dim orderCmd As New MySqlCommand(orderSQL, connection, transaction)
                    orderCmd.Parameters.AddWithValue("@CustomerID", customerID)
                    orderCmd.Parameters.AddWithValue("@OrderDate", DateTime.Now)
                    orderCmd.Parameters.AddWithValue("@Status", "Pending")
                    orderCmd.ExecuteNonQuery()

                    orderID = CType(orderCmd.LastInsertedId, Integer)

                    If SelectedCarIDs IsNot Nothing AndAlso SelectedCarIDs.Count > 0 Then
                        For Each carID As Integer In SelectedCarIDs
                            Try
                                Dim priceQuery As String = "SELECT Price FROM Car WHERE CarID = @ProductID"
                                Dim priceCmd As New MySqlCommand(priceQuery, connection, transaction)
                                priceCmd.Parameters.AddWithValue("@ProductID", carID)
                                Dim price As Decimal = Convert.ToDecimal(priceCmd.ExecuteScalar())

                                Dim carSQL As String = "INSERT INTO OrderItem (OrderID, ProductType, ProductID, Quantity, Price) VALUES (@OrderID, 'Car', @ProductID, 1, @Price)"
                                Dim carCmd As New MySqlCommand(carSQL, connection, transaction)
                                carCmd.Parameters.AddWithValue("@OrderID", orderID)
                                carCmd.Parameters.AddWithValue("@ProductID", carID)
                                carCmd.Parameters.AddWithValue("@Price", price)
                                carCmd.ExecuteNonQuery()
                            Catch ex As Exception
                                MsgBox("An error occurred while inserting car data for CarID " & carID & ": " & ex.Message)
                            End Try
                        Next
                    End If

                    If SelectedCarPartIDs IsNot Nothing AndAlso SelectedCarPartIDs.Count > 0 Then
                        For Each partID As Integer In SelectedCarPartIDs
                            Try
                                Dim partSQL As String = "INSERT INTO OrderItem (OrderID, ProductType, ProductID, Quantity, Price) VALUES (@OrderID, 'CarPart', @ProductID, 1, (SELECT Price FROM CarPart WHERE CarPartID = @ProductID LIMIT 1))"
                                Dim partCmd As New MySqlCommand(partSQL, connection, transaction)
                                partCmd.Parameters.AddWithValue("@OrderID", orderID)
                                partCmd.Parameters.AddWithValue("@ProductID", partID)

                                Dim rowsAffected As Integer = partCmd.ExecuteNonQuery()
                                If rowsAffected = 0 Then
                                    Throw New Exception("Failed to insert order item for CarPartID: " & partID)
                                End If
                            Catch ex As Exception
                                MsgBox("An error occurred while inserting car part data for CarPartID " & partID & ": " & ex.Message)
                            End Try
                        Next
                    End If
                    transaction.Commit()

                    InsertPaymentData(orderID)

                    Me.OrderID = orderID
                    LoadOrderDetails()
                End Using
            End Using
        Catch ex As Exception
            MsgBox("An error occurred while inserting order data: " & ex.Message)
        End Try
    End Sub


    Private Sub InsertPaymentData(orderID As Integer)
        Try
            Dim paymentMethod As String = "Unknown"

            If CheckBox1.Checked Then
                paymentMethod = "Credit Card"
            ElseIf CheckBox3.Checked Then
                paymentMethod = "PayPal"
            ElseIf CheckBox2.Checked Then
                paymentMethod = "Cash"
            End If

            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim paymentSQL As String = "INSERT INTO Payment (OrderID, Amount, PaymentDate, Status, PaymentMethod) VALUES (@OrderID, @Amount, @PaymentDate, @Status, @PaymentMethod)"
                Dim paymentCmd As New MySqlCommand(paymentSQL, connection)
                paymentCmd.Parameters.AddWithValue("@OrderID", orderID)

                Dim amount As Decimal = CalculateTotalPrice(orderID)
                paymentCmd.Parameters.AddWithValue("@Amount", amount)
                paymentCmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now)
                paymentCmd.Parameters.AddWithValue("@Status", "Pending")
                paymentCmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod)

                paymentCmd.ExecuteNonQuery()

                MsgBox("Payment data inserted successfully with amount: " & amount.ToString("C") & " and payment method: " & paymentMethod)
            End Using
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

    Private Sub LoadOrderDetails()
        Dim dt As New DataTable()
        Dim totalPrice As Decimal = 0

        Try
            Using connection As New MySqlConnection(connectionString)
                connection.Open()

                Dim query As String = "SELECT o.OrderID, o.CustomerID, c.Name, c.Email, c.Phone, c.Address, oi.ProductType, 
                                           CASE WHEN oi.ProductType = 'Car' THEN (SELECT Make FROM Car WHERE CarID = oi.ProductID)
                                                WHEN oi.ProductType = 'CarPart' THEN (SELECT Name FROM CarPart WHERE CarPartID = oi.ProductID)
                                           END AS ProductName, oi.Quantity, oi.Price 
                                           FROM CustomerOrder o
                                           JOIN Customer c ON o.CustomerID = c.CustomerID
                                           JOIN OrderItem oi ON o.OrderID = oi.OrderID
                                           WHERE o.OrderID = @OrderID"
                Dim cmd As New MySqlCommand(query, connection)
                cmd.Parameters.AddWithValue("@OrderID", OrderID)
                Dim adapter As New MySqlDataAdapter(cmd)

                adapter.Fill(dt)
            End Using

            DataGridView1.DataSource = dt

            For Each row As DataRow In dt.Rows
                totalPrice += Convert.ToDecimal(row("Price"))
            Next

            Label2.Text = "Total Price: " & totalPrice.ToString("C")

        Catch ex As Exception
            MsgBox("An error occurred while loading order details: " & ex.Message)
        End Try
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Form3.Show()
        Me.Hide()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Form9.Show()
        Me.Hide()
    End Sub
End Class
