using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickRight : MonoBehaviour
{
    private const int NoPointer = int.MinValue;
    private const float MaximumLookDelta = 2.5f;

    public GameObject stick;
    public GameObject backgroundImage;
    [Range(1f, 3f)] public float sensitivity = 1f;
    public float rotationYMaxAngle = 45f;
    public GameObject shotButton;

    public static float positionX;
    public static float positionY;
    public static float rotX;
    public static float rotY;
    public static bool shot;
    public static bool jump;
    public static EasyEvent<bool> OnJoystickRightPressed = new EasyEvent<bool>();

    private static Vector2 s_LookDelta;

    private RectTransform _stickRectTransform;
    private float _stickMovement;
    private Vector2 _lastPointerPosition;
    private Vector2 _shotButtonStartPosition;
    private int _activePointerId = NoPointer;

    private void Start()
    {
        _shotButtonStartPosition = shotButton.transform.position;
        Init();
    }

    private void OnDisable()
    {
        ReleaseShot();
        ResetPointer();
    }

    public void Init()
    {
        _stickMovement = Mathf.Max(1f, 4f * (Screen.width + Screen.height) / 100f);
        _stickRectTransform = stick.GetComponent<RectTransform>();
        ResetPointer();
    }

    public static Vector2 ConsumeLookDelta()
    {
        Vector2 result = Vector2.ClampMagnitude(s_LookDelta, MaximumLookDelta);
        s_LookDelta = Vector2.zero;
        return result;
    }

    public void OnStartMoving(BaseEventData data)
    {
        OnStickyPointerDown(data);
    }

    public void OnStickyPointerDown(BaseEventData data)
    {
        if (!TryGetPointer(data, out PointerEventData pointerData) || !TryClaimPointer(pointerData))
        {
            return;
        }

        backgroundImage.transform.position = pointerData.position;
    }

    public void Move(BaseEventData data)
    {
        if (!TryGetOwnedPointer(data, out PointerEventData pointerData))
        {
            return;
        }

        Vector2 pointerDelta = pointerData.position - _lastPointerPosition;
        _lastPointerPosition = pointerData.position;
        pointerDelta = Vector2.ClampMagnitude(pointerDelta, _stickMovement * 0.5f);

        Vector2 normalizedDelta = pointerDelta * (5f * sensitivity / _stickMovement);
        Vector2 lookDelta = new Vector2(normalizedDelta.x, -normalizedDelta.y);
        s_LookDelta = Vector2.ClampMagnitude(s_LookDelta + lookDelta, MaximumLookDelta);
        positionX = normalizedDelta.x;
        positionY = normalizedDelta.y;
        rotX += lookDelta.x;
        rotY = Mathf.Clamp(rotY + lookDelta.y, -rotationYMaxAngle, rotationYMaxAngle);
        backgroundImage.transform.position = pointerData.position;
    }

    public void ReturnToNormalPosition(BaseEventData data)
    {
        OnStickyPointerUp(data);
    }

    public void OnStickyPointerUp(BaseEventData data)
    {
        if (TryGetOwnedPointer(data, out _))
        {
            ResetPointer();
        }
    }

    public void ShotPress(BaseEventData data)
    {
        if (!TryGetPointer(data, out PointerEventData pointerData) || !TryClaimPointer(pointerData))
        {
            return;
        }

        shotButton.transform.position = pointerData.position;
        if (shot)
        {
            return;
        }

        shot = true;
        OnJoystickRightPressed.Trigger(true);
    }

    public void ShotRelease()
    {
        ReleaseShot();
        ResetPointer();
    }

    private bool TryClaimPointer(PointerEventData pointerData)
    {
        if (_activePointerId != NoPointer && _activePointerId != pointerData.pointerId)
        {
            return false;
        }

        if (_activePointerId == NoPointer)
        {
            _activePointerId = pointerData.pointerId;
            _lastPointerPosition = pointerData.position;
            s_LookDelta = Vector2.zero;
        }

        return true;
    }

    private bool TryGetOwnedPointer(BaseEventData data, out PointerEventData pointerData)
    {
        return TryGetPointer(data, out pointerData) && pointerData.pointerId == _activePointerId;
    }

    private static bool TryGetPointer(BaseEventData data, out PointerEventData pointerData)
    {
        pointerData = data as PointerEventData;
        return pointerData != null;
    }

    private void ReleaseShot()
    {
        if (!shot)
        {
            return;
        }

        shot = false;
        if (shotButton != null)
        {
            shotButton.transform.position = _shotButtonStartPosition;
        }
        OnJoystickRightPressed.Trigger(false);
    }

    private void ResetPointer()
    {
        _activePointerId = NoPointer;
        positionX = 0f;
        positionY = 0f;
        s_LookDelta = Vector2.zero;
        if (_stickRectTransform != null)
        {
            _stickRectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
