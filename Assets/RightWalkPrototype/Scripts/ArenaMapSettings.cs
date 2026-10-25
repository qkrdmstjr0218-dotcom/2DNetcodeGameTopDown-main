using UnityEngine;
namespace RightWalkPrototype
{
    // Optional scene data. Scenes without it retain the original trial layout.
    public sealed partial class ArenaMapSettings : MonoBehaviour
    {
        public string displayName="주조 시설 08 · 4단 순환 전장";
        public Rect cameraBounds=new Rect(-37,-2.1f,74,18);
        public Vector2 overviewCentre=new Vector2(0,6.5f);
        [Min(3.6f)] public float floorSpacing=3.6f;
        [System.Serializable] public struct AmmoSupply {public Vector2 position;public ArenaObjectiveMatch.SupplyAmmo ammo;}
        public AmmoSupply[] ammoSupplies;
        public Vector2[] shotgunSupplies;
        public Vector2[] healthSupplies;
        public Vector2[] respawnPoints;
        [System.Serializable] public struct PatrolRoute { public float left,right,floor; }
        public PatrolRoute[] patrolRoutes;
        public Vector2[] turretSpawns;
        public bool roamingEnemies;
        public bool medicalSupplyDrones;
        public Vector2[] droneSpawns;
        public Vector2[] groundEnemySpawns;
        public bool enemyEntrances;
        [Min(.5f)] public float enemyEntrySeconds=1.4f;
        public bool ContainsCombatPosition(Vector3 position)=>position.x>=cameraBounds.xMin&&position.x<=cameraBounds.xMax&&position.y>=-4&&position.y<=cameraBounds.yMax+8;
    }
}
