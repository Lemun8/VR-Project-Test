using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controls the camera rotation using the device gyroscope or touch/mouse drag.
/// Press the toggle button to switch between the two modes at any time.
/// The first switch to gyroscope satisfies the iOS user gesture requirement.
/// Attach this to the Main Camera GameObject.
/// </summary>
public class GyroscopeCamera : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Smoothing speed for rotation interpolation.")]
    [SerializeField] private float _smoothing = 10f;

    [Tooltip("Touch/mouse drag sensitivity.")]
    [SerializeField] private float _dragSensitivity = 0.15f;

    [Header("UI")]
    [Tooltip("Text on the toggle button — updated to reflect the active mode.")]
    [SerializeField] private Text _buttonLabel;

    [Tooltip("Text label showing the current status.")]
    [SerializeField] private Text _statusText;

    private const string LabelGyroMode = "Switch to Drag";
    private const string LabelDragMode = "Switch to Gyroscope";

    private bool _gyroActive;
    private Quaternion _targetRotation;

    // Touch / mouse drag state
    private Vector2 _lastPointerPos;
    private bool _isDragging;
    private Vector2 _dragRotation;

    private void Start()
    {
        _targetRotation = transform.rotation;
        RefreshUI();
    }

    private void Update()
    {
        if (_gyroActive)
            UpdateFromGyro();
        else
            UpdateDrag();

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _targetRotation,
            Time.deltaTime * _smoothing);
    }

    // -------------------------------------------------------------------------
    // Gyroscope
    // -------------------------------------------------------------------------

    private void UpdateFromGyro()
    {
        Quaternion att  = Input.gyro.attitude;
        _targetRotation = new Quaternion(att.x, att.y, -att.z, -att.w)
                          * Quaternion.Euler(90f, 0f, 0f);
    }

    // -------------------------------------------------------------------------
    // Touch / mouse drag
    // -------------------------------------------------------------------------

    private void UpdateDrag()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return;

                _lastPointerPos = touch.position;
                _isDragging     = true;
            }

            if (_isDragging && touch.phase == TouchPhase.Moved)
            {
                ApplyDragDelta(touch.position - _lastPointerPos);
                _lastPointerPos = touch.position;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                _isDragging = false;

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            _lastPointerPos = Input.mousePosition;
            _isDragging     = true;
        }

        if (_isDragging && Input.GetMouseButton(0))
        {
            ApplyDragDelta((Vector2)Input.mousePosition - _lastPointerPos);
            _lastPointerPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            _isDragging = false;
    }

    private void ApplyDragDelta(Vector2 delta)
    {
        _dragRotation.x -= delta.y * _dragSensitivity;
        _dragRotation.y += delta.x * _dragSensitivity;
        _dragRotation.x  = Mathf.Clamp(_dragRotation.x, -85f, 85f);
        _targetRotation  = Quaternion.Euler(_dragRotation.x, _dragRotation.y, 0f);
    }

    // -------------------------------------------------------------------------
    // Toggle button callback & UI
    // -------------------------------------------------------------------------

    /// <summary>
    /// Toggles between gyroscope and drag mode.
    /// Assign this to the toggle button's OnClick event.
    /// </summary>
    public void OnToggleModePressed()
    {
        _gyroActive = !_gyroActive;

        if (_gyroActive)
            Input.gyro.enabled = true;

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_buttonLabel != null)
            _buttonLabel.text = _gyroActive ? LabelGyroMode : LabelDragMode;

        if (_statusText != null)
            _statusText.text = _gyroActive
                ? "Gyroscope active — tilt your phone to look around"
                : "Drag mode — drag to look around";
    }
}
