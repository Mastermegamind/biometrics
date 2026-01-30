// Global using directives to resolve type conflicts between Avalonia and Windows Forms
// when building with DigitalPersona SDK support

global using BiometricFingerprintsAttendanceSystem.Services.Time;

#if DIGITALPERSONA_SDK
// Explicitly use Avalonia types instead of Windows Forms/System.Drawing equivalents
global using Application = Avalonia.Application;
global using Bitmap = Avalonia.Media.Imaging.Bitmap;
global using Control = Avalonia.Controls.Control;
global using UserControl = Avalonia.Controls.UserControl;
global using Button = Avalonia.Controls.Button;
global using TextBox = Avalonia.Controls.TextBox;
global using Label = Avalonia.Controls.TextBlock;
global using Panel = Avalonia.Controls.Panel;
global using Image = Avalonia.Controls.Image;
global using Timer = System.Threading.Timer;
global using MessageBox = Avalonia.Controls.Window;
global using Color = Avalonia.Media.Color;
global using Brush = Avalonia.Media.IBrush;
global using Brushes = Avalonia.Media.Brushes;
global using Font = Avalonia.Media.FontFamily;
global using Point = Avalonia.Point;
global using Size = Avalonia.Size;
global using Rectangle = Avalonia.Rect;
global using Clipboard = Avalonia.Input.Platform.IClipboard;
global using Cursor = Avalonia.Input.Cursor;
global using Keys = Avalonia.Input.Key;
global using MouseButtons = Avalonia.Input.PointerUpdateKind;
global using Pen = Avalonia.Media.Pen;
global using Graphics = Avalonia.Media.DrawingContext;
global using SolidBrush = Avalonia.Media.SolidColorBrush;
#endif
