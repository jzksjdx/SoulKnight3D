using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickLeft : MonoBehaviour
{
    private const int NoPointer = int.MinValue;

    public GameObject stick;
    public GameObject backgroundImage;
    public GameObject leftAreaForStickyJoystick;
    [Range(1, 10)] public int stickMovementThreshold = 4;
    public bool sticky;
    public bool moveJoystickBaseOnDrag;

    public static float positionX;
    public static float positionY;
    public static float angle;
    public static Vector2 Movement => new Vector2(positionX, positionY);

    private RectTransform _stickRectTransform;
    private float _stickMovement;
    private int _activePointerId = NoPointer;

    private void Start()
    {
        Init();
    }

    private void OnDisable()
    {
        ResetInput();
    }

    public void Init()
    {
        _stickMovement = Mathf.Max(1f, stickMovementThreshold * (Screen.width + Screen.height) / 100f);
        _stickRectTransform = stick.GetComponent<RectTransform>();
        backgroundImage.SetActive(true);
        stick.SetActive(true);
        leftAreaForStickyJoystick.SetActive(sticky);
        ResetInput();
    }

    public void OnStickyPointerDown(BaseEventData data)
    {
        if (!TryGetPointer(data, out PointerEventData pointerData) || !TryClaimPointer(pointerData.pointerId))
        {
            return;
        }

        if (sticky)
        {
            backgroundImage.transform.position = pointerData.position;
        }

        UpdateMovement(pointerData.position);
    }

    public void Move(BaseEventData data)
    {
        if (!TryGetPointer(data, out PointerEventData pointerData) ||
            !TryClaimPointer(pointerData.pointerId))
        {
            return;
        }

        UpdateMovement(pointerData.position);
    }

    public void ReturnToNormalPosition(BaseEventData data)
    {
        if (TryGetOwnedPointer(data, out _))
        {
            ResetInput();
        }
    }

    public void OnStickyPointerUp(BaseEventData data)
    {
        ReturnToNormalPosition(data);
    }

    private void UpdateMovement(Vector2 pointerPosition)
    {
        Vector2 basePosition = backgroundImage.transform.position;
        Vector2 offset = Vector2.ClampMagnitude(pointerPosition - basePosition, _stickMovement);
        Vector2 movement = offset / _stickMovement;
        positionX = movement.x;
        positionY = movement.y;
        angle = Mathf.Atan2(-offset.x, -offset.y);
        stick.transform.position = basePosition + offset;

        if (moveJoystickBaseOnDrag)
        {
            backgroundImage.transform.position = pointerPosition - offset;
        }
    }

    private bool TryClaimPointer(int pointerId)
    {
        if (_activePointerId != NoPointer && _activePointerId != pointerId)
        {
            return false;
        }

        _activePointerId = pointerId;
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

    private void ResetInput()
    {
        _activePointerId = NoPointer;
        positionX = 0f;
        positionY = 0f;
        angle = 0f;
        if (_stickRectTransform != null)
        {
            _stickRectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
