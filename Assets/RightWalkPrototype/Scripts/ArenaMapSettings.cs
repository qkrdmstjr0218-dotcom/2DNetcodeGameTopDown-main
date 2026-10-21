using UnityEngine;
namespace RightWalkPrototype
{
    // Optional scene data. Scenes without it retain the original trial layout.
    public sealed class ArenaMapSettings : MonoBehaviour
    {
        public string displayName="주조 시설 08 · 4단 순환 전장";
        public Rect cameraBounds=new Rect(-37,-2.1f,74,18);
        public Vector2 overviewCentre=new Vector2(0,6.5f);
        public Vector2[] shotgunSupplies;
        [System.Serializable] public struct PatrolRoute { public float left,right,floor; }
        public PatrolRoute[] patrolRoutes;
        public bool ContainsCombatPosition(Vector3 position)=>position.x>=cameraBounds.xMin&&position.x<=cameraBounds.xMax&&position.y>=-4&&position.y<=cameraBounds.yMax+8;
    }
}
