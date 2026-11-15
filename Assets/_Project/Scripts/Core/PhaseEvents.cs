using System;

public static class PhaseEvents
{
    public static event Action StepFinished;

    public static void RaiseStepFinished()
    {
        StepFinished?.Invoke();
    }

    public static void ClearListeners()
    {
        StepFinished = null;
    }
}
