using UnityEngine;
using UnityEngine.UI;

public class UIVignetteFlash : MonoBehaviour
{
    [SerializeField] Image vignette;
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] Color damageFlashColor = new(1f, 0f, 0f, 0.5f);
    [SerializeField] Color healFlashColor = new(0f, 1f, 0f, 0.5f);
    [SerializeField] float fadeSpeed = 3f;

    Color _target;

    void Start()
    {
        if (!playerHealth) playerHealth = FindFirstObjectByType<PlayerHealth>();
        playerHealth.OnDamaged.AddListener(OnDamaged);
        playerHealth.OnHealed.AddListener(OnHealed);
        vignette.color = new Color(0f, 0f, 0f, 0f);
        _target = vignette.color;
    }

    void OnDestroy()
    {
        if (playerHealth)
        {
            playerHealth.OnDamaged.RemoveListener(OnDamaged);
            playerHealth.OnHealed.RemoveListener(OnHealed);
        }
    }

    void OnDamaged(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        _target = damageFlashColor;
    }

    void OnHealed()
    {
        _target = healFlashColor;
    }

    void Update()
    {
        vignette.color = Color.Lerp(vignette.color, _target, Time.deltaTime * fadeSpeed);
        _target = Color.Lerp(_target, new Color(0f, 0f, 0f, 0f), Time.deltaTime * fadeSpeed);
    }
}