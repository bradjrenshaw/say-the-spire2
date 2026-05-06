using System.Linq;
using Godot;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class ModSettingsScreen : Screen
{
    private readonly CategorySetting _category;
    private readonly PanelContainer _root;
    private readonly VBoxContainer _itemList;
    private readonly NavigableContainer _navContainer;
    private readonly System.Collections.Generic.Dictionary<RowContainer, CategorySetting> _rowCategories = new();
    private readonly System.Collections.Generic.Dictionary<RowContainer, HBoxContainer> _rowNodes = new();

    public override Message? ScreenName => Message.Raw(_category.Label);

    public ModSettingsScreen(CategorySetting category)
    {
        _category = category;

        // Build visual layout
        _root = new PanelContainer { Name = "ModSettings_" + category.Key };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Semi-transparent dark background
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
        };
        _root.AddThemeStyleboxOverride("panel", bg);

        // Centered content area
        var centerContainer = new CenterContainer();
        _root.AddChild(centerContainer);

        var contentPanel = new PanelContainer();
        var contentBg = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f, 1f),
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 32,
            ContentMarginRight = 32,
            ContentMarginTop = 24,
            ContentMarginBottom = 24,
        };
        contentPanel.AddThemeStyleboxOverride("panel", contentBg);
        contentPanel.CustomMinimumSize = new Vector2(500, 0);
        centerContainer.AddChild(contentPanel);

        var outerVBox = new VBoxContainer();
        outerVBox.AddThemeConstantOverride("separation", 16);
        contentPanel.AddChild(outerVBox);

        // Title
        var title = new Label
        {
            Text = category.IsRoot ? LocalizationManager.GetOrDefault("ui", "SCREENS.MOD_SETTINGS", "Mod Settings") : category.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        outerVBox.AddChild(title);

        // Separator
        outerVBox.AddChild(new HSeparator());

        // Item list
        _itemList = new VBoxContainer();
        _itemList.AddThemeConstantOverride("separation", 8);
        outerVBox.AddChild(_itemList);

        // Navigation container
        _navContainer = new NavigableContainer
        {
            ContainerLabel = Message.Raw(category.Label),
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = _navContainer;

        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_left");
        ClaimAction("ui_right");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
        ClaimAction("ui_cancel");
        ClaimAction("mega_pause_and_back");
        ClaimAction("mega_top_panel");
        ClaimAction("mega_peek");
        ClaimAction("mega_view_draw_pile");
        ClaimAction("mega_view_discard_pile");
        ClaimAction("mega_view_deck_and_tab_left");
        ClaimAction("mega_view_exhaust_pile_and_tab_right");
        ClaimAction("mega_view_map");
        ClaimAction("mod_settings");

        BuildControls();
    }

    public override void OnPush()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(_root);
        _navContainer.FocusFirst();
    }

    public override void OnFocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = true;

        var remembered = _navContainer.FocusedChild;
        if (remembered != null)
            _navContainer.SetFocusTo(remembered);
        else
            _navContainer.FocusFirst();
    }

    public override void OnUnfocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = false;
    }

    public override void OnPop()
    {
        // Unsubscribe every element from long-lived events (e.g., NullableSetting
        // ResolvedChanged) before freeing the Godot nodes. Container.Detach
        // recurses so row children get cleaned up too.
        _navContainer.Detach();

        if (GodotObject.IsInstanceValid(_root))
        {
            _root.GetParent()?.RemoveChild(_root);
            _root.QueueFree();
        }
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (action.Key == "ui_cancel" || action.Key == "mod_settings")
        {
            ScreenManager.RemoveScreen(this);
            if (_category.IsRoot)
                SpeechManager.Output(Message.Localized("ui", "SPEECH.CLOSED"));
            return true;
        }

        return _navContainer.HandleAction(action);
    }

    private void BuildControls()
    {
        if (_category.HasResetAction)
        {
            var resetLabel = LocalizationManager.GetOrDefault("ui", "SETTINGS.RESET_TO_DEFAULTS", "Reset to defaults");
            var resetButton = new ButtonElement(resetLabel);
            resetButton.OnActivated = () =>
            {
                ResetAllOverrides(_category);
                ReorderRowsBySortPriority();
                SpeechManager.Output(Message.Localized("ui", "SETTINGS.RESET_DONE"));
            };
            _navContainer.Add(resetButton);
            AddControl(resetButton.Node, resetButton);
        }

        foreach (var setting in _category.Children.OrderBy(s => s.SortPriority).ThenBy(s => s.Label))
        {
            if (setting.Hidden) continue;

            switch (setting)
            {
                case CategorySetting cat:
                    if (_category.HasResetAction)
                        AddReorderableCategoryRow(cat);
                    else
                    {
                        var button = new ButtonElement(cat.Label);
                        button.OnActivated = () =>
                        {
                            var subScreen = new ModSettingsScreen(cat);
                            ScreenManager.PushScreen(subScreen);
                        };
                        _navContainer.Add(button);
                        AddControl(button.Node, button);
                    }
                    break;

                case NullableBoolSetting nullableBoolSetting:
                    var nullableCheckbox = new NullableCheckboxElement(nullableBoolSetting);
                    _navContainer.Add(nullableCheckbox);
                    AddControl(nullableCheckbox.Node, nullableCheckbox);
                    break;

                case NullableIntSetting nullableIntSetting:
                    var nullableSlider = new NullableSliderElement(nullableIntSetting);
                    _navContainer.Add(nullableSlider);
                    AddControl(nullableSlider.Node, nullableSlider);
                    break;

                case NullableStringSetting nullableStringSetting:
                    var nullableTextInput = new NullableTextInputElement(nullableStringSetting);
                    _navContainer.Add(nullableTextInput);
                    AddControl(nullableTextInput.Node, nullableTextInput);
                    break;

                case NullableChoiceSetting nullableChoiceSetting:
                    var nullableDropdown = new NullableDropdownElement(nullableChoiceSetting);
                    _navContainer.Add(nullableDropdown);
                    AddControl(nullableDropdown.Node, nullableDropdown);
                    break;

                case BoolSetting boolSetting:
                    var checkbox = new CheckboxElement(boolSetting);
                    _navContainer.Add(checkbox);
                    AddControl(checkbox.Node, checkbox);
                    break;

                case IntSetting intSetting:
                    var slider = new SliderElement(intSetting);
                    _navContainer.Add(slider);
                    AddControl(slider.Node, slider);
                    break;

                case ChoiceSetting choiceSetting:
                    var dropdown = new DropdownElement(choiceSetting);
                    _navContainer.Add(dropdown);
                    AddControl(dropdown.Node, dropdown);
                    break;

                case BindingSetting bindingSetting:
                    var bindingLabel = GetBindingSummary(bindingSetting);
                    var bindingButton = new ButtonElement(bindingLabel);
                    bindingButton.OnActivated = () =>
                    {
                        var screen = new BindingListScreen(bindingSetting);
                        ScreenManager.PushScreen(screen);
                    };
                    _navContainer.Add(bindingButton);
                    AddControl(bindingButton.Node, bindingButton);
                    break;
            }
        }
    }

    private static void ResetAllOverrides(CategorySetting category)
    {
        foreach (var child in category.Children)
        {
            switch (child)
            {
                case NullableBoolSetting n:
                    n.Reset();
                    break;
                // The hidden "order" StringSetting on an announcements parent
                // stores a CSV of announcement keys. Reset it to its default so
                // a user with a stale saved order (e.g. from before a new
                // announcement type was added) gets the canonical attribute
                // order back when they click Reset.
                case StringSetting s when s.Key == "order":
                    s.Set(s.Default);
                    break;
                case CategorySetting c:
                    ResetAllOverrides(c);
                    break;
            }
        }
    }

    private static string GetBindingSummary(BindingSetting setting)
    {
        var action = setting.Action;
        var bindings = action.Bindings;
        if (bindings.Count == 0)
            return $"{action.Label}: (none)";
        var names = string.Join(", ", bindings.Select(b => b.DisplayName));
        return $"{action.Label}: {names}";
    }

    /// <summary>
    /// Builds a three-button row (Configure / Move Up / Move Down) for an
    /// announcement-override category. Move buttons are wired but are no-ops
    /// until persistence for user reordering is implemented.
    /// </summary>
    private void AddReorderableCategoryRow(CategorySetting cat)
    {
        var row = new RowContainer
        {
            // Announced on entry: e.g. "Label horizontal bar, Configure, button, 1 of 3"
            ContainerLabel = Message.Localized("ui", "SETTINGS.HORIZONTAL_BAR_LABEL", new { label = cat.Label }),
            AnnounceName = true,
            AnnouncePosition = true,
        };
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);

        var configureLabel = LocalizationManager.GetOrDefault("ui", "SETTINGS.CONFIGURE", "Configure");
        var configure = new ButtonElement(configureLabel);
        configure.OnActivated = () =>
        {
            var subScreen = new ModSettingsScreen(cat);
            ScreenManager.PushScreen(subScreen);
        };
        row.Add(configure);
        AddRowChild(hbox, configure, row);

        var moveUp = new ButtonElement(LocalizationManager.GetOrDefault("ui", "SETTINGS.MOVE_UP", "Move Up"));
        moveUp.OnActivated = () => MoveRow(row, hbox, -1);
        row.Add(moveUp);
        AddRowChild(hbox, moveUp, row);

        var moveDown = new ButtonElement(LocalizationManager.GetOrDefault("ui", "SETTINGS.MOVE_DOWN", "Move Down"));
        moveDown.OnActivated = () => MoveRow(row, hbox, 1);
        row.Add(moveDown);
        AddRowChild(hbox, moveDown, row);

        _itemList.AddChild(hbox);
        _navContainer.Add(row);
        _rowCategories[row] = cat;
        _rowNodes[row] = hbox;
    }

    /// <summary>
    /// Swap this row with its adjacent row-sibling in the given direction.
    /// Updates the NavigableContainer order, swaps SortPriority so re-entry
    /// shows the new order, moves the Godot HBox nodes in the VBox, and
    /// rewrites the persisted order string consumed by AnnouncementComposer.
    /// Focus stays on the activated move button so repeated presses chain.
    /// </summary>
    private void MoveRow(RowContainer row, HBoxContainer hbox, int direction)
    {
        int index = _navContainer.IndexOf(row);
        int neighbourIndex = -1;
        for (int i = index + direction; i >= 0 && i < _navContainer.Children.Count; i += direction)
        {
            if (_navContainer.Children[i] is RowContainer) { neighbourIndex = i; break; }
        }
        if (neighbourIndex < 0) return; // at the boundary

        var neighbour = (RowContainer)_navContainer.Children[neighbourIndex];

        _navContainer.Swap(index, neighbourIndex);

        if (_rowNodes.TryGetValue(neighbour, out var neighbourHbox))
        {
            int hboxPos = hbox.GetIndex();
            int neighbourPos = neighbourHbox.GetIndex();
            _itemList.MoveChild(hbox, neighbourPos);
            _itemList.MoveChild(neighbourHbox, hboxPos);
        }

        PersistAnnouncementOrder();
        SpeakMoveFeedback(row);
    }

    /// <summary>
    /// Re-sorts the existing reorderable rows in <see cref="_navContainer"/>
    /// (and the underlying Godot VBox) to match each category's current
    /// <see cref="Setting.SortPriority"/>. Called after Reset Defaults so the
    /// UI immediately reflects the priorities that
    /// <see cref="UI.Announcements.AnnouncementRegistry.ApplyOrderToSortPriorities"/>
    /// re-derived. No-op on screens without reorderable rows.
    /// </summary>
    private void ReorderRowsBySortPriority()
    {
        if (!_category.HasResetAction) return;
        if (_rowCategories.Count == 0) return;

        // Indices of every RowContainer in the nav container, in current order.
        var rowSlots = new System.Collections.Generic.List<int>();
        for (int i = 0; i < _navContainer.Children.Count; i++)
        {
            if (_navContainer.Children[i] is RowContainer)
                rowSlots.Add(i);
        }

        // Target row order, sorted by SortPriority (the categories were just
        // reset so this matches the canonical attribute order).
        var targetOrder = _rowCategories
            .OrderBy(kv => kv.Value.SortPriority)
            .Select(kv => kv.Key)
            .ToList();

        for (int slot = 0; slot < targetOrder.Count && slot < rowSlots.Count; slot++)
        {
            var targetRow = targetOrder[slot];
            int targetIdx = rowSlots[slot];
            int currentIdx = _navContainer.IndexOf(targetRow);
            if (currentIdx == targetIdx) continue;

            _navContainer.Swap(currentIdx, targetIdx);

            // Mirror the swap in the Godot VBox so visual order tracks nav order.
            if (_navContainer.Children[currentIdx] is RowContainer displaced
                && _rowNodes.TryGetValue(targetRow, out var targetHbox)
                && _rowNodes.TryGetValue(displaced, out var displacedHbox))
            {
                int targetPos = targetHbox.GetIndex();
                int displacedPos = displacedHbox.GetIndex();
                _itemList.MoveChild(targetHbox, displacedPos);
                _itemList.MoveChild(displacedHbox, targetPos);
            }
        }
    }

    /// <summary>
    /// Speaks where the row landed after a move. Reports the neighbouring
    /// row labels — "between X and Y", "before Y" at the top, "after X" at
    /// the bottom. No-op when the row is the only row in the container.
    /// </summary>
    private void SpeakMoveFeedback(RowContainer row)
    {
        var index = _navContainer.IndexOf(row);
        CategorySetting? prevCat = null;
        CategorySetting? nextCat = null;

        for (int i = index - 1; i >= 0; i--)
        {
            if (_navContainer.Children[i] is RowContainer r && _rowCategories.TryGetValue(r, out var c))
            {
                prevCat = c;
                break;
            }
        }
        for (int i = index + 1; i < _navContainer.Children.Count; i++)
        {
            if (_navContainer.Children[i] is RowContainer r && _rowCategories.TryGetValue(r, out var c))
            {
                nextCat = c;
                break;
            }
        }

        Message? msg = (prevCat, nextCat) switch
        {
            (not null, not null) => Message.Localized("ui", "SETTINGS.MOVED_BETWEEN",
                new { prev = prevCat.Label, next = nextCat.Label }),
            (null, not null) => Message.Localized("ui", "SETTINGS.MOVED_BEFORE",
                new { next = nextCat.Label }),
            (not null, null) => Message.Localized("ui", "SETTINGS.MOVED_AFTER",
                new { prev = prevCat.Label }),
            _ => null,
        };

        if (msg != null)
            SpeechManager.Output(msg);
    }

    private void PersistAnnouncementOrder()
    {
        var orderSetting = _category.Get<StringSetting>("order");
        if (orderSetting == null) return;

        var keys = new System.Collections.Generic.List<string>();
        foreach (var child in _navContainer.Children)
        {
            if (child is RowContainer r && _rowCategories.TryGetValue(r, out var cat))
                keys.Add(cat.Key);
        }
        orderSetting.Set(string.Join(",", keys));
    }

    private void AddRowChild(HBoxContainer hbox, ButtonElement button, RowContainer row)
    {
        var control = (Control)button.Node;
        control.FocusMode = Control.FocusModeEnum.All;
        hbox.AddChild(control);

        control.FocusEntered += () => _navContainer.SetFocusTo(button);
        ((BaseButton)control).Pressed += () => button.Activate();
    }

    private void AddControl(Node node, UIElement element)
    {
        var control = (Control)node;
        control.FocusMode = Control.FocusModeEnum.All;
        _itemList.AddChild(control);

        // Sync keyboard navigation when mouse clicks a control
        control.FocusEntered += () =>
        {
            _navContainer.SetFocusTo(element);
        };

        // Handle mouse activation
        if (element is ButtonElement btn)
        {
            ((BaseButton)control).Pressed += () => btn.Activate();
        }
        else if (element is CheckboxElement cb)
        {
            ((CheckBox)control).Toggled += (_) => cb.SyncFromControl();
        }
        else if (element is NullableCheckboxElement ncb)
        {
            ((CheckBox)control).Toggled += (_) => ncb.SyncFromControl();
        }
        else if (element is NullableSliderElement nsl)
        {
            ((HSlider)control).ValueChanged += (_) => nsl.SyncFromControl();
        }
        else if (element is NullableTextInputElement nti)
        {
            ((LineEdit)control).TextChanged += (_) => nti.SyncFromControl();
        }
        else if (element is NullableDropdownElement ndd)
        {
            ((BaseButton)control).Pressed += () =>
            {
                // Open a choice-selection screen keyed off the nullable's fallback options.
                // SetExplicit is called via ResolvedChanged in NullableDropdownElement.
                // TODO: implement NullableChoiceSelectionScreen if users need it.
            };
        }
        else if (element is SliderElement sl)
        {
            ((HSlider)control).ValueChanged += (_) => sl.SyncFromControl();
        }
        else if (element is DropdownElement dd)
        {
            ((BaseButton)control).Pressed += () =>
            {
                var screen = new ChoiceSelectionScreen(dd.Setting);
                ScreenManager.PushScreen(screen);
            };
        }
    }
}
