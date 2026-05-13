using UnityEngine;
using System.Collections.Generic;

public class WeaponHandFollower : MonoBehaviour
{
    [System.Serializable]
    public struct SpriteOffset
    {
        public string spriteName;
        public Vector3 offset;
    }

    [SerializeField] private List<Transform> weaponTransforms = new List<Transform>();
    [SerializeField] private List<SpriteOffset> offsets = new List<SpriteOffset>();

    private SpriteRenderer playerSR;
    private Dictionary<string, Vector3> offsetDict = new Dictionary<string, Vector3>();

    void Awake()
    {
        playerSR = GetComponent<SpriteRenderer>();
        foreach (var so in offsets)
        {
            if (!offsetDict.ContainsKey(so.spriteName))
                offsetDict.Add(so.spriteName, so.offset);
        }
    }

    void LateUpdate()
    {
        if (playerSR == null || weaponTransforms.Count == 0) return;
        
        Sprite currentSprite = playerSR.sprite;
        if (currentSprite == null) return;

        if (offsetDict.TryGetValue(currentSprite.name, out Vector3 offset))
        {
            foreach (var weapon in weaponTransforms)
            {
                if (weapon != null)
                {
                    weapon.localPosition = offset;
                }
            }
        }
    }

    public void AddOffset(string spriteName, Vector3 offset)
    {
        offsets.Add(new SpriteOffset { spriteName = spriteName, offset = offset });
    }
}
