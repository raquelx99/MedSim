using System;

public static class InstrumentEvents
{
    public static event Action<string> InstrumentUsed;

    public static void RaiseInstrumentUsed(string objectID)
    {
        InstrumentUsed?.Invoke(objectID);
    }

    public static void ClearListeners()
    {
        InstrumentUsed = null;
    }
}
