using System.Collections;
using System.IO;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode hotkey = KeyCode.F12;
    [Tooltip("大於 1 會超取樣放大輸出解析度")]
    [SerializeField] private int supersize = 1;
    [SerializeField] private string filePrefix = "screenshot_";

    void Update()
    {
        if (Input.GetKeyDown(hotkey))
            StartCoroutine(CaptureRoutine());
    }

    /// <summary>可給 UI Button 直接綁定。</summary>
    public void CaptureNow()
    {
        StartCoroutine(CaptureRoutine());
    }

    /// <summary>需要更高解析度時呼叫。</summary>
    public void CaptureNow(int superSizeOverride)
    {
        supersize = Mathf.Max(1, superSizeOverride);
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        // 等待本幀完成，確保包含 UI
        yield return new WaitForEndOfFrame();

        // 擷取螢幕為 Texture（可用 supersize 提高輸出尺寸）
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture(Mathf.Max(1, supersize));

        // 轉 PNG 並寫檔
        byte[] png = tex.EncodeToPNG();
        string dir = Application.persistentDataPath;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string filename = $"{filePrefix}{System.DateTime.Now:yyyyMMdd_HHmmssfff}.png";
        string path = Path.Combine(dir, filename);
        File.WriteAllBytes(path, png);

        Debug.Log($"[ScreenshotPNG] Saved: {path}");

        // 釋放暫存
        Destroy(tex);
    }

    public static string GetSaveDirectory() => Application.persistentDataPath;
}
