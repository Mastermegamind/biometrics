<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim gbEventHandlerStatus As System.Windows.Forms.GroupBox
        Dim lblMaxFingers As System.Windows.Forms.Label
        Dim lblMask As System.Windows.Forms.Label
        Dim lblPrompt As System.Windows.Forms.Label
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.IsSuccess = New System.Windows.Forms.RadioButton()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.txtStaffID = New System.Windows.Forms.TextBox()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.MaxFingers = New System.Windows.Forms.NumericUpDown()
        Me.Mask = New System.Windows.Forms.NumericUpDown()
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.txtName = New System.Windows.Forms.TextBox()
        Me.Panel1 = New System.Windows.Forms.Panel()
        gbEventHandlerStatus = New System.Windows.Forms.GroupBox()
        lblMaxFingers = New System.Windows.Forms.Label()
        lblMask = New System.Windows.Forms.Label()
        lblPrompt = New System.Windows.Forms.Label()
        gbEventHandlerStatus.SuspendLayout()
        CType(Me.MaxFingers, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Mask, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'gbEventHandlerStatus
        '
        gbEventHandlerStatus.Controls.Add(Me.IsSuccess)
        gbEventHandlerStatus.Enabled = False
        gbEventHandlerStatus.Location = New System.Drawing.Point(24, 268)
        gbEventHandlerStatus.Name = "gbEventHandlerStatus"
        gbEventHandlerStatus.Size = New System.Drawing.Size(203, 60)
        gbEventHandlerStatus.TabIndex = 9
        gbEventHandlerStatus.TabStop = False
        gbEventHandlerStatus.Text = "Event handler status"
        '
        'IsSuccess
        '
        Me.IsSuccess.AutoSize = True
        Me.IsSuccess.Location = New System.Drawing.Point(26, 29)
        Me.IsSuccess.Name = "IsSuccess"
        Me.IsSuccess.Size = New System.Drawing.Size(66, 17)
        Me.IsSuccess.TabIndex = 0
        Me.IsSuccess.TabStop = True
        Me.IsSuccess.Text = "Success"
        Me.IsSuccess.UseVisualStyleBackColor = True
        '
        'lblMaxFingers
        '
        lblMaxFingers.Location = New System.Drawing.Point(21, 242)
        lblMaxFingers.Name = "lblMaxFingers"
        lblMaxFingers.Size = New System.Drawing.Size(148, 20)
        lblMaxFingers.TabIndex = 7
        lblMaxFingers.Text = "Max. enrolled fingers count"
        lblMaxFingers.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'lblMask
        '
        lblMask.Location = New System.Drawing.Point(21, 216)
        lblMask.Name = "lblMask"
        lblMask.Size = New System.Drawing.Size(148, 20)
        lblMask.TabIndex = 5
        lblMask.Text = "Fingerprint Mask"
        lblMask.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(11, 119)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(38, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Name:"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(10, 88)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(56, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Matric No:"
        '
        'txtStaffID
        '
        Me.txtStaffID.Font = New System.Drawing.Font("Times New Roman", 12.0!)
        Me.txtStaffID.Location = New System.Drawing.Point(72, 80)
        Me.txtStaffID.Name = "txtStaffID"
        Me.txtStaffID.Size = New System.Drawing.Size(162, 26)
        Me.txtStaffID.TabIndex = 2
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.DarkSlateGray
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Font = New System.Drawing.Font("Palatino Linotype", 9.75!, System.Drawing.FontStyle.Bold)
        Me.Button1.ForeColor = System.Drawing.SystemColors.ButtonHighlight
        Me.Button1.Location = New System.Drawing.Point(113, 153)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(121, 36)
        Me.Button1.TabIndex = 0
        Me.Button1.Text = "Start Enrollment"
        Me.Button1.UseVisualStyleBackColor = False
        '
        'MaxFingers
        '
        Me.MaxFingers.Enabled = False
        Me.MaxFingers.Location = New System.Drawing.Point(175, 241)
        Me.MaxFingers.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.MaxFingers.Name = "MaxFingers"
        Me.MaxFingers.Size = New System.Drawing.Size(52, 20)
        Me.MaxFingers.TabIndex = 8
        Me.MaxFingers.Value = New Decimal(New Integer() {10, 0, 0, 0})
        '
        'Mask
        '
        Me.Mask.Enabled = False
        Me.Mask.Location = New System.Drawing.Point(175, 215)
        Me.Mask.Maximum = New Decimal(New Integer() {1023, 0, 0, 0})
        Me.Mask.Name = "Mask"
        Me.Mask.Size = New System.Drawing.Size(52, 20)
        Me.Mask.TabIndex = 6
        '
        'ComboBox1
        '
        Me.ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Location = New System.Drawing.Point(24, 507)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(175, 21)
        Me.ComboBox1.TabIndex = 0
        Me.ComboBox1.Visible = False
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(24, 346)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(203, 23)
        Me.Button2.TabIndex = 0
        Me.Button2.Text = "Button2"
        Me.Button2.UseVisualStyleBackColor = True
        Me.Button2.Visible = False
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(24, 370)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(203, 23)
        Me.Button3.TabIndex = 0
        Me.Button3.Text = "Button2"
        Me.Button3.UseVisualStyleBackColor = True
        Me.Button3.Visible = False
        '
        'txtName
        '
        Me.txtName.Font = New System.Drawing.Font("Times New Roman", 12.0!)
        Me.txtName.Location = New System.Drawing.Point(55, 112)
        Me.txtName.Name = "txtName"
        Me.txtName.ReadOnly = True
        Me.txtName.Size = New System.Drawing.Size(182, 26)
        Me.txtName.TabIndex = 10
        '
        'Panel1
        '
        Me.Panel1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Location = New System.Drawing.Point(243, 12)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(618, 597)
        Me.Panel1.TabIndex = 3
        '
        'lblPrompt
        '
        lblPrompt.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        lblPrompt.Font = New System.Drawing.Font("Tahoma", 8.25!)
        lblPrompt.Location = New System.Drawing.Point(11, 33)
        lblPrompt.Name = "lblPrompt"
        lblPrompt.Size = New System.Drawing.Size(223, 37)
        lblPrompt.TabIndex = 18
        lblPrompt.Text = "Search by Name, Click on Start Enrollment to open Fingerprint Enrollment Form."
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(873, 617)
        Me.Controls.Add(lblPrompt)
        Me.Controls.Add(Me.txtName)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.ComboBox1)
        Me.Controls.Add(Me.MaxFingers)
        Me.Controls.Add(Me.Mask)
        Me.Controls.Add(gbEventHandlerStatus)
        Me.Controls.Add(lblMaxFingers)
        Me.Controls.Add(lblMask)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.txtStaffID)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Form1"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Fingerprints Enrollment"
        gbEventHandlerStatus.ResumeLayout(False)
        gbEventHandlerStatus.PerformLayout()
        CType(Me.MaxFingers, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Mask, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtStaffID As System.Windows.Forms.TextBox
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents MaxFingers As System.Windows.Forms.NumericUpDown
    Friend WithEvents Mask As System.Windows.Forms.NumericUpDown
    Private WithEvents IsSuccess As System.Windows.Forms.RadioButton
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Public WithEvents ComboBox1 As ComboBox
    Public WithEvents txtName As TextBox
End Class
