using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace GlobalDeformationDemo
{
    public sealed class DeformationDemoRecorder : MonoBehaviour
    {
        private string outputDirectory;
        private int frameRate;
        private int frameCount;
        private bool enabledForCapture;

        private void Awake()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            enabledForCapture = HasArgument(arguments, "--capture-demo");
            if (!enabledForCapture)
            {
                enabled = false;
                return;
            }

            outputDirectory = ReadArgument(arguments, "--capture-dir", Path.Combine(Application.dataPath, "..", "CaptureFrames"));
            frameRate = ReadInteger(arguments, "--capture-fps", 30);
            frameCount = ReadInteger(arguments, "--capture-frames", 420);
            Directory.CreateDirectory(outputDirectory);
            Screen.SetResolution(1280, 720, false);
            Time.captureFramerate = frameRate;
            Application.runInBackground = true;
        }

        private IEnumerator Start()
        {
            if (!enabledForCapture) yield break;
            yield return null;
            GlobalDeformationDemoApp app = FindObjectOfType<GlobalDeformationDemoApp>();
            app.PlayAutoDemo();
            yield return CaptureFrames();
        }

        private IEnumerator CaptureFrames()
        {
            Camera cameraToCapture = Camera.main;
            if (cameraToCapture == null)
            {
                Application.Quit(3);
                yield break;
            }

            RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            target.Create();
            Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            for (int index = 0; index < frameCount; index++)
            {
                yield return new WaitForEndOfFrame();
                Canvas.ForceUpdateCanvases();
                RenderTexture previousTarget = cameraToCapture.targetTexture;
                RenderTexture previousActive = RenderTexture.active;
                cameraToCapture.targetTexture = target;
                cameraToCapture.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0, false);
                texture.Apply(false, false);
                File.WriteAllBytes(Path.Combine(outputDirectory, $"frame_{index:D5}.png"), texture.EncodeToPNG());
                cameraToCapture.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
            }

            Time.captureFramerate = 0;
            Destroy(texture);
            target.Release();
            Destroy(target);
            File.WriteAllText(
                Path.Combine(outputDirectory, "capture_complete.txt"),
                $"frames={frameCount.ToString(CultureInfo.InvariantCulture)}\nfps={frameRate.ToString(CultureInfo.InvariantCulture)}\n");
            yield return new WaitForSecondsRealtime(0.25f);
            Application.Quit(0);
        }

        private static bool HasArgument(string[] arguments, string key)
        {
            foreach (string argument in arguments)
            {
                if (string.Equals(argument, key, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string ReadArgument(string[] arguments, string key, string fallback)
        {
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], key, StringComparison.OrdinalIgnoreCase)) return arguments[index + 1];
            }
            return fallback;
        }

        private static int ReadInteger(string[] arguments, string key, int fallback)
        {
            return int.TryParse(ReadArgument(arguments, key, fallback.ToString(CultureInfo.InvariantCulture)), out int value)
                ? Mathf.Max(1, value)
                : fallback;
        }
    }
}
