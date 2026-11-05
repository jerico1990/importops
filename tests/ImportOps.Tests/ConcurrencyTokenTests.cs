using ImportOps.Application.Common;

namespace ImportOps.Tests;

public sealed class ConcurrencyTokenTests
{
    [Fact]
    public void EncodeDecode_RoundTrip()
    {
        var original = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var encoded = ConcurrencyToken.Encode(original);
        var decoded = ConcurrencyToken.Decode(encoded);

        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Decode_InvalidBase64_ThrowsValidationError()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ConcurrencyToken.Decode("not-base64"));

        Assert.Equal(ErrorCodes.Validation, ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }
}
