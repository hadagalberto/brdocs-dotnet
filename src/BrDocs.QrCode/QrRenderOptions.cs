namespace BrDocs.QrCode;

/// <summary>Options controlling the SVG and PNG renderings of a <see cref="QrCode"/>.</summary>
public sealed class QrRenderOptions
{
    /// <summary>
    /// Width of the light quiet zone, in modules, added on every side. ISO/IEC 18004 requires at
    /// least 4; the default is 4. Lower values are rejected.
    /// </summary>
    public int QuietZoneModules { get; init; } = 4;

    /// <summary>
    /// Size of one module in device pixels for the PNG output. The default is 4. Must be positive.
    /// SVG output always uses a unit viewBox and is resolution independent.
    /// </summary>
    public int ModulePixels { get; init; } = 4;

    /// <summary>The SVG fill colour of dark modules. The default is <c>#000000</c>.</summary>
    public string Foreground { get; init; } = "#000000";

    /// <summary>The SVG fill colour of the quiet zone and light modules. The default is <c>#FFFFFF</c>.</summary>
    public string Background { get; init; } = "#FFFFFF";
}
