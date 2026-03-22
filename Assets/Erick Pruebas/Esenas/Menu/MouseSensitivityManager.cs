using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class MouseSensitivityManager : MonoBehaviour
{
    public static MouseSensitivityManager instance;

    public float mouseSensitivity = 100f;
    public Slider sensitivitySlider;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        // Cargar sensibilidad guardada
        mouseSensitivity = PlayerPrefs.GetFloat("sensitivity", 100f);

        // Actualizar slider visualmente
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = mouseSensitivity;
        }
    }

    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;

        // Guardar valor
        PlayerPrefs.SetFloat("sensitivity", value);
    }
}
