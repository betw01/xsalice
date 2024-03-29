﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
using SharpDX;
using Color = System.Drawing.Color;

namespace KarthusRForRAWR
{
    internal class Program
    {
        public const string ChampionName = "Karthus";

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static int qWidth = 200;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static Spellbook book = ObjectManager.Player.Spellbook;
        public static SpellDataInst eSpell = book.GetSpell(SpellSlot.E);

        public static Obj_AI_Hero SelectedTarget = null;

        //summoner 
        public static SpellSlot IgniteSlot;

        //Menu
        public static Menu menu;

        private static Obj_AI_Hero Player;

        //mana manager
        public static int[] qMana = { 20, 20 , 26 , 32 , 38 , 44 };
        public static int[] wMana = { 100, 100, 100, 100, 100, 100 };
        public static int[] eMana = { 30, 30 , 42 , 54 , 66 , 78 };
        public static int[] rMana = { 150, 150 , 175 , 200 };

        public static int lastPing = 0;
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
            Q = new Spell(SpellSlot.Q, 875);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 520);
            R = new Spell(SpellSlot.R, float.MaxValue);

            Q.SetSkillshot(.6f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.25f, 50f, 1600f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(3f, float.MaxValue, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

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
                        new KeyBind(menu.Item("Harass_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0],
                        KeyBindType.Toggle)));
            menu.SubMenu("Keys")
               .AddItem(
                   new MenuItem("wTar", "Cast W On Selected").SetValue(new KeyBind("W".ToCharArray()[0],
                       KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("lastHitQ", "Last Hith Q").SetValue(new KeyBind("A".ToCharArray()[0],
                        KeyBindType.Press)));
            menu.SubMenu("Keys")
                .AddItem(
                    new MenuItem("LaneClearActive", "Farm!").SetValue(
                        new KeyBind(menu.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Spell Menu
            menu.AddSubMenu(new Menu("Spell", "Spell"));
            //Q Menu
            menu.SubMenu("Spell").AddSubMenu(new Menu("QSpell", "QSpell"));
            menu.SubMenu("Spell").SubMenu("QSpell").AddItem(new MenuItem("qAA", "Auto Q AAing target").SetValue(new KeyBind("I".ToCharArray()[0], KeyBindType.Toggle)));
            menu.SubMenu("Spell").SubMenu("QSpell").AddItem(new MenuItem("qImmo", "Auto Q Immobile").SetValue(true));
            menu.SubMenu("Spell").SubMenu("QSpell").AddItem(new MenuItem("qDash", "Auto Q Dashing").SetValue(true));
            //W
            menu.SubMenu("Spell").AddSubMenu(new Menu("WSpell", "WSpell"));
            menu.SubMenu("Spell").SubMenu("WSpell").AddItem(new MenuItem("wTower", "Auto W Enemy in Tower").SetValue(true));
            menu.SubMenu("Spell").SubMenu("WSpell").AddItem(new MenuItem("wIfMana", "Only W If Have Mana").SetValue(true));
            //E
            menu.SubMenu("Spell").AddSubMenu(new Menu("ESpell", "ESpell"));
            menu.SubMenu("Spell").SubMenu("ESpell").AddItem(new MenuItem("eManaCombo", "Min Mana Combo").SetValue(new Slider(10, 0, 100)));
            menu.SubMenu("Spell").SubMenu("ESpell").AddItem(new MenuItem("eManaHarass", "Min Mana Harass").SetValue(new Slider(70, 0, 100)));
            //R
            menu.SubMenu("Spell").AddSubMenu(new Menu("RSpell", "RSpell"));
            menu.SubMenu("Spell").SubMenu("RSpell").AddItem(new MenuItem("rPing", "Ping if Enemy Is Killable").SetValue(true));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo")
                .AddItem(
                    new MenuItem("tsModes", "TS Modes").SetValue(
                        new StringList(new[] { "Orbwalker/LessCast", "Low HP%", "NearMouse", "CurrentHP" }, 0)));
            menu.SubMenu("Combo").AddItem(new MenuItem("selected", "Focus Selected Target").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("qHit", "Q HitChance").SetValue(new Slider(3, 1, 4)));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("ignite", "Use Ignite").SetValue(true));
            menu.SubMenu("Combo")
                .AddItem(new MenuItem("igniteMode", "Mode").SetValue(new StringList(new[] { "Combo", "KS" }, 0)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("qHit2", "Q HitChance").SetValue(new Slider(3, 1, 4)));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("minMana", "Min Mana >").SetValue(new Slider(60, 0, 100)));

            //Farming menu:
            menu.AddSubMenu(new Menu("Farm", "Farm"));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseInt", "Use E to Interrupt").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("UseGap", "Use E for GapCloser").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));

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
            menu.SubMenu("Drawings").AddItem(new MenuItem("drawUlt", "Killable With ult").SetValue(true));
            menu.SubMenu("Drawings")
                .AddItem(dmgAfterComboItem);
            menu.AddToMainMenu();

            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Game.PrintChat(ChampionName + " Loaded! --- by xSalice");
        }

        static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            GamePacket g = new GamePacket(args.PacketData);
            if (g.Header != 0xFE)
                return;

            if (menu.Item("qAA").GetValue<KeyBind>().Active)
            {
                if (Packet.MultiPacket.OnAttack.Decoded(args.PacketData).Type == Packet.AttackTypePacket.TargetedAA)
                {
                    g.Position = 1;
                    var k = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(g.ReadInteger());
                    if (k is Obj_AI_Hero && k.IsEnemy)
                    {
                        if (Vector3.Distance(k.Position, Player.Position) <= Q.Range)
                        {
                            Q.Cast(k.Position, packets());
                        }
                    }
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            double damage = 0d;

            
            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E) * 2;

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            if (R.IsReady())
                damage += getUltDmg((Obj_AI_Hero) enemy);

            return (float)damage;
        }

        private static void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), false, "Combo");
        }

        private static void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false, "Harass");
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, string Source)
        {
            Obj_AI_Hero target = getTarget();
            bool hasmana = manaCheck();
            float dmg = GetComboDamage(target);
            int IgniteMode = menu.Item("igniteMode").GetValue<StringList>().SelectedIndex;
            var minManaHarass = menu.Item("minMana").GetValue<Slider>().Value;
            Obj_AI_Hero eTar = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (Source == "Harass" && getManaPercentage() < minManaHarass)
                return;

            if (target == null)
                return;

            Q.Width = 1;

            //W
            if (useW  && W.IsReady() && Player.Distance(target) <= W.Range && shouldW(target) &&
                W.GetPrediction(target).Hitchance >= HitChance.High)
            {
                W.Cast(target, packets());
            }

            //Q
            if (useQ && Q.IsReady())
            {
                var qPred = Q.GetPrediction(target);

                if (qPred.Hitchance >= getHit(Source))
                    Q.Cast(qPred.CastPosition);

            }

            //E
            if (useE && E.IsReady() && Player.Distance(eTar) < E.Range && eSpell.ToggleState == 1 && hasManaForE(Source))
            {
                E.Cast(packets());
            }

            //Ignite
            if (menu.Item("ignite").GetValue<bool>() && IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && Source == "Combo" && hasmana)
            {
                if (IgniteMode == 0 && dmg > target.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                }
            }
        }

        public static bool shouldW(Obj_AI_Hero target)
        {
            if (menu.Item("wIfMana").GetValue<bool>() && manaCheck())
                return true;

            return false;
        }

        public static float getManaPercentage()
        {
            return Player.Mana/Player.MaxMana*100;
        }

        public static float getUltDmg(Obj_AI_Hero target)
        {
            double dmg = 0;

            dmg += Player.GetSpellDamage(target, SpellSlot.R);

            dmg -= target.HPRegenRate* 3.25;

            if (Items.HasItem(3155, target))
            {
                dmg = dmg - 250;
            }

            if (Items.HasItem(3156, target))
            {
                dmg = dmg - 400;
            }

            return (float) dmg;
        }

        public static void drawEnemyKillable()
        {
            int kill = 0;

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            x =>!x.IsDead && x.IsEnemy && x.IsVisible))
            {
                if (getUltDmg(enemy) > enemy.Health - 30)
                {
                    if (menu.Item("rPing").GetValue<bool>() && Environment.TickCount - lastPing > 15000)
                    {
                        Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(enemy.ServerPosition.X,
                            enemy.ServerPosition.Y, 0, 0, Packet.PingType.Normal)).Process();
                        lastPing = Environment.TickCount;

                    }
                    kill++;
                }
            }

            if (kill > 0)
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(wts[0] - 100, wts[1], Color.Red, "RAWR Killable with R: " + kill);
            }
            else
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(wts[0] - 100, wts[1], Color.White, "RAWR Killable with R: " + kill);
            }
        }


        public static bool hasManaForE(string source)
        {
            var eManaCombo = menu.Item("eManaCombo").GetValue<Slider>().Value;
            var eManaHarass = menu.Item("eManaHarass").GetValue<Slider>().Value;

            if (source == "Combo" && getManaPercentage() > eManaCombo)
                return true;

            if (source == "Harass" && getManaPercentage() > eManaHarass)
                return true;

            return false;
        }

        public static void autoQ()
        {
            var qDashing = menu.Item("qImmo").GetValue<bool>();
            var qImmo = menu.Item("qDash").GetValue<bool>();

            if (!Q.IsReady())
                return;

            if (!qDashing && !qImmo)
                return;

            foreach (
                var target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            x => !x.IsDead && x.IsEnemy && x.IsVisible))
            {
                if (Q.GetPrediction(target).Hitchance == HitChance.Immobile && qImmo && Player.Distance(target) < Q.Range)
                {
                    Q.Cast(target, packets());
                    return;
                }

                if (Q.GetPrediction(target).Hitchance == HitChance.Dashing && qDashing && Player.Distance(target) < Q.Range)
                {
                    Q.Cast(target, packets());
                }
            }
        }

        public static HitChance getHit(string Source)
        {
            var hitC = HitChance.High;
            var qHit = menu.Item("qHit").GetValue<Slider>().Value;
            var harassQHit = menu.Item("qHit2").GetValue<Slider>().Value;

            // HitChance.Low = 3, Medium , High .... etc..
            if (Source == "Combo")
            {
                switch (qHit)
                {
                    case 1:
                        hitC = HitChance.Low;
                        break;
                    case 2:
                        hitC = HitChance.Medium;
                        break;
                    case 3:
                        hitC = HitChance.High;
                        break;
                    case 4:
                        hitC = HitChance.VeryHigh;
                        break;
                }
            }
            else if (Source == "Harass")
            {
                switch (harassQHit)
                {
                    case 1:
                        hitC = HitChance.Low;
                        break;
                    case 2:
                        hitC = HitChance.Medium;
                        break;
                    case 3:
                        hitC = HitChance.High;
                        break;
                    case 4:
                        hitC = HitChance.VeryHigh;
                        break;
                }
            }

            return hitC;
        }

        public static void checkUnderTower()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x =>Player.Distance(x) < W.Range && x.IsValidTarget(W.Range) && !x.IsDead && x.IsEnemy &&x.IsVisible))
            {
                if (enemy != null)
                {
                    foreach (var turret in ObjectManager.Get<Obj_AI_Turret>())
                    {
                        if (turret != null && turret.IsValid && turret.IsAlly && turret.Health > 0)
                        {
                            if (Vector2.Distance(enemy.Position.To2D(), turret.Position.To2D()) < 750 && W.IsReady())
                            {
                                var vec = enemy.ServerPosition +
                                          Vector3.Normalize(enemy.ServerPosition - Player.ServerPosition)*100;

                                W.Cast(vec, packets());
                                return;
                            }
                        }
                    }
                }
            }
        }

        public static Obj_AI_Hero getTarget()
        {
            int tsMode = menu.Item("tsModes").GetValue<StringList>().SelectedIndex;
            var focusSelected = menu.Item("selected").GetValue<bool>();

            var range = W.Range;

            if (!W.IsReady())
                range = Q.Range;

            Obj_AI_Hero getTar = SimpleTs.GetTarget(range, SimpleTs.DamageType.Magical);

            SelectedTarget = (Obj_AI_Hero)Hud.SelectedUnit;

            if (focusSelected && SelectedTarget != null && SelectedTarget.IsEnemy && SelectedTarget.Type == GameObjectType.obj_AI_Hero)
            {
                if (Player.Distance(SelectedTarget) < range && !SelectedTarget.IsDead && SelectedTarget.IsVisible &&
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

            if (getTar != null)
            {
                LXOrbwalker.ForcedTarget = getTar;
                Hud.SelectedUnit = getTar;
                return getTar;
            }

            return null;
        }

        public static void smartKS()
        {
            if (!menu.Item("smartKS").GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => Player.Distance(x) < Q.Range && x.IsValidTarget() && x.IsEnemy && !x.IsDead))
            {
                //Q
                if (Player.Distance(target.ServerPosition) <= Q.Range &&
                    (Player.GetSpellDamage(target, SpellSlot.Q)) > target.Health + 30)
                {
                    if (Q.IsReady())
                    {
                        Q.Cast(target, packets());
                        return;
                    }
                }

                //E
                if (Player.Distance(target.ServerPosition) <= E.Range && eSpell.ToggleState == 1 &&
                    (Player.GetSpellDamage(target, SpellSlot.E)) > target.Health + 30)
                {
                    if (E.IsReady())
                    {
                        E.Cast(packets());
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

        public static void checkEState()
        {
            if (eSpell.ToggleState == 1)
                return;

            var target = ObjectManager.Get<Obj_AI_Hero>().Count(x => Player.Distance(x) < E.Range && x.IsValidTarget(E.Range) && !x.IsDead && x.IsEnemy &&
                                                                     x.IsVisible);

            //return if Target in range
            if (target > 1)
                return;

            //check if around minion
            if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range,
                MinionTypes.All, MinionTeam.NotAlly);

                if (allMinionsE.Count > 0)
                    return;
            }

            if (E.IsReady())
                E.Cast();
        }

        public static bool manaCheck()
        {
            int totalMana = qMana[Q.Level] + wMana[W.Level] + eMana[E.Level] + rMana[R.Level];

            if (Player.Mana >= totalMana)
                return true;

            return false;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsChannelingImportantSpell())
                return;
            
           
            smartKS();

            autoQ();

            checkEState();

            if (menu.Item("wTower").GetValue<bool>())
                checkUnderTower();


            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                
                if (menu.Item("lastHitQ").GetValue<KeyBind>().Active)
                    lastHitQ();

                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm();

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();
            }

            if (menu.Item("wTar").GetValue<KeyBind>().Active)
            {
                var target = (Obj_AI_Hero)Hud.SelectedUnit;

                if (target != null && target.IsEnemy && target.Type == GameObjectType.obj_AI_Hero)
                {
                    if (W.GetPrediction(target).Hitchance >= HitChance.High)
                        W.Cast(target, packets());
                }
            }
        }

        public static void lastHitQ()
        {
            if (!Q.IsReady())
                return;

            List<Obj_AI_Base> allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                Q.Range + qWidth, MinionTypes.All, MinionTeam.NotAlly);

            if (allMinionsQ.Count > 0)
            {
                foreach (var minion in allMinionsQ)
                {
                    var health = HealthPrediction.GetHealthPrediction(minion, 700);

                    Q.Width = 200;
                    var qPred = Q.GetCircularFarmLocation(allMinionsQ);

                    if (qPred.MinionsHit == 1)
                    {
                        if (Player.GetSpellDamage(minion, SpellSlot.Q) - 15 > health)
                            Q.Cast(minion,packets());
                    }
                    else
                    {
                        if (Player.GetSpellDamage(minion, SpellSlot.Q, 1) - 15 > health)
                            Q.Cast(minion,packets());
                    }
                }
            }
        }
        public static bool packets()
        {
            return menu.Item("packet").GetValue<bool>();
        }

        private static void Farm()
        {
            List<Obj_AI_Base> allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                Q.Range + qWidth, MinionTypes.All, MinionTeam.NotAlly);
            List<Obj_AI_Base> allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range,
                MinionTypes.All, MinionTeam.NotAlly);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
            var useE = menu.Item("UseEFarm").GetValue<bool>();

            if (useQ && Q.IsReady() && allMinionsQ.Count > 0)
            {
                Q.Width = 200;
                MinionManager.FarmLocation qPos = Q.GetCircularFarmLocation(allMinionsQ);

                if (qPos.MinionsHit > 1)
                    Q.Cast(qPos.Position, packets());
            }

            if (useE && allMinionsE.Count > 0 && E.IsReady() && eSpell.ToggleState == 1)
            {
                MinionManager.FarmLocation ePos = E.GetCircularFarmLocation(allMinionsE);

                if (ePos.MinionsHit > 1)
                    E.Cast(ePos.Position, packets());
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (Spell spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            if(R.IsReady() && menu.Item("drawUlt").GetValue<bool>())
                drawEnemyKillable();
        }


        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!menu.Item("UseGap").GetValue<bool>()) return;

            if (W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range))
                W.Cast(gapcloser.Sender, packets());
        }

    }
}