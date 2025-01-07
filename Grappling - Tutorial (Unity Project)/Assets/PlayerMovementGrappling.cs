using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovementGrappling : MonoBehaviour
{
    [Header("Movimento")]
    private float moveSpeed; // Velocidade atual do jogador
    public float walkSpeed; // Velocidade de caminhada
    public float sprintSpeed; // Velocidade de corrida
    public float swingSpeed; // Velocidade ao balançar (swing)

    public float groundDrag; // Arrasto no chão para controlar a desaceleração

    [Header("Pulo")]
    public float jumpForce; // Força do pulo
    public float jumpCooldown; // Tempo de espera entre pulos
    public float airMultiplier; // Multiplicador de movimento no ar
    bool readyToJump; // Verifica se o jogador pode pular

    [Header("Agachar")]
    public float crouchSpeed; // Velocidade ao agachar
    public float crouchYScale; // Escala em Y ao agachar
    private float startYScale; // Escala inicial em Y

    [Header("Teclas")]
    public KeyCode jumpKey = KeyCode.Space; // Tecla para pular
    public KeyCode sprintKey = KeyCode.LeftShift; // Tecla para correr
    public KeyCode crouchKey = KeyCode.LeftControl; // Tecla para agachar

    [Header("Verificação do Chão")]
    public float playerHeight; // Altura do jogador
    public LayerMask whatIsGround; // Camadas que representam o chão
    bool grounded; // Verifica se o jogador está no chão

    [Header("Manuseio de Inclinações")]
    public float maxSlopeAngle; // Ângulo máximo de inclinação que o jogador pode subir
    private RaycastHit slopeHit; // Informações do raycast para detectar inclinações
    private bool exitingSlope; // Verifica se o jogador está saindo de uma inclinação

    [Header("Efeitos de Câmera")]
    public PlayerCam cam; // Referência à câmera do jogador
    public float grappleFov = 95f; // Campo de visão ao usar o gancho

    public Transform orientation; // Orientação do jogador

    float horizontalInput; // Entrada horizontal (teclas A/D ou setas)
    float verticalInput; // Entrada vertical (teclas W/S ou setas)

    Vector3 moveDirection; // Direção do movimento

    Rigidbody rb; // Componente Rigidbody do jogador

    public MovementState state; // Estado atual do movimento
    public enum MovementState
    {
        freeze, // Congelado (imóvel)
        grappling, // Usando o gancho
        swinging, // Balançando (swing)
        walking, // Caminhando
        sprinting, // Correndo
        crouching, // Agachado
        air // No ar
    }

    public bool freeze; // Verifica se o jogador está congelado
    public bool activeGrapple; // Verifica se o jogador está usando o gancho
    public bool swinging; // Verifica se o jogador está balançando

    private void Start()
    {
        rb = GetComponent<Rigidbody>(); // Obtém o Rigidbody
        rb.freezeRotation = true; // Impede a rotação do Rigidbody

        readyToJump = true; // Permite o pulo inicialmente

        startYScale = transform.localScale.y; // Armazena a escala inicial em Y
    }

    private void Update()
    {
        // Verifica se o jogador está no chão
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput(); // Processa as entradas do jogador
        SpeedControl(); // Controla a velocidade do jogador
        StateHandler(); // Gerencia o estado do movimento

        // Aplica o arrasto no chão, exceto durante o gancho
        if (grounded && !activeGrapple)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        TextStuff(); // Atualiza textos de debug
    }

    private void FixedUpdate()
    {
        MovePlayer(); // Move o jogador
    }

    private void MyInput()
    {
        // Obtém as entradas do jogador
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Verifica se o jogador pode pular
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump(); // Executa o pulo

            Invoke(nameof(ResetJump), jumpCooldown); // Reinicia o pulo após o cooldown
        }

        // Inicia o agachamento
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse); // Aplica uma força para baixo
        }

        // Para o agachamento
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        // Modo - Congelado
        if (freeze)
        {
            state = MovementState.freeze;
            moveSpeed = 0;
            rb.velocity = Vector3.zero;
        }

        // Modo - Gancho
        else if (activeGrapple)
        {
            state = MovementState.grappling;
            moveSpeed = sprintSpeed;
        }

        // Modo - Balançando
        else if (swinging)
        {
            state = MovementState.swinging;
            moveSpeed = swingSpeed;
        }

        // Modo - Agachado
        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        // Modo - Correndo
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        // Modo - Caminhando
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        // Modo - No Ar
        else
        {
            state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        if (activeGrapple) return; // Não move o jogador durante o gancho
        if (swinging) return; // Não move o jogador durante o balanço

        // Calcula a direção do movimento com base na orientação
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // Movimento em inclinações
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force); // Aplica força para baixo
        }

        // Movimento no chão
        else if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // Movimento no ar
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        // Desativa a gravidade em inclinações
        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (activeGrapple) return; // Não controla a velocidade durante o gancho

        // Limita a velocidade em inclinações
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        // Limita a velocidade no chão ou no ar
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // Limita a velocidade se necessário
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        // Reseta a velocidade vertical
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse); // Aplica a força do pulo
    }

    private void ResetJump()
    {
        readyToJump = true; // Permite o próximo pulo
        exitingSlope = false; // Reseta o estado de saída da inclinação
    }

    private bool enableMovementOnNextTouch;
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f); // Define a velocidade após um pequeno delay

        Invoke(nameof(ResetRestrictions), 3f); // Reseta as restrições após 3 segundos
    }

    private Vector3 velocityToSet;
    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.velocity = velocityToSet; // Aplica a velocidade calculada

        cam.DoFov(grappleFov); // Ajusta o campo de visão da câmera
    }

    public void ResetRestrictions()
    {
        activeGrapple = false; // Desativa o gancho
        cam.DoFov(85f); // Reseta o campo de visão da câmera
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions(); // Reseta as restrições ao tocar no chão

            GetComponent<Grappling>().StopGrapple(); // Para o gancho
        }
    }

    private bool OnSlope()
    {
        // Verifica se o jogador está em uma inclinação
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        // Calcula a direção do movimento na inclinação
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        // Calcula a velocidade necessária para pular até uma posição
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) 
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }

    #region Texto & Debug

    public TextMeshProUGUI text_speed; // Texto para exibir a velocidade
    public TextMeshProUGUI text_mode; // Texto para exibir o modo de movimento
    private void TextStuff()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Exibe a velocidade atual e a velocidade máxima
        if (OnSlope())
            text_speed.SetText("Speed: " + Round(rb.velocity.magnitude, 1) + " / " + Round(moveSpeed, 1));

        else
            text_speed.SetText("Speed: " + Round(flatVel.magnitude, 1) + " / " + Round(moveSpeed, 1));

        text_mode.SetText(state.ToString()); // Exibe o estado atual
    }

    public static float Round(float value, int digits)
    {
        // Arredonda um valor para um número específico de casas decimais
        float mult = Mathf.Pow(10.0f, (float)digits);
        return Mathf.Round(value * mult) / mult;
    }

    #endregion
}
