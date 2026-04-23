using UnityEngine;

public class VictorySceneInitializer : MonoBehaviour
{
    private void Start()
    {
        Camera[] allCameras = Resources.FindObjectsOfTypeAll<Camera>();

        foreach (Camera cam in allCameras)
        {
            if (cam.gameObject.scene == gameObject.scene)
            {
                cam.gameObject.SetActive(true);
                cam.enabled = true;

                if (cam.CompareTag("MainCamera") == false)
                {
                    cam.tag = "MainCamera";
                }

                Debug.Log($"[VictoryFix] Cámara {cam.name} activada y configurada.");
            }
        }
    }
}