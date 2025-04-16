using UnityEngine;
using UnityEngine.UI;

public class DeleteSlider : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;
    public Text Size;

    private void OnEnable()
    {
        slider.value = 0;
        Size.text = 0.ToString();
        slider.onValueChanged.AddListener(ChangeColor);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(ChangeColor);
    }

    private void ChangeColor(float value)
    {
        if (slider.maxValue != 0)
        {
            fillImage.color = Color.Lerp(Color.white, Color.red, value / slider.maxValue);
        }
        Size.text = ((int)value).ToString();
    }
}
