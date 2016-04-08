using System;
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
    public static class Insec
    {
        public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public static int LastJump;
        public static Vector3 LastLeftClick = new Vector3();
        public static Vector3 InsecPoint = new Vector3();
        public static void Initialize()
        {
            Game.OnUpdate += Game_OnUpdate;
            CustomEvents.Unit.OnDash += Unit_OnDash;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Program.drawinsecLine)
                return;
            var target = TargetSelector.GetSelectedTarget();
            if (target.IsValidTarget() && !target.IsZombie)
            {
                Render.Circle.DrawCircle(target.Position, 100, Color.Yellow);
                if (InsecPoint.IsValid())
                {
                    var point = target.Distance(InsecPoint) >= 400 ?
                        target.Position.Extend(InsecPoint, 400) : InsecPoint;
                    Drawing.DrawLine(Drawing.WorldToScreen(target.Position)
                        , Drawing.WorldToScreen(point), 3, Color.Red);
                }
            }
            if (InsecPoint.IsValid())
                Render.Circle.DrawCircle(InsecPoint, 100, Color.Pink);
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.WM_KEYDOWN && args.WParam == Program.insecpointkey)
            {
                LastLeftClick = Game.CursorPos;
            }

        }

        private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            switch (Program.insecmode)
            {
                case 0:
                    var hero = HeroManager.Allies.Where(x => !x.IsMe && !x.IsDead)
                        .OrderByDescending(x => x.Distance(Player.Position)).LastOrDefault();
                    if (hero != null)
                        InsecPoint = hero.Position;
                    break;
                case 1:
                    var turret = GameObjects.AllyTurrets.OrderByDescending(x => x.Distance(Player.Position)).LastOrDefault();
                    if (turret != null)
                        InsecPoint = turret.Position;
                    break;
                case 2:
                    InsecPoint = Game.CursorPos;
                    break;
                case 3:
                    InsecPoint = LastLeftClick;
                    break;
            }
            if (!Program.insec)
                return;
            if (OrbwalkCommands.CanMove())
            {
                OrbwalkCommands.MoveTo(Game.CursorPos);
            }
            if (Environment.TickCount - LastJump < 1500)
                return;
            if (!InsecPoint.IsValid())
                return;
            var target = TargetSelector.GetSelectedTarget();
            if (!target.IsValidTarget() || target.IsZombie)
                return;
            if (!Program._r2.IsReady())
                return;

            //case 2
            var sold2 = Soldiers.soldier
                    .Where(x => Player.Distance(x.Position) <= 1100)
                    .OrderBy(x => x.Position.Distance(target.Position)).FirstOrDefault();
            if (sold2 != null && Program.Eisready)
            {
                Vector2 start2 = sold2.Position.To2D().Extend(InsecPoint.To2D(), -0);
                Vector2 end2 = start2.Extend(Player.Position.To2D(), 750);
                float width2 = Program._r.Level == 3 ? 125 * 6 / 2 :
                            Program._r.Level == 2 ? 125 * 5 / 2 :
                            125 * 4 / 2;
                var Rect2 = new Geometry.Polygon.Rectangle(start2, end2, width2 - 100);
                var Predicted2 = Prediction.GetPrediction(target,
                    Game.Ping / 1000f + Player.Distance(sold2.Position) / 1700f - 0.15f).UnitPosition;
                if (Rect2.IsInside(target.Position) && Rect2.IsInside(Predicted2))
                {
                    var time = sold2.Position.Distance(Player.Position) * 1000 / 1700;
                    Program._e.Cast(sold2.Position);
                    Utility.DelayAction.Add((int)time + 150, () => Program._r2.Cast(InsecPoint));
                    Utility.DelayAction.Add((int)time - 150, () => Program._q2.Cast(InsecPoint));
                    LastJump = Environment.TickCount;
                    return;
                }
            }

            //case 3
            var posW3 = Player.Position.Extend(target.Position, Program._w.Range);
            if (Program.Eisready && Program._w.IsReady())
            {
                Vector2 start3 = posW3.To2D().Extend(InsecPoint.To2D(), 0);
                Vector2 end3 = start3.Extend(Player.Position.To2D(), 750);
                float width3 = Program._r.Level == 3 ? 125 * 6 / 2 :
                            Program._r.Level == 2 ? 125 * 5 / 2 :
                            125 * 4 / 2;
                var Rect3 = new Geometry.Polygon.Rectangle(start3, end3, width3 - 100);
                var Predicted3 = Prediction.GetPrediction(target,
                    Game.Ping / 1000f + Player.Distance(posW3) / 1700f - 0.15f).UnitPosition;
                if (Rect3.IsInside(target.Position) && Rect3.IsInside(Predicted3))
                {
                    var time = posW3.Distance(Player.Position) * 1000 / 1700;
                    Program._w.Cast(posW3);
                    Utility.DelayAction.Add(50 + Game.Ping - 8, () => Program._e.Cast(posW3));
                    Utility.DelayAction.Add(500 + Game.Ping - 8 + 150, () => Program._r2.Cast(InsecPoint));
                    Utility.DelayAction.Add(500 + Game.Ping - 8 - 150, () => Program._q2.Cast(InsecPoint));
                    LastJump = Environment.TickCount;
                    return;
                }
            }

            //case 1
            Vector2 start1 = Player.Position.To2D().Extend(InsecPoint.To2D(), -300);
            Vector2 end1 = start1.Extend(Player.Position.To2D(), 750);
            float width1 = Program._r.Level == 3 ? 125 * 6 / 2 :
                        Program._r.Level == 2 ? 125 * 5 / 2 :
                        125 * 4 / 2;
            var Rect1 = new Geometry.Polygon.Rectangle(start1, end1, width1 - 100);
            var Predicted1 = Prediction.GetPrediction(target, Game.Ping / 1000f + 0.25f).UnitPosition;
            if (Rect1.IsInside(target.Position) && Rect1.IsInside(Predicted1))
            {
                Program._r2.Cast(InsecPoint);
                LastJump = Environment.TickCount;
                return;
            }

            //case 4
            if (Program.Eisready && Program.Qisready())
            {
                var sold4 = Soldiers.soldier
                    .Where(x => Player.Distance(x.Position) <= 1100)
                    .OrderBy(x => x.Position.Distance(target.Position)).FirstOrDefault();
                var posW4 = Player.Position.Extend(target.Position, Program._w.Range);
                if (sold4 != null && sold4.Position.Distance(target.Position) <= posW4.Distance(target.Position))
                {
                    var time = (Player.Distance(sold4.Position) + sold4.Position.Distance(target.Position)) / 1700f;
                    var Predicted4 = Prediction.GetPrediction(target,
                        Game.Ping / 1000f + time - 0.15f).UnitPosition;
                    if (target.Distance(sold4.Position) <= 875 - 100)
                    {
                        Vector2 start4 = target.Position.To2D().Extend(InsecPoint.To2D(), -300);
                        Vector2 end4 = start4.Extend(Player.Position.To2D(), 750);
                        float width4 = Program._r.Level == 3 ? 125 * 6 / 2 :
                                    Program._r.Level == 2 ? 125 * 5 / 2 :
                                    125 * 4 / 2;
                        var Rect4 = new Geometry.Polygon.Rectangle(start4, end4, width4 - 100);
                        if (Rect4.IsInside(Predicted4))
                        {
                            var timetime = sold4.Position.Distance(Player.Position) * 1000 / 1700;
                            Program._e.Cast(sold4.Position);
                            Utility.DelayAction.Add((int)timetime - 150, () => Program._q2.Cast(target.Position));
                            Utility.DelayAction.Add((int)(time * 1000) + 300 - 150, () => Program._r2.Cast(InsecPoint));
                            LastJump = Environment.TickCount;
                            return;
                        }
                    }
                }
                else if (Program._w.IsReady())
                {
                    var time = Player.Distance(target.Position) / 1700f;
                    var Predicted4 = Prediction.GetPrediction(target,
                        Game.Ping / 1000f + time - 0.15f).UnitPosition;
                    if (target.Distance(Player.Position) <= 875 + 450 - 100)
                    {
                        Vector2 start4 = target.Position.To2D().Extend(InsecPoint.To2D(), -300);
                        Vector2 end4 = start4.Extend(Player.Position.To2D(), 750);
                        float width4 = Program._r.Level == 3 ? 125 * 6 / 2 :
                                    Program._r.Level == 2 ? 125 * 5 / 2 :
                                    125 * 4 / 2;
                        var Rect4 = new Geometry.Polygon.Rectangle(start4, end4, width4 - 100);
                        if (Rect4.IsInside(Predicted4))
                        {
                            Program._w.Cast(posW4);
                            Utility.DelayAction.Add(50 + Game.Ping - 8, () => Program._e.Cast(posW4));
                            Utility.DelayAction.Add((int)(time * 1000) + 300 + Game.Ping - 8, () => Program._r2.Cast(InsecPoint));
                            Utility.DelayAction.Add(500 + Game.Ping - 8, () => Program._q2.Cast(target.Position));
                            LastJump = Environment.TickCount;
                            return;
                        }
                    }
                }
            }


        }
    }
}
