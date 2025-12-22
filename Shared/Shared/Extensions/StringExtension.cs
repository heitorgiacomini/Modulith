namespace Shared.Extensions;

public static class StringExtension
{
    public static Boolean HasContent(this String? value)
    {
        return !String.IsNullOrEmpty(value);
    }
}
