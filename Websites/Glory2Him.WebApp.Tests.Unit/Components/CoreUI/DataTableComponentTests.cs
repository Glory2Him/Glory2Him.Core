// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class DataTableComponentTests : BunitContext
    {
        private sealed class Row
        {
            public string Name { get; set; } = string.Empty;
            public int Order { get; set; }
        }

        private static List<Row> CreateRows(int count) =>
            Enumerable.Range(1, count)
                .Select(index => new Row { Name = $"Row{index:D2}", Order = index })
                .ToList();

        private static IReadOnlyList<DataTableColumn<Row>> Columns =>
            new List<DataTableColumn<Row>>
            {
                new DataTableColumn<Row> { Title = "Name", Value = row => row.Name },
                new DataTableColumn<Row> { Title = "Order", Value = row => row.Order },
            };

        private IRenderedComponent<DataTable<Row>> RenderTable(
            List<Row> rows,
            int pageSize = 10) =>
                Render<DataTable<Row>>(parameters => parameters
                    .Add(table => table.Items, rows)
                    .Add(table => table.Columns, Columns)
                    .Add(table => table.PageSize, pageSize));

        [Fact]
        public void ShouldRenderHeadersAndRows()
        {
            // given
            List<Row> rows = CreateRows(count: 3);

            // when
            IRenderedComponent<DataTable<Row>> renderedTable = RenderTable(rows);

            // then
            renderedTable.FindAll("thead th")[0].TextContent.Trim().Should().Be("Name");
            renderedTable.FindAll("tbody tr").Should().HaveCount(3);
            renderedTable.Markup.Should().Contain("Row01");
        }

        [Fact]
        public void ShouldPageItemsAccordingToPageSize()
        {
            // given
            List<Row> rows = CreateRows(count: 12);

            // when
            IRenderedComponent<DataTable<Row>> renderedTable = RenderTable(rows, pageSize: 5);

            // then
            renderedTable.FindAll("tbody tr").Should().HaveCount(5);
            renderedTable.Instance.PageCount.Should().Be(3);
            renderedTable.Markup.Should().Contain("Page 1 of 3");
        }

        [Fact]
        public void ShouldAdvanceToNextPageWhenNextClicked()
        {
            // given
            List<Row> rows = CreateRows(count: 12);
            IRenderedComponent<DataTable<Row>> renderedTable = RenderTable(rows, pageSize: 5);

            // when
            renderedTable.Find("button.datatable-next").Click();

            // then
            renderedTable.Instance.CurrentPage.Should().Be(2);
            renderedTable.Markup.Should().Contain("Row06");
        }

        [Fact]
        public void ShouldFilterRowsWhenSearching()
        {
            // given
            List<Row> rows = CreateRows(count: 12);
            IRenderedComponent<DataTable<Row>> renderedTable = RenderTable(rows);

            // when
            renderedTable.Find("input.datatable-search").Input("Row07");

            // then
            renderedTable.FindAll("tbody tr").Should().HaveCount(1);
            renderedTable.Markup.Should().Contain("Row07");
        }

        [Fact]
        public void ShouldSortRowsWhenSortableHeaderClicked()
        {
            // given
            List<Row> rows = CreateRows(count: 3);
            IRenderedComponent<DataTable<Row>> renderedTable = RenderTable(rows);

            // when (click Name twice → descending)
            renderedTable.FindAll("thead th")[0].Click();
            renderedTable.FindAll("thead th")[0].Click();

            // then
            renderedTable.Instance.SortAscending.Should().BeFalse();
            renderedTable.FindAll("tbody tr")[0].TextContent.Should().Contain("Row03");
        }
    }
}
