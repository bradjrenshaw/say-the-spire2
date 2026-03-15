using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CrystalSphereGameScreen : Screen
{
    public static CrystalSphereGameScreen? Current { get; private set; }

    private static readonly FieldInfo? CellContainerField =
        AccessTools.Field(typeof(NCrystalSphereScreen), "_cellContainer");
    private static readonly FieldInfo? EntityField =
        AccessTools.Field(typeof(NCrystalSphereScreen), "_entity");

    private readonly NCrystalSphereScreen _screen;
    private readonly Elements.GridContainer _grid = new()
    {
        AnnounceName = false,
        AnnouncePosition = true,
    };
    private readonly System.Collections.Generic.Dictionary<Control, UIElement> _registry = new();

    // 2D array for fast neighbor lookup
    private NCrystalSphereCell?[,]? _cells;
    private int _rows;
    private int _cols;

    private CrystalSphereMinigame? _minigame;
    private CrystalSphereMinigame.CrystalSphereToolType _lastTool;
    private int _lastDivinationCount;

    public override string? ScreenName => "Crystal Sphere";

    public CrystalSphereGameScreen(NCrystalSphereScreen screen)
    {
        _screen = screen;
    }

    public override void OnPush()
    {
        Current = this;
        BuildRegistry();
        AnnounceInstructions();
        Log.Info($"[AccessibilityMod] CrystalSphereGameScreen pushed ({_registry.Count} cells registered)");
    }

    public override void OnPop()
    {
        _registry.Clear();
        _grid.Clear();
        _cells = null;
        if (Current == this) Current = null;
    }

    public override UIElement? GetElement(Control control)
    {
        return _registry.TryGetValue(control, out var element) ? element : null;
    }

    public override void OnUpdate()
    {
        if (_cells == null) return;

        // Announce tool changes
        if (_minigame != null && _minigame.CrystalSphereTool != _lastTool)
        {
            _lastTool = _minigame.CrystalSphereTool;
            var toolName = _lastTool == CrystalSphereMinigame.CrystalSphereToolType.Big
                ? "Big Divination, 3 by 3" : "Small Divination, 1 by 1";
            SpeechManager.Output(Message.Raw(toolName));
        }

        // Announce divination count changes
        if (_minigame != null && _minigame.DivinationCount != _lastDivinationCount)
        {
            _lastDivinationCount = _minigame.DivinationCount;
            SpeechManager.Output(Message.Raw($"{_lastDivinationCount} divinations remaining"));
        }

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                var cell = _cells[row, col];
                if (cell == null || !GodotObject.IsInstanceValid(cell)) continue;

                // Keep all cells focusable
                cell.FocusMode = Control.FocusModeEnum.All;

                // Wire focus neighbors — no wrapping, clamp to edges
                cell.FocusNeighborTop = (row > 0 ? _cells[row - 1, col] : cell)?.GetPath();
                cell.FocusNeighborBottom = (row < _rows - 1 ? _cells[row + 1, col] : cell)?.GetPath();
                cell.FocusNeighborLeft = (col > 0 ? _cells[row, col - 1] : cell)?.GetPath();
                cell.FocusNeighborRight = (col < _cols - 1 ? _cells[row, col + 1] : cell)?.GetPath();
            }
        }
    }

    private void AnnounceInstructions()
    {
        try
        {
            var title = new LocString("events", "CRYSTAL_SPHERE.minigame.instructions.title").GetFormattedText();
            var desc = new LocString("events", "CRYSTAL_SPHERE.minigame.instructions.description").GetFormattedText();

            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(title))
                parts.Add(ProxyElement.StripBbcode(title));
            if (!string.IsNullOrEmpty(desc))
                parts.Add(ProxyElement.StripBbcode(desc));

            if (_minigame != null)
                parts.Add($"{_minigame.DivinationCount} divinations remaining");

            if (parts.Count > 0)
                SpeechManager.Output(Message.Raw(string.Join(". ", parts)));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] CrystalSphere instructions error: {e.Message}");
        }
    }

    private void BuildRegistry()
    {
        _registry.Clear();
        _grid.Clear();

        var cellContainer = CellContainerField?.GetValue(_screen) as Control;
        if (cellContainer == null)
        {
            Log.Error("[AccessibilityMod] CrystalSphere: could not get _cellContainer");
            return;
        }

        _minigame = EntityField?.GetValue(_screen) as CrystalSphereMinigame;
        _lastTool = _minigame?.CrystalSphereTool ?? CrystalSphereMinigame.CrystalSphereToolType.None;
        _lastDivinationCount = _minigame?.DivinationCount ?? 0;

        var cellList = cellContainer.GetChildren().OfType<NCrystalSphereCell>().ToList();

        // Determine grid dimensions from cell entities
        _rows = 0;
        _cols = 0;
        foreach (var cell in cellList)
        {
            if (cell.Entity == null) continue;
            if (cell.Entity.Y + 1 > _rows) _rows = cell.Entity.Y + 1;
            if (cell.Entity.X + 1 > _cols) _cols = cell.Entity.X + 1;
        }

        _cells = new NCrystalSphereCell[_rows, _cols];

        foreach (var cell in cellList)
        {
            var entity = cell.Entity;
            if (entity == null) continue;

            _cells[entity.Y, entity.X] = cell;

            var proxy = new ProxyCrystalSphereCell(cell);
            _grid.Add(proxy, entity.X, entity.Y);
            _registry[cell] = proxy;

            // Connect our own FocusEntered handler — RefreshFocus may not fire
            // on these cells since ConnectSignals isn't guaranteed for scene instances
            cell.Connect(Control.SignalName.FocusEntered, Callable.From(() =>
            {
                UIManager.QueueFocus(cell, proxy);
            }));
        }

        RootElement = _grid;
        Log.Info($"[AccessibilityMod] CrystalSphere grid: {_rows}x{_cols}, {cellList.Count} cells");
    }
}
