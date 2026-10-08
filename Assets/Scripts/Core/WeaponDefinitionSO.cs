using UnityEngine;
namespace TopDownShooter.Core
{
    [CreateAssetMenu(menuName = "TopDownShooter/Config/Weapon")]
    public sealed class WeaponDefinitionSO : ScriptableObject
    {
        public string displayName = "Automatic";
        [Min(0.05f)] public float interval = 0.12f;
        [Min(1)] public int damage = 1;
        [Range(1, 8)] public int pellets = 1;
        [Tooltip("0 = unlimited target penetration; walls still block.")]
        [Min(0)] public int targets = 1;
        [Min(1)] public float speed = 24;
        [Min(1)] public float range = 16;
        [Range(0, 45)] public float spread = 1.5f;
        [Range(0, 30)] public float sprintSpread = 6;
        public float visualLength = 0.7f;
        public Color color = new Color(0.6f, 0.85f, 1);
    }
}
