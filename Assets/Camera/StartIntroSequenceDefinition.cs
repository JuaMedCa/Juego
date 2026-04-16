using UnityEngine;

[CreateAssetMenu(fileName = "StartIntroSequence", menuName = "Dark Fall/Start Intro Sequence")]
public class StartIntroSequenceDefinition : ScriptableObject
{
    [SerializeField] Texture2D[] slideTextures = new Texture2D[0];
    [SerializeField] AudioClip[] narrationClips = new AudioClip[0];
    [SerializeField] string[] subtitles = new string[0];
    [SerializeField] float[] narrationVolumes = new float[0];
    [SerializeField, Min(0f)] float fadeDuration = 0.45f;
    [SerializeField, Min(0f)] float fallbackStepDuration = 3.5f;
    [SerializeField, Min(0f)] float betweenSlidesDelay = 0.15f;
    [SerializeField] Color backdropColor = Color.black;
    [SerializeField] Vector3 introCameraPosition = new Vector3(20000f, 20000f, 20000f);

    public int StepCount => Mathf.Max(GetLength(slideTextures), GetLength(narrationClips), GetLength(subtitles));
    public bool HasSteps => StepCount > 0;
    public float FadeDuration => Mathf.Max(0f, fadeDuration);
    public float FallbackStepDuration => Mathf.Max(0.1f, fallbackStepDuration);
    public float BetweenSlidesDelay => Mathf.Max(0f, betweenSlidesDelay);
    public Color BackdropColor => backdropColor;
    public Vector3 IntroCameraPosition => introCameraPosition;

    public Texture2D GetSlideTexture(int index)
    {
        return IsValidIndex(slideTextures, index) ? slideTextures[index] : null;
    }

    public AudioClip GetNarrationClip(int index)
    {
        return IsValidIndex(narrationClips, index) ? narrationClips[index] : null;
    }

    public string GetSubtitle(int index)
    {
        return IsValidIndex(subtitles, index) ? subtitles[index] : string.Empty;
    }

    public float GetNarrationVolume(int index)
    {
        if (!IsValidIndex(narrationVolumes, index))
        {
            return 1f;
        }

        return Mathf.Clamp01(narrationVolumes[index]);
    }

    static int GetLength<T>(T[] collection)
    {
        return collection == null ? 0 : collection.Length;
    }

    static bool IsValidIndex<T>(T[] collection, int index)
    {
        return collection != null && index >= 0 && index < collection.Length;
    }
}
