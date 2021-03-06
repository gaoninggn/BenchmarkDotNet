﻿using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Reports
{
    public class SummaryTable
    {
        public Summary Summary { get; }

        public SummaryTableColumn[] Columns { get; }
        public int ColumnCount { get; }

        public string[] FullHeader { get; }
        public string[][] FullContent { get; }
        public bool[] FullContentStartOfGroup { get; }
        public string[][] FullContentWithHeader { get; }
        public bool[] IsDefault { get; }

        internal SummaryTable(Summary summary)
        {
            Summary = summary;

            if (summary.HasCriticalValidationErrors)
            {
                Columns = new SummaryTableColumn[0];
                ColumnCount = 0;
                FullHeader = new string[0];
                FullContent = new string[0][];
                FullContentStartOfGroup = new bool[0];
                FullContentWithHeader = new string[0][];
                IsDefault = new bool[0];
                return;
            }

            var columns = summary.GetColumns();

            ColumnCount = columns.Length;
            FullHeader = columns.Select(c => c.ColumnName).ToArray();

            var orderProvider = summary.Config.GetOrderProvider() ?? DefaultOrderProvider.Instance;
            FullContent = summary.Reports.Select(r => columns.Select(c => c.GetValue(summary, r.Benchmark)).ToArray()).ToArray();
            IsDefault = columns.Select(c => summary.Reports.All(r => c.IsDefault(summary, r.Benchmark))).ToArray();
            var groupKeys = summary.Benchmarks.Select(b => orderProvider.GetGroupKey(b, summary)).ToArray();
            FullContentStartOfGroup = new bool[summary.Reports.Length];

            if (groupKeys.Distinct().Count() > 1 && FullContentStartOfGroup.Length > 0)
            {
                FullContentStartOfGroup[0] = true;
                for (int i = 1; i < summary.Reports.Length; i++)
                    FullContentStartOfGroup[i] = groupKeys[i] != groupKeys[i - 1];
            }            

            var full = new List<string[]> { FullHeader };
            full.AddRange(FullContent);
            FullContentWithHeader = full.ToArray();

            Columns = Enumerable.Range(0, columns.Length).Select(i => new SummaryTableColumn(this, i, columns[i].AlwaysShow)).ToArray();
        }

        public class SummaryTableColumn
        {
            public int Index { get; }
            public string Header { get; }
            public string[] Content { get; }
            public bool NeedToShow { get; }
            public int Width { get; }
            public bool IsDefault { get; }

            public SummaryTableColumn(SummaryTable table, int index, bool alwaysShow)
            {
                Index = index;
                Header = table.FullHeader[index];
                Content = table.FullContent.Select(line => line[index]).ToArray();
                NeedToShow = alwaysShow || Content.Distinct().Count() > 1;
                Width = Math.Max(Header.Length, Content.Any() ? Content.Max(line => line.Length) : 0) + 1;
                IsDefault = table.IsDefault[index];
            }

            public override string ToString() => Header;
        }
    }
}