using Photon.Pun;
using UnityEngine;

public class PlayerColor : MonoBehaviourPun
{
    [SerializeField]
    private Renderer playerRenderer;

    private void Start()
    {
        if (playerRenderer == null)
        {
            playerRenderer = GetComponent<Renderer>();
        }

        if (playerRenderer == null)
        {
            return;
        }

        if (photonView.IsMine)
        {
            playerRenderer.material.color = Color.blue;
        }
        else
        {
            playerRenderer.material.color = Color.red;
        }
    }
}
