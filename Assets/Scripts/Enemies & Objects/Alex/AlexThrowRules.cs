using System;

public enum AlexThrowKind
{
    Dollar = 0,
    PumpkinA = 1,
    PumpkinF = 2,
    PumpkinG = 3
}

public static class AlexThrowRules
{
    public static bool IsSuccess(AlexThrowKind kind, bool isStrike,
        double timingError, double beatWindow)
    {
        return kind == AlexThrowKind.Dollar
            ? !isStrike
            : !isStrike || Math.Abs(timingError) <= beatWindow;
    }
}
