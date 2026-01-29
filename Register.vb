Imports System.Diagnostics
Imports Emgu.CV.Structure
Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO
Imports System.Security.Cryptography
Imports MySql.Data.MySqlClient
Imports System.Data
Public Class Register
    'Declaration of all variables, vectors And haarcascades
    Dim currentFrame As Image(Of Bgr, [Byte])
    Dim grabber As Capture
    Dim face As HaarCascade
    Dim eye As HaarCascade
    'Dim font As New MCvFont(CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5, 0.5)

    Private Sub label8_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub btnStart_Click(sender As Object, e As EventArgs)
        Try
            grabber = New Capture()
            grabber.QueryFrame()
            Timer1.Start()
        Catch ex As Exception
            MsgBox(ex.ToString, MsgBoxStyle.Exclamation, "")
        End Try
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs)
        Try
            'Label3.Text = "0"

            'Get the current frame form capture device
            currentFrame = grabber.QueryFrame().Resize(250, 192, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC)


            'Show the faces procesed and recognized
            pictureBox1.Image = currentFrame.ToBitmap()

        Catch ex As Exception
            MsgBox(ex.ToString, MsgBoxStyle.Exclamation, "")
        End Try
    End Sub

    Private Sub btnCapture_Click(sender As Object, e As EventArgs)
        Try

            grabber.Dispose()
            Timer1.Stop()

        Catch ex As Exception
            MsgBox(ex.ToString, MsgBoxStyle.Exclamation, "")
        End Try
    End Sub

    Public Sub GenerateID()
        'Dim alphabets As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        'Dim small_alphabets As String = "abcdefghijklmnopqrstuvwxyz"
        Dim numbers As String = "1234567890"

        Dim characters As String = numbers
        'If rbType.SelectedItem.Value = "1" Then
        'characters += Convert.ToString(alphabets & small_alphabets) & numbers
        characters += Convert.ToString(numbers)
        'End If
        Dim length As Integer = Integer.Parse("4")
        Dim id As String = String.Empty
        For i As Integer = 0 To length - 1
            Dim character As String = String.Empty
            Do
                Dim index As Integer = New Random().Next(0, characters.Length)
                character = characters.ToCharArray()(index).ToString()
            Loop While id.IndexOf(character) <> -1
            id += character
        Next
        txtMatricNo.Text = id.Trim()
    End Sub

    Private Sub Register_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        GenerateID()
    End Sub
    'Public Sub SavePicture()
    '    Dim stream As New MemoryStream
    '    pictureBox1.Image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg)
    '    Dim picc As Byte()
    '    picc = stream.ToArray()
    'End Sub
    Public Sub RegisterEmp()
        Try
            'Connection String
            Dim constr As String = Module1.con.ConnectionString()
            Using cnn As MySqlConnection = New MySqlConnection(constr)
                Using com As MySqlCommand = New MySqlCommand("SELECT name, matricno, department FROM students WHERE name = '" & txtName.Text & "' and matricno='" & txtMatricNo.Text & "' and department='" & cmbDept.Text & "'")
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

                            Dim stream As New MemoryStream
                            pictureBox1.Image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg)
                            Dim picc As Byte()
                            picc = stream.ToArray()

                            Dim comd As New MySqlCommand
                            Dim cn As New MySqlConnection
                            cn.ConnectionString = Module1.con.ConnectionString()
                            comd.CommandType = System.Data.CommandType.Text

                            comd.CommandText = "Insert into students (matricno,name,faculty,department,bloodgroup,gradyear,gender,passport) Values ('" & txtMatricNo.Text & "','" _
                                & txtName.Text & "','" & cmbFaculty.Text & "','" & cmbDept.Text & "','" & cmbBloodGroup.Text & "','" & cmbGradYear.Text & "','" & cmbGender.Text & "',@pic)"
                            comd.Parameters.AddWithValue("@pic", picc)
                            comd.Connection = cn
                            cn.Open()

                            'pic.Image.Save(AppDomain.CurrentDomain.BaseDirectory + txtUserId.Text + ".jpg")

                            'Run Query
                            comd.ExecuteNonQuery()
                            cn.Close()
                            'finger()

                            Reset()
                            MsgBox("Submitted Successfully", MsgBoxStyle.Information, "Biometric Fingerprints Student Attendance System")
                            GenerateID()
                        End If

                    End Using
                    'con.Close()
                End Using
            End Using


        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub
    Public Sub Reset()
        txtName.Text = Nothing
        cmbGender.SelectedIndex = -1
        cmbBloodGroup.SelectedIndex = -1
        cmbDept.SelectedIndex = -1
        cmbFaculty.SelectedIndex = -1
        cmbGender.SelectedIndex = -1
        cmbGradYear.SelectedIndex = -1
        pictureBox1.Image = Nothing

    End Sub

    Private Sub btnSubmit_Click(sender As Object, e As EventArgs) Handles btnSubmit.Click
        RegisterEmp()
    End Sub

    Private Sub btnUploadPhoto_Click(sender As Object, e As EventArgs) Handles btnUploadPhoto.Click
        'open file dialog
        Dim OpenFileDialog As OpenFileDialog = New OpenFileDialog()
        OpenFileDialog.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp"
        If (OpenFileDialog.ShowDialog() = DialogResult.OK) Then
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage
            pictureBox1.Image = New Bitmap(OpenFileDialog.FileName)
        End If
    End Sub

    Private Sub btnCancel_Click_1(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub

    Private Sub label8_Click_1(sender As Object, e As EventArgs) Handles label8.Click

    End Sub

    Private Sub btnStart_Click_1(sender As Object, e As EventArgs) Handles btnStart.Click
        Try
            grabber = New Capture()
            grabber.QueryFrame()
            Timer1.Start()
        Catch ex As Exception
            MsgBox(ex.ToString, MsgBoxStyle.Exclamation, "")
        End Try
    End Sub

    Private Sub btnCapture_Click_1(sender As Object, e As EventArgs) Handles btnCapture.Click
        Try

            grabber.Dispose()
            Timer1.Stop()

        Catch ex As Exception
            MsgBox(ex.ToString, MsgBoxStyle.Exclamation, "")
        End Try
    End Sub

    Private Sub pictureBox1_Click(sender As Object, e As EventArgs) Handles pictureBox1.Click

    End Sub

    Private Sub Timer1_Tick_1(sender As Object, e As EventArgs) Handles Timer1.Tick
        Try
            'Label3.Text = "0"

            'Get the current frame form capture device
            currentFrame = grabber.QueryFrame().Resize(240, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC)


            'Show the faces procesed and recognized
            pictureBox1.Image = currentFrame.ToBitmap()

        Catch ex As Exception
            MsgBox(ex.ToString, MsgBoxStyle.Exclamation, "")
        End Try
    End Sub
End Class