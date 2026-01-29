Imports MySql.Data.MySqlClient
Public Class Login
    Private Sub btnLogin_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
        Try
            Dim con As New MySqlConnection
            Dim cmd As New MySqlCommand
            Dim rd As MySqlDataReader

            'Connection String
            con.ConnectionString = Module1.con.ConnectionString()
            'con.ConnectionString = "SERVER=db5003509137.hosting-data.io;PORT=3306;DATABASE=dbs2852812;UID=dbu1447841;password=Hello123,"
            cmd.Connection = con
            con.Open()
            cmd.CommandText = "select usertype from registration where username = '" & txtusername.Text & "' and password = '" & txtpass.Text & "' "

            rd = cmd.ExecuteReader

            If rd.Read Then
                txtUserType.Text = (rd.GetString(0))
            Else
                MessageBox.Show("Invalid username or password")
            End If

            rd.Close()

            If (txtUserType.Text.Trim() = "Administrator") Then
                Me.Hide()
                Dim parentForm As New Main()
                parentForm.Show()

                parentForm.lblUser.Text = txtusername.Text
                parentForm.lblUserType.Text = txtUserType.Text

            End If

            If (txtUserType.Text.Trim() = "Staff") Then
                Me.Hide()
                Dim usa As New User()
                usa.Show()

                usa.lblUser.Text = txtusername.Text
                usa.lblUserType.Text = txtUserType.Text

            End If


            'If rd.HasRows Then
            '    Dim parentForm As New Main()
            '    parentForm.Show()
            '    txtpass.Text = String.Empty
            '    txtusername.Text = String.Empty
            '    Me.Hide()
            'Else
            '    MessageBox.Show("Invalid Login or Password")
            'End If
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub Login_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        txtusername.Text = "Username"
        txtpass.Text = "Password"
    End Sub
    Private Sub txtusername_MouseEnter(sender As Object, e As System.EventArgs) Handles txtusername.MouseEnter
        If (txtusername.Text = "Username") Then
            txtusername.Text = ""
        End If
    End Sub
    Private Sub txtusername_MouseLeave(sender As Object, e As System.EventArgs) Handles txtusername.MouseLeave
        If (txtusername.Text = "") Then
            txtusername.Text = "Username"
        End If
    End Sub
    Private Sub txtpass_MouseEnter(sender As Object, e As System.EventArgs) Handles txtpass.MouseEnter
        If (txtpass.Text = "Password") Then
            txtpass.Text = ""
        End If
    End Sub

    Private Sub txtpass_MouseLeave(sender As Object, e As System.EventArgs) Handles txtpass.MouseLeave
        If (txtpass.Text = "") Then
            txtpass.Text = "Password"
        End If
    End Sub

    Private Sub btnCancel_Click_1(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub
End Class