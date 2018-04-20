﻿using Darkages.Types;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;

namespace Darkages.Storage
{
    public class TemplateStorage<T> where T : Template, new()
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static string StoragePath;

        static TemplateStorage()
        {
            if (ServerContext.StoragePath == null)
                ServerContext.LoadConstants();

            StoragePath = $@"{ServerContext.StoragePath}\templates";

            var tmp = new T();

            StoragePath = Path.Combine(StoragePath, "%");

            if (tmp is SkillTemplate)
                StoragePath = StoragePath.Replace("%", "Skills");

            if (tmp is SpellTemplate)
                StoragePath = StoragePath.Replace("%", "Spells");

            if (tmp is MonsterTemplate)
                StoragePath = StoragePath.Replace("%", "Monsters");

            if (tmp is ItemTemplate)
                StoragePath = StoragePath.Replace("%", "Items");

            if (tmp is MundaneTemplate)
                StoragePath = StoragePath.Replace("%", "Mundanes");

            if (tmp is WorldMapTemplate)
                StoragePath = StoragePath.Replace("%", "WorldMaps");


            if (!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
        }

        public void CacheFromStorage()
        {
            var results = new List<T>();
            var asset_names = Directory.GetFiles(
                StoragePath,
                "*.json",
                SearchOption.TopDirectoryOnly);

            if (asset_names.Length == 0)
                return;

            foreach (var asset in asset_names)
            {
                var tmp = new T();

                if (tmp is SkillTemplate)
                {
                    var template =
                        StorageManager.SkillBucket.Load<SkillTemplate>(Path.GetFileNameWithoutExtension(asset));
                    ServerContext.GlobalSkillTemplateCache[template.Name] = template;

                    logger.Info(" -> {0} Loaded From {1}", template.Name, Path.GetFileName(asset));
                }
                else if (tmp is SpellTemplate)
                {
                    var template =
                        StorageManager.SpellBucket.Load<SpellTemplate>(Path.GetFileNameWithoutExtension(asset));
                    ServerContext.GlobalSpellTemplateCache[template.Name] = template;

                    logger.Info(" -> {0} Loaded From {1}", template.Name, Path.GetFileName(asset));
                }
                else if (tmp is MonsterTemplate)
                {
                    var template =
                        StorageManager.MonsterBucket.Load<MonsterTemplate>(Path.GetFileNameWithoutExtension(asset));
                    ServerContext.GlobalMonsterTemplateCache.Add(template);
                    template.NextAvailableSpawn = DateTime.UtcNow;

                    logger.Info(" -> {0} Loaded From {1}", template.Name, Path.GetFileName(asset));
                }
                else if (tmp is MundaneTemplate)
                {
                    var template =
                        StorageManager.MundaneBucket.Load<MundaneTemplate>(Path.GetFileNameWithoutExtension(asset));
                    ServerContext.GlobalMundaneTemplateCache[template.Name] = template;

                    logger.Info(" -> {0} Loaded From {1}", template.Name, Path.GetFileName(asset));
                }
                else if (tmp is ItemTemplate)
                {
                    var template =
                        StorageManager.ItemBucket.Load<ItemTemplate>(Path.GetFileNameWithoutExtension(asset));
                    ServerContext.GlobalItemTemplateCache[template.Name] = template;

                    logger.Info(" -> {0} Loaded From {1}", template.Name, Path.GetFileName(asset));
                }
                else if (tmp is WorldMapTemplate)
                {
                    var template =
                        StorageManager.WorldMapBucket.Load<WorldMapTemplate>(Path.GetFileNameWithoutExtension(asset));
                    ServerContext.GlobalWorldMapTemplateCache[template.FieldNumber] = template;

                    logger.Info(" -> {0} Loaded From {1}", template.Name, Path.GetFileName(asset));
                }
            }
        }

#pragma warning disable CS0693 
        public T Load<T>(string Name) where T: class, new()
#pragma warning restore CS0693
        {
            var path = Path.Combine(StoragePath, string.Format("{0}.json", Name.ToLower()));

            if (!File.Exists(path))
                return null;

            using (var s = File.OpenRead(path))
            {
                using (var f = new StreamReader(s))
                {
                    return (T)JsonConvert.DeserializeObject<T>(f.ReadToEnd(), StorageManager.Settings);
                }
            }
        }

        public void Save(T obj, bool replace = false)
        {
            if (replace)
            {
                var path = Path.Combine(StoragePath, string.Format("{0}.json", obj.Name.ToLower()));

                if (File.Exists(path))
                    File.Delete(path);

                var objString = JsonConvert.SerializeObject(obj,  Formatting.Indented, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });
                File.WriteAllText(path, objString);
            }
            else
            {
                var path = MakeUnique(Path.Combine(StoragePath, string.Format("{0}.json", obj.Name.ToLower())))
                    .FullName;

                var objString = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });
                File.WriteAllText(path, objString);
            }
        }

        public FileInfo MakeUnique(string path)
        {
            var dir = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var fileExt = Path.GetExtension(path);

            for (var i = 1; ; ++i)
            {
                if (!File.Exists(path))
                    return new FileInfo(path);

                path = Path.Combine(dir, fileName + " " + i + fileExt);
            }
        }
        public void Delete(ItemTemplate obj)
        {
            var path = Path.Combine(StoragePath, string.Format("{0}.json", obj.Name.ToLower()));

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}