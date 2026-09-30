using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EndingSequence : MonoBehaviour
{
    [Header("Target")]
    public Transform tree;                  // the apple tree root

    [Header("Player")]
    public GameObject player;

    [Header("HUD")]
    public GameObject HUD;

    [Header("Scene change")]
    public string mainMenuScene = "MainMenu";   // must match your scene name exactly
    public float fadeInSeconds = 2f;


    [Header("Camera move")]
    public float riseSeconds = 12f;
    public float cameraDistance = 9f;
    public float startHeight = 0.4f;     // just above the roots
    public float endHeight = 7.6f;     // clear of the canopy at 6.13
    public float lookStart = 1.0f;     // looking up at the trunk
    public float lookEnd = 5.4f;     // ending on the crown

    [Header("Text and fade")]
    public string message = "Thanks for playing";
    public float textFadeIn = 2.5f;
    public float textAppearsAt = 0.45f;     // fraction through the rise
    public float holdSeconds = 2.5f;
    public float fadeOutSeconds = 3f;

    private bool started;

    public void Play()
    {

        if (started) return;
        started = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (tree == null) { Debug.LogError("EndingSequence: no tree assigned", this); yield break; }

        // hand over from gameplay cameras
        foreach (Camera c in Camera.allCameras) c.gameObject.SetActive(false);
        if (player != null) player.SetActive(false);

        if (HUD != null) HUD.SetActive(false);

        GameObject camGO = new GameObject("EndingCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 50f;
        cam.clearFlags = CameraClearFlags.Skybox;

        // UI
        GameObject canvasGO = new GameObject("EndingCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        TextMeshProUGUI label = new GameObject("Message").AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(canvasGO.transform, false);
        label.text = message;
        label.fontSize = 84;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 1f, 0f);
        RectTransform lr = label.rectTransform;
        lr.anchorMin = new Vector2(0.5f, 0.5f);
        lr.anchorMax = new Vector2(0.5f, 0.5f);
        lr.sizeDelta = new Vector2(1400, 300);
        lr.anchoredPosition = new Vector2(0, -320);

        Image fade = new GameObject("Fade").AddComponent<Image>();
        fade.transform.SetParent(canvasGO.transform, false);
        fade.color = new Color(0f, 0f, 0f, 0f);
        fade.raycastTarget = false;
        RectTransform fr = fade.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
        fade.transform.SetAsLastSibling();   // must sit over the text

        // a fixed viewing direction, so the rise reads as vertical
        Vector3 basePos = tree.position;
        Vector3 back = -tree.forward;
        if (back.sqrMagnitude < 0.001f) back = Vector3.back;
        back.y = 0f; back.Normalize();

        float t = 0f;
        while (t < riseSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / riseSeconds));

            camGO.transform.position = basePos + back * cameraDistance
                                     + Vector3.up * Mathf.Lerp(startHeight, endHeight, k);
            Vector3 look = basePos + Vector3.up * Mathf.Lerp(lookStart, lookEnd, k);
            camGO.transform.rotation = Quaternion.LookRotation(look - camGO.transform.position);

            // text eases in partway up
            float tk = Mathf.Clamp01((t - riseSeconds * textAppearsAt) / textFadeIn);
            label.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, tk));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(holdSeconds);

        t = 0f;
        while (t < fadeOutSeconds)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / fadeOutSeconds));
            yield return null;
        }
        fade.color = Color.black;

        // keep the black overlay and this coroutine alive across the scene load
        DontDestroyOnLoad(canvasGO);
        transform.SetParent(null);          // DontDestroyOnLoad only works on root objects
        DontDestroyOnLoad(gameObject);

        // reset anything the game changed
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        AsyncOperation load = SceneManager.LoadSceneAsync(mainMenuScene);
        while (!load.isDone) yield return null;
        yield return null;                  // let the menu scene render one frame under the black

        // fade from black to normal
        t = 0f;
        while (t < fadeInSeconds)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / fadeInSeconds));
            yield return null;
        }

        Destroy(canvasGO);
        Destroy(gameObject);
    }


}