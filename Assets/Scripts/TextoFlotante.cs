using UnityEngine;
using TMPro;
using Fusion;

public class TextoFlotante : NetworkBehaviour
{
    private TextMeshPro texto;

    public override void Spawned()
    {
        texto = GetComponentInChildren<TextMeshPro>();

        // Mostrar texto solo si es el jugador local
        if (HasInputAuthority)
        {
            texto.text = "Este es tu carro";
        }
        else
        {
            texto.text = "";
        }
    }

    void LateUpdate()
    {
        if (texto != null && Camera.main != null)
        {
            texto.transform.rotation = Quaternion.LookRotation(texto.transform.position - Camera.main.transform.position);
        }
    }
}
