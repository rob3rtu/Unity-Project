using System;
using TMPro;
using UnityEngine;

public class SpeedIndicator : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _plane;

    [SerializeField]
    private RectTransform _crosshair;

    [SerializeField]
    private TextMeshProUGUI _speedText;

    [SerializeField]
    private Color _colorFast;

    [SerializeField]
    private Color _colorMedium;

    [SerializeField]
    private Color _colorSlow;

    [SerializeField]
    private float _xOffset;

    void Update()
    {
        _speedText.transform.position = _crosshair.position + Vector3.left * _xOffset;

        float speed = _plane.velocity.magnitude;
        if (speed > 37)
        {
            _speedText.color = _colorFast;
        }
        else if (speed < 24)
        {
            _speedText.color = _colorSlow;
        }
        else
        {
            _speedText.color = _colorMedium;
        }
        _speedText.text = Math.Floor(_plane.velocity.magnitude).ToString();
    }
}

