using UnityEngine;
using Fungus;

public class CameraMove : MonoBehaviour
{
    public Camera mainCamera;
    public Canvas canvas;
    public RectTransform position1Rect, position2Rect, position3Rect, position4Rect, 
                         position5Rect, position6Rect, position7Rect;
    
    [Tooltip("Speed of camera movement")]
    public float moveSpeed = 5f;

    private Vector3 targetPosition;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        targetPosition = mainCamera.transform.position;
    }

    void Update()
    {
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * moveSpeed);
    }

    [CommandInfo("Camera", "Move Camera", "Moves the camera to the specified position (1-7)")]
    public class MoveCameraCommand : Command
    {
        [Tooltip("Position number (1-7)")]
        [SerializeField] protected int positionNumber = 1;

        public override void OnEnter()
        {
            CameraMove cameraMove = FindObjectOfType<CameraMove>();
            if (cameraMove != null)
            {
                cameraMove.MoveCamera(positionNumber);
            }
            else
            {
                Debug.LogError("CameraMove component not found in the scene.");
            }
            
            Continue();
        }
    }

    public void MoveCamera(int position)
    {
        Debug.Log($"MoveCamera called with position: {position}");

        RectTransform targetRect = null;

        switch (position)
        {
            case 1:
                targetRect = position1Rect;
                break;
            case 2:
                targetRect = position2Rect;
                break;
            case 3:
                targetRect = position3Rect;
                break;
            case 4:
                targetRect = position4Rect;
                break;
            case 5:
                targetRect = position5Rect;
                break;
            case 6:
                targetRect = position6Rect;
                break;
            case 7:
                targetRect = position7Rect;
                break;
            default:
                Debug.LogWarning("Invalid position number: " + position);
                return;
        }

        if (targetRect != null)
        {
            Vector3 rectPosition = RectTransformToWorldPosition(targetRect);
            targetPosition = new Vector3(rectPosition.x, mainCamera.transform.position.y, mainCamera.transform.position.z);
            Debug.Log($"Setting target position to: {targetPosition}");
        }
        else
        {
            Debug.LogError($"Target RectTransform for position {position} is null.");
        }
    }

    private Vector3 RectTransformToWorldPosition(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return (corners[0] + corners[2]) / 2;
    }

    [ContextMenu("Check Setup")]
    private void CheckSetup()
    {
        Debug.Log("Checking CameraMove setup:");
        Debug.Log($"Main Camera: {(mainCamera != null ? "Set" : "Not set")}");
        Debug.Log($"Canvas: {(canvas != null ? "Set" : "Not set")}");
        Debug.Log($"Position 1: {(position1Rect != null ? "Set" : "Not set")}");
        Debug.Log($"Position 2: {(position2Rect != null ? "Set" : "Not set")}");
        Debug.Log($"Position 3: {(position3Rect != null ? "Set" : "Not set")}");
        Debug.Log($"Position 4: {(position4Rect != null ? "Set" : "Not set")}");
        Debug.Log($"Position 5: {(position5Rect != null ? "Set" : "Not set")}");
        Debug.Log($"Position 6: {(position6Rect != null ? "Set" : "Not set")}");
        Debug.Log($"Position 7: {(position7Rect != null ? "Set" : "Not set")}");
        Debug.Log($"Move Speed: {moveSpeed}");
    }
}