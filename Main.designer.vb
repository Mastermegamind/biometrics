<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Main
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
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Main))
        Me.StatusStrip2 = New System.Windows.Forms.StatusStrip()
        Me.toolStripStatusLabel4 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblUser = New System.Windows.Forms.ToolStripStatusLabel()
        Me.toolStripStatusLabel6 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblUserType = New System.Windows.Forms.ToolStripStatusLabel()
        Me.menuStrip3 = New System.Windows.Forms.MenuStrip()
        Me.RegistrationToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.EnrollmentMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.verificationToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ClockInToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ClockOutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AdminToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AddUserToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AttendanceReportToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.LogoutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.StatusStrip2.SuspendLayout()
        Me.menuStrip3.SuspendLayout()
        Me.SuspendLayout()
        '
        'StatusStrip2
        '
        Me.StatusStrip2.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.toolStripStatusLabel4, Me.lblUser, Me.toolStripStatusLabel6, Me.lblUserType})
        Me.StatusStrip2.Location = New System.Drawing.Point(0, 449)
        Me.StatusStrip2.Name = "StatusStrip2"
        Me.StatusStrip2.Size = New System.Drawing.Size(971, 22)
        Me.StatusStrip2.TabIndex = 18
        Me.StatusStrip2.Text = "StatusStrip2"
        '
        'toolStripStatusLabel4
        '
        Me.toolStripStatusLabel4.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.toolStripStatusLabel4.Name = "toolStripStatusLabel4"
        Me.toolStripStatusLabel4.Size = New System.Drawing.Size(86, 17)
        Me.toolStripStatusLabel4.Text = "Logged in As :"
        '
        'lblUser
        '
        Me.lblUser.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblUser.Image = CType(resources.GetObject("lblUser.Image"), System.Drawing.Image)
        Me.lblUser.Name = "lblUser"
        Me.lblUser.Size = New System.Drawing.Size(49, 17)
        Me.lblUser.Text = "User"
        '
        'toolStripStatusLabel6
        '
        Me.toolStripStatusLabel6.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.toolStripStatusLabel6.Name = "toolStripStatusLabel6"
        Me.toolStripStatusLabel6.Size = New System.Drawing.Size(11, 17)
        Me.toolStripStatusLabel6.Text = ":"
        '
        'lblUserType
        '
        Me.lblUserType.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblUserType.Name = "lblUserType"
        Me.lblUserType.Size = New System.Drawing.Size(62, 17)
        Me.lblUserType.Text = "UserType"
        '
        'menuStrip3
        '
        Me.menuStrip3.BackColor = System.Drawing.Color.White
        Me.menuStrip3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.menuStrip3.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.RegistrationToolStripMenuItem, Me.EnrollmentMenuItem1, Me.verificationToolStripMenuItem1, Me.AdminToolStripMenuItem, Me.LogoutToolStripMenuItem})
        Me.menuStrip3.Location = New System.Drawing.Point(0, 0)
        Me.menuStrip3.Name = "menuStrip3"
        Me.menuStrip3.Size = New System.Drawing.Size(971, 77)
        Me.menuStrip3.TabIndex = 17
        Me.menuStrip3.Text = "menuStrip3"
        '
        'RegistrationToolStripMenuItem
        '
        Me.RegistrationToolStripMenuItem.Font = New System.Drawing.Font("Bookman Old Style", 11.25!)
        Me.RegistrationToolStripMenuItem.Image = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.register
        Me.RegistrationToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
        Me.RegistrationToolStripMenuItem.Name = "RegistrationToolStripMenuItem"
        Me.RegistrationToolStripMenuItem.Size = New System.Drawing.Size(84, 73)
        Me.RegistrationToolStripMenuItem.Text = "Register"
        Me.RegistrationToolStripMenuItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        '
        'EnrollmentMenuItem1
        '
        Me.EnrollmentMenuItem1.Font = New System.Drawing.Font("Bookman Old Style", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.EnrollmentMenuItem1.Image = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.fingerprint___Copy
        Me.EnrollmentMenuItem1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
        Me.EnrollmentMenuItem1.Name = "EnrollmentMenuItem1"
        Me.EnrollmentMenuItem1.Size = New System.Drawing.Size(105, 73)
        Me.EnrollmentMenuItem1.Text = "Enrollment"
        Me.EnrollmentMenuItem1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        '
        'verificationToolStripMenuItem1
        '
        Me.verificationToolStripMenuItem1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ClockInToolStripMenuItem, Me.ClockOutToolStripMenuItem})
        Me.verificationToolStripMenuItem1.Font = New System.Drawing.Font("Bookman Old Style", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.verificationToolStripMenuItem1.Image = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.fingerprint_ver_removebg_preview
        Me.verificationToolStripMenuItem1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
        Me.verificationToolStripMenuItem1.Name = "verificationToolStripMenuItem1"
        Me.verificationToolStripMenuItem1.Size = New System.Drawing.Size(109, 73)
        Me.verificationToolStripMenuItem1.Text = "Verification"
        Me.verificationToolStripMenuItem1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        '
        'ClockInToolStripMenuItem
        '
        Me.ClockInToolStripMenuItem.Name = "ClockInToolStripMenuItem"
        Me.ClockInToolStripMenuItem.Size = New System.Drawing.Size(149, 24)
        Me.ClockInToolStripMenuItem.Text = "Time-In"
        '
        'ClockOutToolStripMenuItem
        '
        Me.ClockOutToolStripMenuItem.Name = "ClockOutToolStripMenuItem"
        Me.ClockOutToolStripMenuItem.Size = New System.Drawing.Size(149, 24)
        Me.ClockOutToolStripMenuItem.Text = "Time-Out"
        '
        'AdminToolStripMenuItem
        '
        Me.AdminToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AddUserToolStripMenuItem, Me.AttendanceReportToolStripMenuItem})
        Me.AdminToolStripMenuItem.Font = New System.Drawing.Font("Bookman Old Style", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.AdminToolStripMenuItem.Image = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.registration
        Me.AdminToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
        Me.AdminToolStripMenuItem.Name = "AdminToolStripMenuItem"
        Me.AdminToolStripMenuItem.Size = New System.Drawing.Size(69, 73)
        Me.AdminToolStripMenuItem.Text = "Admin"
        Me.AdminToolStripMenuItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        '
        'AddUserToolStripMenuItem
        '
        Me.AddUserToolStripMenuItem.Name = "AddUserToolStripMenuItem"
        Me.AddUserToolStripMenuItem.Size = New System.Drawing.Size(218, 24)
        Me.AddUserToolStripMenuItem.Text = "Add User"
        '
        'AttendanceReportToolStripMenuItem
        '
        Me.AttendanceReportToolStripMenuItem.Name = "AttendanceReportToolStripMenuItem"
        Me.AttendanceReportToolStripMenuItem.Size = New System.Drawing.Size(218, 24)
        Me.AttendanceReportToolStripMenuItem.Text = "Attendance Report"
        '
        'LogoutToolStripMenuItem
        '
        Me.LogoutToolStripMenuItem.Font = New System.Drawing.Font("Bookman Old Style", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LogoutToolStripMenuItem.Image = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.logout
        Me.LogoutToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
        Me.LogoutToolStripMenuItem.Name = "LogoutToolStripMenuItem"
        Me.LogoutToolStripMenuItem.Size = New System.Drawing.Size(70, 73)
        Me.LogoutToolStripMenuItem.Text = "Logout"
        Me.LogoutToolStripMenuItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        '
        'Main
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackgroundImage = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.bg_FRAttendanceSystem
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.ClientSize = New System.Drawing.Size(971, 471)
        Me.Controls.Add(Me.StatusStrip2)
        Me.Controls.Add(Me.menuStrip3)
        Me.DoubleBuffered = True
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Main"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Biometric Fingerprints Student Attendance System"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.StatusStrip2.ResumeLayout(False)
        Me.StatusStrip2.PerformLayout()
        Me.menuStrip3.ResumeLayout(False)
        Me.menuStrip3.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents StatusStrip2 As StatusStrip
    Friend WithEvents toolStripStatusLabel4 As ToolStripStatusLabel
    Friend WithEvents lblUser As ToolStripStatusLabel
    Private WithEvents toolStripStatusLabel6 As ToolStripStatusLabel
    Public WithEvents lblUserType As ToolStripStatusLabel
    Friend WithEvents menuStrip3 As MenuStrip
    Friend WithEvents RegistrationToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents EnrollmentMenuItem1 As ToolStripMenuItem
    Friend WithEvents verificationToolStripMenuItem1 As ToolStripMenuItem
    Friend WithEvents AdminToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents AddUserToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LogoutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Timer1 As Timer
    Friend WithEvents ClockInToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ClockOutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents AttendanceReportToolStripMenuItem As ToolStripMenuItem
End Class
