namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint.Libfprint;

/// <summary>
/// libfprint scan type enumeration.
/// </summary>
internal enum FpScanType
{
    Swipe = 0,
    Press = 1
}

/// <summary>
/// libfprint finger position enumeration.
/// Maps to the app's FingerPosition enum.
/// </summary>
internal enum FpFinger
{
    Unknown = 0,
    LeftThumb = 1,
    LeftIndexFinger = 2,
    LeftMiddleFinger = 3,
    LeftRingFinger = 4,
    LeftLittleFinger = 5,
    RightThumb = 6,
    RightIndexFinger = 7,
    RightMiddleFinger = 8,
    RightRingFinger = 9,
    RightLittleFinger = 10
}

/// <summary>
/// libfprint device features.
/// </summary>
[Flags]
internal enum FpDeviceFeature
{
    None = 0,
    Capture = 1 << 0,
    Identify = 1 << 1,
    Verify = 1 << 2,
    StorageList = 1 << 3,
    StorageDelete = 1 << 4,
    StorageClear = 1 << 5,
    DuplicatesCheck = 1 << 6,
    AlwaysOnFpSensor = 1 << 7
}

/// <summary>
/// libfprint device error codes.
/// </summary>
public enum FpDeviceError
{
    General = 0,
    NotSupported = 1,
    NotOpen = 2,
    AlreadyOpen = 3,
    Busy = 4,
    ProtoError = 5,
    DataInvalid = 6,
    DataNotFound = 7,
    DataFull = 8,
    DataDuplicate = 9,
    Retry = 10,
    RetryTooShort = 11,
    RetryCenter = 12,
    RetryRemoveAndRetry = 13
}
