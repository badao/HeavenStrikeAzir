using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;

namespace Rengar
{
    class Program
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static Orbwalking.Orbwalker Orbwalker;

        private static Spell Q, W, E, R;

        private static Menu Menu;

        private static string mode { get { return Menu.Item("ComboMode").GetValue<StringList>().SelectedValue; } }
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Rengar")
                return;

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W,0);
            E = new Spell(SpellSlot.E,1000);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0.25f, 70, 1500, true, SkillshotType.SkillshotLine);
            E.MinHitChance = HitChance.Medium;
            W.SetSkillshot(0.25f, 500, 2000, false, SkillshotType.SkillshotCircle);
            W.MinHitChance = HitChance.Medium;
            //Q.SetSkillshot(300, 50, 2000, false, SkillshotType.SkillshotLine);


            Menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Rengar.Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);
            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);

            Menu spellMenu = Menu.AddSubMenu(new Menu("Spells", "Spells"));
            spellMenu.AddItem(new MenuItem("ComboMode", "ComboMode").SetValue(new StringList(new[] { "Snare", "Burst"},0)));
            Menu Clear = spellMenu.AddSubMenu(new Menu("Clear","Clear"));
            Clear.AddItem(new MenuItem("useQ", "use Q").SetValue(true));
            Clear.AddItem(new MenuItem("useE", "use E").SetValue(true));
            Clear.AddItem(new MenuItem("useW", "use W").SetValue(true));
            Clear.AddItem(new MenuItem("Save", "Save 5  FEROCITY").SetValue(false));
            Menu auto = Menu.AddSubMenu(new Menu("Misc", "Misc"));
            auto.AddItem(new MenuItem("AutoHeal","Auto W if HP <").SetValue(new Slider(20,0,100)));
            auto.AddItem(new MenuItem("Interrupt", "Interrupt with E").SetValue(true));


            Menu.AddToMainMenu();

            //Drawing.OnDraw += Drawing_OnDraw;

            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += AfterAttack;
            Orbwalking.OnAttack += OnAttack;
            Obj_AI_Base.OnProcessSpellCast += oncast;
            CustomEvents.Unit.OnDash += Unit_OnDash;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Game.PrintChat("Welcome to Rengar World");
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Interrupt && Player.Mana == 5 && E.IsReady())
            {
                if (sender.IsValidTarget(E.Range))
                {
                    E.Cast(sender);
                }
            }
        }
        private static bool useQ { get { return Menu.Item("useQ").GetValue<bool>(); } }
        private static bool useE { get { return Menu.Item("useE").GetValue<bool>(); } }
        private static bool useW { get { return Menu.Item("useW").GetValue<bool>(); } }
        private static bool Save { get { return Menu.Item("Save").GetValue<bool>(); } }
        private static int Heal { get { return Menu.Item("AutoHeal").GetValue<Slider>().Value; } }
        private static bool Interrupt { get { return Menu.Item("Interrupt").GetValue<bool>(); } }
        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;
            //checkbuff();
            KillSteall();
            if(Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                combo();
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                clear();
            }
        }
        public static void OnAttack(AttackableUnit unit, AttackableUnit target)
        {

            if (unit.IsMe && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (ItemData.Youmuus_Ghostblade.GetItem().IsReady())
                    ItemData.Youmuus_Ghostblade.GetItem().Cast();
            }
        }
        public static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (mode == "Snare" && Player.Mana == 5)
                {
                    if (HasItem())
                        CastItem();
                }
                else if (Q.IsReady())
                {
                    Q.Cast();
                }
                else
                {
                    if (HasItem())
                        CastItem();
                }
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (Player.Mana < 5 || (Player.Mana == 5 && !Save))
                {
                    if (Q.IsReady() && useQ)
                    {
                        Q.Cast();
                    }
                    else
                    {
                        if (HasItem())
                            CastItem();
                    }
                }
                else
                {
                    if (HasItem())
                        CastItem();
                }
            }
        }
        public static void oncast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var spell = args.SData;
            if (!sender.IsMe)
                return;
            //Game.Say(spell.Name);
            if (spell.Name.ToLower().Contains("rengarq"))
            {
                //Game.PrintChat("reset");
                Orbwalking.ResetAutoAttackTimer();
            }
            //if (spell.Name.ToLower().Contains("rengarw")) ;
            //if (spell.Name.ToLower().Contains("rengare")) ;
        }
        public static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (!sender.IsMe)
                return;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && HasItem())
            {
                Utility.DelayAction.Add(
                               (int)(/*Player.AttackCastDelay * 1000 + */args.Duration - 40 - Game.Ping/2), () => CastItem());
            }
            //Game.Say("dash");
        }


        public static void combo()
        {
            if (!Player.HasBuff("RengarR"))
            {
                if (mode == "Snare")
                {
                    if (Player.Mana < 5)
                    {
                        var targetW = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                        if (W.IsReady() && targetW.IsValidTarget() && !targetW.IsZombie)
                        {
                            W.Cast(targetW);
                        }
                        var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                        {
                            E.Cast(targetE);
                        }
                        foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                        {
                            if (E.IsReady())
                                E.Cast(target);
                        }
                    }
                    else
                    {
                        var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                        {
                            E.Cast(targetE);
                        }
                        foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                        {
                            if (E.IsReady())
                                E.Cast(target);
                        }
                    }
                }
                else if (mode == "Burst")
                {
                    if (Player.Mana < 5)
                    {
                        var targetW = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                        if (W.IsReady() && targetW.IsValidTarget() && !targetW.IsZombie)
                        {
                            W.Cast(targetW);
                        }
                        var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                        {
                            E.Cast(targetE);
                        }
                        foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                        {
                            if (E.IsReady())
                                E.Cast(target);
                        }
                    }
                    else if (Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) == 0)
                    {
                        var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                        {
                            E.Cast(targetE);
                        }
                        foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                        {
                            if (E.IsReady())
                                E.Cast(target);
                        }
                    }
                }
                else Game.Say("stupid");
            }
        }
        public static void clear()
        {
            if (Player.Mana < 5 || (Player.Mana == 5 && !Save))
            {
                var targetW1 = MinionManager.GetMinions(Player.Position, 500, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                var targetE1 = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                var targetW2 = MinionManager.GetMinions(Player.Position, 500, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                var targetE2 = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (W.IsReady() && targetW1 != null && useW)
                {
                    W.Cast(targetW1);
                }
                if (W.IsReady() && targetW2 != null && useW)
                {
                    W.Cast(targetW2);
                }
                if (E.IsReady() && targetE1 != null && useE)
                {
                    E.Cast(targetE1);
                }
                if (E.IsReady() && targetE2 != null && useE)
                {
                    E.Cast(targetE2);
                }
            }
        }
        public static void KillSteall()
        {
            if (Player.Health*100/Player.MaxHealth <= Heal && Player.Mana == 5 && W.IsReady())
            {
                W.Cast();
            }
            if (W.IsReady())
            {
                foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(500) && !x.IsZombie))
                {
                    if (target.Health <= W.GetDamage(target))
                        W.Cast(target);
                }
            }
            if (E.IsReady())
            {
                foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                {
                    if (target.Health <= W.GetDamage(target))
                        E.Cast(target);
                }
            }
        }
        public static bool HasItem()
        {
            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady() || ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void CastItem()
        {

            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady())
                ItemData.Tiamat_Melee_Only.GetItem().Cast();
            if (ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
                ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
        }
        public static void checkbuff()
        {
            String temp = "";
            foreach (var buff in Player.Buffs)
            {
                temp += (buff.Name + "(" + buff.Count + ")" + ", ");
            }
            Game.Say(temp);
        }
    }
}
