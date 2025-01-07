using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grappling : MonoBehaviour
{
    [Header("Referências")]
    private PlayerMovementGrappling pm; // Referência ao script de movimento do jogador
    public Transform cam; // Referência à câmera do jogador
    public Transform gunTip; // Ponto de origem do gancho (ponta da arma)
    public LayerMask whatIsGrappleable; // Camadas que podem ser agarradas pelo gancho
    public LineRenderer lr; // Renderizador de linha para mostrar o cabo do gancho

    [Header("Configurações do Gancho")]
    public float maxGrappleDistance; // Distância máxima que o gancho pode alcançar
    public float grappleDelayTime; // Tempo de delay antes de executar o gancho
    public float overshootYAxis; // Altura extra para o gancho

    private Vector3 grapplePoint; // Ponto onde o gancho se conecta

    [Header("Tempo de Recarga")]
    public float grapplingCd; // Tempo de recarga do gancho
    private float grapplingCdTimer; // Temporizador para controlar o tempo de recarga

    [Header("Entrada do Jogador")]
    public KeyCode grappleKey = KeyCode.Mouse1; // Tecla para ativar o gancho

    private bool grappling; // Verifica se o jogador está usando o gancho

    private void Start()
    {
        pm = GetComponent<PlayerMovementGrappling>(); // Obtém o componente de movimento do jogador
    }

    private void Update()
    {
        // Verifica se a tecla do gancho foi pressionada
        if (Input.GetKeyDown(grappleKey)) StartGrapple();

        // Reduz o temporizador de recarga se ele for maior que zero
        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        // Atualiza a posição do cabo do gancho (comentado por enquanto)
        // if (grappling)
        //    lr.SetPosition(0, gunTip.position);
    }

    private void StartGrapple()
    {
        // Verifica se o gancho está em recarga
        if (grapplingCdTimer > 0) return;

        grappling = true; // Ativa o estado de gancho

        pm.freeze = true; // Congela o movimento do jogador

        RaycastHit hit;
        // Lança um raio para detectar se há algo agarravel na direção da câmera
        if(Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point; // Define o ponto de agarramento

            Invoke(nameof(ExecuteGrapple), grappleDelayTime); // Executa o gancho após o delay
        }
        else
        {
            // Se não houver nada agarravel, define um ponto no máximo da distância
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;

            Invoke(nameof(StopGrapple), grappleDelayTime); // Para o gancho após o delay
        }

        // Ativa o renderizador de linha e define a posição do cabo (comentado por enquanto)
        //lr.enabled = true;
        //lr.SetPosition(1, grapplePoint);
    }

    private void ExecuteGrapple()
    {
        pm.freeze = false; // Libera o movimento do jogador

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        // Calcula a posição relativa do ponto de agarramento em relação ao jogador
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        // Ajusta a altura máxima do arco se o ponto de agarramento estiver abaixo do jogador
        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        // Faz o jogador pular em direção ao ponto de agarramento
        pm.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 1f); // Para o gancho após 1 segundo
    }

    public void StopGrapple()
    {
        pm.freeze = false; // Libera o movimento do jogador

        grappling = false; // Desativa o estado de gancho

        grapplingCdTimer = grapplingCd; // Reinicia o temporizador de recarga

        // Desativa o renderizador de linha (comentado por enquanto)
        //lr.enabled = false;
    }

    // Retorna se o jogador está usando o gancho
    public bool IsGrappling()
    {
        return grappling;
    }

    // Retorna o ponto de agarramento
    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }
}
