﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using System.Text.RegularExpressions;
using Color = System.Drawing.Color;

namespace HeavenStrikeAzir
{
    public static class Soldiers
    {
        public static Obj_AI_Hero Player { get{ return ObjectManager.Player; } }
        private static int LastWTick;
        public static List<GameObject> soldier = new List<GameObject>();

        public static List<Obj_AI_Hero> enemies = new List<Obj_AI_Hero>();

        public static List<Obj_AI_Minion> autoattackminions = new List<Obj_AI_Minion>();

        public static List<Obj_AI_Minion> soldierattackminions = new List<Obj_AI_Minion>();

        public static List<SplashAutoAttackMinion> splashautoattackminions = new List<SplashAutoAttackMinion>();

        public static List<SplashAutoAttackChampion> splashautoattackchampions = new List<SplashAutoAttackChampion>();

        public static void AzirSoldier()
        {
            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnDelete += GameObject_OnDelete;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            //Game.PrintChat(args.SData.Name);
            if (/*args.Slot == SpellSlot.W*/args.SData.Name == "AzirW")
                LastWTick = Environment.TickCount;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            //if (sender.Name.ToLower().Contains("azir"))
            //    Game.PrintChat(sender.Name + " oncreate");   
            if (sender.Name == "Azir_Base_P_Soldier_Ring.troy" && Math.Abs(Environment.TickCount - LastWTick) <= 250)
                soldier.Add(sender);
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            //if (sender.Name.ToLower().Contains("azir"))
            //    Game.PrintChat(sender.Name + " ondelete");
            if (sender.Name == "Azir_Base_P_Soldier_Ring.troy")
                soldier.RemoveAll(x => x.NetworkId == sender.NetworkId);
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            var soldierandtargetminion = new List<SoldierAndTargetMinion>();
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget()).ToList();
            minions.AddRange(GameObjects.Jungle.Where(x=> x.IsValidTarget()));
            var minionspredictedposition = new List<MinionPredictedPosition>();
            foreach (var x in minions)
            {
                minionspredictedposition
                    .Add(new MinionPredictedPosition
                        (x, Prediction.GetPrediction(x, Player.AttackCastDelay + Game.Ping / 1000).UnitPosition));
            }
            var championpredictedposition = new List<ChampionPredictedPosition>();
            foreach (var x in HeroManager.Enemies.Where(x => x.IsValidTarget()))
            {
                championpredictedposition
                    .Add(new ChampionPredictedPosition
                        (x, Prediction.GetPrediction(x, Player.AttackCastDelay + Game.Ping / 1000).UnitPosition));
            }
            enemies = new List<Obj_AI_Hero>();
            foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie))
            {
                if (soldier.Any(x => x.Position.Distance(hero.Position) <= 300 + hero.BoundingRadius && Player.Distance(x.Position) <= 925))
                    enemies.Add(hero);
            }
            soldierattackminions = new List<Obj_AI_Minion>();
            foreach (var minion in minions)
            {
                var Soldiers = soldier.Where
                    (x => x.Position.Distance(minion.Position) <= 300 + minion.BoundingRadius && Player.Distance(x.Position) <= 925)
                    .ToList();
                if (Soldiers.Any())
                {
                    soldierattackminions.Add(minion);
                    soldierandtargetminion.Add(new SoldierAndTargetMinion(minion, Soldiers));
                }
            }
            autoattackminions = new List<Obj_AI_Minion>();
            foreach (var minion in minions.Where(x => x.IsValidTarget(Orbwalking.GetRealAutoAttackRange(x))))
            {
                if (!soldierattackminions.Any(x => x.NetworkId == minion.NetworkId))
                    autoattackminions.Add(minion);
            }
            splashautoattackchampions = new List<SplashAutoAttackChampion>();
            foreach (var mainminion in soldierandtargetminion)
            {
                var mainminionpredictedposition =
                    Prediction.GetPrediction(mainminion.Minion, Player.AttackCastDelay + Game.Ping / 1000).UnitPosition;
                List<Obj_AI_Hero> splashchampions = new List<Obj_AI_Hero>();
                foreach (var hero in championpredictedposition)
                {
                    foreach (var mainminionsoldier in mainminion.Soldier)
                        if (Geometry.Distance(hero.Position.To2D(), mainminionsoldier.Position.To2D(),
                            mainminionsoldier.Position.To2D().Extend(mainminionpredictedposition.To2D(), 450), false)
                            <= hero.Hero.BoundingRadius + 50)
                        {
                            splashchampions.Add(hero.Hero);
                        }
                }
                if (splashchampions.Any())
                    splashautoattackchampions.Add(new SplashAutoAttackChampion(mainminion.Minion, splashchampions));
            }
            splashautoattackminions = new List<SplashAutoAttackMinion>();
            foreach (var mainminion in soldierandtargetminion)
            {
                var mainminionpredictedposition =
                    Prediction.GetPrediction(mainminion.Minion, Player.AttackCastDelay + Game.Ping / 1000).UnitPosition;
                List<Obj_AI_Minion> splashminions = new List<Obj_AI_Minion>();
                foreach (var minion in minionspredictedposition)
                {
                    foreach (var mainminionsoldier in mainminion.Soldier)
                        if (Geometry.Distance(minion.Position.To2D(), mainminionsoldier.Position.To2D(),
                            mainminionsoldier.Position.To2D().Extend(mainminionpredictedposition.To2D(), 450), false)
                            <= minion.Minion.BoundingRadius + 50)
                        {
                            splashminions.Add(minion.Minion);
                            break;
                        }
                }
                splashautoattackminions.Add(new SplashAutoAttackMinion(mainminion.Minion, splashminions));
            }
        }
    }
    public class SplashAutoAttackMinion
    {
        public Obj_AI_Minion MainMinion;
        public List<Obj_AI_Minion> SplashAutoAttackMinions;
        public SplashAutoAttackMinion(Obj_AI_Minion mainminion, List<Obj_AI_Minion> splashautoattackminions)
        {
            MainMinion = mainminion;
            SplashAutoAttackMinions = splashautoattackminions;
        }
    }
    public class SplashAutoAttackChampion
    {
        public Obj_AI_Minion MainMinion;
        public List<Obj_AI_Hero> SplashAutoAttackChampions;
        public SplashAutoAttackChampion(Obj_AI_Minion mainminion, List<Obj_AI_Hero> splashAutoAttackChampions)
        {
            MainMinion = mainminion;
            SplashAutoAttackChampions = splashAutoAttackChampions;
        }
    }
    public class MinionPredictedPosition
    {
        public Obj_AI_Minion Minion;
        public Vector3 Position;
        public MinionPredictedPosition(Obj_AI_Minion minion, Vector3 position)
        {
            Minion = minion;
            Position = position;
        }
    }
    public class ChampionPredictedPosition
    {
        public Obj_AI_Hero Hero;
        public Vector3 Position;
        public ChampionPredictedPosition(Obj_AI_Hero hero, Vector3 position)
        {
            Hero = hero;
            Position = position;
        }
    }
    public class SoldierAndTargetMinion
    {
        public Obj_AI_Minion Minion;
        public List<GameObject> Soldier;
        public SoldierAndTargetMinion(Obj_AI_Minion minion, List<GameObject> soldier)
        {
            Minion = minion;
            Soldier = soldier;
        }
    }
}
