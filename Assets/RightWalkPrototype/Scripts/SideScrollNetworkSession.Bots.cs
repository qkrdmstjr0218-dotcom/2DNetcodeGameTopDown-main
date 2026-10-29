using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkSession
    {
        int botDifficulty=-1;
        public int BotDifficulty=>botDifficulty<0?botDifficulty=Mathf.Clamp(PlayerPrefs.GetInt("RightWalk.BotDifficulty",1),0,2):botDifficulty;
        public string BotDifficultyName=>BotDifficulty==0?"쉬움":BotDifficulty==2?"어려움":"보통";
        public bool SetBotDifficulty(int level)
        {
            if(!CanEditBots||level<0||level>2)return false;
            botDifficulty=level;PlayerPrefs.SetInt("RightWalk.BotDifficulty",level);PlayerPrefs.Save();return true;
        }
        public int BotCount
        {get{int n=0;foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsBot)n++;return n;}}
        public bool CanEditBots=>SoloPractice&&!TutorialMode&&NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsHost&&ArenaObjectiveMatch.PracticeRoom;
        public bool AddPracticeBot()
        {
            if(!CanEditBots)return false;
            var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
            int count=0;foreach(var p in players)if(p.IsSpawned)count++;
            var prefab=NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
            if(count>=4||prefab==null)return false;
            var obj=Instantiate(prefab);obj.GetComponent<SideScrollNetworkPlayer>().PrepareBot();
            obj.GetComponent<NetworkObject>().Spawn();return true;
        }
        public bool RemovePracticeBot()
        {
            if(!CanEditBots)return false;
            SideScrollNetworkPlayer last=null;
            foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                if(p.IsSpawned&&p.IsBot&&(last==null||p.PlayerNumber>last.PlayerNumber))last=p;
            if(last==null)return false;last.GetComponent<NetworkObject>().Despawn();return true;
        }
    }
}
