using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CoinRainUI : MonoBehaviour
{
    [SerializeField] private List<Sprite> coinSprites;
    [SerializeField] private int coinCount = 30;
    [SerializeField] private float fallSpeedMin = 390f;
    [SerializeField] private float fallSpeedMax = 780f;
[SerializeField] private float rotationSpeedMin = 50f;
    [SerializeField] private float rotationSpeedMax = 200f;

    private List<GameObject> activeCoins = new List<GameObject>();
    private RectTransform canvasRect;

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
    }

    public void SetSprites(List<Sprite> sprites)
    {
        coinSprites = sprites;
    }

    public void StartRain()
    {
        if (coinSprites == null || coinSprites.Count == 0) return;

        StartCoroutine(RainCoroutine());
    }

    private System.Collections.IEnumerator RainCoroutine()
    {
        for (int i = 0; i < coinCount; i++)
        {
            SpawnCoin();
            if (i % 3 == 0) yield return new WaitForSeconds(0.05f);
        }
    }

    private void SpawnCoin()
    {
        GameObject coin = new GameObject("RainCoin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        coin.transform.SetParent(transform, false);
        
        RectTransform rt = coin.GetComponent<RectTransform>();
        Image img = coin.GetComponent<Image>();

        img.sprite = coinSprites[Random.Range(0, coinSprites.Count)];
        img.raycastTarget = false;
        img.preserveAspect = true;

        // Randomize initial position above the screen
        float startX = Random.Range(0, canvasRect.rect.width);
        float startY = canvasRect.rect.height + 50f;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(startX, startY);
        
        // Randomize scale
        float scale = Random.Range(1f, 2f);
        rt.localScale = new Vector3(scale, scale, 1f);

        activeCoins.Add(coin);

        float fallSpeed = Random.Range(fallSpeedMin, fallSpeedMax);
        float rotSpeed = Random.Range(rotationSpeedMin, rotationSpeedMax) * (Random.value > 0.5f ? 1 : -1);

        StartCoroutine(FallAndRotate(coin, fallSpeed, rotSpeed));
    }

    private System.Collections.IEnumerator FallAndRotate(GameObject coin, float fallSpeed, float rotSpeed)
    {
        RectTransform rt = coin.GetComponent<RectTransform>();
        while (rt != null && rt.anchoredPosition.y > -100f)
        {
            rt.anchoredPosition += Vector2.down * fallSpeed * Time.deltaTime;
            rt.Rotate(Vector3.forward, rotSpeed * Time.deltaTime);
            yield return null;
        }

        if (coin != null)
        {
            activeCoins.Remove(coin);
            Destroy(coin);
        }
    }
}
