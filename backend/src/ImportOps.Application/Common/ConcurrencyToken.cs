namespace ImportOps.Application.Common;

public static class ConcurrencyToken
{
    public static byte[] Decode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessException(ErrorCodes.Validation, "rowVersion es obligatorio.", 400);

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new BusinessException(ErrorCodes.Validation, "rowVersion no tiene un formato Base64 válido.", 400);
        }
    }

    public static string Encode(byte[] value) => Convert.ToBase64String(value);
}
