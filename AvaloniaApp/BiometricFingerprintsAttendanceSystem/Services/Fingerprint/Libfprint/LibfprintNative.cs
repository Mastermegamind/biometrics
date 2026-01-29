using System.Runtime.InteropServices;

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint.Libfprint;

/// <summary>
/// P/Invoke declarations for libfprint2.so.
/// Reference: https://fprint.freedesktop.org/libfprint-dev/
/// </summary>
internal static partial class LibfprintNative
{
    private const string LibFprint = "libfprint-2.so.2";
    private const string LibGLib = "libglib-2.0.so.0";
    private const string LibGObject = "libgobject-2.0.so.0";
    private const string LibGio = "libgio-2.0.so.0";

    // ==================== Context Management ====================

    [LibraryImport(LibFprint, EntryPoint = "fp_context_new")]
    internal static partial IntPtr fp_context_new();

    [LibraryImport(LibGObject, EntryPoint = "g_object_unref")]
    internal static partial void fp_context_unref(IntPtr context);

    // ==================== Device Discovery ====================

    [LibraryImport(LibFprint, EntryPoint = "fp_context_enumerate")]
    internal static partial void fp_context_enumerate(IntPtr context);

    [LibraryImport(LibFprint, EntryPoint = "fp_context_get_devices")]
    internal static partial IntPtr fp_context_get_devices(IntPtr context);

    // GPtrArray helpers (GLib)
    [LibraryImport(LibGLib, EntryPoint = "g_ptr_array_ref")]
    internal static partial IntPtr g_ptr_array_ref(IntPtr array);

    [LibraryImport(LibGLib, EntryPoint = "g_ptr_array_unref")]
    internal static partial void g_ptr_array_unref(IntPtr array);

    // ==================== Device Operations ====================

    [LibraryImport(LibFprint, EntryPoint = "fp_device_open_sync")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool fp_device_open_sync(
        IntPtr device,
        IntPtr cancellable,
        out IntPtr error);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_close_sync")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool fp_device_close_sync(
        IntPtr device,
        IntPtr cancellable,
        out IntPtr error);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_get_name")]
    internal static partial IntPtr fp_device_get_name(IntPtr device);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_get_driver")]
    internal static partial IntPtr fp_device_get_driver(IntPtr device);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_get_device_id")]
    internal static partial IntPtr fp_device_get_device_id(IntPtr device);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_get_scan_type")]
    internal static partial FpScanType fp_device_get_scan_type(IntPtr device);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_get_nr_enroll_stages")]
    internal static partial int fp_device_get_nr_enroll_stages(IntPtr device);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_supports_capture")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool fp_device_supports_capture(IntPtr device);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_supports_identify")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool fp_device_supports_identify(IntPtr device);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_has_feature")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool fp_device_has_feature(IntPtr device, FpDeviceFeature feature);

    // ==================== Capture Operations ====================

    [LibraryImport(LibFprint, EntryPoint = "fp_device_capture_sync")]
    internal static partial IntPtr fp_device_capture_sync(
        IntPtr device,
        [MarshalAs(UnmanagedType.Bool)] bool waitForFinger,
        IntPtr cancellable,
        out IntPtr error);

    // ==================== Image Operations ====================

    [LibraryImport(LibFprint, EntryPoint = "fp_image_get_width")]
    internal static partial uint fp_image_get_width(IntPtr image);

    [LibraryImport(LibFprint, EntryPoint = "fp_image_get_height")]
    internal static partial uint fp_image_get_height(IntPtr image);

    [LibraryImport(LibFprint, EntryPoint = "fp_image_get_ppmm")]
    internal static partial double fp_image_get_ppmm(IntPtr image);

    [LibraryImport(LibFprint, EntryPoint = "fp_image_get_data")]
    internal static partial IntPtr fp_image_get_data(IntPtr image, out nuint length);

    [LibraryImport(LibFprint, EntryPoint = "fp_image_get_minutiae")]
    internal static partial IntPtr fp_image_get_minutiae(IntPtr image, out int nrMinutiae);

    [LibraryImport(LibGObject, EntryPoint = "g_object_unref")]
    internal static partial void fp_image_unref(IntPtr image);

    // ==================== Print (Template) Operations ====================

    [LibraryImport(LibFprint, EntryPoint = "fp_print_new")]
    internal static partial IntPtr fp_print_new(IntPtr device);

    [LibraryImport(LibFprint, EntryPoint = "fp_print_serialize")]
    internal static partial IntPtr fp_print_serialize(
        IntPtr print,
        out nuint length,
        out IntPtr error);

    [LibraryImport(LibFprint, EntryPoint = "fp_print_deserialize")]
    internal static partial IntPtr fp_print_deserialize(
        IntPtr data,
        nuint length,
        out IntPtr error);

    [LibraryImport(LibFprint, EntryPoint = "fp_print_get_finger")]
    internal static partial FpFinger fp_print_get_finger(IntPtr print);

    [LibraryImport(LibFprint, EntryPoint = "fp_print_set_finger")]
    internal static partial void fp_print_set_finger(IntPtr print, FpFinger finger);

    [LibraryImport(LibGObject, EntryPoint = "g_object_unref")]
    internal static partial void fp_print_unref(IntPtr print);

    [LibraryImport(LibGObject, EntryPoint = "g_object_unref")]
    internal static partial void g_object_unref(IntPtr obj);

    // ==================== Enrollment Operations ====================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void FpEnrollProgressCallback(
        IntPtr device,
        int completed,
        IntPtr print,
        int stage,
        IntPtr userData,
        IntPtr error);

    [LibraryImport(LibFprint, EntryPoint = "fp_device_enroll_sync")]
    internal static partial IntPtr fp_device_enroll_sync(
        IntPtr device,
        IntPtr templatePrint,
        IntPtr cancellable,
        FpEnrollProgressCallback progressCallback,
        IntPtr progressData,
        out IntPtr error);

    // ==================== Verification Operations ====================

    [LibraryImport(LibFprint, EntryPoint = "fp_device_verify_sync")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool fp_device_verify_sync(
        IntPtr device,
        IntPtr enrolledPrint,
        IntPtr cancellable,
        out IntPtr matchedPrint,
        IntPtr matchCallback,
        IntPtr matchData,
        out IntPtr error);

    // ==================== Identification Operations ====================

    [LibraryImport(LibFprint, EntryPoint = "fp_device_identify_sync")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool fp_device_identify_sync(
        IntPtr device,
        IntPtr prints,       // GPtrArray of FpPrint*
        IntPtr cancellable,
        out IntPtr matchedPrint,
        out IntPtr print,
        IntPtr matchCallback,
        IntPtr matchData,
        out IntPtr error);

    // ==================== GError Handling ====================

    [LibraryImport(LibGLib, EntryPoint = "g_error_free")]
    internal static partial void g_error_free(IntPtr error);

    [LibraryImport(LibGLib, EntryPoint = "g_free")]
    internal static partial void g_free(IntPtr mem);

    [LibraryImport(LibGLib, EntryPoint = "g_quark_to_string")]
    internal static partial IntPtr g_quark_to_string(uint quark);

    [LibraryImport(LibGio, EntryPoint = "g_cancellable_new")]
    internal static partial IntPtr g_cancellable_new();

    [LibraryImport(LibGio, EntryPoint = "g_cancellable_cancel")]
    internal static partial void g_cancellable_cancel(IntPtr cancellable);

    // ==================== GMainContext (required for sync operations) ====================

    [LibraryImport(LibGLib, EntryPoint = "g_main_context_new")]
    internal static partial IntPtr g_main_context_new();

    [LibraryImport(LibGLib, EntryPoint = "g_main_context_unref")]
    internal static partial void g_main_context_unref(IntPtr context);

    [LibraryImport(LibGLib, EntryPoint = "g_main_context_push_thread_default")]
    internal static partial void g_main_context_push_thread_default(IntPtr context);

    [LibraryImport(LibGLib, EntryPoint = "g_main_context_pop_thread_default")]
    internal static partial void g_main_context_pop_thread_default(IntPtr context);

    [LibraryImport(LibGLib, EntryPoint = "g_main_context_iteration")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool g_main_context_iteration(IntPtr context, [MarshalAs(UnmanagedType.Bool)] bool mayBlock);

    // ==================== GPtrArray Operations ====================

    [LibraryImport(LibGLib, EntryPoint = "g_ptr_array_new")]
    internal static partial IntPtr g_ptr_array_new();

    [LibraryImport(LibGLib, EntryPoint = "g_ptr_array_new_with_free_func")]
    internal static partial IntPtr g_ptr_array_new_with_free_func(IntPtr elementFreeFunc);

    [LibraryImport(LibGLib, EntryPoint = "g_ptr_array_add")]
    internal static partial void g_ptr_array_add(IntPtr array, IntPtr data);

    [LibraryImport(LibGLib, EntryPoint = "g_ptr_array_free")]
    internal static partial IntPtr g_ptr_array_free(IntPtr array, [MarshalAs(UnmanagedType.Bool)] bool freeSeg);

    /// <summary>
    /// Gets the length of a GPtrArray by reading the len field.
    /// GPtrArray structure: { gpointer *pdata; guint len; }
    /// </summary>
    internal static uint GetPtrArrayLength(IntPtr array)
    {
        if (array == IntPtr.Zero) return 0;
        // len is at offset sizeof(IntPtr) in the GPtrArray struct
        return (uint)Marshal.ReadInt32(array, IntPtr.Size);
    }

    /// <summary>
    /// Gets an element from a GPtrArray by index.
    /// </summary>
    internal static IntPtr GetPtrArrayIndex(IntPtr array, uint index)
    {
        if (array == IntPtr.Zero) return IntPtr.Zero;
        // pdata is the first field (pointer to array of pointers)
        var pdata = Marshal.ReadIntPtr(array);
        if (pdata == IntPtr.Zero) return IntPtr.Zero;
        // Read the pointer at the given index
        return Marshal.ReadIntPtr(pdata, (int)(index * (uint)IntPtr.Size));
    }
}
