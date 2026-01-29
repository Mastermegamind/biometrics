using Microsoft.Win32.SafeHandles;

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint.Libfprint;

/// <summary>
/// Safe handle for GMainContext pointer.
/// </summary>
internal sealed class GMainContextHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public GMainContextHandle() : base(true) { }

    public GMainContextHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            LibfprintNative.g_main_context_unref(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for FpContext pointer.
/// </summary>
internal sealed class FpContextHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public FpContextHandle() : base(true) { }

    public FpContextHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            LibfprintNative.fp_context_unref(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for FpDevice pointer.
/// Note: Device handles are owned by the context and should not be freed directly.
/// </summary>
internal sealed class FpDeviceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly FpContextHandle? _context;
    private bool _isOpen;

    public FpDeviceHandle(IntPtr handle, FpContextHandle context) : base(false)
    {
        SetHandle(handle);
        _context = context;
    }

    public bool IsOpen => _isOpen;

    public void MarkOpen() => _isOpen = true;
    public void MarkClosed() => _isOpen = false;

    protected override bool ReleaseHandle()
    {
        if (_isOpen && !IsInvalid)
        {
            LibfprintNative.fp_device_close_sync(handle, IntPtr.Zero, out _);
            _isOpen = false;
        }
        return true;
    }
}

/// <summary>
/// Safe handle for FpPrint pointer.
/// </summary>
internal sealed class FpPrintHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public FpPrintHandle() : base(true) { }

    public FpPrintHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            LibfprintNative.fp_print_unref(handle);
        }
        return true;
    }
}

/// <summary>
/// Safe handle for FpImage pointer.
/// </summary>
internal sealed class FpImageHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public FpImageHandle() : base(true) { }

    public FpImageHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            LibfprintNative.fp_image_unref(handle);
        }
        return true;
    }
}
