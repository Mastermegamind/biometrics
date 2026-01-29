namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Represents the position/identity of a finger for enrollment and verification.
/// Values match fprintd finger naming conventions.
/// </summary>
public enum FingerPosition
{
    Unknown = 0,

    // Right hand
    RightThumb = 1,
    RightIndexFinger = 2,
    RightMiddleFinger = 3,
    RightRingFinger = 4,
    RightLittleFinger = 5,

    // Left hand
    LeftThumb = 6,
    LeftIndexFinger = 7,
    LeftMiddleFinger = 8,
    LeftRingFinger = 9,
    LeftLittleFinger = 10
}

public static class FingerPositionExtensions
{
    /// <summary>
    /// Gets the fprintd-compatible finger name.
    /// </summary>
    public static string ToFprintdName(this FingerPosition finger) => finger switch
    {
        FingerPosition.RightThumb => "right-thumb",
        FingerPosition.RightIndexFinger => "right-index-finger",
        FingerPosition.RightMiddleFinger => "right-middle-finger",
        FingerPosition.RightRingFinger => "right-ring-finger",
        FingerPosition.RightLittleFinger => "right-little-finger",
        FingerPosition.LeftThumb => "left-thumb",
        FingerPosition.LeftIndexFinger => "left-index-finger",
        FingerPosition.LeftMiddleFinger => "left-middle-finger",
        FingerPosition.LeftRingFinger => "left-ring-finger",
        FingerPosition.LeftLittleFinger => "left-little-finger",
        _ => "any"
    };

    /// <summary>
    /// Parses an fprintd finger name to FingerPosition.
    /// </summary>
    public static FingerPosition FromFprintdName(string name) => name.ToLowerInvariant() switch
    {
        "right-thumb" => FingerPosition.RightThumb,
        "right-index-finger" => FingerPosition.RightIndexFinger,
        "right-middle-finger" => FingerPosition.RightMiddleFinger,
        "right-ring-finger" => FingerPosition.RightRingFinger,
        "right-little-finger" => FingerPosition.RightLittleFinger,
        "left-thumb" => FingerPosition.LeftThumb,
        "left-index-finger" => FingerPosition.LeftIndexFinger,
        "left-middle-finger" => FingerPosition.LeftMiddleFinger,
        "left-ring-finger" => FingerPosition.LeftRingFinger,
        "left-little-finger" => FingerPosition.LeftLittleFinger,
        _ => FingerPosition.Unknown
    };

    /// <summary>
    /// Gets a human-readable display name for the finger.
    /// </summary>
    public static string ToDisplayName(this FingerPosition finger) => finger switch
    {
        FingerPosition.RightThumb => "Right Thumb",
        FingerPosition.RightIndexFinger => "Right Index Finger",
        FingerPosition.RightMiddleFinger => "Right Middle Finger",
        FingerPosition.RightRingFinger => "Right Ring Finger",
        FingerPosition.RightLittleFinger => "Right Little Finger",
        FingerPosition.LeftThumb => "Left Thumb",
        FingerPosition.LeftIndexFinger => "Left Index Finger",
        FingerPosition.LeftMiddleFinger => "Left Middle Finger",
        FingerPosition.LeftRingFinger => "Left Ring Finger",
        FingerPosition.LeftLittleFinger => "Left Little Finger",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets whether this is a finger on the left hand.
    /// </summary>
    public static bool IsLeftHand(this FingerPosition finger) =>
        finger >= FingerPosition.LeftThumb && finger <= FingerPosition.LeftLittleFinger;

    /// <summary>
    /// Gets whether this is a finger on the right hand.
    /// </summary>
    public static bool IsRightHand(this FingerPosition finger) =>
        finger >= FingerPosition.RightThumb && finger <= FingerPosition.RightLittleFinger;
}
