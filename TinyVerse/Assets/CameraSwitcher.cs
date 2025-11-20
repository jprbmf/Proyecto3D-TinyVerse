using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;          // Referencia al jugador (root)
    public Camera mainCamera;         // Cámara principal

    [Header("Offsets")]
    public Vector3 firstPersonOffset = new Vector3(0f, 0.8f, 0f); // Relativo al player
    public Vector3 thirdPersonOffset = new Vector3(0f, 3f, -5f);  // Relativo a la rotación del player
    public Vector3 topDownOffset = new Vector3(0f, 10f, 0f);      // Encima del jugador

    [Header("Ratón")]
    public float mouseSensitivityX = 200f;
    public float mouseSensitivityY = 200f;
    [Range(0f, 0.5f)] public float rotationSmoothTime = 0.05f;
    public float minPitch = -60f;
    public float maxPitch = 80f;
    public bool lockCursor = true;

    private enum CameraView { FirstPerson, ThirdPerson, TopDown }
    [SerializeField] private CameraView currentView = CameraView.ThirdPerson;

    private float yaw;          // Rotación horizontal
    private float pitch;        // Rotación vertical
    private float yawSmooth;
    private float pitchSmooth;
    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;

    void Start()
    {
        // Buscar automáticamente el jugador si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Asegurar que haya una cámara asignada
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (player == null || mainCamera == null)
        {
            Debug.LogError("CameraSwitcher: falta asignar player o mainCamera.");
            enabled = false;
            return;
        }

        // Inicializar yaw/pitch según la rotación actual del jugador
        Vector3 euler = player.eulerAngles;
        yaw = yawSmooth = euler.y;
        pitch = pitchSmooth = 0f;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Empezar en tercera persona por defecto
        SetThirdPersonView(true);
    }

    void Update()
    {
        HandleViewSwitchInput();

        // Ratón solo afecta en 1ª y 3ª persona
        if (currentView == CameraView.FirstPerson || currentView == CameraView.ThirdPerson)
        {
            HandleMouseLook();
        }

        FollowPlayer();
        HandleCursorToggle();
    }

    private void HandleViewSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SetFirstPersonView(false);
        }
        else if (Input.GetKeyDown(KeyCode.Y))
        {
            SetThirdPersonView(false);
        }
        else if (Input.GetKeyDown(KeyCode.U))
        {
            SetTopDownView(false);
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        yawSmooth = Mathf.SmoothDampAngle(yawSmooth, yaw, ref yawSmoothVelocity, rotationSmoothTime);
        pitchSmooth = Mathf.SmoothDampAngle(pitchSmooth, pitch, ref pitchSmoothVelocity, rotationSmoothTime);

        // Rotar el cuerpo del jugador en horizontal
        player.rotation = Quaternion.Euler(0f, yawSmooth, 0f);
    }

    private void FollowPlayer()
    {
        switch (currentView)
        {
            case CameraView.FirstPerson:
                {
                    // Offset relativo al jugador
                    Vector3 targetPos = player.position + player.TransformVector(firstPersonOffset);

                    mainCamera.transform.position = Vector3.Lerp(
                        mainCamera.transform.position,
                        targetPos,
                        Time.deltaTime * 20f
                    );

                    // Mirar en la dirección de yaw/pitch
                    mainCamera.transform.rotation = Quaternion.Euler(pitchSmooth, yawSmooth, 0f);
                    break;
                }

            case CameraView.ThirdPerson:
                {
                    // Offset relativo a la rotación del jugador
                    Vector3 rotatedOffset = Quaternion.Euler(0f, yawSmooth, 0f) * thirdPersonOffset;
                    Vector3 desiredPos = player.position + rotatedOffset;

                    mainCamera.transform.position = Vector3.Lerp(
                        mainCamera.transform.position,
                        desiredPos,
                        Time.deltaTime * 10f
                    );

                    // Mirar al jugador (un poco más alto para centrar)
                    Vector3 lookTarget = player.position + Vector3.up * firstPersonOffset.y;
                    mainCamera.transform.LookAt(lookTarget);
                    break;
                }

            case CameraView.TopDown:
                {
                    Vector3 targetPos = player.position + topDownOffset;

                    mainCamera.transform.position = Vector3.Lerp(
                        mainCamera.transform.position,
                        targetPos,
                        Time.deltaTime * 10f
                    );

                    mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    break;
                }
        }
    }

    private void HandleCursorToggle()
    {
        if (!lockCursor) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Los bool "instant" permiten que al cambiar de vista puedas forzar un snap
    private void SetFirstPersonView(bool instant)
    {
        currentView = CameraView.FirstPerson;

        if (instant)
        {
            Vector3 targetPos = player.position + player.TransformVector(firstPersonOffset);
            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = Quaternion.Euler(pitchSmooth, yawSmooth, 0f);
        }
    }

    private void SetThirdPersonView(bool instant)
    {
        currentView = CameraView.ThirdPerson;

        if (instant)
        {
            Vector3 rotatedOffset = Quaternion.Euler(0f, yawSmooth, 0f) * thirdPersonOffset;
            Vector3 desiredPos = player.position + rotatedOffset;
            mainCamera.transform.position = desiredPos;

            Vector3 lookTarget = player.position + Vector3.up * firstPersonOffset.y;
            mainCamera.transform.LookAt(lookTarget);
        }
    }

    private void SetTopDownView(bool instant)
    {
        currentView = CameraView.TopDown;

        if (instant)
        {
            Vector3 targetPos = player.position + topDownOffset;
            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
