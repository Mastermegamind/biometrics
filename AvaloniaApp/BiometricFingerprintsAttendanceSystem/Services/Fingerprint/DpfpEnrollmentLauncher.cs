// Launcher service for the DPFP WinForms enrollment form
// Provides a bridge between Avalonia and WinForms for enrollment

#if DIGITALPERSONA_SDK
using WinForms = System.Windows.Forms;
#endif

using BiometricFingerprintsAttendanceSystem.Services.Data;

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Result from launching the DPFP enrollment UI.
/// </summary>
public sealed class EnrollmentLaunchResult
{
    public bool Success { get; init; }
    public bool Cancelled { get; init; }
    public string? ErrorMessage { get; init; }
    public List<FingerprintTemplate> Templates { get; init; } = new();
    public int EnrolledCount { get; init; }
}

/// <summary>
/// Service to launch the native DigitalPersona enrollment UI.
/// Falls back to indicating Avalonia UI should be used on non-Windows platforms.
/// </summary>
public static class DpfpEnrollmentLauncher
{
    /// <summary>
    /// Checks if the native DPFP enrollment UI is available.
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
#if DIGITALPERSONA_SDK
            return OperatingSystem.IsWindows();
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Launches the native DPFP enrollment form and returns the results.
    /// Must be called from the UI thread.
    /// </summary>
    /// <param name="existingTemplates">Optional existing templates for re-enrollment.</param>
    /// <param name="maxFingers">Maximum number of fingers to enroll (default 10).</param>
    /// <returns>Enrollment result with templates.</returns>
    public static Task<EnrollmentLaunchResult> LaunchAsync(
        IReadOnlyList<FingerprintTemplate>? existingTemplates = null,
        int maxFingers = 10)
    {
#if DIGITALPERSONA_SDK
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(new EnrollmentLaunchResult
            {
                Success = false,
                ErrorMessage = "Native DPFP enrollment UI is only available on Windows."
            });
        }

        return Task.Run(() =>
        {
            try
            {
                // Must run on STA thread for WinForms
                EnrollmentLaunchResult? result = null;
                var thread = new Thread(() =>
                {
                    result = LaunchFormInternal(existingTemplates, maxFingers);
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                return result ?? new EnrollmentLaunchResult
                {
                    Success = false,
                    ErrorMessage = "Enrollment form returned no result."
                };
            }
            catch (Exception ex)
            {
                return new EnrollmentLaunchResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to launch enrollment form: {ex.Message}"
                };
            }
        });
#else
        return Task.FromResult(new EnrollmentLaunchResult
        {
            Success = false,
            ErrorMessage = "DPFP SDK not available. Build with IncludeFingerprintSdks=true."
        });
#endif
    }

#if DIGITALPERSONA_SDK
    private static EnrollmentLaunchResult LaunchFormInternal(
        IReadOnlyList<FingerprintTemplate>? existingTemplates,
        int maxFingers)
    {
        WinForms.Application.EnableVisualStyles();
        WinForms.Application.SetCompatibleTextRenderingDefault(false);

        using var form = new DpfpEnrollmentForm
        {
            MaxEnrollFingerCount = maxFingers
        };

        // Set existing templates if provided
        if (existingTemplates != null && existingTemplates.Count > 0)
        {
            var templates = new DPFP.Template?[10];
            int mask = 0;

            foreach (var t in existingTemplates)
            {
                if (t.FingerIndex >= 1 && t.FingerIndex <= 10 && t.TemplateData.Length > 0)
                {
                    try
                    {
                        using var stream = new MemoryStream(t.TemplateData);
                        templates[t.FingerIndex - 1] = new DPFP.Template(stream);
                        mask |= (1 << (t.FingerIndex - 1));
                    }
                    catch
                    {
                        // Invalid template data, skip
                    }
                }
            }

            form.SetExistingTemplates(templates);
        }

        // Show the form
        var dialogResult = form.ShowDialog();

        if (dialogResult != WinForms.DialogResult.OK)
        {
            return new EnrollmentLaunchResult
            {
                Success = false,
                Cancelled = true
            };
        }

        // Convert templates to FingerprintTemplate list
        var resultTemplates = new List<FingerprintTemplate>();
        for (int i = 0; i < 10; i++)
        {
            var template = form.Templates[i];
            if (template != null)
            {
                using var stream = new MemoryStream();
                template.Serialize(stream);

                resultTemplates.Add(new FingerprintTemplate
                {
                    Finger = GetFingerName(i + 1),
                    FingerIndex = i + 1,
                    TemplateData = stream.ToArray()
                });
            }
        }

        return new EnrollmentLaunchResult
        {
            Success = true,
            Templates = resultTemplates,
            EnrolledCount = form.EnrolledCount
        };
    }

    private static string GetFingerName(int fingerIndex)
    {
        return fingerIndex switch
        {
            1 => "RightThumb",
            2 => "RightIndex",
            3 => "RightMiddle",
            4 => "RightRing",
            5 => "RightLittle",
            6 => "LeftThumb",
            7 => "LeftIndex",
            8 => "LeftMiddle",
            9 => "LeftRing",
            10 => "LeftLittle",
            _ => $"Finger{fingerIndex}"
        };
    }
#endif
}
