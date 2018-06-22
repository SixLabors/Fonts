// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tables
{
    internal class TableLoader
    {
        public TableLoader()
        {
            // we will hard code mapping registration in here for all the tables
            this.Register(NameTable.Load);
            this.Register(CMapTable.Load);
            this.Register(HeadTable.Load);
            this.Register(HoizontalHeadTable.Load);
            this.Register(HorizontalMetricsTable.Load);
            this.Register(MaximumProfileTable.Load);
            this.Register(OS2Table.Load);
            this.Register(IndexLocationTable.Load);
            this.Register(GlyphTable.Load);
            this.Register(KerningTable.Load);
        }

        public static TableLoader Default { get; } = new TableLoader();

        private readonly Dictionary<string, Func<FontReader, Table>> loaders = new Dictionary<string, Func<FontReader, Table>>();
        private readonly Dictionary<Type, string> types = new Dictionary<Type, string>();
        private readonly Dictionary<Type, Func<FontReader, Table>> typesLoaders = new Dictionary<Type, Func<FontReader, Table>>();

        public string GetTag(Type type)
        {
            if (this.types.ContainsKey(type))
            {
                return this.types[type];
            }

            return null;
        }

        internal IEnumerable<Type> RegisterdTypes()
        {
            return this.types.Keys;
        }

        internal IEnumerable<string> RegisterdTags()
        {
            return this.types.Values;
        }

        private void Register<T>(string tag, Func<FontReader, T> createFunc)
            where T : Table
        {
            lock (this.loaders)
            {
                if (!this.loaders.ContainsKey(tag))
                {
                    this.loaders.Add(tag, createFunc);
                    this.types.Add(typeof(T), tag);
                    this.typesLoaders.Add(typeof(T), createFunc);
                }
            }
        }

        private void Register<T>(Func<FontReader, T> createFunc)
            where T : Table
        {
            string name =
                typeof(T).GetTypeInfo()
                    .CustomAttributes
                    .First(x => x.AttributeType == typeof(TableNameAttribute))
                    .ConstructorArguments[0].Value.ToString();

            this.Register(name, createFunc);
        }

        internal Table Load(string tag, FontReader reader)
        {
            // loader missing register an unknow type loader and carry on
            if (!this.loaders.ContainsKey(tag))
            {
                return new UnknownTable(tag);
            }

            return this.loaders[tag]?.Invoke(reader);
        }

        internal TTable Load<TTable>(FontReader reader)
            where TTable : Table
        {
            // loader missing register an unknow type loader and carry on
            if (this.typesLoaders.ContainsKey(typeof(TTable)))
            {
                return (TTable)this.typesLoaders[typeof(TTable)]?.Invoke(reader);
            }

            throw new Exception("font table not registered");
        }
    }
}