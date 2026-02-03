// WinForms Enrollment Form using the actual DPFP.Gui.Enrollment.EnrollmentControl
// This provides the exact same UI as the DigitalPersona SDK samples
// Only available when DIGITALPERSONA_SDK is defined

#if DIGITALPERSONA_SDK
// Use aliases to avoid conflicts with Avalonia types
using WinForms = System.Windows.Forms;
using WinDrawing = System.Drawing;
using DPFP;
using DPFP.Gui.Enrollment;

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// WinForms enrollment form using the native DigitalPersona EnrollmentControl.
/// Provides the exact same 10-finger hand UI as the SDK samples.
/// </summary>
public sealed class DpfpEnrollmentForm : WinForms.Form
{
    private readonly EnrollmentControl _enrollmentControl;
    private readonly WinForms.ListBox _eventsListBox;
    private readonly WinForms.Button _closeButton;
    private readonly WinForms.Label _statusLabel;

    /// <summary>
    /// Array of enrolled templates (index 0-9 for fingers 1-10).
    /// </summary>
    public Template?[] Templates { get; } = new Template?[10];

    /// <summary>
    /// Bitmask of enrolled fingers.
    /// </summary>
    public int EnrolledFingersMask => _enrollmentControl.EnrolledFingerMask;

    /// <summary>
    /// Maximum number of fingers that can be enrolled.
    /// </summary>
#pragma warning disable WFO1000 // Not using WinForms designer serialization
    public int MaxEnrollFingerCount
    {
        get => _enrollmentControl.MaxEnrollFingerCount;
        set => _enrollmentControl.MaxEnrollFingerCount = value;
    }
#pragma warning restore WFO1000

    /// <summary>
    /// Gets the count of enrolled fingers.
    /// </summary>
    public int EnrolledCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < 10; i++)
            {
                if (Templates[i] != null) count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Event raised when a finger is successfully enrolled.
    /// </summary>
    public event Action<int, Template>? FingerEnrolled;

    /// <summary>
    /// Event raised when a finger template is deleted.
    /// </summary>
    public event Action<int>? FingerDeleted;

    public DpfpEnrollmentForm()
    {
        InitializeComponent();

        // Initialize the EnrollmentControl
        _enrollmentControl = new EnrollmentControl
        {
            Location = new WinDrawing.Point(1, -3),
            Size = new WinDrawing.Size(492, 314),
            MaxEnrollFingerCount = 10,
            EnrolledFingerMask = 0,
            AutoSizeMode = WinForms.AutoSizeMode.GrowAndShrink
        };

        // Wire up events
        _enrollmentControl.OnEnroll += EnrollmentControl_OnEnroll;
        _enrollmentControl.OnDelete += EnrollmentControl_OnDelete;
        _enrollmentControl.OnStartEnroll += EnrollmentControl_OnStartEnroll;
        _enrollmentControl.OnComplete += EnrollmentControl_OnComplete;
        _enrollmentControl.OnFingerTouch += EnrollmentControl_OnFingerTouch;
        _enrollmentControl.OnFingerRemove += EnrollmentControl_OnFingerRemove;
        _enrollmentControl.OnSampleQuality += EnrollmentControl_OnSampleQuality;
        _enrollmentControl.OnReaderConnect += EnrollmentControl_OnReaderConnect;
        _enrollmentControl.OnReaderDisconnect += EnrollmentControl_OnReaderDisconnect;
        _enrollmentControl.OnCancelEnroll += EnrollmentControl_OnCancelEnroll;

        // Events list box
        _eventsListBox = new WinForms.ListBox
        {
            Location = new WinDrawing.Point(16, 330),
            Size = new WinDrawing.Size(460, 100),
            BackColor = WinDrawing.SystemColors.InactiveBorder
        };

        // Status label
        _statusLabel = new WinForms.Label
        {
            Location = new WinDrawing.Point(16, 314),
            Size = new WinDrawing.Size(460, 16),
            Text = "Select a finger to enroll",
            ForeColor = WinDrawing.Color.FromArgb(71, 85, 105)
        };

        // Close button
        _closeButton = new WinForms.Button
        {
            Text = "Done",
            Location = new WinDrawing.Point(401, 440),
            Size = new WinDrawing.Size(75, 30),
            DialogResult = WinForms.DialogResult.OK,
            BackColor = WinDrawing.Color.FromArgb(37, 99, 235),
            ForeColor = WinDrawing.Color.White,
            FlatStyle = WinForms.FlatStyle.Flat
        };
        _closeButton.FlatAppearance.BorderSize = 0;

        // Add controls
        Controls.Add(_enrollmentControl);
        Controls.Add(_statusLabel);
        Controls.Add(_eventsListBox);
        Controls.Add(_closeButton);

        AcceptButton = _closeButton;
        CancelButton = _closeButton;
    }

    private void InitializeComponent()
    {
        Text = "Fingerprint Enrollment";
        ClientSize = new WinDrawing.Size(492, 480);
        FormBorderStyle = WinForms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = WinForms.FormStartPosition.CenterScreen;
        BackColor = WinDrawing.Color.White;
    }

    /// <summary>
    /// Sets the initial enrolled fingers mask (for re-enrollment scenarios).
    /// </summary>
    public void SetEnrolledFingersMask(int mask)
    {
        _enrollmentControl.EnrolledFingerMask = mask;
    }

    /// <summary>
    /// Sets existing templates (for re-enrollment scenarios).
    /// </summary>
    public void SetExistingTemplates(Template?[] templates)
    {
        for (int i = 0; i < Math.Min(templates.Length, 10); i++)
        {
            Templates[i] = templates[i];
        }

        // Update the mask based on existing templates
        int mask = 0;
        for (int i = 0; i < 10; i++)
        {
            if (Templates[i] != null)
            {
                mask |= (1 << i);
            }
        }
        _enrollmentControl.EnrolledFingerMask = mask;
    }

    private void LogEvent(string message)
    {
        if (_eventsListBox.InvokeRequired)
        {
            _eventsListBox.Invoke(() => LogEvent(message));
            return;
        }

        _eventsListBox.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} - {message}");
        if (_eventsListBox.Items.Count > 50)
        {
            _eventsListBox.Items.RemoveAt(_eventsListBox.Items.Count - 1);
        }
    }

    private void UpdateStatus(string message)
    {
        if (_statusLabel.InvokeRequired)
        {
            _statusLabel.Invoke(() => UpdateStatus(message));
            return;
        }
        _statusLabel.Text = message;
    }

    // Event Handlers - matching the SDK sample pattern

    private void EnrollmentControl_OnEnroll(object control, int finger, Template template, ref DPFP.Gui.EventHandlerStatus status)
    {
        // Store the template (finger is 1-based, array is 0-based)
        Templates[finger - 1] = template;

        LogEvent($"Finger {finger} enrolled successfully");
        UpdateStatus($"Finger {finger} enrolled! {EnrolledCount}/10 fingers enrolled.");

        FingerEnrolled?.Invoke(finger, template);
    }

    private void EnrollmentControl_OnDelete(object control, int finger, ref DPFP.Gui.EventHandlerStatus status)
    {
        // Clear the template
        Templates[finger - 1] = null;

        LogEvent($"Finger {finger} deleted");
        UpdateStatus($"Finger {finger} removed. {EnrolledCount}/10 fingers enrolled.");

        FingerDeleted?.Invoke(finger);
    }

    private void EnrollmentControl_OnStartEnroll(object control, string readerSerialNumber, int finger)
    {
        LogEvent($"Started enrollment for finger {finger}");
        UpdateStatus($"Enrolling finger {finger}... Place finger on scanner.");
    }

    private void EnrollmentControl_OnComplete(object control, string readerSerialNumber, int finger)
    {
        LogEvent($"Capture complete for finger {finger}");
    }

    private void EnrollmentControl_OnFingerTouch(object control, string readerSerialNumber, int finger)
    {
        LogEvent($"Finger {finger} touched sensor");
        UpdateStatus($"Finger detected. Keep finger on scanner...");
    }

    private void EnrollmentControl_OnFingerRemove(object control, string readerSerialNumber, int finger)
    {
        LogEvent($"Finger {finger} removed from sensor");
    }

    private void EnrollmentControl_OnSampleQuality(object control, string readerSerialNumber, int finger, DPFP.Capture.CaptureFeedback feedback)
    {
        var quality = feedback switch
        {
            DPFP.Capture.CaptureFeedback.Good => "Good quality",
            DPFP.Capture.CaptureFeedback.TooLight => "Too light - press harder",
            DPFP.Capture.CaptureFeedback.TooNoisy => "Too noisy - clean sensor",
            DPFP.Capture.CaptureFeedback.LowContrast => "Low contrast - adjust position",
            DPFP.Capture.CaptureFeedback.NotEnoughFeatures => "Not enough features - try again",
            DPFP.Capture.CaptureFeedback.TooSmall => "Too small - center finger",
            DPFP.Capture.CaptureFeedback.TooShort => "Swipe too short",
            DPFP.Capture.CaptureFeedback.TooSlow => "Swipe too slow",
            DPFP.Capture.CaptureFeedback.TooFast => "Swipe too fast",
            DPFP.Capture.CaptureFeedback.TooSkewed => "Finger too skewed",
            _ => feedback.ToString()
        };

        LogEvent($"Quality: {quality}");
        if (feedback != DPFP.Capture.CaptureFeedback.Good)
        {
            UpdateStatus(quality);
        }
    }

    private void EnrollmentControl_OnReaderConnect(object control, string readerSerialNumber, int finger)
    {
        LogEvent($"Reader connected: {readerSerialNumber}");
        UpdateStatus("Scanner connected. Select a finger to enroll.");
    }

    private void EnrollmentControl_OnReaderDisconnect(object control, string readerSerialNumber, int finger)
    {
        LogEvent($"Reader disconnected: {readerSerialNumber}");
        UpdateStatus("Scanner disconnected! Please reconnect.");
    }

    private void EnrollmentControl_OnCancelEnroll(object control, string readerSerialNumber, int finger)
    {
        LogEvent($"Enrollment cancelled for finger {finger}");
        UpdateStatus("Enrollment cancelled. Select a finger to try again.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _enrollmentControl?.Dispose();
            _eventsListBox?.Dispose();
            _closeButton?.Dispose();
            _statusLabel?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Result from the DPFP enrollment form.
/// </summary>
public sealed class DpfpEnrollmentResult
{
    /// <summary>
    /// Whether the enrollment was completed (user clicked Done).
    /// </summary>
    public bool Completed { get; init; }

    /// <summary>
    /// Array of enrolled templates (index 0-9 for fingers 1-10).
    /// </summary>
    public Template?[] Templates { get; init; } = new Template?[10];

    /// <summary>
    /// Bitmask of enrolled fingers.
    /// </summary>
    public int EnrolledFingersMask { get; init; }

    /// <summary>
    /// Count of enrolled fingers.
    /// </summary>
    public int EnrolledCount { get; init; }
}

#endif
