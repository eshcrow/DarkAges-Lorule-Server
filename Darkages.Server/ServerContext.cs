﻿///************************************************************************
//Project Lorule: A Dark Ages Server (http://darkages.creatorlink.net/index/)
//Copyright(C) 2018 TrippyInc Pty Ltd
//
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
//*************************************************************************/
using Darkages.Common;
using Darkages.Network.Game;
using Darkages.Network.Object;
using Darkages.Script.Context;
using Darkages.Storage;
using Darkages.Storage.locales.Buffs;
using Darkages.Types;
using Mono.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Class = Darkages.Types.Class;

namespace Darkages
{
    public class ServerContext : ObjectManager
    {
        internal static volatile object SyncObj = new object();

        public static int Errors, DefaultPort;

        public static bool Running, Paused;

        public static GameServer Game;

        public static ServerConstants Config;

        public static IPAddress Ipaddress => IPAddress.Parse(File.ReadAllText("server.tbl"));

        public static string GlobalMessage { get; internal set; }

        public static string StoragePath = @"..\..\..\LORULE_DATA";

        public static List<Metafile> GlobalMetaCache = new List<Metafile>();

        public static Dictionary<int, Area> GlobalMapCache =
            new Dictionary<int, Area>();

        public static List<MonsterTemplate> GlobalMonsterTemplateCache =
            new List<MonsterTemplate>();

        public static Dictionary<string, SkillTemplate> GlobalSkillTemplateCache =
            new Dictionary<string, SkillTemplate>();

        public static Dictionary<string, SpellTemplate> GlobalSpellTemplateCache =
            new Dictionary<string, SpellTemplate>();

        public static Dictionary<string, ItemTemplate> GlobalItemTemplateCache =
            new Dictionary<string, ItemTemplate>();

        public static Dictionary<string, MundaneTemplate> GlobalMundaneTemplateCache =
            new Dictionary<string, MundaneTemplate>();

        public static List<WarpTemplate> GlobalWarpTemplateCache =
            new List<WarpTemplate>();

        public static Dictionary<int, WorldMapTemplate> GlobalWorldMapTemplateCache =
            new Dictionary<int, WorldMapTemplate>();

        public static Dictionary<string, Reactor> GlobalReactorCache =
            new Dictionary<string, Reactor>();

        public static Dictionary<string, Buff> GlobalBuffCache 
            = new Dictionary<string, Buff>();

        public static Dictionary<string, Debuff> GlobalDeBuffCache
            = new Dictionary<string, Debuff>();

        public static Dictionary<string, Aisling> ConnectedBots
            = new Dictionary<string, Aisling>();

        public static Board[] Community = new Board[7];

        public static void Log(string message, params object[] args)
        {
            Console.WriteLine(string.Format(message, args));
        }


        public static void LoadSkillTemplates()
        {
            StorageManager.SkillBucket.CacheFromStorage();
            Log("Skill Templates Loaded: {0}", GlobalSkillTemplateCache.Count);
        }

        public static void LoadSpellTemplates()
        {
            StorageManager.SpellBucket.CacheFromStorage();
            Log("Spell Templates Loaded: {0}", GlobalSpellTemplateCache.Count);
        }

        public static void LoadItemTemplates()
        {
            StorageManager.ItemBucket.CacheFromStorage();
            Log("Item Templates Loaded: {0}", GlobalItemTemplateCache.Count);
        }

        public static void LoadMonsterTemplates()
        {
            StorageManager.MonsterBucket.CacheFromStorage();
            Log("Monster Templates Loaded: {0}", GlobalMonsterTemplateCache.Count);
        }

        public static void LoadMundaneTemplates()
        {
            StorageManager.MundaneBucket.CacheFromStorage();
            Log("Mundane Templates Loaded: {0}", GlobalMundaneTemplateCache.Count);
        }

        public static void LoadWarpTemplates()
        {
            StorageManager.WarpBucket.CacheFromStorage();
            Log("Warp Templates Loaded: {0}", GlobalWarpTemplateCache.Count);
        }

        public static void LoadWorldMapTemplates()
        {
            StorageManager.WorldMapBucket.CacheFromStorage();
            Log("World Map Templates Loaded: {0}", GlobalWorldMapTemplateCache.Count);
        }

        public static void LoadMaps()
        {
            StorageManager.AreaBucket.CacheFromStorage();
            Log("Map Templates Loaded: {0}", GlobalMapCache.Count);
        }

        private static void StartServers()
        {
#if DEBUG
            ServerContext.Config.DebugMode = true;
#endif

            redo:
            {
                if (Errors > Config.ERRORCAP)
                    Process.GetCurrentProcess().Kill();

                try
                {

                    Game = new GameServer(Config.ConnectionCapacity);
                    Game.Start(DefaultPort);
                }
                catch (Exception)
                {
                    { ++DefaultPort; Errors++; }
                    goto redo;
                }
            }

        
            Running = true;
        }

        /// <summary>
        ///     EP
        /// </summary>
        public virtual void Start()
        {
            Startup();
        }

        public virtual void Shutdown()
        {
            Running = false;
            {
                EmptyCacheCollectors();
                Game?.Abort();
                Game = null;
            }
        }

        public static void Startup()
        {
            Log(Config.SERVER_TITLE + " Server: Starting up...");
            Log("----------------------------------------------");
            {
                try
                {
                    LoadConstants();
                    LoadAndCacheStorage();
                    InitScriptEvaluators();
                    StartServers();
                }
                catch (Exception)
                {
                    Log("Startup Error.");
                }
            }
            Log("----------------------------------------------");
            Log("{0} World Server: Online.", Config.SERVER_TITLE);
        }


        private static void EmptyCacheCollectors()
        {
            GlobalItemTemplateCache = new Dictionary<string, ItemTemplate>();
            GlobalMapCache = new Dictionary<int, Area>();
            GlobalMetaCache = new List<Metafile>();
            GlobalMonsterTemplateCache = new List<MonsterTemplate>();
            GlobalMundaneTemplateCache = new Dictionary<string, MundaneTemplate>();
            GlobalSkillTemplateCache = new Dictionary<string, SkillTemplate>();
            GlobalSpellTemplateCache = new Dictionary<string, SpellTemplate>();
            GlobalWarpTemplateCache = new List<WarpTemplate>();
            GlobalWorldMapTemplateCache = new Dictionary<int, WorldMapTemplate>();
            GlobalReactorCache = new Dictionary<string, Reactor>();
            GlobalBuffCache = new Dictionary<string, Buff>();
            GlobalDeBuffCache = new Dictionary<string, Debuff>();
        }

        public static void LoadConstants()
        {
            var _config_ = StorageManager.Load<ServerConstants>();

            if (_config_ == null)
            {
                Log("No config found. Generating defaults.");
                Config = new ServerConstants();
                StorageManager.Save(Config);
            }
            else
            {
                Config = StorageManager.Load<ServerConstants>();
            }

            InitFromConfig();
        }

        public static void InitFromConfig()
        {
            DefaultPort = Config.SERVER_PORT;
            {
                if (!Directory.Exists(StoragePath))
                    Directory.CreateDirectory(StoragePath);
            }
        }

        public static void LoadMetaDatabase()
        {
            Log("Loading Meta Database");
            {
                var files = MetafileManager.GetMetafiles();

                if (files.Count > 0)
                    GlobalMetaCache.AddRange(files);
            }
            Log("Building Meta Cache: {0} loaded.", GlobalMetaCache.Count);
        }

        public static void SaveCommunityAssets()
        {
            List<Board> tmp;

            lock (Community)
            {
                tmp = new List<Board>(Community);
            }

            foreach (var asset in tmp)
            {
                asset.Save();
            }
        }

        public static void CacheCommunityAssets()
        {
            if (Community != null)
            {
                var boards = Board.CacheFromStorage();

                if (boards.Find(i => i.Index == 0) == null)
                {
                    boards.Add(new Board("Mail", 0, true));
                }

                Community = boards.OrderBy(i => i.Index).ToArray();
            }
        }

        public static void LoadAndCacheStorage()
        {
            Paused = true;
            EmptyCacheCollectors();

            lock (SyncObj)
            {
                LoadMaps();
                LoadSkillTemplates();
                LoadSpellTemplates();
                LoadItemTemplates();
                LoadMonsterTemplates();
                LoadMundaneTemplates();
                LoadWarpTemplates();
                LoadWorldMapTemplates();
                CacheCommunityAssets();
                BindTemplates();
                LoadMetaDatabase();
                LoadExtensions();
            }

           

            var c = new SkillTemplate();

            c.Name = "Claw Fist";
            c.Icon = 59;
            c.Pane = Pane.Spells;
            c.NpcKey = "Kung-Fu Master";
            c.LevelRate = 0.03;
            c.Group = "Attack Bonus";
            c.MaxLevel = 100;
            c.MissAnimation = 33;
            c.Name = "Claw Fist";
            c.PostQualifers = PostQualifer.BreakInvisible;
            c.ScriptName = "Claw Fist";
            c.Sound = 21;
            c.TargetAnimation = 0;
            c.TierLevel = Tier.Tier1;
            c.FailMessage = "failed.";
            c.Description = "Doubles all unarmed assail damage for it's duration.";
            c.Type = SkillScope.Ability;
            c.Buff = new buff_clawfist();
            c.Cooldown = 10;
            c.Prerequisites = new LearningPredicate()
            {
                Class_Required = Class.Monk,
                Con_Required = 30,
                Dex_Required = 32,
                Gold_Required = 50000,
                ExpLevel_Required = 29,
                Skill_Required = "Double Punch",
                Skill_Level_Required = 70,
                Items_Required = new List<ItemPredicate>()
                {
                    new ItemPredicate()
                    {
                         AmountRequired = 1,
                         Item = "Gargoyle Fiend's Skull"
                    },
                    new ItemPredicate()
                    {
                         AmountRequired = 1,
                         Item = "Frog's Leg"
                    },
                },
            };

            GlobalSkillTemplateCache["Claw Fist"] = c;

            GlobalBuffCache["Claw Fist"] = new buff_clawfist();


            GlobalItemTemplateCache["Iron Gauntlet"] = new ItemTemplate()
            {
                Name = "Iron Gauntlet",
                AcModifer = new StatusOperator(StatusOperator.Operator.Add, 2),
                ManaModifer = new StatusOperator(StatusOperator.Operator.Remove, 200),
                LevelRequired = 33,
                Class = Class.Peasant,
                CanStack = false,
                MaxDurability = 10000,
                Value = 6000,
                DropRate = 0.19,
                ScriptName = "Gauntlet",
                EquipmentSlot = (ItemSlots.LArm),
                CarryWeight = 3,
                Description = "Sturdy Iron Gauntlets, However heavy, Reduces -1000 mana to the bearer.",
                DisplayImage = 0x80F9,
                Image = 0x80F9,
                NpcKey = "Alf Stewert"
            };

            //SyncStorage();

            Paused = false;
        }

        private static void SyncStorage()
        {
            foreach (var obj in GlobalItemTemplateCache.Select(i => i.Value))
                StorageManager.ItemBucket.SaveOrReplace(obj);

            foreach (var obj in GlobalSpellTemplateCache.Select(i => i.Value))
                StorageManager.SpellBucket.SaveOrReplace(obj);

            foreach (var obj in GlobalSkillTemplateCache.Select(i => i.Value))
                StorageManager.SkillBucket.SaveOrReplace(obj);

            foreach (var obj in GlobalMonsterTemplateCache)
                StorageManager.MonsterBucket.SaveOrReplace(obj);

            foreach (var obj in GlobalMundaneTemplateCache.Select(i => i.Value))
                StorageManager.MundaneBucket.SaveOrReplace(obj);
        }

        private static void LoadExtensions()
        {
            CacheBuffs();
            Log("Building Buff Cache: {0} loaded.", GlobalBuffCache.Count);
            CacheDebuffs();
            Log("Building Debuff Cache: {0} loaded.", GlobalDeBuffCache.Count);
        }

        private static void InitScriptEvaluators()
        {
            Log("Loading Script Evaluator...");

            try
            {
                InitScriptEvaluator();
                Log("Loading Script Evaluator... Success");
            }
            catch
            {
                Log("Loading Script Evaluator... Error.");
            }
        }

        public static void InitScriptEvaluator()
        {
            Evaluator.Init(new string[0]);
            var assembly = Assembly.Load("Darkages.Server");
            Evaluator.ReferenceAssembly(assembly);

            @"  using Darkages.Common;
                using Darkages.Network.Game;
                using Darkages.Network.Object;
                using Darkages.Script.Context;
                using Darkages.Storage;
                using Darkages.Types;
                using System;
                using System.Collections.Generic;
                using System.Diagnostics;
                using System.IO;
                using System.Linq;
                using System.Net;
                using System.Reflection;
                using System.Threading.Tasks;".Run();

            Context.Items["game"] = Game;
        }

        private static void CacheDebuffs()
        {
            var listOfDebuffs = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                 from assemblyType in domainAssembly.GetTypes()
                                 where typeof(Debuff).IsAssignableFrom(assemblyType)
                                 select assemblyType).ToArray();

            foreach (var debuff in listOfDebuffs)
            {
                GlobalDeBuffCache[debuff.Name] = (Debuff)Activator.CreateInstance(debuff);
            }
        }

        private static void CacheBuffs()
        {
            var listOfBuffs = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                               from assemblyType in domainAssembly.GetTypes()
                               where typeof(Buff).IsAssignableFrom(assemblyType)
                               select assemblyType).ToArray();

            foreach (var buff in listOfBuffs)
            {
                GlobalBuffCache[buff.Name] = (Buff)Activator.CreateInstance(buff);
            }
        }

        private static void BindTemplates()
        {
            foreach (var spell in ServerContext.GlobalSpellTemplateCache.Values)
                spell.Prerequisites?.AssociatedWith(spell);
            foreach (var skill in ServerContext.GlobalSkillTemplateCache.Values)
                skill.Prerequisites?.AssociatedWith(skill);
        }
    }
}
