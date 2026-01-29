Imports System.Security.Cryptography
Imports MySql.Data.MySqlClient
Imports System
Imports System.IO
Imports System.Data
Public Class AdminReg
    Private Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click
        Try
            'Connection String
            Dim constr As String = Module1.con.ConnectionString()
            Using cnn As MySqlConnection = New MySqlConnection(constr)
                Using com As MySqlCommand = New MySqlCommand("SELECT username, email, contactno FROM registration WHERE name = '" & txtName.Text & "'")
                    com.CommandType = CommandType.Text
                    com.Connection = cnn
                    cnn.Open()
                    Using sdr As MySqlDataReader = com.ExecuteReader()
                        If sdr.Read() Then
                            MsgBox("Duplicate Record Detected", MsgBoxStyle.Information, "Biometric Fingerprints Student Attendance System")
                            cnn.Close()
                        Else


                            'Dim fingerprintData As MemoryStream = New MemoryStream
                            'Template.Serialize(fingerprintData)
                            'fingerprintData.Position = 0
                            'Dim br As BinaryReader = New BinaryReader(fingerprintData)
                            'Dim bytes() As Byte = br.ReadBytes(CType(fingerprintData.Length, Int32))

                            Dim comd As New MySqlCommand
                            Dim cn As New MySqlConnection
                            cn.ConnectionString = Module1.con.ConnectionString()
                            comd.CommandType = System.Data.CommandType.Text

                            comd.CommandText = "Insert into Registration (username, usertype, password, name, contactno, email) Values ('" & txtUsername.Text & "','" & cmbUserType.Text & "','" & txtPassword.Text & "','" & txtName.Text & "','" & txtContact_no.Text & "','" & txtEmail.Text & "')"
                            'comd.Parameters.AddWithValue("@finga", bytes)
                            comd.Connection = cn
                            cn.Open()

                            'pic.Image.Save(AppDomain.CurrentDomain.BaseDirectory + txtUserId.Text + ".jpg")

                            'Run Query
                            comd.ExecuteNonQuery()
                            cn.Close()
                            'finger()
                            reset()
                            MsgBox("Submitted Successfully", MsgBoxStyle.Information, "Biometric Fingerprints Student Attendance System")
                        End If

                    End Using
                    'con.Close()
                End Using
            End Using


        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Public Sub reset()
        txtEmail.Text = String.Empty
        txtName.Text = String.Empty
        txtContact_no.Text = String.Empty
        txtPassword.Text = String.Empty
        txtUsername.Text = String.Empty

    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub
End Class