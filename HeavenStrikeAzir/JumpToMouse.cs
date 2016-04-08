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
    public static class JumpToMouse
    {
        public static Obj_AI_Hero Player { get{ return ObjectManager.Player; } }
        public static int LastJump;
        public static void Initialize()
        {
            Game.OnUpdate += Game_OnUpdate;
            CustomEvents.Unit.OnDash += Unit_OnDash;
        }

        private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            //if (!sender.IsMe)
            //    return;
            //Game.PrintChat(args.Speed.ToString());
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            //Game.PrintChat(Player.Distance(Game.CursorPos).ToString());
            //Game.PrintChat(GameObjects.EnemyMinions.Count().ToString());
            if (!Program.eqmouse)
                return;
            if (OrbwalkCommands.CanMove())
            {
                OrbwalkCommands.MoveTo(Game.CursorPos);
            }
            if (Environment.TickCount - LastJump < 500)
                return;
            if (Program.Eisready && Program.Qisready())
            {
                var position = Game.CursorPos;
                var distance = Player.Position.Distance(position);
                var sold = Soldiers.soldier
                    .Where(x => Player.Distance(x.Position) <= 1100)
                    .OrderBy(x => x.Position.Distance(Game.CursorPos)).FirstOrDefault();
                var posW = Player.Position.Extend(position, Program._w.Range);
                if (distance < 875)
                {
                    if (sold != null)
                    {
                        Program._e.Cast(sold.Position);
                        Utility.DelayAction.Add(50, () => Program._q.Cast(position));
                        LastJump = Environment.TickCount;
                    }
                    else if (Program._w.IsReady())
                    {
                        Program._w.Cast(posW);
                        Utility.DelayAction.Add(50 + Game.Ping - 8, () => Program._e.Cast(posW));
                        Utility.DelayAction.Add(500 + Game.Ping - 8, () => Program._q.Cast(position));
                        LastJump = Environment.TickCount;
                    }
                }
                else
                {
                    if (sold != null && sold.Position.Distance(position) <= posW.Distance(position))
                    {
                        var time = sold.Position.Distance(Player.Position) * 1000 / 1700;
                        Program._e.Cast(sold.Position);
                        Utility.DelayAction.Add((int)time - 150, () => Program._q.Cast(position));
                        LastJump = Environment.TickCount;
                    }
                    else if (Program._w.IsReady())
                    {
                        Program._w.Cast(posW);
                        Utility.DelayAction.Add(50 + Game.Ping - 8, () => Program._e.Cast(posW));
                        Utility.DelayAction.Add(500 + Game.Ping - 8, () => Program._q.Cast(position));
                        LastJump = Environment.TickCount;
                    }
                }

            }
        }


    }
}
