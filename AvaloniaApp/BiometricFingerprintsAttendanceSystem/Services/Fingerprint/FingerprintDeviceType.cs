namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Supported fingerprint device types.
/// </summary>
public enum FingerprintDeviceType
{
    /// <summary>
    /// No device configured.
    /// </summary>
    None,

    // ==================== Windows SDK Devices ====================

    /// <summary>
    /// DigitalPersona U.are.U 4500 fingerprint reader (Windows SDK).
    /// USB VID:PID 05ba:000a
    /// </summary>
    DigitalPersona4500,

    /// <summary>
    /// Mantra MFS100 fingerprint scanner (Windows SDK).
    /// </summary>
    MantraMfs100,

    /// <summary>
    /// Windows Biometric Framework (WBF) - uses Windows Hello infrastructure.
    /// Works with any WBF-compatible fingerprint reader.
    /// </summary>
    WindowsBiometric,

    // ==================== Linux Devices (libfprint/fprintd) ====================

    /// <summary>
    /// Direct libfprint2 bindings for Linux.
    /// Provides raw template access for application-managed enrollment.
    /// Preferred over Fprintd for database-stored templates.
    /// </summary>
    LibfprintDirect,

    /// <summary>
    /// Linux fprintd/libfprint compatible devices (system-managed).
    /// Auto-detects any device supported by libfprint.
    /// Note: Does not provide raw template data - use LibfprintDirect instead.
    /// </summary>
    Fprintd,

    /// <summary>
    /// Validity Sensors fingerprint readers (Linux).
    /// USB VID: 138a - Common in Dell/HP laptops.
    /// </summary>
    ValiditySensors,

    /// <summary>
    /// Goodix fingerprint sensors (Linux).
    /// USB VID: 27c6 - Common in modern laptops.
    /// </summary>
    Goodix,

    /// <summary>
    /// Synaptics fingerprint sensors (Linux).
    /// USB VID: 06cb - Common in Lenovo/Dell laptops.
    /// </summary>
    Synaptics,

    /// <summary>
    /// Elan Microelectronics fingerprint sensors (Linux).
    /// USB VID: 04f3 - Common in ASUS/Acer laptops.
    /// </summary>
    Elan,

    /// <summary>
    /// FocalTech fingerprint sensors (Linux).
    /// USB VID: 2808
    /// </summary>
    FocalTech,

    /// <summary>
    /// LighTuning fingerprint sensors (Linux).
    /// USB VID: 1c7a
    /// </summary>
    LighTuning,

    /// <summary>
    /// Upek/AuthenTec fingerprint sensors (Linux).
    /// USB VID: 147e - Legacy devices, now owned by Apple.
    /// </summary>
    Upek,

    // ==================== Auto Detection ====================

    /// <summary>
    /// Auto-detect device based on platform and available hardware.
    /// Linux: Uses fprintd with auto-detected device
    /// Windows: Tries DigitalPersona SDK, falls back to WBF
    /// </summary>
    Auto
}
