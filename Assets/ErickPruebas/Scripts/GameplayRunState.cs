using UnityEngine;

public static class GameplayRunState
{
    public static bool NotesTabHintShown { get; private set; }
    public static bool FirstFuelThoughtShown { get; private set; }
    public static bool MapPickupHintShown { get; private set; }
    public static bool FirstMapOpenSeen { get; private set; }
    public static bool FirstMapCloseInsightShown { get; private set; }
    public static bool RunHintShown { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        ResetState();
    }

    public static void ResetState()
    {
        NotesTabHintShown = false;
        FirstFuelThoughtShown = false;
        MapPickupHintShown = false;
        FirstMapOpenSeen = false;
        FirstMapCloseInsightShown = false;
        RunHintShown = false;
    }

    public static bool TryConsumeNotesTabHint()
    {
        if (NotesTabHintShown)
        {
            return false;
        }

        NotesTabHintShown = true;
        return true;
    }

    public static bool TryConsumeFirstFuelThought()
    {
        if (FirstFuelThoughtShown)
        {
            return false;
        }

        FirstFuelThoughtShown = true;
        return true;
    }

    public static bool TryConsumeMapPickupHint()
    {
        if (MapPickupHintShown)
        {
            return false;
        }

        MapPickupHintShown = true;
        return true;
    }

    public static bool RegisterFirstMapOpen()
    {
        if (FirstMapOpenSeen)
        {
            return false;
        }

        FirstMapOpenSeen = true;
        return true;
    }

    public static bool TryConsumeFirstMapCloseInsight()
    {
        if (!FirstMapOpenSeen || FirstMapCloseInsightShown)
        {
            return false;
        }

        FirstMapCloseInsightShown = true;
        return true;
    }

    public static bool TryConsumeRunHint()
    {
        if (RunHintShown)
        {
            return false;
        }

        RunHintShown = true;
        return true;
    }
}
