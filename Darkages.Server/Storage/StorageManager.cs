﻿using Darkages.Network.Game;
using Darkages.Network.Object;
using Darkages.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Darkages.Storage
{
    public class StorageManager
    {     
        
        public static JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            //NullValueHandling = NullValueHandling.Ignore,
            //ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //ObjectCreationHandling = ObjectCreationHandling.Auto,
            Formatting = Formatting.Indented,
            //TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        };

        public static AislingStorage AislingBucket = new AislingStorage();
        public static AreaStorage AreaBucket = new AreaStorage();
        public static WarpStorage WarpBucket = new WarpStorage();

        public static TemplateStorage<SkillTemplate> SkillBucket = new TemplateStorage<SkillTemplate>();
        public static TemplateStorage<SpellTemplate> SpellBucket = new TemplateStorage<SpellTemplate>();
        public static TemplateStorage<ItemTemplate> ItemBucket = new TemplateStorage<ItemTemplate>();
        public static TemplateStorage<MonsterTemplate> MonsterBucket = new TemplateStorage<MonsterTemplate>();
        public static TemplateStorage<MundaneTemplate> MundaneBucket = new TemplateStorage<MundaneTemplate>();
        public static TemplateStorage<WorldMapTemplate> WorldMapBucket = new TemplateStorage<WorldMapTemplate>();

        static StorageManager()
        {
        }

        public static T Load<T>() where T : class, new()
        {
            try
            {
                var obj = new T();

                if (obj is ServerConstants)
                {
                    var StoragePath = $@"{ServerContext.StoragePath}\lorule_config";
                    var path = Path.Combine(StoragePath, string.Format("{0}.json", "global"));

                    if (!File.Exists(path))
                        return null;

                    using (var s = File.OpenRead(path))
                    using (var f = new StreamReader(s))
                    {
                        return JsonConvert.DeserializeObject<T>(f.ReadToEnd(), Settings);
                    }
                }

                if (obj is GameServer)
                {
                    var StoragePath = $@"{ServerContext.StoragePath}\context";
                    var path = Path.Combine(StoragePath, string.Format("{0}.json", "context"));

                    if (!File.Exists(path))
                        return null;

                    using (var s = File.OpenRead(path))
                    using (var f = new StreamReader(s))
                    {
                        var objd = JsonConvert.DeserializeObject<GameServer>(f.ReadToEnd(), Settings);
                        return objd as T;
                    }
                }

                if (obj is ObjectService)
                {
                    var StoragePath = $@"{ServerContext.StoragePath}\states";
                    var path = Path.Combine(StoragePath, string.Format("{0}.json", "state_objcache"));

                    if (!File.Exists(path))
                        return null;

                    using (var s = File.OpenRead(path))
                    using (var f = new StreamReader(s))
                    {
                        return JsonConvert.DeserializeObject<T>(f.ReadToEnd(), Settings);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string Save<T>(T obj)
        {
            try
            {
                if (obj is ServerConstants)
                {
                    var StoragePath = $@"{ServerContext.StoragePath}\lorule_config";

                    if (!Directory.Exists(StoragePath))
                        Directory.CreateDirectory(StoragePath);

                    var path = Path.Combine(StoragePath, string.Format("{0}.json", "global"));
                    var objString = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });

                    File.WriteAllText(path, objString);
                    return objString;
                }

                if (obj is ObjectService)
                {
                    var StoragePath = $@"{ServerContext.StoragePath}\states";

                    if (!Directory.Exists(StoragePath))
                        Directory.CreateDirectory(StoragePath);

                    var path = Path.Combine(StoragePath, string.Format("{0}.json", "state_objcache"));
                    var objString = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });

                    File.WriteAllText(path, objString);
                    return objString;
                }

                if (obj is GameServer)
                {
                    var StoragePath = $@"{ServerContext.StoragePath}\context";

                    if (!Directory.Exists(StoragePath))
                        Directory.CreateDirectory(StoragePath);

                    var path = Path.Combine(StoragePath, string.Format("{0}.json", "context"));
                    var objString = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });

                    File.WriteAllText(path, objString);
                    return objString;
                }


                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}