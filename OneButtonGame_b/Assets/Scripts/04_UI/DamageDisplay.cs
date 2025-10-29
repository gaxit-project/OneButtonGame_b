using UnityEngine;
using TMPro;
using System.Collections;

public class DamageDisplay : MonoBehaviour
{
    public TextMeshProUGUI damageText;
    public float displayDuration = 0.5f;
    public float fadeDuration = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        if(damageText != null)
        {
            damageText.alpha = 0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowDamage(int damageAmount)
    {
        StopAllCoroutines();
        StartCoroutine(ShowAndFadeText(damageAmount));
    }

    private IEnumerator ShowAndFadeText(int damageAmount)
    {
        damageText.text = "-" + damageAmount.ToString();
        damageText.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        float timer = 0f;
        while (timer < displayDuration)
        {
            damageText.alpha  =Mathf.Lerp(1f, 0f, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        damageText.alpha = 0f;
    }
}
