namespace BiometricFingerprintsAttendanceSystem.Services.Api;

/// <summary>
/// Demo wrapper for BiometricsApiClient that provides mock data for demo student
/// while still allowing real API calls for other students.
/// </summary>
public sealed class DemoBiometricsApiClient
{
    private readonly BiometricsApiClient _realClient;
    private readonly string _demoStudentRegNo;
    private readonly string _demoStudentName;
    private readonly string _demoStudentClass;
    private readonly Dictionary<string, List<FingerprintTemplatePayload>> _demoEnrollments = new();

    public DemoBiometricsApiClient(
        BiometricsApiClient realClient,
        string demoStudentRegNo,
        string demoStudentName,
        string demoStudentClass)
    {
        _realClient = realClient;
        _demoStudentRegNo = demoStudentRegNo;
        _demoStudentName = demoStudentName;
        _demoStudentClass = demoStudentClass;
    }

    public async Task<StudentProfile?> GetStudentByRegAsync(string regNo, CancellationToken cancellationToken = default)
    {
        // Return demo student data if it matches the demo registration number
        if (string.Equals(regNo, _demoStudentRegNo, StringComparison.OrdinalIgnoreCase))
        {
            return new StudentProfile
            {
                RegNo = _demoStudentRegNo,
                Name = _demoStudentName,
                ClassName = _demoStudentClass,
                Email = $"{_demoStudentRegNo.ToLowerInvariant()}@demo.local",
                Phone = "000-000-0000",
                Passport = string.Empty
            };
        }

        // Otherwise, try the real API
        try
        {
            return await _realClient.GetStudentByRegAsync(regNo, cancellationToken);
        }
        catch
        {
            // If API is unavailable and it's the demo student, return demo data
            if (string.Equals(regNo, _demoStudentRegNo, StringComparison.OrdinalIgnoreCase))
            {
                return new StudentProfile
                {
                    RegNo = _demoStudentRegNo,
                    Name = _demoStudentName,
                    ClassName = _demoStudentClass,
                    Email = $"{_demoStudentRegNo.ToLowerInvariant()}@demo.local",
                    Phone = "000-000-0000",
                    Passport = string.Empty
                };
            }
            throw;
        }
    }

    public async Task<EnrollmentStatus?> GetEnrollmentStatusAsync(string regNo, CancellationToken cancellationToken = default)
    {
        // Check if demo student is enrolled in-memory
        if (string.Equals(regNo, _demoStudentRegNo, StringComparison.OrdinalIgnoreCase))
        {
            return new EnrollmentStatus
            {
                IsEnrolled = _demoEnrollments.ContainsKey(_demoStudentRegNo.ToUpperInvariant())
            };
        }

        // Otherwise, try the real API
        try
        {
            return await _realClient.GetEnrollmentStatusAsync(regNo, cancellationToken);
        }
        catch
        {
            // If API is unavailable for demo student, check in-memory
            if (string.Equals(regNo, _demoStudentRegNo, StringComparison.OrdinalIgnoreCase))
            {
                return new EnrollmentStatus
                {
                    IsEnrolled = _demoEnrollments.ContainsKey(_demoStudentRegNo.ToUpperInvariant())
                };
            }
            throw;
        }
    }

    public async Task<ApiResult> SubmitEnrollmentAsync(EnrollmentSubmission submission, CancellationToken cancellationToken = default)
    {
        // Handle demo student enrollment in-memory
        if (string.Equals(submission.RegNo, _demoStudentRegNo, StringComparison.OrdinalIgnoreCase))
        {
            var key = _demoStudentRegNo.ToUpperInvariant();
            _demoEnrollments[key] = new List<FingerprintTemplatePayload>(submission.Templates);
            return new ApiResult { Success = true, Message = "Demo enrollment saved successfully." };
        }

        // Otherwise, try the real API
        try
        {
            return await _realClient.SubmitEnrollmentAsync(submission, cancellationToken);
        }
        catch
        {
            // If API is unavailable for demo student, save in-memory
            if (string.Equals(submission.RegNo, _demoStudentRegNo, StringComparison.OrdinalIgnoreCase))
            {
                var key = _demoStudentRegNo.ToUpperInvariant();
                _demoEnrollments[key] = new List<FingerprintTemplatePayload>(submission.Templates);
                return new ApiResult { Success = true, Message = "Demo enrollment saved locally." };
            }
            throw;
        }
    }

    public async Task<StudentProfile?> IdentifyAsync(IdentifyRequest request, CancellationToken cancellationToken = default)
    {
        // In demo mode, check if the fingerprint matches demo student enrollment
        var key = _demoStudentRegNo.ToUpperInvariant();
        if (_demoEnrollments.TryGetValue(key, out var templates))
        {
            // Simple check: if we have demo enrollments, return demo student on identify
            // In real usage, this would do fingerprint matching
            // For demo purposes, any fingerprint scan returns the demo student if enrolled
            return new StudentProfile
            {
                RegNo = _demoStudentRegNo,
                Name = _demoStudentName,
                ClassName = _demoStudentClass,
                Email = $"{_demoStudentRegNo.ToLowerInvariant()}@demo.local",
                Phone = "000-000-0000",
                Passport = string.Empty
            };
        }

        // Otherwise, try the real API
        try
        {
            return await _realClient.IdentifyAsync(request, cancellationToken);
        }
        catch
        {
            // If API unavailable but demo student is enrolled, return demo student
            if (_demoEnrollments.ContainsKey(key))
            {
                return new StudentProfile
                {
                    RegNo = _demoStudentRegNo,
                    Name = _demoStudentName,
                    ClassName = _demoStudentClass,
                    Email = $"{_demoStudentRegNo.ToLowerInvariant()}@demo.local",
                    Phone = "000-000-0000",
                    Passport = string.Empty
                };
            }
            throw;
        }
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        // In demo mode, always return true even if API is unavailable
        try
        {
            return await _realClient.PingAsync(cancellationToken);
        }
        catch
        {
            return true; // Demo mode considers API always "available"
        }
    }

    public bool IsDemoStudent(string regNo) =>
        string.Equals(regNo, _demoStudentRegNo, StringComparison.OrdinalIgnoreCase);

    public bool HasDemoEnrollment =>
        _demoEnrollments.ContainsKey(_demoStudentRegNo.ToUpperInvariant());

    public IReadOnlyList<FingerprintTemplatePayload>? GetDemoEnrollment()
    {
        var key = _demoStudentRegNo.ToUpperInvariant();
        return _demoEnrollments.TryGetValue(key, out var templates) ? templates : null;
    }

    public void ClearDemoEnrollment()
    {
        _demoEnrollments.Remove(_demoStudentRegNo.ToUpperInvariant());
    }
}
