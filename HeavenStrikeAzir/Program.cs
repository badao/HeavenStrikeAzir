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

        private static List<Obj_AI_Hero> enemies = new List<Obj_AI_Hero>();

        private static List<Obj_AI_Minion> autoattackminions = new List<Obj_AI_Minion>();

        private static List<Obj_AI_Minion> soldierattackminions = new List<Obj_AI_Minion>();

        private static bool qeWaitQ, waitEjumpTarget, setEjumpTarget, waitQjumpTarget, setQjumpTarget;
        private static bool waitEjumpmouse, setEjumpMouse, waitQjumpmouse, setQjumpMouse;
        private static bool waitEjumpmax, setEjumpMax, waitQjumpmax, setQjumpMax;

        private static Vector3 qePosQ, posEjumpTarget, posEjumpMouse, posEjumpMax,posQjumpMax,posQjumpMouse;

        private static int qcount,ecount;
        private static bool Eisready { get { return Player.Mana >= _e.Instance.ManaCost && Utils.GameTimeTickCount - ecount >= _e.Instance.Cooldown * 1000f; } }

        private static string
            drawQ = "Draw Q", drawW = "Draw W", drawQE = "Draw Q+E";
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
            _q = new Spell(SpellSlot.Q, 1175);
            _w = new Spell(SpellSlot.W, 450);
            _e = new Spell(SpellSlot.E, 900);
            _r = new Spell(SpellSlot.R, 250);
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
            spellMenu.AddItem(new MenuItem("EQmouse", "E Q to mouse").SetValue(new KeyBind('G', KeyBindType.Press)));
            spellMenu.AddItem(new MenuItem("EQmax", "E Q to max").SetValue(new KeyBind('H', KeyBindType.Press)));
            spellMenu.AddItem(new MenuItem("knocktarget", "E-Q Selected Target").SetValue(new KeyBind('T', KeyBindType.Press)));
            spellMenu.AddItem(new MenuItem("insec", "Insec Selected").SetValue(new KeyBind('Y', KeyBindType.Press)));
            spellMenu.AddItem(new MenuItem("insecmode", "Insec Mode").SetValue(new StringList( new [] {"nearest ally","nearest turret","mouse"},0)));
            //combo
            Menu Combo = spellMenu.AddSubMenu(new Menu("Combo", "Combo"));
            Combo.AddItem(new MenuItem("QC", "Q").SetValue(true));
            Combo.AddItem(new MenuItem("WC", "W").SetValue(true));
            //Harass
            Menu Harass = spellMenu.AddSubMenu(new Menu("Harass", "Harass"));
            Harass.AddItem(new MenuItem("QH", "Q").SetValue(true));
            Harass.AddItem(new MenuItem("WH", "W").SetValue(true));
            //Clear
            Menu Clear = spellMenu.AddSubMenu(new Menu("Clear", "Clear"));
            Clear.AddItem(new MenuItem("QC", "Q").SetValue(true));
            Clear.AddItem(new MenuItem("WC", "W").SetValue(true));
            Clear.AddItem(new MenuItem("ManaClear", "Min Mana Clear").SetValue(new Slider(40,0,100)));
            //auto
            Menu Auto = spellMenu.AddSubMenu(new Menu("Auto", "Auto"));
            Auto.AddItem(new MenuItem("RKS", "use R KS").SetValue(true));
            Auto.AddItem(new MenuItem("RTOWER", "R target to Tower").SetValue(true));
            Auto.AddItem(new MenuItem("RGAP", "R anti GAP").SetValue(false));
            //auto menu
            //Menu auto = spellMenu.AddSubMenu(new Menu("Auto", "Auto"));
            //Drawing
            Menu Draw = _menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Draw.AddItem(new MenuItem(drawQ, drawQ).SetValue(true));
            Draw.AddItem(new MenuItem(drawW, drawW).SetValue(true));
            //Attach to root
            _menu.AddToMainMenu();

            //Listen to events
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            //Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var target = gapcloser.Sender;
            if (target.IsEnemy && _r.IsReady() && target.IsValidTarget() && !target.IsZombie && RGAP)
            {
                if (target.IsValidTarget(250)) _r.Cast(target.Position);
            }
        }
        private static bool eqmax { get { return _menu.Item("EQmax").GetValue<KeyBind>().Active; } }
        private static bool eqmouse { get { return _menu.Item("EQmouse").GetValue<KeyBind>().Active; } }
        private static bool RTOWER { get { return _menu.Item("RTOWER").GetValue<bool>(); } }
        private static bool RKS { get { return _menu.Item("RKS").GetValue<bool>(); } }
        private static bool RGAP { get { return _menu.Item("RGAP").GetValue<bool>(); } }
        private static bool qcombo { get { return _menu.Item("QC").GetValue<bool>(); } }
        private static bool wcombo { get { return _menu.Item("WC").GetValue<bool>(); } }
        private static bool qharass { get { return _menu.Item("QH").GetValue<bool>(); } }
        private static bool wharass { get { return _menu.Item("WH").GetValue<bool>(); } }
        private static bool qclear { get { return _menu.Item("QC").GetValue<bool>(); } }
        private static bool wclear { get { return _menu.Item("WC").GetValue<bool>(); } }
        private static int manaclear { get { return _menu.Item("ManaClear").GetValue<Slider>().Value; } }
        private static bool knocktarget { get { return _menu.Item("knocktarget").GetValue<KeyBind>().Active; } }
        private static bool insec { get { return _menu.Item("insec").GetValue<KeyBind>().Active; } }
        private static int insecmode { get { return _menu.Item("insecmode").GetValue<StringList>().SelectedIndex; } }
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            //Game.Say(args.SData.Name);
            if (args.SData.Name.ToLower().Contains("attack"))
                lastAA = Utils.GameTimeTickCount - Game.Ping / 2;
            if (args.SData.Name.ToLower().Contains("azirq"))
            {
                Qtick = Utils.GameTimeTickCount;
                qeWaitQ = false;
                qcount = Utils.GameTimeTickCount;
                if ((knocktarget || insec))
                {
                    waitQjumpTarget = false;
                }
                if (eqmouse)
                {
                   waitQjumpmouse = false;
                }
                if (eqmax)
                {
                    waitQjumpmax = false;
                }
            }
            if (args.SData.Name.ToLower().Contains("azirw"))
            {
                if ((knocktarget || insec) && setEjumpTarget)
                {
                    waitEjumpTarget = true;
                    setEjumpTarget = false;
                    setQjumpTarget = true;
                }
                if (eqmouse && setEjumpMouse)
                {
                    waitEjumpmouse = true;
                    setEjumpMouse = false;
                    setQjumpMouse = true;
                }
                if (eqmax && setEjumpMax)
                {
                    waitEjumpmax = true;
                    setEjumpMax = false;
                    setQjumpMax = true;
                }
            }
            if (args.SData.Name.ToLower().Contains("azire"))
            {
                ecount = Utils.GameTimeTickCount;
                if ((knocktarget || insec) && setQjumpTarget)
                {
                    waitEjumpTarget = false;
                    waitQjumpTarget = true;
                    setQjumpTarget = false;
                }
                if (eqmouse && setQjumpMouse)
                {
                    waitEjumpmouse = false;
                    waitQjumpmouse = true;
                    setQjumpMouse = false;
                }
                if (eqmax && setQjumpMax)
                {
                    waitEjumpmax = false;
                    waitQjumpmax = true;
                    setQjumpMax = false;
                }
            }
            if (args.SData.Name.ToLower().Contains("azirr"))
            {

            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //Game.PrintChat(_e.Instance.Cooldown.ToString());
            // azir soldier
            azirsoldier();
            //if (soldier.Any())
            //{
            //    //Game.Say(soldier.Count.ToString());
            //    Game.Say(soldier.Last().Position.Distance(Player.Position).ToString());
            //}
            //auto
            Auto();
            //azir();
            var x = _orbwalker.ActiveMode;
            if (x != Orbwalking.OrbwalkingMode.None)
            {
                _orbwalker.SetAttack(false);
            }
            else _orbwalker.SetAttack(true);
            //combo
            if (x == Orbwalking.OrbwalkingMode.Combo) Combo();
            //harass
            else if (x == Orbwalking.OrbwalkingMode.Mixed) Harass();
            // clear && lasthit
            else if (x != Orbwalking.OrbwalkingMode.None) Clear();
            //qe
            //if (_menu.Item(spellEQtoMouse).GetValue<KeyBind>().Active)
            //    QE();
            if (eqmouse)
            {
                JumpTomouse();
                solvejumptomouse();
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                waitQjumpmouse = false;
                waitEjumpmouse = false;
                setQjumpMouse = false;
                setEjumpMouse = false;
            }
            if (eqmax)
            {
                JumpToMax();
                solvejumptomax();
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                waitQjumpmax = false;
                waitEjumpmax = false;
                setQjumpMax = false;
                setEjumpMax = false;
            }
            if (knocktarget)
            {
                JumpToTarget();
                solvejumptotarget();
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else if (insec)
            {
                //if (_r.IsReady())
                //{
                    JumpToTarget();
                    solvejumptotarget();
                    Rinsec();
                //}
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
        }

        private static void Clear()
        {
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                var target = GetClearMinionsAndBuildings();
                if (CanDoAttack() && target != null)
                {
                    Orbwalking.LastAATick = Utils.GameTimeTickCount + Game.Ping + 200 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                var target = GetClearMinionsAndBuildings();
                if (CanDoAttack() && target != null)
                {
                    Orbwalking.LastAATick = Utils.GameTimeTickCount + Game.Ping + 200 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
        }
        private static void Harass()
        {
            if (enemies.Any() && CanDoAttack())
            {
                var target = enemies.OrderByDescending(x => x.Health).LastOrDefault();
                Orbwalking.LastAATick = Utils.GameTimeTickCount + Game.Ping + 200 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (!enemies.Any() && CanDoAttack())
            {
                var target = _orbwalker.GetTarget();
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    Orbwalking.LastAATick = Utils.GameTimeTickCount + Game.Ping + 200 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
                else
                {
                    var minion = GetClearMinionsAndBuildings();
                    if (minion.IsValidTarget())
                    {
                        Orbwalking.LastAATick = Utils.GameTimeTickCount + Game.Ping + 200 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
                }
            }
            if (_w.IsReady() && CanMove() && !enemies.Any() && wharass)
            {
                var target = TargetSelector.GetTarget(_w.Range + 300, TargetSelector.DamageType.Magical);
                if (target.IsValidTarget() && !target.IsZombie && !enemies.Contains(target))
                {
                    var x = Player.Distance(target.Position) > _w.Range ? Player.Position.Extend(target.Position, _w.Range)
                        : target.Position;
                    _w.Cast(x);
                }
            }
            if (_q.IsReady() && CanMove() && qharass)
            {
                var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                foreach (var obj in soldier)
                {
                    _q.SetSkillshot(0.0f, 65f, 1500f, false, SkillshotType.SkillshotLine, obj.Position, Player.Position);
                    _q.Cast(target);
                }
            }
            if (_w.IsReady() && CanMove() && !enemies.Any() && !soldier.Any() && wharass && Qisready())
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
                Orbwalking.LastAATick = Utils.GameTimeTickCount + Game.Ping + 200 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (!enemies.Any() && CanDoAttack())
            {
                var target = _orbwalker.GetTarget();
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    Orbwalking.LastAATick = Utils.GameTimeTickCount + Game.Ping + 200 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
            if (_w.IsReady() && CanMove() && wcombo)
            {
                var target = TargetSelector.GetTarget(_w.Range + 300, TargetSelector.DamageType.Magical);
                if (target.IsValidTarget() && !target.IsZombie && !enemies.Contains(target))
                {
                    var x = Player.Distance(target.Position) > _w.Range ? Player.Position.Extend(target.Position, _w.Range)
                        : target.Position;
                    _w.Cast(x);
                }
            }
            if (_q.IsReady() && CanMove() && qcombo)
            {
                var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                foreach (var obj in soldier)
                {
                    _q.SetSkillshot(0.0f, 65f, 1500f, false, SkillshotType.SkillshotLine, obj.Position, Player.Position);
                    _q.Cast(target);
                }
            }
            if (_w.IsReady() && CanMove() && !soldier.Any() && wcombo && Qisready())
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
        private static void JumpToMax()
        {
            if (setEjumpMax || setQjumpMax || waitEjumpmax || waitQjumpmax) return;
            if (Eisready && Utils.GameTimeTickCount - qcount >= _q.Instance.Cooldown * 1000)
            {
                if (soldier.Any())
                {
                    var Sold = soldier.OrderByDescending(x => x.Position.Distance(Game.CursorPos)).LastOrDefault();
                    var disSold = Sold.Position.Distance(Game.CursorPos);
                    if (!_w.IsReady())
                    {
                        if (Sold.Position.Distance(Game.CursorPos) < Player.Distance(Game.CursorPos) && Player.Distance(Sold.Position) >= _w.Range)
                        {
                            if (Player.Distance(Sold.Position) <= 1200)
                            {
                                _e.Cast(Sold.Position);
                                setQjumpMax = true;
                                posQjumpMax = Sold.Position.Extend(Game.CursorPos, _q.Range - 600);
                                return;
                            }
                        }
                    }
                    else if (_w.IsReady())
                    {
                   
                        if (Sold.Position.Distance(Game.CursorPos) < Player.Distance(Game.CursorPos) && Player.Distance(Sold.Position) >= _w.Range
                            && Player.Distance(Sold.Position) <= 1200 )
                        {
                            var posW = Player.Position.Extend(Game.CursorPos, _w.Range);
                            if (Game.CursorPos.Distance(Sold.Position) < Game.CursorPos.Distance(posW))
                            {
                                _e.Cast(Sold.Position);
                                setQjumpMax = true;
                                posQjumpMax = Sold.Position.Extend(Game.CursorPos, _q.Range - 600);
                                return;
                            }
                            else
                            {
                                _w.Cast(posW);
                                posEjumpMax = posW;
                                setEjumpMax = true;
                                posQjumpMax = posW.Extend(Game.CursorPos, _q.Range - 600);
                                return;
                            }
                           
                        }
                        else
                        {
                            var posW = Player.Position.Extend(Game.CursorPos, _w.Range);
                            _w.Cast(posW);
                            posEjumpMax = posW;
                            setEjumpMax = true;
                            posQjumpMax = posW.Extend(Game.CursorPos, _q.Range - 600);
                            return;
                        }
                    }

                }
                else if (_w.IsReady())
                {
                    var posW = Player.Position.Extend(Game.CursorPos, _w.Range);
                    _w.Cast(posW);
                    posEjumpMax = posW;
                    setEjumpMax = true;
                    posQjumpMax = posW.Extend(Game.CursorPos, _q.Range - 600);
                    return;
                }
            }
        }
        private static void solvejumptomax()
        {
            if (waitEjumpmax == true)
            {
                _e.Cast(posEjumpMax);
            }
            if (waitQjumpmax == true)
            {
                if (Player.ServerPosition.Distance(posQjumpMax) <= _q.Range - 300)
                {
                    _q.Cast(posQjumpMax);
                }
            }

        }
        private static void JumpTomouse()
        {
            //if (Eisready && Utils.GameTimeTickCount - qcount >= _q.Instance.Cooldown * 1000)
            //{
            //    if (soldier.Any())
            //    {
            //        var Sold = soldier.OrderByDescending(x => x.Position.Distance(Game.CursorPos)).LastOrDefault();
            //        var disSold = Sold.Position.Distance(Game.CursorPos);
            //        if (disSold < _q.Range - 400 || Player.Distance(Sold.Position) > Player.Distance(Game.CursorPos))
            //        {
            //            if (Player.Distance(Sold.Position) <= 1200)
            //            {
            //                _e.Cast(Sold.Position);
            //                setQjumpMouse = true;
            //                return;
            //            }
            //            else if (_w.IsReady())
            //            {
            //                var posW = Player.Position.Extend(Game.CursorPos, _w.Range);
            //                var disW = Player.Distance(Game.CursorPos) - _w.Range;
            //                if (disW < _q.Range - 700)
            //                {
            //                    _w.Cast(posW);
            //                    posEjumpMouse = posW;
            //                    setEjumpMouse = true;
            //                    return;
            //                }
            //            }
            //        }
            //    }
            //    else if (_w.IsReady())
            //    {
            //        var posW = Player.Position.Extend(Game.CursorPos, _w.Range);
            //        var disW = Player.Distance(Game.CursorPos) - _w.Range;
            //        if (disW < _q.Range - 700)
            //        {
            //            _w.Cast(posW);
            //            posEjumpMouse = posW;
            //            setEjumpMouse = true;
            //            return;
            //        }
            //    }
            //}
            if (setEjumpMouse || setQjumpMouse || waitEjumpmouse || waitQjumpmouse) return;
            if (Eisready && Utils.GameTimeTickCount - qcount >= _q.Instance.Cooldown * 1000)
            {
                if (soldier.Any())
                {
                    var Sold = soldier.OrderByDescending(x => x.Position.Distance(Game.CursorPos)).LastOrDefault();
                    var disSold = Sold.Position.Distance(Game.CursorPos);
                    if (!_w.IsReady())
                    {
                        if (Sold.Position.Distance(Game.CursorPos) < Player.Distance(Game.CursorPos) && Player.Distance(Sold.Position) >= _w.Range)
                        {
                            if (Player.Distance(Sold.Position) <= 1200)
                            {
                                _e.Cast(Sold.Position);
                                setQjumpMouse = true;
                                posQjumpMouse = Sold.Position.Distance(Game.CursorPos) > _q.Range - 600 ?
                                                Sold.Position.Extend(Game.CursorPos, _q.Range - 600) :
                                                Game.CursorPos;
                                return;
                            }
                        }
                    }
                    else if (_w.IsReady())
                    {

                        if (Sold.Position.Distance(Game.CursorPos) < Player.Distance(Game.CursorPos) && Player.Distance(Sold.Position) >= _w.Range
                            && Player.Distance(Sold.Position) <= 1200)
                        {
                            var posW = Player.Position.Extend(Game.CursorPos, _w.Range);
                            if (Game.CursorPos.Distance(Sold.Position) < Game.CursorPos.Distance(posW))
                            {
                                _e.Cast(Sold.Position);
                                setQjumpMouse = true;
                                posQjumpMouse = Sold.Position.Distance(Game.CursorPos) > _q.Range - 600 ?
                                                Sold.Position.Extend(Game.CursorPos, _q.Range - 600) :
                                                Game.CursorPos;
                                return;
                            }
                            else
                            {
                                _w.Cast(posW);
                                posEjumpMouse = posW;
                                setEjumpMouse = true;
                                posQjumpMouse = posW.Distance(Game.CursorPos) > _q.Range - 600 ?
                                                posW.Extend(Game.CursorPos, _q.Range - 600) :
                                                Game.CursorPos;
                                return;
                            }

                        }
                        else
                        {
                            var posW = Player.Position.Extend(Game.CursorPos, _w.Range);
                            _w.Cast(posW);
                            posEjumpMouse = posW;
                            setEjumpMouse = true;
                            posQjumpMouse = posW.Distance(Game.CursorPos) > _q.Range - 600 ?
                                            posW.Extend(Game.CursorPos, _q.Range - 600) :
                                            Game.CursorPos;
                            return;
                        }
                    }

                }
                else if (_w.IsReady())
                {
                    var posW = Player.Position.Extend(Game.CursorPos, _w.Range);
                    _w.Cast(posW);
                    posEjumpMouse = posW;
                    setEjumpMouse = true;
                    posQjumpMouse = posW.Distance(Game.CursorPos) > _q.Range - 600 ?
                                    posW.Extend(Game.CursorPos, _q.Range - 600) :
                                    Game.CursorPos;
                    return;
                }
            }
        }
        private static void solvejumptomouse()
        {
            if (waitEjumpmouse == true)
            {
                _e.Cast(posEjumpMouse);
            }
            if (waitQjumpmouse == true)
            {
                if (Player.ServerPosition.Distance(posQjumpMouse) <= _q.Range - 300)
                {
                    _q.Cast(posQjumpMouse);
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
                    if (Geometry.Distance(target.Position.To2D(), Player.Position.To2D(), x.Position.To2D(), true, true) <= target.BoundingRadius + Player.BoundingRadius && Eisready
                        && Player.Distance(x.Position) <= 900)
                    {
                        _e.Cast(x.Position);
                        return;
                    }
                }
                if (Eisready && Utils.GameTimeTickCount - qcount >= _q.Instance.Cooldown *1000)
                {

                    if (soldier.Any())
                    {
                        var Sold = soldier.OrderByDescending(x => x.Position.Distance(target.Position)).LastOrDefault();
                        var disSold = Sold.Position.Distance(target.Position);
                        if (disSold < _q.Range - 400 || Player.Distance(Sold.Position) > Player.Distance(target.Position))
                        {
                            if (Player.Distance(Sold.Position) <= 1200)
                            {
                                _e.Cast(Sold.Position);
                                setQjumpTarget = true;
                                return;
                            }
                            else if (_w.IsReady())
                            {
                                var posW = Player.Position.Extend(target.Position, _w.Range);
                                var disW = Player.Distance(target.Position) - _w.Range;
                                if (disW < _q.Range - 700)
                                {
                                    _w.Cast(posW);
                                    posEjumpTarget = posW;
                                    setEjumpTarget = true;
                                    return;
                                }
                            }
                        }

                    }
                    else if (_w.IsReady())
                    {
                        var posW = Player.Position.Extend(target.Position, _w.Range);
                        var disW = Player.Distance(target.Position) - _w.Range;
                        if (disW < _q.Range - 700)
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
            if (target.IsValidTarget() && !target.IsZombie)
            {
                if (waitEjumpTarget == true)
                {
                    _e.Cast(posEjumpTarget);
                }
                var targetpos = Prediction.GetPrediction(target,Player.Distance(target.Position)/1600).UnitPosition;
                var castposition = Player.Distance(target.Position) > Player.Distance(targetpos) ? target.Position : targetpos;
                if (waitQjumpTarget == true)
                {
                    if (Player.ServerPosition.Distance(castposition) <= _q.Range - 300)
                    {
                        _q.Cast(castposition);
                    }
                }
            }


        }
        private static void Rinsec()
        {
            var mode = insecmode;
            var target = TargetSelector.GetSelectedTarget();
            if (target != null)
            {
                //var targetfuturepos = Prediction.GetPrediction(target, 0.1f).UnitPosition;
                bool caninsec = Player.Distance(target.Position) <= 300;
                switch (mode)
                {
                    case 0:
                        var hero = HeroManager.Allies.Where(x => !x.IsMe && !x.IsDead).OrderByDescending(x => x.Distance(Player.Position)).LastOrDefault();
                        if (hero != null && caninsec && Player.ServerPosition.Distance(hero.Position)+100  >= target.Distance(hero.Position))
                        {
                            var pos = Player.Position.Extend(hero.Position, 250);
                            _r.Cast(pos);
                        }
                        break;
                    case 1:
                        var turret = ObjectManager.Get<Obj_AI_Turret>().Where(x => x.IsAlly && !x.IsDead).OrderByDescending(x => x.Distance(Player.Position)).LastOrDefault();
                        if (turret != null && caninsec && Player.ServerPosition.Distance(turret.Position) + 100 >= target.Distance(turret.Position))
                        {
                            var pos = Player.Position.Extend(turret.Position, 250);
                            _r.Cast(pos);
                        }
                        break;
                    case 2:
                        if (caninsec && Player.ServerPosition.Distance(Game.CursorPos)+100  >= target.Distance(Game.CursorPos))
                        {
                            var pos = Player.Position.Extend(Game.CursorPos, 250);
                            _r.Cast(pos);
                        }
                        break;
                }
            }
        }
        private static void Auto()
        {
            if (RKS)
            {
                if (_r.IsReady())
                {
                    foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(250) && !x.IsZombie && x.Health < _r.GetDamage(x)))
                    {
                        _r.Cast(hero.Position);
                    }
                }
            }
            if(RTOWER)
            {
                if (_r.IsReady())
                {
                    var turret = ObjectManager.Get<Obj_AI_Turret>().Where(x => x.IsAlly && !x.IsDead).OrderByDescending(x => x.Distance(Player.Position)).LastOrDefault();
                    foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(250) && !x.IsZombie))
                    {
                        if (Player.ServerPosition.Distance(turret.Position)+100 >= hero.Distance(turret.Position) && hero.Distance(turret.Position) <= 775 + 250)
                        {
                            var pos = Player.Position.Extend(turret.Position, 250);
                            _r.Cast(pos);
                        }
                    }
                }
            }
        }

        private static void azir()
        {
            String temp = "";
            foreach (var obj in ObjectManager.Get<GameObject>().Where(x => x.Position.Distance(Game.CursorPos) <= 100))
            {
                temp += obj.Name + " ,";
            }
            if (temp != "") Game.Say(temp);
        }
        private static AttackableUnit GetClearMinionsAndBuildings()
        {
            AttackableUnit result = null;
             /*Killable Minion*/

            var MinionList = new List<Obj_AI_Minion>();
            MinionList.AddRange(soldierattackminions);
            MinionList.AddRange(autoattackminions);
            MinionList
                    .Where(
                        minion =>
                            minion.IsValidTarget() && 
                            !Orbwalking.IsWard(minion))
                            .OrderByDescending(minion => minion.CharData.BaseSkinName.Contains("Siege"))
                            .ThenBy(minion => minion.CharData.BaseSkinName.Contains("Super"))
                            .ThenBy(minion => minion.Health)
                            .ThenByDescending(minion => minion.MaxHealth);
            if (_orbwalker.ActiveMode == Orbwalking. OrbwalkingMode.LaneClear ||_orbwalker. ActiveMode ==Orbwalking. OrbwalkingMode.Mixed ||
                   _orbwalker. ActiveMode ==Orbwalking. OrbwalkingMode.LastHit)
            {
                foreach (var minion in MinionList)
                {
                    var t = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2;
                    var predHealth = HealthPrediction.GetHealthPrediction(minion, t, 70);

                    if (minion.Team != GameObjectTeam.Neutral && MinionManager.IsMinion(minion, true))
                    {
                        if (predHealth > 0 && predHealth <= Player.GetAutoAttackDamage(minion, true))
                        {
                            return minion;
                        }
                    }
                }
            }
            if (_orbwalker. ActiveMode ==Orbwalking. OrbwalkingMode.LaneClear)
            {
                /* turrets */
                foreach (var turret in
                    ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsValidTarget() && Orbwalking.InAutoAttackRange(t)))
                {
                    return turret;
                }

                /* inhibitor */
                foreach (var turret in
                    ObjectManager.Get<Obj_BarracksDampener>().Where(t => t.IsValidTarget() && Orbwalking.InAutoAttackRange(t)))
                {
                    return turret;
                }

                /* nexus */
                foreach (var nexus in
                    ObjectManager.Get<Obj_HQ>().Where(t => t.IsValidTarget() && Orbwalking.InAutoAttackRange(t)))
                {
                    return nexus;
                }
            }
            /*Champions*/
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
            {
                var target = _orbwalker.GetTarget();
                if (target is Obj_AI_Hero && target.IsValidTarget())
                    return target;
            }
             /*Jungle minions*/
            if(_orbwalker. ActiveMode ==Orbwalking. OrbwalkingMode.LaneClear ||_orbwalker. ActiveMode ==Orbwalking. OrbwalkingMode.Mixed)
            {
                result =
                    MinionList
                        .Where(
                            mob =>
                                mob.IsValidTarget() && mob.Team == GameObjectTeam.Neutral && mob.CharData.BaseSkinName != "gangplankbarrel")
                        .MaxOrDefault(mob => mob.MaxHealth);
                if (result != null)
                {
                    return result;
                }
            }


            /*Lane Clear minions*/
            if (_orbwalker. ActiveMode ==Orbwalking. OrbwalkingMode.LaneClear)
            {
                if (!ShouldWait())
                {
                    var t = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2;
                    result = (from minion in
                                  MinionList
                                      .Where(minion => minion.IsValidTarget() && minion.CharData.BaseSkinName != "gangplankbarrel")
                              let predHealth =
                                  HealthPrediction.LaneClearHealthPrediction(
                                      minion, (int)((Player.AttackDelay * 1000) * t), 70)
                              where
                                  predHealth >= 2 * Player.GetAutoAttackDamage(minion) ||
                                  Math.Abs(predHealth - minion.Health) < float.Epsilon
                              select minion).MaxOrDefault(m => m.Health);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return result;

        }
        private static bool ShouldWait()
        {
            var t = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2;
            return
            (
                autoattackminions
                    .Any(
                        minion =>
                            minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                            HealthPrediction.LaneClearHealthPrediction(
                                minion, t, 70) <=
                            Player.GetAutoAttackDamage(minion))
                ||
                soldierattackminions
                    .Any(
                        minion =>
                            minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                            HealthPrediction.LaneClearHealthPrediction(
                                minion, t, 70) <=
                            _w.GetDamage(minion))
            );
          
        }
        private static void azirsoldier()
        {
            soldier = new List<GameObject>();
            foreach (var obj in ObjectManager.Get<GameObject>().Where(x => x.Name == "Azir_Base_P_Soldier_Ring.troy"))
            {
                soldier.Add(obj);
            }
            enemies = new List<Obj_AI_Hero>();
            foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie))
            {
                if (soldier.Any(x => x.Position.Distance(hero.Position) <= 300 + hero.BoundingRadius && Player.Distance(x.Position) <= 900))
                    enemies.Add(hero);
            }
            soldierattackminions = new List<Obj_AI_Minion>();
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsValidTarget()))
            {
                if (soldier.Any(x => x.Position.Distance(minion.Position) <= 300 + minion.BoundingRadius && Player.Distance(x.Position) <= 900))
                    soldierattackminions.Add(minion);
            }
            autoattackminions = new List<Obj_AI_Minion>();
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsValidTarget(Orbwalking.GetRealAutoAttackRange(x))))
            {
                if (!soldierattackminions.Any(x => x.NetworkId == minion.NetworkId))
                    autoattackminions.Add(minion);
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
        private static bool  Qisready()
        {
            if (Utils.GameTimeTickCount - Qtick >= _q.Instance.Cooldown * 1000)
            {
                return true;
            }
            else
                return false;
        }
        private static int Qtick;
    }
}
