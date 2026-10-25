using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RightWalkPrototype;

// Targeted migration: move complete props/decks, reshape ramps, never stretch
// characters, cover heights, deck thickness, or regenerate the existing map.
public static class FoundryFloorSpacingBuilder
{
    public const float Spacing=6.8f;
    static float Old,Delta;
    static void Raise(Transform t,float amount){t.position+=Vector3.up*amount;}
    static Vector2 Floor(Vector2 p){p.y+=Mathf.Round(p.y/Old)*Delta;return p;}
    static void Ramp(Transform t,Vector2 a,Vector2 b,bool extended)
    {
        if(a.x>b.x){var swap=a;a=b;b=swap;}
        a.y*=Spacing/Old;b.y*=Spacing/Old;
        PlaceRamp(t,a,b,extended);
    }
    static void PlaceRamp(Transform t,Vector2 a,Vector2 b,bool extended)
    {
        var d=b-a;var normal=new Vector2(-d.normalized.y,d.normalized.x);
        t.position=(a+b)*.5f-normal*.1f;
        t.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        t.localScale=new Vector3(d.magnitude,.2f,1);
    }
    [MenuItem("Tools/Right Walk Prototype/Increase Foundry Floor Spacing")]
    public static void Apply()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=RelayFoundryBuilder.ScenePath)throw new Exception("Requires saved Foundry edit mode");
        var map=UnityEngine.Object.FindFirstObjectByType<ArenaMapSettings>();
        if(Mathf.Abs(map.floorSpacing-Spacing)<.001f){RepairRampEnds(map);RepairBalconyApproaches(map);EditorSceneManager.SaveScene(scene);return;}
        Old=map.floorSpacing;Delta=Spacing-Old;
        if(Mathf.Abs(Old-3.6f)>.001f&&Mathf.Abs(Old-4.8f)>.001f)throw new Exception("Unsupported starting floor spacing");
        Undo.RegisterFullObjectHierarchyUndo(map.gameObject,"Increase Foundry floor spacing");
        foreach(Transform t in map.transform)
        {
            string n=t.name;
            if(n.StartsWith("Middle "))Raise(t,Delta);
            else if(n.StartsWith("Upper "))Raise(t,2*Delta);
            else if(n.StartsWith("Crown ")&&!n.StartsWith("Crown access"))Raise(t,3*Delta);
            else if(n.StartsWith("Jump shortcut"))Raise(t,(n.Contains("lower")?.5f:1.5f)*Delta);
            else if(n=="Central ammunition depot")Raise(t,Delta);
            else if(n=="Foundry uplinks")foreach(Transform point in t)point.position=Floor(point.position);
            else if(n=="Left boundary"||n=="Right boundary")
            {Raise(t,1.5f*Delta);t.localScale+=Vector3.up*(3*Delta);}
            else if(n=="Backdrop"||n=="Recessed bay"||n=="Background column")
            {t.position=new Vector3(t.position.x,t.position.y*Spacing/Old,t.position.z);t.localScale=new Vector3(t.localScale.x,t.localScale.y*Spacing/Old,t.localScale.z);}
            else if(n=="Inset wall panel")
            {Raise(t,Mathf.Round((t.position.y-1.6f)/Old)*Delta);t.localScale+=Vector3.up*Delta;}
            else if(n=="Title")Raise(t,3*Delta);
            else if(n=="West sign"||n=="East sign")Raise(t,2*Delta);
            else if(n.StartsWith("Player ")||n.StartsWith("Spawn pad"))t.position=Floor(t.position);
        }
        foreach(int side in new[]{-1,1})
        {
            Ramp(map.transform.Find("Outer lower "+side),new Vector2(side*35,0),new Vector2(side*25,Old),true);
            Ramp(map.transform.Find("Inner lower "+side),new Vector2(side*15,0),new Vector2(side*5,Old),true);
            Ramp(map.transform.Find("Outer upper "+side),new Vector2(side*35,Old),new Vector2(side*25,2*Old),true);
            Ramp(map.transform.Find("Inner upper "+side),new Vector2(side*16,Old),new Vector2(side*6,2*Old),true);
            Ramp(map.transform.Find("Crown access "+side),new Vector2(side*23,2*Old),new Vector2(side*10,3*Old),true);
        }
        var balconies=map.transform.Find(FoundryEntryBuilder.RootName);
        foreach(Transform t in balconies)
        {
            if(t.name.StartsWith("Outer spawn balcony")||t.name.StartsWith("Balcony solid edge")||t.name.StartsWith("Balcony arrival sign"))Raise(t,3*Delta);
            else if(t.name.StartsWith("Outer balcony access"))
            {
                int side=t.name.EndsWith("-1")?-1:1;
                var a=new Vector2(side*25,2*Old);var b=new Vector2(side*31,3*Old);if(a.x>b.x){var swap=a;a=b;b=swap;}Ramp(t,a,b,false);
            }
            else if(t.name=="Balcony passable dash")
            {
                var d=t.right*t.localScale.x;d.y*=Spacing/Old;
                t.position=new Vector3(t.position.x,t.position.y*Spacing/Old,t.position.z);
                t.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);t.localScale=new Vector3(d.magnitude,t.localScale.y,1);
            }
            else if(t.name=="Enemy entrance decorations")foreach(Transform part in t)
            {if(part.name.StartsWith("Drone exit"))Raise(part,3*Delta);else part.position=Floor(part.position);}
        }
        var direction=map.transform.Find(FoundryWestBridgeBuilder.RootName+"/Bypass direction");if(direction!=null)Raise(direction,Delta);
        if(map.ammoSupplies!=null)for(int i=0;i<map.ammoSupplies.Length;i++){var item=map.ammoSupplies[i];item.position=Floor(item.position);map.ammoSupplies[i]=item;}
        Vector2[] Convert(Vector2[] values){if(values==null)return null;return Array.ConvertAll(values,Floor);}
        map.shotgunSupplies=Convert(map.shotgunSupplies);map.healthSupplies=Convert(map.healthSupplies);map.respawnPoints=Convert(map.respawnPoints);
        map.groundEnemySpawns=Convert(map.groundEnemySpawns);map.turretSpawns=Convert(map.turretSpawns);
        if(map.droneSpawns!=null)for(int i=0;i<map.droneSpawns.Length;i++)map.droneSpawns[i]+=Vector2.up*(3*Delta);
        if(map.patrolRoutes!=null)for(int i=0;i<map.patrolRoutes.Length;i++){var route=map.patrolRoutes[i];route.floor=Floor(new Vector2(0,route.floor)).y;map.patrolRoutes[i]=route;}
        var bounds=map.cameraBounds;bounds.yMax+=3*Delta;map.cameraBounds=bounds;map.overviewCentre=bounds.center;map.floorSpacing=Spacing;
        RepairRampEnds(map);RepairBalconyApproaches(map);
        EditorUtility.SetDirty(map);Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static void RepairRampEnds(ArenaMapSettings map)
    {
        foreach(Transform t in map.transform)
        {
            bool crown=t.name.StartsWith("Crown access ");
            if(!crown&&!t.name.StartsWith("Outer lower ")&&!t.name.StartsWith("Outer upper ")&&!t.name.StartsWith("Inner lower ")&&!t.name.StartsWith("Inner upper "))continue;
            float run=crown?13:10;var scale=t.localScale;scale.x=Mathf.Sqrt(run*run+map.floorSpacing*map.floorSpacing);t.localScale=scale;
        }
    }
    // Preserve the old .8 rise/run: the short original access cannot be mounted
    // by the unchanged jump at the new height. Move its entrance past the cover.
    public static void RepairBalconyApproaches(ArenaMapSettings map)
    {
        var root=map.transform.Find(FoundryEntryBuilder.RootName);if(root==null)return;
        float start=31-map.floorSpacing/.8f;
        foreach(int side in new[]{-1,1})
        {
            var ramp=root.Find("Outer balcony access "+side);if(ramp==null)continue;
            var a=ramp.TransformPoint(new Vector3(-.5f,.5f));var b=ramp.TransformPoint(new Vector3(.5f,.5f));
            float oldStart=Mathf.Abs(a.y<b.y?a.x:b.x);
            if(Mathf.Abs(oldStart-start)>.001f)
            {
                float factor=(31-start)/(31-oldStart);
                foreach(Transform dash in root)if(dash.name=="Balcony passable dash"&&Mathf.Sign(dash.position.x)==side)
                {
                    var p=dash.position;p.x=side*(start+(Mathf.Abs(p.x)-oldStart)*factor);dash.position=p;
                    var d=dash.right*dash.localScale.x;d.x*=factor;
                    dash.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);dash.localScale=new Vector3(d.magnitude,dash.localScale.y,1);
                }
                var low=new Vector2(side*start,2*map.floorSpacing);var high=new Vector2(side*31,3*map.floorSpacing);
                PlaceRamp(ramp,side<0?high:low,side<0?low:high,false);
            }
            var cover=map.transform.Find("Upper cover "+side);if(cover!=null){var p=cover.position;p.x=side*(start-2);cover.position=p;}
        }
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
    }
}
