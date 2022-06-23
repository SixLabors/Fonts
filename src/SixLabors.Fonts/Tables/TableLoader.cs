// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.Cff;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Tables.TrueType.Hinting;

namespace SixLabors.Fonts.Tables
{
    internal class TableLoader
    {
        private readonly Dictionary<string, Func<FontReader, Table?>> loaders = new();
        private readonly Dictionary<Type, string> types = new();
        private readonly Dictionary<Type, Func<FontReader, Table?>> typesLoaders = new();

        public TableLoader()
        {
            // We will hard code mapping registration in here for all the tables
            this.Register(NameTable.Load);
            this.Register(CMapTable.Load);
            this.Register(HeadTable.Load);
            this.Register(HorizontalHeadTable.Load);
            this.Register(HorizontalMetricsTable.Load);
            this.Register(VerticalHeadTable.Load);
            this.Register(VerticalMetricsTable.Load);
            this.Register(MaximumProfileTable.Load);
            this.Register(OS2Table.Load);
            this.Register(IndexLocationTable.Load);
            this.Register(GlyphTable.Load);
            this.Register(KerningTable.Load);
            this.Register(ColrTable.Load);
            this.Register(CpalTable.Load);
            this.Register(GPosTable.Load);
            this.Register(GSubTable.Load);
            this.Register(CvtTable.Load);
            this.Register(FpgmTable.Load);
            this.Register(PrepTable.Load);
            this.Register(GlyphDefinitionTable.Load);
            this.Register(PostTable.Load);
            this.Register(Cff1Table.Load);
            this.Register(Cff2Table.Load);
            this.Register(FVarTable.Load);
            this.Register(AVarTable.Load);
            this.Register(GVarTable.Load);
        }

        public static TableLoader Default { get; } = new();

        public string? GetTag(Type type)
        {
            this.types.TryGetValue(type, out string? value);

            return value;
        }

        public string GetTag<TType>()
        {
            this.types.TryGetValue(typeof(TType), out string? value);
            return value!;
        }

        internal IEnumerable<Type> RegisteredTypes() => this.types.Keys;

        internal IEnumerable<string> RegisteredTags() => this.types.Values;

        private void Register<T>(string tag, Func<FontReader, T?> createFunc)
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

        private void Register<T>(Func<FontReader, T?> createFunc)
            where T : Table
        {
            // TODO: static abstract members in interfaces when available.
            // https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-abstract-interface-methods
            string? name =
                typeof(T).GetTypeInfo()
                    .CustomAttributes
                    .First(x => x.AttributeType == typeof(TableNameAttribute))
                    .ConstructorArguments[0].Value!.ToString();

            this.Register(name!, createFunc);
        }

        internal Table? Load(string tag, FontReader reader)

             // loader missing? register an unknown type loader and carry on
             => this.loaders.TryGetValue(tag, out Func<FontReader, Table?>? func)
                ? func.Invoke(reader)
                : new UnknownTable(tag);

        internal TTable? Load<TTable>(FontReader reader)
            where TTable : Table
        {
            // loader missing register an unknown type loader and carry on
            if (this.typesLoaders.TryGetValue(typeof(TTable), out Func<FontReader, Table?>? func))
            {
                return (TTable?)func.Invoke(reader);
            }

            throw new Exception("Font table not registered.");
        }
    }
}
