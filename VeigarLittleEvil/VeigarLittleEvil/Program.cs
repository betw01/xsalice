﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
using SharpDX;
using Color = System.Drawing.Color;

namespace VeigarLittleEvil
{
    internal class Program
    {
        public const string ChampionName = "Veigar";

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static Obj_AI_Hero SelectedTarget = null;

        //item and summoner
        public static Items.Item Dfg;
        public static SpellSlot IgniteSlot;

        //mana manager
        public static int[] qMana = {60, 60, 65, 70, 75, 80};
        public static int[] wMana = {70, 70, 80, 90, 100, 110};
        public static int[] eMana = {80, 80, 90, 100, 110, 120};
        public static int[] rMana = {125, 125, 175, 225};

        //Menu
        public static Menu menu;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            //check to see if correct champ
            if (Player.BaseSkinName != ChampionName) return;

            //intalize spell
            Q = new Spell(SpellSlot.Q, 650);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 1005);
            R = new Spell(SpellSlot.R, 650);

            W.SetSkillshot(1.25f, 230f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(.2f, 330f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            Dfg = Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline ||
                  Utility.Map.GetMap()._MapType == Utility.Map.MapType.CrystalScar
                ? new Items.Item(3188, 750)
                : new Items.Item(3128, 750);

            //Create the menu
            menu = new Menu(ChampionName, ChampionName, true);

            //Orbwalker submenu
            var orbwalkerMenu = new Menu("My Orbwalker", "my_Orbwalker");
            LXOrbwalker.AddToMenu(orbwalkerMenu);
            menu.AddSubMenu(orbwalkerMenu);

            //Target selector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);


            //Keys
            menu.AddSubMenu(new Menu("Keys", "Keys"));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(menu.Item("Combo_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(
                        new KeyBind(menu.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0],
                        KeyBindType.Toggle)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("LastHitQQ", "Last hit with Q").SetValue(new KeyBind("A".ToCharArray()[0],
                        KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("LastHitQQ2", "Last hit with Q(Togg)").SetValue(new KeyBind("J".ToCharArray()[0],
                        KeyBindType.Toggle)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("wPoke", "Cast W Only on Stun").SetValue(new KeyBind("N".ToCharArray()[0],
                        KeyBindType.Toggle)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("escape", "Escape").SetValue(new KeyBind(
                        menu.Item("Flee_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo")
                .AddItem(
                    new MenuItem("tsModes", "TS Modes").SetValue(
                        new StringList(new[] {"Orbwalker/LessCast", "Low HP%", "NearMouse", "CurrentHP"}, 0)));
            menu.SubMenu("Combo").AddItem(new MenuItem("selected", "Focus Selected Target").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("waitW", "Wait For W to E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("dfg", "Use DFG").SetValue(true));
            menu.SubMenu("Combo")
                .AddItem(new MenuItem("dfgMode", "Mode").SetValue(new StringList(new[] { "Combo", "DFG-R" }, 0)));
            menu.SubMenu("Combo").AddItem(new MenuItem("ignite", "Use Ignite").SetValue(true));
            menu.SubMenu("Combo")
                .AddItem(new MenuItem("igniteMode", "Mode").SetValue(new StringList(new[] {"Combo", "KS"}, 0)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("mana", "If Mana > %").SetValue(new Slider(75, 0, 100)));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use R to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use W for GapCloser").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("overKill", "Over Kill Check").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));

            menu.SubMenu("Misc").AddSubMenu(new Menu("Dont use R on", "DontUlt"));

            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                menu.SubMenu("Misc")
                    .SubMenu("DontUlt")
                    .AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

            menu.SubMenu("Misc").AddSubMenu(new Menu("Dont use DFG on", "DontDFG"));

            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                menu.SubMenu("Misc")
                    .SubMenu("DontDFG")
                    .AddItem(new MenuItem("DontDFG" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

            //Damage after combo:
            MenuItem dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };

            //Drawings menu:
            menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("manaStatus", "Mana status").SetValue(true));
            menu.SubMenu("Drawings")
                .AddItem(dmgAfterComboItem);
            menu.AddToMainMenu();

            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            double damage = 0d;

            if (Dfg.IsReady())
                damage += Player.GetItemDamage(enemy, Damage.DamageItems.Dfg)/1.2;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);

            if (Dfg.IsReady())
                damage = damage*1.2;

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            if (Items.HasItem(3155, (Obj_AI_Hero)enemy))
            {
                damage = damage - 250;
            }

            if (Items.HasItem(3156, (Obj_AI_Hero)enemy))
            {
                damage = damage - 400;
            }
            return (float) damage;
        }

        private static void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), "Combo");
        }

        private static void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false, "Harass");
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, string Source)
        {
            Obj_AI_Hero target = getTarget();

            int IgniteMode = menu.Item("igniteMode").GetValue<StringList>().SelectedIndex;
            int dfgMode = menu.Item("dfgMode").GetValue<StringList>().SelectedIndex;

            bool hasMana = manaCheck();

            float dmg = GetComboDamage(target);
            bool waitW = menu.Item("waitW").GetValue<bool>();

            if (Source == "Harass")
            {
                int mana = menu.Item("mana").GetValue<Slider>().Value;
                float manaPercent = Player.Mana/Player.MaxMana*100;

                if (manaPercent < mana)
                    return;
            }

            if (useE && target != null && E.IsReady() && Player.Distance(target) < E.Range)
            {
                if (!waitW || W.IsReady())
                {
                    castE(target);
                    return;
                }
            }

            if (useW && target != null && Player.Distance(target) <= W.Range)
            {
                if (menu.Item("wPoke").GetValue<KeyBind>().Active)
                {
                    var pred = W.GetPrediction(target);
                    if (pred.Hitchance == HitChance.Immobile && W.IsReady())
                        W.Cast(target.ServerPosition, Packets());
                }
                else if(W.IsReady())
                {
                    PredictionOutput pred = Prediction.GetPrediction(target, 1.25f);
                    if (pred.Hitchance >= HitChance.High && W.IsReady())
                        W.Cast(pred.CastPosition, Packets());
                }
            }

            //dfg
            if (target != null && Dfg.IsReady() && menu.Item("dfg").GetValue<bool>() && dfgMode == 0 && 
                GetComboDamage(target) > target.Health + 30 && Source == "Combo" && hasMana)
            {
                if ((menu.Item("DontDFG" + target.BaseSkinName) != null &&
                     menu.Item("DontDFG" + target.BaseSkinName).GetValue<bool>() == false))
                    Dfg.Cast(target);
            }

            //Ignite
            if (target != null && menu.Item("ignite").GetValue<bool>() && IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && Source == "Combo" && hasMana)
            {
                if (IgniteMode == 0 && dmg > target.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                }
            }

            //Q
            if (useQ && Q.IsReady() && Player.Distance(target) <= Q.Range && target != null)
            {
                Q.CastOnUnit(target, Packets());
            }

            //R
            if (target != null && R.IsReady())
            {
                useR = rTarget(target) && useR;
                if (useR)
                {
                    castR(target, dmg);
                }
            }
        }

        public static Obj_AI_Hero getTarget()
        {
            int tsMode = menu.Item("tsModes").GetValue<StringList>().SelectedIndex;
            var focusSelected = menu.Item("selected").GetValue<bool>();

            var range = E.Range;

            Obj_AI_Hero getTar = SimpleTs.GetTarget(range, SimpleTs.DamageType.Magical);

            SelectedTarget = (Obj_AI_Hero)Hud.SelectedUnit;

            if (focusSelected && SelectedTarget != null && SelectedTarget.IsEnemy && SelectedTarget.Type == GameObjectType.obj_AI_Hero)
            {
                if (Player.Distance(SelectedTarget) < 1300 && !SelectedTarget.IsDead && SelectedTarget.IsVisible &&
                    SelectedTarget.IsValidTarget())
                {
                    //Game.PrintChat("focusing selected target");
                    LXOrbwalker.ForcedTarget = SelectedTarget;
                    return SelectedTarget;
                }

                SelectedTarget = null;
                return getTar;
            }

            if (tsMode == 0)
            {
                Hud.SelectedUnit = getTar;
                return getTar;
            }

            foreach (
                Obj_AI_Hero target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            x =>
                                Player.Distance(x) < range && x.IsValidTarget(range) && !x.IsDead && x.IsEnemy &&
                                x.IsVisible))
            {
                if (tsMode == 1)
                {
                    float tar1hp = target.Health / target.MaxHealth * 100;
                    float tar2hp = getTar.Health / getTar.MaxHealth * 100;
                    if (tar1hp < tar2hp)
                        getTar = target;
                }

                if (tsMode == 2)
                {
                    if (target.Distance(Game.CursorPos) < getTar.Distance(Game.CursorPos))
                        getTar = target;
                }

                if (tsMode == 3)
                {
                    if (target.Health < getTar.Health)
                        getTar = target;
                }
            }

            Hud.SelectedUnit = getTar;
            LXOrbwalker.ForcedTarget = getTar;
            return getTar;
        }

        public static void smartKS()
        {
            if (!menu.Item("smartKS").GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => Player.Distance(x) < 900 && x.IsValidTarget() && x.IsEnemy && !x.IsDead))
            {
                if (target != null)
                {
                    //Q
                    if (Player.Distance(target.ServerPosition) <= Q.Range &&
                        (Player.GetSpellDamage(target, SpellSlot.Q)) > target.Health + 30)
                    {
                        if (Q.IsReady())
                        {
                            Q.CastOnUnit(target, Packets());
                            return;
                        }
                    }

                    //R
                    if (Player.Distance(target.ServerPosition) <= R.Range &&
                        (Player.GetSpellDamage(target, SpellSlot.R)) > target.Health + 50)
                    {
                        if (R.IsReady() && rTarget(target))
                        {
                            R.CastOnUnit(target, Packets());
                            return;
                        }
                    }

                    if ((menu.Item("DontDFG" + target.BaseSkinName) != null &&
                         menu.Item("DontDFG" + target.BaseSkinName).GetValue<bool>() == false))
                    {
                        //dfg + Q + R
                        if (Dfg.IsReady() && Q.IsReady() && R.IsReady() && Player.Distance(target.ServerPosition) <= 750 &&
                            Player.Distance(target.ServerPosition) < Q.Range &&
                            Player.GetItemDamage(target, Damage.DamageItems.Dfg) +
                            ((Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.R))*
                             1.2) >
                            target.Health + 60)
                        {
                            if (rTarget(target))
                            {
                                Dfg.Cast(target);
                                Q.CastOnUnit(target, Packets());
                                R.CastOnUnit(target, Packets());
                                return;
                            }
                        }

                        //dfg + Q
                        if (Dfg.IsReady() && Q.IsReady() && Player.Distance(target.ServerPosition) <= 750 &&
                            Player.Distance(target.ServerPosition) < Q.Range &&
                            Player.GetItemDamage(target, Damage.DamageItems.Dfg) +
                            (Player.GetSpellDamage(target, SpellSlot.Q)*1.2) > target.Health + 30)
                        {
                            Dfg.Cast(target);
                            Q.CastOnUnit(target, Packets());
                            return;
                        }

                        //dfg + R
                        if (Dfg.IsReady() && R.IsReady() && Player.Distance(target.ServerPosition) <= 750 &&
                            Player.Distance(target.ServerPosition) < R.Range &&
                            Player.GetItemDamage(target, Damage.DamageItems.Dfg) +
                            (Player.GetSpellDamage(target, SpellSlot.R)*1.2) > target.Health + 50)
                        {
                            if (rTarget(target))
                            {
                                Dfg.Cast(target);
                                R.CastOnUnit(target, Packets());
                                return;
                            }
                        }

                        //dfg
                        if (Dfg.IsReady() && Player.GetItemDamage(target, Damage.DamageItems.Dfg) > target.Health + 30 &&
                            Player.Distance(target.ServerPosition) <= 750)
                        {
                            Dfg.Cast(target);
                            return;
                        }

                    }

                    //ignite
                    if (target != null && menu.Item("ignite").GetValue<bool>() && IgniteSlot != SpellSlot.Unknown &&
                        Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                        Player.Distance(target.ServerPosition) <= 600)
                    {
                        int IgniteMode = menu.Item("igniteMode").GetValue<StringList>().SelectedIndex;
                        if (Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health + 20)
                        {
                            Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                        }
                    }
                }
            }
        }

        public static bool rTarget(Obj_AI_Hero target)
        {
            if ((menu.Item("DontUlt" + target.BaseSkinName) != null &&
                 menu.Item("DontUlt" + target.BaseSkinName).GetValue<bool>() == false))
                return true;
            return false;
        }

        public static bool manaCheck()
        {
            int totalMana = qMana[Q.Level] + wMana[W.Level] + eMana[E.Level] + rMana[R.Level];

            if (Player.Mana >= totalMana)
                return true;

            return false;
        }

        public static void castE(Obj_AI_Hero target)
        {
            PredictionOutput pred = Prediction.GetPrediction(target, E.Delay);
            Vector2 castVec = pred.UnitPosition.To2D() -
                              Vector2.Normalize(pred.UnitPosition.To2D() - Player.Position.To2D())*E.Width;

            if (pred.Hitchance >= HitChance.High && E.IsReady())
            {
                E.Cast(castVec);
            }
        }

        public static void castR(Obj_AI_Hero target, float dmg)
        {
            if (menu.Item("overKill").GetValue<bool>() && Player.GetSpellDamage(target, SpellSlot.Q) > target.Health)
                return;

            if (Player.Distance(target) > R.Range)
                return;

            //dfg + R
            if (Dfg.IsReady() && R.IsReady() && Player.Distance(target.ServerPosition) < R.Range &&
                Player.GetItemDamage(target, Damage.DamageItems.Dfg) +
                (Player.GetSpellDamage(target, SpellSlot.R) * 1.2) > target.Health + 50)
            {
                Dfg.Cast(target);
                R.CastOnUnit(target, Packets());
                return;
            }

            if (dmg > target.Health + 20 && R.IsReady())
            {
                R.CastOnUnit(target, Packets());
                return;
            }
        }

        public static void lastHit()
        {
            if (!Orbwalking.CanMove(40)) return;

            List<Obj_AI_Base> allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

            if (Q.IsReady())
            {
                foreach (Obj_AI_Base minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        HealthPrediction.GetHealthPrediction(minion, (int)((minion.Distance(Player) / 1500) * 1000 + .25f * 1000), 100) <
                        Player.GetSpellDamage(minion, SpellSlot.Q) - 35)
                    {
                        if (Q.IsReady())
                        {
                            Q.Cast(minion, Packets());
                            return;
                        }
                    }
                }
            }
        }

        public static Obj_AI_Hero GetNearestEnemy(Obj_AI_Hero unit)
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsEnemy && x.IsValid)
                .OrderBy(x => unit.ServerPosition.Distance(x.ServerPosition))
                .FirstOrDefault();
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            smartKS();

            if (menu.Item("escape").GetValue<KeyBind>().Active)
            {
                if (E.IsReady())
                    castE(GetNearestEnemy(Player));
                LXOrbwalker.Orbwalk(Game.CursorPos, null);
            }
            else if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("LastHitQQ").GetValue<KeyBind>().Active)
                {
                    lastHit();
                }

                if (menu.Item("LastHitQQ2").GetValue<KeyBind>().Active)
                {
                    lastHit();
                }

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();
            }
        }

        public static bool Packets()
        {
            return menu.Item("packet").GetValue<bool>();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (Spell spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            if (menu.Item("manaStatus").GetValue<bool>())
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);

                if (manaCheck())
                    Drawing.DrawText(wts[0] - 30, wts[1], Color.White, "Mana Rdy");
                else
                    Drawing.DrawText(wts[0] - 30, wts[1], Color.White, "No Mana Full Combo");
            }
        }

        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!menu.Item("UseGap").GetValue<bool>()) return;

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
                castE((Obj_AI_Hero) gapcloser.Sender);
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!menu.Item("UseInt").GetValue<bool>()) return;

            if (Player.Distance(unit) < E.Range && unit != null && E.IsReady())
            {
                castE((Obj_AI_Hero) unit);
            }
        }
    }
}