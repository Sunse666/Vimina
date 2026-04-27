namespace Vimina.Core.Automation;

public static class LabelGenerator
{
    private static readonly string[] PredefinedLabels = new[]
    {
        "DJ","DK","DL","SJ","SK","SL","AJ","AK","AL",
        "JD","JK","JL","KD","KJ","KL","LD","LK","LJ",
        "DS","DA","DH","SD","SA","SH","AD","AS","AH",
        "JH","JA","JS","KH","KA","KS","LH","LA","LS",
        "DR","DE","DT","SR","SE","ST","AR","AE","AT",
        "RD","RS","RA","RJ","RK","RL","ED","ES","EA","EJ","EK","EL",
        "TD","TS","TA","TJ","TK","TL","GD","GS","GJ","GK",
    };

    private const string Chars = "ASDFGHJKLQWERTYUIOPZXCVBNM";
    private static int _labelIndex = 0;

    public static void Reset() => _labelIndex = 0;

    public static string Next()
    {
        _labelIndex++;
        if (_labelIndex <= PredefinedLabels.Length)
            return PredefinedLabels[_labelIndex - 1];

        var index = _labelIndex - PredefinedLabels.Length;
        var first = (index - 1) / Chars.Length;
        var second = (index - 1) % Chars.Length;

        if (first < Chars.Length)
            return $"{Chars[first]}{Chars[second]}";

        return $"Z{index}";
    }
}
