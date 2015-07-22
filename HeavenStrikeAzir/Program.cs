using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;

namespace HeavenSTrikeAzir
{
    class Program
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _w, _e, _r;

        private static Menu _menu;

        private static float lastAA;

        private static List<GameObject> soldier = new List<GameObject>();

        private static List <Obj_AI_Hero> enemies = new List<Obj_AI_Hero>();

        private static bool qeWaitQ, waitEjumpTarget,setEjumpTarget, waitQjumpTarget,setQjumpTarget;

        private static Vector3 qePosQ, posEjumpTarget;

        private static int qcount;

        private static string 
            drawQ = "Draw Q" , drawW = "Draw W", drawQE = "Draw Q+E";
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            //Verify Champion
            if (Player.ChampionName != "Azir")
                return;


            //Spells
            _q = new Spell(SpellSlot.Q,1175);
            _w = new Spell(SpellSlot.W,450);
            _e = new Spell(SpellSlot.E,900);
            _r = new Spell(SpellSlot.R,250);
            // from detuks :D
            _q.SetSkillshot(0.0f, 65, 1500, false, SkillshotType.SkillshotLine);
            _q.MinHitChance = HitChance.Medium;
            //Menu instance
            _menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            //Orbwalker
            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            //Targetsleector
            _menu.AddSubMenu(orbwalkerMenu);
            Menu ts = _menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);
            //spell menu
            Menu spellMenu = _menu.AddSubMenu(new Menu("Spells", "Spells"));
            spellMenu.AddItem(new MenuItem("knocktarget", "knocktarget").SetValue(new KeyBind('T', KeyBindType.Press)));
            //auto menu
            //Menu auto = spellMenu.AddSubMenu(new Menu("Auto", "Auto"));
            //Drawing
            Menu Draw = _menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Draw.AddItem(new MenuItem(drawQ, drawQ).SetValue(true));
            Draw.AddItem(new MenuItem(drawW, drawW).SetValue(true));
            Draw.AddItem(new MenuItem(drawQE, drawQE).SetValue(true));
            //Attach to root
            _menu.AddToMainMenu();

            //Listen to events
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            //Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }
        private static bool knocktarget { get { return _menu.Item("knocktarget").GetValue<KeyBind>().Active; } }
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            //Game.Say(args.SData.Name);
            if (args.SData.Name.ToLower().Contains("attack"))
                lastAA = Utils.GameTimeTickCount - Game.Ping / 2;
            if (args.SData.Name.ToLower().Contains("azirq"))
            {
                qeWaitQ = false;
                qcount = Utils.GameTimeTickCount;
                if (knocktarget)
                {
                    waitQjumpTarget = false;
                }
            }
            if (args.SData.Name.ToLower().Contains("azirw"))
            {
                if (knocktarget && setEjumpTarget)
                {
                    waitEjumpTarget = true;
                    setEjumpTarget = false;
                    setQjumpTarget = true;
                }
            }
            if (args.SData.Name.ToLower().Contains("azire"))
            {
                if (knocktarget && setQjumpTarget)
                {
                    waitEjumpTarget = false;
                    waitQjumpTarget = true;
                    setQjumpTarget = false;
                }
            }
            if (args.SData.Name.ToLower().Contains("azirr"))
            {

            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // azir soldier
            azirsoldier();
            //azir();
            var x = _orbwalker.ActiveMode;
            if (x == Orbwalking.OrbwalkingMode.Combo ||x == Orbwalking.OrbwalkingMode.Mixed)
            {
                _orbwalker.SetAttack(false);
            }
            else _orbwalker.SetAttack(true);
            //combo
            if (x == Orbwalking.OrbwalkingMode.Combo) Combo();
            //harass
            if (x == Orbwalking.OrbwalkingMode.Mixed) Harass();
            //qe
            //if (_menu.Item(spellEQtoMouse).GetValue<KeyBind>().Active)
            //    QE();
            if (knocktarget)
            {
                JumpToTarget();
                solvejumptotarget();
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                waitQjumpTarget = false;
                waitEjumpTarget = false;
                setQjumpTarget = false;
                setEjumpTarget = false;
            }


        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (_menu.Item(drawQ).GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, _q.Range, Color.Yellow);
            if (_menu.Item(drawW).GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, _w.Range, Color.Yellow);
            if (_menu.Item(drawQE).GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, _q.Range + _w.Range, Color.Yellow);
            foreach (var x in soldier)
            {
                Render.Circle.DrawCircle(x.Position, 200, Color.Yellow);
            }
        }
        private static void Harass()
        {
            if (enemies.Any() && CanDoAttack())
            {
                var target = enemies.OrderByDescending(x => x.Health).LastOrDefault();
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (!enemies.Any() && CanDoAttack())
            {
                var target = _orbwalker.GetTarget();
                if (target.IsValidTarget() && !target.IsZombie)
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (_w.IsReady() && CanMove() && !enemies.Any())
            {
                var target = TargetSelector.GetTarget(_w.Range + 300, TargetSelector.DamageType.Magical);
                if (target.IsValidTarget() && !target.IsZombie && !enemies.Contains(target))
                {
                    var x = Player.Distance(target.Position) > _w.Range ? Player.Position.Extend(target.Position, _w.Range)
                        : target.Position;
                    _w.Cast(x);
                }
            }
            if (_q.IsReady() && CanMove())
            {
                var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                foreach (var obj in soldier)
                {
                    _q.SetSkillshot(0.0f, 65f, 1500f, false, SkillshotType.SkillshotLine, obj.Position, Player.Position);
                    _q.Cast(target);
                }
            }
            if (_w.IsReady() && CanMove() && !enemies.Any() &&!soldier.Any())
            {
                var target = TargetSelector.GetTarget(_w.Range + 300, TargetSelector.DamageType.Magical);
                if (target == null || !target.IsValidTarget() || target.IsZombie)
                {
                    var tar = HeroManager.Enemies.Where(x => x.IsValidTarget(_q.Range) && !x.IsZombie).OrderByDescending(x => Player.Distance(x.Position)).LastOrDefault();
                    if (tar.IsValidTarget() && !tar.IsZombie)
                    {
                        var x = Player.Distance(tar.Position) > _w.Range ? Player.Position.Extend(tar.Position, _w.Range)
                            : tar.Position;
                        _w.Cast(x);
                    }
                }
            }
        }
        private static void Combo()
        {
            if (enemies.Any() && CanDoAttack())
            {
                var target = enemies.OrderByDescending(x => x.Health).LastOrDefault();
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (!enemies.Any() && CanDoAttack())
            {
                var target = _orbwalker.GetTarget();
                if (target.IsValidTarget() && !target.IsZombie)
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (_w.IsReady() && CanMove())
            {
                var target = TargetSelector.GetTarget(_w.Range + 300, TargetSelector.DamageType.Magical);
                if (target.IsValidTarget() && !target.IsZombie && !enemies.Contains(target))
                {
                    var x = Player.Distance(target.Position) > _w.Range ? Player.Position.Extend(target.Position, _w.Range)
                        : target.Position;
                    _w.Cast(x);
                }
            }
            if (_q.IsReady() && CanMove())
            {
                var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                foreach (var obj in soldier)
                {
                    _q.SetSkillshot(0.0f, 65f, 1500f, false, SkillshotType.SkillshotLine, obj.Position, Player.Position);
                    _q.Cast(target);
                }
            }
            if (_w.IsReady() && CanMove() && !soldier.Any())
            {
                var target = TargetSelector.GetTarget(_w.Range + 300, TargetSelector.DamageType.Magical);
                if (target == null || !target.IsValidTarget() || target.IsZombie)
                {
                    var tar = HeroManager.Enemies.Where(x => x.IsValidTarget(_q.Range) && !x.IsZombie).OrderByDescending(x => Player.Distance(x.Position)).LastOrDefault();
                    if (tar.IsValidTarget() && !tar.IsZombie)
                    {
                        var x = Player.Distance(tar.Position) > _w.Range ? Player.Position.Extend(tar.Position, _w.Range)
                            : tar.Position;
                        _w.Cast(x);
                    }
                }
            }

        }
        private static void JumpToTarget()
        {
            var target = TargetSelector.GetSelectedTarget();
            if (target.IsValidTarget() && !target.IsZombie)
            {
                foreach (var x in soldier)
                {
                    if (Geometry.Distance(target.Position.To2D(),Player.Position.To2D(),x.Position.To2D(),true,true) <= target.BoundingRadius + Player.BoundingRadius && _e.IsReady()
                        &&  Player.Distance(x.Position) <= 900)
                    {
                        _e.Cast(x.Position);
                        return;
                    }
                }
                if (_e.IsReady() && Utils.GameTimeTickCount - qcount >= _q.Instance.Cooldown)
                {
                    //if (_w.IsReady() && soldier.Any())
                    //{
                    //    var posW = Player.Position.Extend(target.Position, _w.Range);
                    //    var disW = Player.Distance(target.Position) - _w.Range;
                    //    var Sold = soldier.OrderByDescending(x => x.Position.Distance(target.Position)).LastOrDefault();
                    //    var disSold = Sold.Position.Distance(target.Position);
                    //    if (disW < _q.Range - 500 || disSold < _q.Range - 500)
                    //    {
                    //        if (disW < disSold)
                    //        {
                    //            _w.Cast(posW);
                    //            posEjumpTarget = posW;
                    //            setEjumpTarget = true;
                    //            return;
                    //        }
                    //        else if (Player.Distance(Sold.Position) <= 900)
                    //        {
                    //            _e.Cast(Sold.Position);
                    //            setQjumpTarget = true;
                    //            return;
                    //        }
                    //    }

                    //}
                    //else 
                    if (soldier.Any())
                    {
                        var Sold = soldier.OrderByDescending(x => x.Position.Distance(target.Position)).LastOrDefault();
                        var disSold = Sold.Position.Distance(target.Position);
                        if (disSold < _q.Range - 500)
                        {
                            if (Player.Distance(Sold.Position) <= 900)
                            {
                                _e.Cast(Sold.Position);
                                setQjumpTarget = true;
                                return;
                            }
                        }
                    }
                    if (_w.IsReady())
                    {
                        var posW = Player.Position.Extend(target.Position, _w.Range);
                        var disW = Player.Distance(target.Position) - _w.Range;
                        if (disW < _q.Range - 700 )
                        {
                            _w.Cast(posW);
                            posEjumpTarget = posW;
                            setEjumpTarget = true;
                            return;
                        }
                    }
                }
            }
        }
        private static void solvejumptotarget()
        {
            var target = TargetSelector.GetSelectedTarget();
            if (waitEjumpTarget == true)
            {
                _e.Cast(posEjumpTarget);
            }
            if (waitQjumpTarget == true)
            {
                if (Player.ServerPosition.Distance(target.Position) <= _q.Range - 300)
                {
                    _q.Cast(target.Position);
                }
            }
            
        }
        
        private static void azir()
        {
            String temp = "";
            foreach(var obj in ObjectManager.Get<GameObject>().Where(x => x.Position.Distance(Game.CursorPos) <=100))
            {
                temp += obj.Name + " ," ;
            }
            if (temp != "")Game.Say(temp);
        }
        private static void azirsoldier()
        {
            soldier = new List<GameObject>();
            foreach (var obj in ObjectManager.Get<GameObject>().Where(x => x.Name == "Azir_Base_P_Soldier_Ring.troy" ))
            {
                soldier.Add(obj);
            }
            enemies = new List<Obj_AI_Hero>();
            foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie))
            {
                if (soldier.Any(x => x.Position.Distance(hero.Position) <= 300 + hero.BoundingRadius && Player.Distance(x.Position) <= 900))
                    enemies.Add(hero);
            }
        }
        public static bool CanDoAttack()
        {
            if (lastAA <= Utils.TickCount)
            {
                return Utils.GameTimeTickCount + Game.Ping / 2 + 25 >= lastAA + Player.AttackDelay * 1000;
            }

            return false;
        }
        public static bool CanMove()
        {
            return (Utils.GameTimeTickCount + Game.Ping / 2 >= lastAA + Player.AttackCastDelay * 1000 + 80);
        }
    }
}
