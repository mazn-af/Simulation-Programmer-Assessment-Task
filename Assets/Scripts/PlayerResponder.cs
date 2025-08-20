using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class PlayerResponder : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -19.62f; 
    [SerializeField] private float groundStick = -2f;

    [Header("Mouse Look")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSensitivity = 120f; 
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Tools")]
    [SerializeField] private MonoBehaviour[] toolBehaviours;
    private IUsableTool[] tools;
    private int currentToolIndex = 0;

    private CharacterController controller;
    private float verticalVelocity;
    private float yaw;   
    private float pitch;
    public System.Action<int> OnToolChanged;        
    public int CurrentToolIndex => currentToolIndex;
    public int ToolCount => tools?.Length ?? 0;     


    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        tools = new IUsableTool[toolBehaviours.Length];
        for (int i = 0; i < toolBehaviours.Length; i++)
        {
            tools[i] = toolBehaviours[i] as IUsableTool;
            if (toolBehaviours[i] != null && tools[i] == null)
                Debug.LogWarning($"Tool on index {i} does not implement IUsableTool.");
        }
        ActivateTool(currentToolIndex);
    }

    void Update()
    {
        HandleLook();
        HandleMove();
        HandleJumpAndGravity();
        HandleTools();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (playerCamera)
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && z > 0.1f;
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = (transform.right * x + transform.forward * z) * speed;
        move.y = 0f; 

        Vector3 velocity = new Vector3(move.x, verticalVelocity, move.z);
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleJumpAndGravity()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = groundStick;

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void HandleTools()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ActivateTool(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ActivateTool(1);

        bool usingTool = Input.GetMouseButton(0);
        var tool = GetCurrentTool();
        if (tool != null)
        {
            tool.Use(usingTool);

            if (Input.GetKeyDown(KeyCode.E))
                tool.TriggerOnce();
        }
    }

    private IUsableTool GetCurrentTool()
    {
        if (tools == null || tools.Length == 0) return null;
        if (currentToolIndex < 0 || currentToolIndex >= tools.Length) return null;
        return tools[currentToolIndex];
    }

    private void ActivateTool(int index)
    {
        if (tools == null || tools.Length == 0) return;

        for (int i = 0; i < tools.Length; i++)
        {
            if (toolBehaviours[i] != null)
                toolBehaviours[i].gameObject.SetActive(i == index);
        }
        currentToolIndex = Mathf.Clamp(index, 0, tools.Length - 1);
        OnToolChanged?.Invoke(currentToolIndex);
    }

    public void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}


public interface IUsableTool
{
    void Use(bool isUsing);

    void TriggerOnce();
}
