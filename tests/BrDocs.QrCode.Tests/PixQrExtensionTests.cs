using BrDocs.QrCode;
using Xunit;

namespace BrDocs.QrCode.Tests;

public class PixQrExtensionTests
{
    private const string BcbPayload =
        "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D";

    [Fact]
    public void ToQrSvg_EncodesTheWholeBrCode()
    {
        PixPayload payload = PixPayload.Parse(BcbPayload);

        string svg = payload.ToQrSvg();

        Assert.Equal(QrCode.Encode(payload.ToString()).ToSvg(), svg);
        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", svg);
        Assert.Contains("<path ", svg);
        Assert.Contains("shape-rendering=\"crispEdges\"", svg);
    }

    [Fact]
    public void ToQrCode_UsesByteModeAndTheRequestedErrorCorrection()
    {
        PixPayload payload = PixPayload.Parse(BcbPayload);

        QrCode code = payload.ToQrCode(QrErrorCorrection.H);

        Assert.Equal(QrErrorCorrection.H, code.ErrorCorrection);
        Assert.Equal(QrEncoding.Byte, code.Mode);
        Assert.Equal((4 * code.Version) + 17, code.Size);
        Assert.Equal(BcbPayload, payload.ToString());
        Assert.Equal(payload.ToQrSvg(QrErrorCorrection.H), code.ToSvg());
    }

    [Fact]
    public void ToQrSvg_HonoursRenderOptionsAndRejectsNull()
    {
        PixPayload payload = PixPayload.Parse(BcbPayload);
        QrRenderOptions options = new() { QuietZoneModules = 6, ModulePixels = 3 };

        Assert.Equal(payload.ToQrCode().ToSvg(options), payload.ToQrSvg(QrErrorCorrection.M, options));
        Assert.Throws<ArgumentNullException>(() => PixPayloadQrExtensions.ToQrSvg(null!));
        Assert.Throws<ArgumentNullException>(() => PixPayloadQrExtensions.ToQrCode(null!));
    }
}
