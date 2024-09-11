using System.Numerics;
using rlImGui_cs;
using ImGuiNET;

namespace Myriad;

/// <summary>
/// Debbie is the debug system for the engine, unlike the `Pepper` class, Debbie is not named after anyone
/// Currently she is used to perform various debug functions and cheats, as well as display debug information
/// </summary>

static class Debbie {
    // Public properties
    public static bool ShowCommandPrompt { get; set; }
    public static bool ShowHelpWindow { get; set; }

    // Debug UI Options
    private static bool _debugMenuCollapsed = false;
    private static bool _showBrushMenu = false;
    private static bool _showWorldMenu = false;
    private static bool _showMaterialsMenu = false;
    private static bool _showStatsMenu = false;

    private static int _materialInspectorIndex = 0;
    private static string _loadedMaterialPath;
    private static string _loadedMaterialData;
    private static Material _loadedMaterial;

    private static bool _showMaterialEditor = false;
    private static string _editorMaterialPath;
    private static string _editorMaterialData;

    private static string _commandInput = "";
    private static string _lastCommand = "";

    private static Dictionary<string, Action> _commands = new() {
        { "help", () => ShowHelpWindow = !ShowHelpWindow },
        { "stats", () => _showStatsMenu = !_showStatsMenu },
        { "clear", () => Cheats.FillWorld(Engine.World, "air") },
        { "fill", () => Cheats.FillWorld(Engine.World, Canvas.BrushMaterial) },
        { "nuke", () => Cheats.NukeWorld(Engine.World) },
    };

    public static void Init() {
        LoadMaterial();

        Pepper.Log("Debbie initialized", LogType.System);
    }

    private static void SaveMaterial() {
        // if (Config.VerboseLogging) { Pepper.Log($"Material data saved to '{_editorMaterialPath}'", LogType.System); }
    }

    private static void LoadMaterial() {
        _loadedMaterial = Materials.Index[Materials.ByID[_materialInspectorIndex]];
        _loadedMaterialPath = $"materials/{_loadedMaterial.Type}/{_loadedMaterial.ID}.toml";
        _loadedMaterialData = File.OpenText(_loadedMaterialPath).ReadToEnd();
        // if (Config.VerboseLogging) { Pepper.Log($"Loaded material data file at '{_loadedMaterialPath}'", LogType.System); }
    }

    public static void ApplyCommand(string command=null) {
        if (command != null) { _commandInput = command; }
        if (_commandInput == "r") { _commandInput = _lastCommand; }

        if (int.TryParse(_commandInput, out int c)) {
            ShowCommandPrompt = false;
            Canvas.BrushSize = c;
            _commandInput = "";
        } else if (Materials.ByID.Contains(_commandInput)) {
            ShowCommandPrompt = false;
            Canvas.BrushMaterialID = Materials.ByID.IndexOf(_commandInput);
            _lastCommand = _commandInput;
            _commandInput = "";
        } else if (_commands.ContainsKey(_commandInput)) {
            _commands[_commandInput].Invoke();
            ShowCommandPrompt = false;
            _lastCommand = _commandInput;
            _commandInput = "";
        } else {
            Pepper.Log($"Command '{_commandInput}' not found", LogType.System, LogLevel.Warning);
        }
    }

    public static void Draw() {
        rlImGui.Begin();

        // Menu bar
        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(Config.Resolution.X, 35), ImGuiCond.Always);
        if (ImGui.Begin("Debug Menu", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration| ImGuiWindowFlags.NoBackground)) {
            if (_debugMenuCollapsed) {
                if (ImGui.ArrowButton("showMenuButton", ImGuiDir.Right)) { _debugMenuCollapsed = false; }
            } else {
                if (ImGui.Button("Brush")) {  _showBrushMenu = !_showBrushMenu; } ImGui.SameLine();
                if (ImGui.Button("World")) { _showWorldMenu = !_showWorldMenu; } ImGui.SameLine();
                if (ImGui.Button("Materials")) { _showMaterialsMenu = !_showMaterialsMenu; } ImGui.SameLine();
                if (ImGui.Button("Stats")) { _showStatsMenu = ! _showStatsMenu; } ImGui.SameLine();
                if (ImGui.ArrowButton("hideMenuButton", ImGuiDir.Left)) { _debugMenuCollapsed = true; }
            }
        }

        // Brush
        if (_showBrushMenu && ImGui.Begin("Brush", ref _showBrushMenu, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)) {
            ImGui.Combo("Material", ref Canvas.BrushMaterialID, [.. Materials.Names], Materials.ByID.Count);

            ImGui.RadioButton("Paint", ref Canvas.BrushMode, 0); ImGui.SameLine();
            ImGui.RadioButton("Erase", ref Canvas.BrushMode, 1);

            ImGui.RadioButton("Brush", ref Canvas.BrushType, 0); ImGui.SameLine();
            ImGui.RadioButton("Rectangle", ref Canvas.BrushType, 1);

            ImGui.Checkbox("Paint on top", ref Canvas.PaintOnTop);

            if (Canvas.BrushType == 0) { ImGui.SliderInt("Size", ref Canvas.BrushSize, 1, 16); }
        }

        // World
        if (_showWorldMenu && ImGui.Begin("World", ref _showWorldMenu, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)) {
            ImGui.SeparatorText("Simulation");
            ImGui.Text("Ticks per second");
            ImGui.InputInt("", ref Engine.TicksPerSecond); ImGui.SameLine();
            if (ImGui.Button("Apply")) { Engine.SetTicksPerSecond(Engine.TicksPerSecond); }
            ImGui.Checkbox("Multithreading", ref Config.Multithreading);

            ImGui.SeparatorText("Overlays");
            ImGui.Checkbox("Chunk borders", ref Config.DrawChunkBorders);
            ImGui.Checkbox("Update rects", ref Config.DrawUpdateRects);

            ImGui.SeparatorText("Cheats");
            if (ImGui.Button("Clear")) { Cheats.FillWorld(Engine.World, "air"); }
            ImGui.SameLine(); if (ImGui.Button("Fill")) { Cheats.FillWorld(Engine.World, Canvas.BrushMaterial); }
            ImGui.SameLine(); if (ImGui.Button("NUKE!")) { Cheats.NukeWorld(Engine.World); }
        }

        // Material Inspector
        if (_showMaterialsMenu && ImGui.Begin("Material Inspector", ref _showMaterialsMenu, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)) {
            var CursorMat = Engine.World.InBounds(Engine.MousePos) ? Engine.World.Get(Engine.MousePos).ID : "air";
            ImGui.Text($"Cursor: {CursorMat}");
            ImGui.NewLine();

            ImGui.SetNextItemWidth(250);
            if (ImGui.Combo("", ref _materialInspectorIndex, [.. Materials.Names], Materials.Count)) { LoadMaterial(); }

            ImGui.BeginTable("materialInspectorTable", 2, ImGuiTableFlags.SizingStretchProp);
            foreach (var Property in _loadedMaterial.GetType().GetProperties()[..^2]) {
                var Value = Property.GetValue(_loadedMaterial);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Property.Name);
                ImGui.TableNextColumn();
                ImGui.Text(Property.GetValue(_loadedMaterial).ToString());
            } ImGui.EndTable();

            if (_loadedMaterial.Tags.Count > 0) {
                ImGui.SeparatorText("Tags");
                var TagsList = "";
                foreach (var Tag in _loadedMaterial.Tags) { TagsList += Tag + ", "; }
                ImGui.TextWrapped(TagsList[..^2]);
            }

            ImGui.NewLine();
            if (ImGui.Button("Edit Material")) {
                _editorMaterialPath = _loadedMaterialPath;
                _editorMaterialData = _loadedMaterialData;
                _showMaterialEditor = true;
            }

            if (ImGui.Button("Reload Materials")) {
                Materials.ReloadMaterials();
                LoadMaterial();
            }
        }

        // Material Editor
        if (_showMaterialEditor && ImGui.Begin("Material Editor", ref _showMaterialEditor, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)) {
            if (ImGui.Button("Save")) { SaveMaterial(); } ImGui.SameLine();
            if (ImGui.Button("Revert")) { _editorMaterialData = File.OpenText(_editorMaterialPath).ReadToEnd(); }; ImGui.SameLine();
            if (ImGui.Button("Cancel")) { _showMaterialEditor = false; }
            ImGui.InputTextMultiline("", ref _editorMaterialData, 2048, new Vector2(270, 300));
            ImGui.Text(_editorMaterialPath);
        }

        // Stats Window
        if (_showStatsMenu && ImGui.Begin("Stats", ref _showStatsMenu, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)) {
            ImGui.Text($"Mouse: {Engine.MousePos}");
            ImGui.Text($"Chunk: {Engine.World.GetChunk(Engine.MousePos).Position}");
            ImGui.Text($"Material: {Materials.GetName(Engine.World.Get(Engine.MousePos).ID)}");
            ImGui.Text($"Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
        }

        // Command Prompt
        if (ShowCommandPrompt) {
            ImGui.SetNextWindowPos(new Vector2(Config.Resolution.X / 2 - 137.5f, Config.Resolution.Y / 2 - 25), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(275, 35), ImGuiCond.Always);
            if (ImGui.Begin("Material Search", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize| ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)) {
                ImGui.SetKeyboardFocusHere();
                if (ImGui.InputText("", ref _commandInput, 64, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)) {
                    ApplyCommand();
                } ImGui.SameLine();
                if (ImGui.Button("Apply")) { ApplyCommand(); }
            }
        }

        // Help Window
        if (ShowHelpWindow && ImGui.Begin("Help", ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.SeparatorText("Commands");
            ImGui.Text("[material id] - Change the brush material");
            ImGui.Text("[number] - Change the brush size");
            ImGui.Text("clear - Clear the world");
            ImGui.Text("fill - Fill the world with the current material");
            ImGui.Text("nuke - Destroy the world");
            ImGui.NewLine();

            ImGui.SeparatorText("Hotkeys");
            ImGui.Text("Tab - Open command prompt");
            ImGui.Text("Shift + Enter - Repeat last command");
            ImGui.Text("Shift + / - Toggle help window");
            ImGui.Text("Escape - Close command prompt or quit");
            ImGui.Text("Space - Pause/unpause the simulation");
            ImGui.Text("T - Advance the simulation by one tick (while paused)");
            ImGui.NewLine();

            ImGui.SeparatorText("Material Editor");
            ImGui.Text("- \"Reload Materials\" will scan for any changes to material data files and apply them.");
        }

        // End
        rlImGui.End();
    }
}
