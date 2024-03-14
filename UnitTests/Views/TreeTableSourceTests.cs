﻿using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class TreeTableSourceTests : IDisposable
{
    private readonly Rune _origChecked;
    private readonly Rune _origUnchecked;
    private readonly ITestOutputHelper _output;

    public TreeTableSourceTests (ITestOutputHelper output)
    {
        _output = output;

        _origChecked = ConfigurationManager.Glyphs.Checked;
        _origUnchecked = ConfigurationManager.Glyphs.UnChecked;
        ConfigurationManager.Glyphs.Checked = new Rune ('☑');
        ConfigurationManager.Glyphs.UnChecked = new Rune ('☐');
    }

    public void Dispose ()
    {
        ConfigurationManager.Glyphs.Checked = _origChecked;
        ConfigurationManager.Glyphs.UnChecked = _origUnchecked;
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTreeTableSource_BasicExpanding_WithKeyboard ()
    {
        TableView tv = GetTreeTable (out _);

        tv.Style.GetOrCreateColumnStyle (1).MinAcceptableWidth = 1;

        tv.Draw ();

        var expected =
            @"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        Assert.Equal (2, tv.Table.Rows);

        // top left is selected cell
        Assert.Equal (0, tv.SelectedRow);
        Assert.Equal (0, tv.SelectedColumn);

        // when pressing right we should expand the top route
        tv.NewKeyDownEvent (Key.CursorRight);

        tv.Draw ();

        expected =
            @"
│Name             │Description         │
├─────────────────┼────────────────────┤
│├-Lost Highway   │Exciting night road │
││ ├─Ford Trans-Am│Talking thunderbird │
││ └─DeLorean     │Time travelling car │
│└+Route 66       │Great race course   │
";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        // when pressing left we should collapse the top route again
        tv.NewKeyDownEvent (Key.CursorLeft);

        tv.Draw ();

        expected =
            @"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │
";

        TestHelpers.AssertDriverContentsAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTreeTableSource_BasicExpanding_WithMouse ()
    {
        TableView tv = GetTreeTable (out _);

        tv.Style.GetOrCreateColumnStyle (1).MinAcceptableWidth = 1;

        tv.Draw ();

        var expected =
            @"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        Assert.Equal (2, tv.Table.Rows);

        // top left is selected cell
        Assert.Equal (0, tv.SelectedRow);
        Assert.Equal (0, tv.SelectedColumn);

        Assert.True (tv.OnMouseEvent (new MouseEvent { X = 2, Y = 2, Flags = MouseFlags.Button1Clicked }));

        tv.Draw ();

        expected =
            @"
│Name             │Description         │
├─────────────────┼────────────────────┤
│├-Lost Highway   │Exciting night road │
││ ├─Ford Trans-Am│Talking thunderbird │
││ └─DeLorean     │Time travelling car │
│└+Route 66       │Great race course   │
";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        // Clicking to the right/left of the expand/collapse does nothing
        tv.OnMouseEvent (new MouseEvent { X = 3, Y = 2, Flags = MouseFlags.Button1Clicked });
        tv.Draw ();
        TestHelpers.AssertDriverContentsAre (expected, _output);
        tv.OnMouseEvent (new MouseEvent { X = 1, Y = 2, Flags = MouseFlags.Button1Clicked });
        tv.Draw ();
        TestHelpers.AssertDriverContentsAre (expected, _output);

        // Clicking on the + again should collapse
        tv.OnMouseEvent (new MouseEvent { X = 2, Y = 2, Flags = MouseFlags.Button1Clicked });
        tv.Draw ();

        expected =
            @"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │";

        TestHelpers.AssertDriverContentsAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTreeTableSource_CombinedWithCheckboxes ()
    {
        TableView tv = GetTreeTable (out TreeView<IDescribedThing> treeSource);

        CheckBoxTableSourceWrapperByIndex checkSource;
        tv.Table = checkSource = new CheckBoxTableSourceWrapperByIndex (tv, tv.Table);
        tv.Style.GetOrCreateColumnStyle (2).MinAcceptableWidth = 1;

        tv.Draw ();

        var expected =
            @"
    │ │Name          │Description          │
├─┼──────────────┼─────────────────────┤
│☐│├+Lost Highway│Exciting night road  │
│☐│└+Route 66    │Great race course    │
";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        Assert.Equal (2, tv.Table.Rows);

        // top left is selected cell
        Assert.Equal (0, tv.SelectedRow);
        Assert.Equal (0, tv.SelectedColumn);

        // when pressing right we move to tree column
        tv.NewKeyDownEvent (Key.CursorRight);

        // now we are in tree column
        Assert.Equal (0, tv.SelectedRow);
        Assert.Equal (1, tv.SelectedColumn);

        Application.Top.NewKeyDownEvent (Key.CursorRight);

        tv.Draw ();

        expected =
            @"

│ │Name             │Description       │
├─┼─────────────────┼──────────────────┤
│☐│├-Lost Highway   │Exciting night roa│
│☐││ ├─Ford Trans-Am│Talking thunderbir│
│☐││ └─DeLorean     │Time travelling ca│
│☐│└+Route 66       │Great race course │
";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.Space);
        tv.Draw ();

        expected =
            @"

│ │Name             │Description       │
├─┼─────────────────┼──────────────────┤
│☐│├-Lost Highway   │Exciting night roa│
│☑││ ├─Ford Trans-Am│Talking thunderbir│
│☐││ └─DeLorean     │Time travelling ca│
│☐│└+Route 66       │Great race course │
";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        IDescribedThing [] selectedObjects = checkSource.CheckedRows.Select (treeSource.GetObjectOnRow).ToArray ();
        IDescribedThing selected = Assert.Single (selectedObjects);

        Assert.Equal ("Ford Trans-Am", selected.Name);
        Assert.Equal ("Talking thunderbird car", selected.Description);
    }

    private TableView GetTreeTable (out TreeView<IDescribedThing> tree)
    {
        var tableView = new TableView ();
        tableView.ColorScheme = Colors.ColorSchemes ["TopLevel"];
        tableView.ColorScheme = Colors.ColorSchemes ["TopLevel"];
        tableView.Bounds = new Rectangle (0, 0, 40, 6);

        tableView.Style.ShowHorizontalHeaderUnderline = true;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.SmoothHorizontalScrolling = true;

        tree = new TreeView<IDescribedThing> ();
        tree.AspectGetter = d => d.Name;

        tree.TreeBuilder = new DelegateTreeBuilder<IDescribedThing> (
                                                                     d => d is Road r
                                                                              ? r.Traffic
                                                                              : Enumerable.Empty<IDescribedThing> ()
                                                                    );

        tree.AddObject (
                        new Road
                        {
                            Name = "Lost Highway",
                            Description = "Exciting night road",
                            Traffic = new List<Car>
                            {
                                new () { Name = "Ford Trans-Am", Description = "Talking thunderbird car" },
                                new () { Name = "DeLorean", Description = "Time travelling car" }
                            }
                        }
                       );

        tree.AddObject (
                        new Road
                        {
                            Name = "Route 66",
                            Description = "Great race course",
                            Traffic = new List<Car>
                            {
                                new () { Name = "Pink Compact", Description = "Penelope Pitstop's car" },
                                new () { Name = "Mean Machine", Description = "Dick Dastardly's car" }
                            }
                        }
                       );

        tableView.Table = new TreeTableSource<IDescribedThing> (
                                                                tableView,
                                                                "Name",
                                                                tree,
                                                                new Dictionary<string, Func<IDescribedThing, object>> { { "Description", d => d.Description } }
                                                               );

        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.LayoutSubviews ();

        Application.Top.Add (tableView);
        Application.Top.EnsureFocus ();
        Assert.Equal (tableView, Application.Top.MostFocused);

        return tableView;
    }

    private class Car : IDescribedThing
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    private interface IDescribedThing
    {
        string Description { get; }
        string Name { get; }
    }

    private class Road : IDescribedThing
    {
        public List<Car> Traffic { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}