// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SixLabors.Fonts.Tables.AdvancedTypographic;
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
            this.Register(NameTable.TableName, NameTable.Load);
            this.Register(CMapTable.TableName, CMapTable.Load);
            this.Register(HeadTable.TableName, HeadTable.Load);
            this.Register(HorizontalHeadTable.TableName, HorizontalHeadTable.Load);
            this.Register(HorizontalMetricsTable.TableName, HorizontalMetricsTable.Load);
            this.Register(VerticalHeadTable.TableName, VerticalHeadTable.Load);
            this.Register(VerticalMetricsTable.TableName, VerticalMetricsTable.Load);
            this.Register(MaximumProfileTable.TableName, MaximumProfileTable.Load);
            this.Register(OS2Table.TableName, OS2Table.Load);
            this.Register(IndexLocationTable.TableName, IndexLocationTable.Load);
            this.Register(GlyphTable.TableName, GlyphTable.Load);
            this.Register(KerningTable.TableName, KerningTable.Load);
            this.Register(ColrTable.TableName, ColrTable.Load);
            this.Register(CpalTable.TableName, CpalTable.Load);
            this.Register(GPosTable.TableName, GPosTable.Load);
            this.Register(GSubTable.TableName, GSubTable.Load);
            this.Register(CvtTable.TableName, CvtTable.Load);
            this.Register(FpgmTable.TableName, FpgmTable.Load);
            this.Register(PrepTable.TableName, PrepTable.Load);
            this.Register(GlyphDefinitionTable.TableName, GlyphDefinitionTable.Load);
            this.Register(PostTable.TableName, PostTable.Load);
            this.Register(Cff1Table.TableName, Cff1Table.Load);
            this.Register(Cff2Table.TableName, Cff2Table.Load);
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
