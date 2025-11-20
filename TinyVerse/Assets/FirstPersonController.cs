using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [System.Serializable]
    public class MovementSettings
    {
        [Header("Movimiento")]
        public float walkSpeed = 2.5f;         // Velocidad andando hacia adelante
        public float runSpeed = 5f;           // Velocidad corriendo hacia adelante
        public float backwardSpeed = 3f;      // Velocidad hacia atrás
        public float strafeSpeed = 3.5f;      // Velocidad lateral
        [Range(0f, 1f)]
        public float movementSmoothTime = 0.1f;

        [Header("Salto / Gravedad")]
        public float jumpForce = 7f;
        public float gravity = 20f;
    }

    [System.Serializable]
    public class AnimationSettings
    {
        [Header("Animator")]
        public Animator animator;
        public string horizontalParam = "horizontal";
        public string verticalParam = "vertical";
        [Range(0f, 20f)]
        public float animatorLerpSpeed = 10f; // Qué rápido se interpolan los valores
    }

    public MovementSettings movement = new MovementSettings();
    public AnimationSettings animSettings = new AnimationSettings();

    private CharacterController controller;

    // Movimiento
    private Vector3 currentHorizontalVelocity = Vector3.zero;
    private Vector3 horizontalVelocitySmoothRef = Vector3.zero;
    private float verticalVelocity = 0f;

    // Valores actuales de animación
    private float animHorizontal = 0f;
    private float animVertical = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animSettings.animator == null)
        {
            // Intentamos pillar un Animator en este objeto o en hijos
            animSettings.animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        HandleMovement();
        UpdateAnimatorParameters();
    }

    private void HandleMovement()
    {
        // Input crudo (sin suavizado) de WASD
        float inputX = Input.GetAxisRaw("Horizontal"); // A (-1) / D (+1)
        float inputZ = Input.GetAxisRaw("Vertical");   // S (-1) / W (+1)

        Vector3 inputDir = new Vector3(inputX, 0f, inputZ).normalized;

        // Decidir velocidad según la dirección y si se está corriendo
        bool isMovingForward = inputZ > 0f;
        bool isMovingBackward = inputZ < 0f;
        bool isMovingSideways = inputZ == 0f && inputX != 0f;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isMovingForward;

        float targetSpeed = 0f;

        if (inputDir.sqrMagnitude > 0f)
        {
            if (isMovingForward)
            {
                targetSpeed = isSprinting ? movement.runSpeed : movement.walkSpeed;
            }
            else if (isMovingBackward)
            {
                targetSpeed = movement.backwardSpeed;
            }
            else if (isMovingSideways)
            {
                targetSpeed = movement.strafeSpeed;
            }
        }

        // Transformar input local (WASD) en dirección global según la rotación del player
        Vector3 desiredMove = transform.TransformDirection(inputDir) * targetSpeed;

        // Suavizar la velocidad horizontal
        currentHorizontalVelocity = Vector3.SmoothDamp(
            currentHorizontalVelocity,
            desiredMove,
            ref horizontalVelocitySmoothRef,
            movement.movementSmoothTime
        );

        // Gravedad y salto
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f; // pegar al suelo

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = movement.jumpForce;
            }
        }
        else
        {
            verticalVelocity -= movement.gravity * Time.deltaTime;
        }

        Vector3 finalVelocity = currentHorizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }

    private void UpdateAnimatorParameters()
    {
        if (animSettings.animator == null) return;

        float inputX = Input.GetAxisRaw("Horizontal"); // A (-1) / D (+1)
        float inputZ = Input.GetAxisRaw("Vertical");   // S (-1) / W (+1)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputZ > 0f;

        float targetHorizontal = 0f;
        float targetVertical = 0f;

        // HORIZONTAL (frente / atrás)
        if (inputZ > 0f)
        {
            // Adelante
            targetHorizontal = isSprinting ? 2f : 1f;
        }
        else if (inputZ < 0f)
        {
            // Atrás
            targetHorizontal = -1.5f;
        }
        else
        {
            targetHorizontal = 0f;
        }

        // VERTICAL (izquierda / derecha)
        if (inputX < 0f)
        {
            // Izquierda
            targetVertical = 1f;
        }
        else if (inputX > 0f)
        {
            // Derecha
            targetVertical = -1f;
        }
        else
        {
            targetVertical = 0f;
        }

        // Suavizar transición de valores para que el blendtree vaya fluido
        animHorizontal = Mathf.MoveTowards(
            animHorizontal,
            targetHorizontal,
            animSettings.animatorLerpSpeed * Time.deltaTime
        );

        animVertical = Mathf.MoveTowards(
            animVertical,
            targetVertical,
            animSettings.animatorLerpSpeed * Time.deltaTime
        );

        animSettings.animator.SetFloat(animSettings.horizontalParam, animHorizontal);
        animSettings.animator.SetFloat(animSettings.verticalParam, animVertical);
    }
}
