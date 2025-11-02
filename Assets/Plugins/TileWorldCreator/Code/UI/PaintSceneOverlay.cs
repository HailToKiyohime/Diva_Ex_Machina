/*

  _____ _ _    __        __         _     _  ____                _             
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __ 
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |   
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|   
                                                                               
	TileWorldCreator V4 (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.UI;
using GiantGrey.TileWorldCreator.Utilities;
using UnityEngine.SceneManagement;


namespace GiantGrey.TileWorldCreator
{
    [Overlay(typeof(SceneView), "TileWorldCreator", true)]
    [TWCOverlayIconAttribute()]
     [InitializeOnLoad]
    public class PaintSceneOverlay : Overlay
    {
    
        private string selectedLayerGuid = string.Empty;
        private static bool altClick;
        private static int selectedManager = 0;
        private static bool undoPerfomed;

        private List<TileWorldCreatorManager> managers = new List<TileWorldCreatorManager>();

        [SerializeField]
        private int brushSize = 1;
        private Color colorDarkGrey = new Color(50f/255f,50f/255f,50f/255f);
        private bool lastDisplayed;
        private VisualElement root;
        private static string currentScene;
        private int undoGroup = -1;

        static PaintSceneOverlay()
        {
            currentScene = EditorSceneManager.GetActiveScene().name;
            EditorApplication.hierarchyChanged -= HierarchyChanged;
            EditorApplication.hierarchyChanged += HierarchyChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
            ObjectChangeEvents.changesPublished -= ChangesPublished;
            ObjectChangeEvents.changesPublished += ChangesPublished;
        }

        private static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (undoPerfomed)
            {
                for (int i = 0; i < stream.length; ++i)
                {
                    var type = stream.GetEventType(i);
                    switch (type)
                    {
                        case ObjectChangeKind.ChangeAssetObjectProperties:
                            stream.GetChangeAssetObjectPropertiesEvent(i, out var changeAssetObjectPropertiesEvent);
                            var changeAsset = EditorUtility.InstanceIDToObject(changeAssetObjectPropertiesEvent.instanceId);
                            // var changeAssetPath = AssetDatabase.GUIDToAssetPath(changeAssetObjectPropertiesEvent.guid);
                            // Debug.Log($"{type}: {changeAsset} at {changeAssetPath} in scene {changeAssetObjectPropertiesEvent.scene}.");
                            if (changeAsset is BlueprintLayer layer)
                            {
                                // Update map
                                var managers = new List<TileWorldCreatorManager>();
                                var _m = GameObject.FindObjectsByType<TileWorldCreatorManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
                                foreach (var m in _m)
                                {
                                    managers.Add(m);
                                }

                                managers[selectedManager].GenerateCompleteMap();
                            }
                            break;
                    }
                }

                undoPerfomed = false;
            }
        }

        static void HierarchyChanged()
        {
            var _p = new PaintSceneOverlay();
            _p.CreatePanelContent();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void ScriptReload()
        {
            altClick = false;
        }

        static void OnUndoRedo()
        {
            undoPerfomed = true;
        }


        public override void OnCreated()
        {
            lastDisplayed = displayed;
            EditorApplication.update += CheckDisplayed;

            base.OnCreated();
        }

        public override void OnWillBeDestroyed()
        {
            // Always unsubscribe to avoid leaks
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
            EditorApplication.update -= CheckDisplayed;

            if (selectedManager < managers.Count && managers.Count > 0)
            {
                if (managers[selectedManager].configuration != null)
                {
                    managers[selectedManager].configuration.showPaintGrid = false;
                    managers[selectedManager].configuration.showGizmos = false;
                }
            }
            
            base.OnWillBeDestroyed();
        }

        private void CheckDisplayed()
        {
            if (displayed != lastDisplayed)
            {
                if (displayed)
                {
                }
                else
                {
                    // Overlay hidden
                    if (selectedManager < managers.Count && managers.Count > 0)
                    {
                        if (managers[selectedManager].configuration != null)
                        {
                            managers[selectedManager].configuration.showPaintGrid = false;
                            managers[selectedManager].configuration.showGizmos = false;
                        }
                    }
                }

                lastDisplayed = displayed;
            }
        }


        public override VisualElement CreatePanelContent()
        {
            if (root == null)
            {
                root = new VisualElement();
            }

            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;


            try
            {
                managers[selectedManager].configuration.showPaintGrid = false;
                managers[selectedManager].configuration.showGizmos = false;
            }
            catch
            {
            }

            BuildPanel();

            return root;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            BuildPanel();
        }



        void BuildPanel()
        {
            root.Clear();

            root.style.backgroundColor = Color.black;

            // Make sure to update the build panel when layers has been changed
            root.schedule.Execute(x => 
            {
                if (managers == null || managers.Count == 0)
                {
                    return;
                }
                if (managers[selectedManager] != null)
                {
                    if (managers[selectedManager].configuration != null)
                    {
                        if (managers[selectedManager].configuration.layerChanged)
                        {
                            BuildPanel();
                            managers[selectedManager].configuration.layerChanged = false;
                        }
                    }

                    // managers[selectedManager].configuration.showPaintGrid = isPaintMode;
                }

            }).Every(1000);

            managers = new List<TileWorldCreatorManager>();
            var _m = GameObject.FindObjectsByType<TileWorldCreatorManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            foreach (var m in _m)
            {
                managers.Add(m);
            }

            var _headerBanner = new VisualElement();
            _headerBanner.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("PaintBanner.twc");
            _headerBanner.style.backgroundColor = Color.black;
            _headerBanner.style.width = 150;
            _headerBanner.style.height = 50;
            _headerBanner.style.alignSelf = Align.Center;
            _headerBanner.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center); //BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit);
            _headerBanner.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center); //BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit);   
            _headerBanner.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);  //BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleToFit);

            root.Add(_headerBanner);

            var _managerLabel = new Label();
            _managerLabel.style.fontSize = 14;
            _managerLabel.style.marginTop = 5;
            _managerLabel.text = "TWC Managers";

            root.Add(_managerLabel);

           
            for (int i = 0; i < managers.Count; i++)
            {
                int index = i;
                var managerButton = new Button(() =>
                {
                    if (managers[selectedManager].configuration != null)
                    {
                        managers[selectedManager].configuration.showPaintGrid = false;
                        managers[selectedManager].configuration.showGizmos = false;
                    }

                    selectedManager = index;
                    EditorPrefs.SetInt("TWC_SELECTEDMANAGERINDEX", selectedManager);
                    if (managers[index].gameObject != null)
                        Selection.activeGameObject = managers[index].gameObject;

                    BuildPanel();
                })
                {
                    text = managers[i].name
                };

                if (i == selectedManager)
                    managerButton.SetBorder(2, TileWorldCreatorColor.Blue.GetColor());

                root.Add(managerButton);
            }

            selectedManager = EditorPrefs.GetInt("TWC_SELECTEDMANAGERINDEX");
            if (selectedManager > managers.Count - 1)
            {
                selectedManager = 0;
            }

            if (managers == null || managers.Count == 0)
            {
              
                var _lblError = new Label();
                _lblError.style.whiteSpace = WhiteSpace.Normal;
                _lblError.text = "No TileWorldCreator managers in scene.";
                root.Add(_lblError);
                return;
            }

            if (managers[selectedManager].configuration == null)
            {
                var _lblError = new Label();
                _lblError.style.whiteSpace = WhiteSpace.Normal;
                _lblError.text = "Selected manager does not have a configuration";
                root.Add(_lblError);
                return;
            }

            //  var _lblExecute = new Label();
            // _lblExecute.text = "Execute All Layers";
            // _lblExecute.style.marginTop = 5;

            // var _execute = new Button();
            // _execute.style.height = 35;
            // _execute.style.marginTop = 10;
            // _execute.text = "Execute All Layers";
            // _execute.RegisterCallback<ClickEvent>(evt => 
            // {
           
            //     managers[selectedManager].ExecuteBlueprintLayers();
            //     managers[selectedManager].ExecuteBuildLayers(ExecutionMode.FromScratch);
              
            // });

            var _execute = new Button(() =>
            {
                managers[selectedManager].ExecuteBlueprintLayers();
                managers[selectedManager].ExecuteBuildLayers(ExecutionMode.FromScratch);
            })
            {
                text = "Execute All Layers",
                style =
                {
                    height = 35,
                    marginTop = 10
                }
            };

            // var _lblPaint = new Label();
            // _lblPaint.style.fontSize = 14;
            // _lblPaint.style.marginTop = 5;
            // _lblPaint.text = "2. Paint Mode";

            // root.Add(_lblPaint);

            var _paintContainer = new VisualElement();
            _paintContainer.style.backgroundColor = colorDarkGrey;
            _paintContainer.SetBorder(2);
            _paintContainer.SetMargin(5, 5, 5, 5);
            _paintContainer.SetPadding(5, 5, 5, 5);
            _paintContainer.style.display = DisplayStyle.None;

            var _gizmosContainer = new VisualElement();
            _gizmosContainer.style.backgroundColor = colorDarkGrey;
            _gizmosContainer.SetBorder(2);
            _gizmosContainer.SetMargin(5, 5, 5, 5);
            _gizmosContainer.SetPadding(5, 5, 5, 5);
            _gizmosContainer.style.display = managers[selectedManager].configuration.showGizmos ? DisplayStyle.Flex : DisplayStyle.None;

            var _buildLayersScrollView = new ScrollView();
            _buildLayersScrollView.style.maxHeight = 300;

            var _blueprintLayersScrollView = new ScrollView();
            _blueprintLayersScrollView.style.maxHeight = 300;

            var _toolbar = new VisualElement();
            _toolbar.SetMargin ( 2, 2, 2, 2);
            _toolbar.style.backgroundColor = colorDarkGrey;
            _toolbar.style.flexDirection = FlexDirection.Row;

            var _paintModeButton = new ToolbarButtonElement(new Vector2Int(40, 40), "", TileWorldCreatorUtilities.LoadImage("paintMode.twc"), true);
            _paintModeButton.Toggle = managers[selectedManager].configuration.showPaintGrid;
            

            // _paintContainer.SetEnabled(isPaintMode);
            
            var _showGizmosButton = new ToolbarButtonElement(new Vector2Int(40, 40), "", TileWorldCreatorUtilities.LoadImage("gizmos.twc"), true);
            _showGizmosButton.Toggle = managers[selectedManager].configuration.showGizmos;
            _showGizmosButton.tooltip = "Enable Gizmos for selected layer";

            _paintModeButton.clickable.clicked += () =>
            {
                // isPaintMode = !isPaintMode;

                if (managers[selectedManager] != null)
                {
                    managers[selectedManager].configuration.showGizmos = false;
                }

                _showGizmosButton.Toggle = false;
                _paintModeButton.Toggle = true;

                _gizmosContainer.style.display = DisplayStyle.None;
                _paintContainer.style.display = DisplayStyle.Flex;
              
                // if (!managers[selectedManager].configuration.showPaintGrid)
                // {
                //     managers[selectedManager].paintedPositions = new HashSet<Vector2>();
                //     // managers[selectedManager].paintModeActive = false;
                //     SceneView.duringSceneGui -= OnSceneGUI;
                //     // _paintModeButton.SetBorder(0);
                // }
                // else
                // {
                //     managers[selectedManager].paintedPositions = new HashSet<Vector2>();
                //     // managers[selectedManager].paintModeActive = true;
                //     SceneView.duringSceneGui += OnSceneGUI;
                //     // _paintModeButton.SetBorder(2, Color.green);
                // }

                managers[selectedManager].paintedPositions = new HashSet<Vector2>();
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;

            };


            _showGizmosButton.clickable.clicked += () =>
            {
                if (managers[selectedManager] != null)
                {
                    managers[selectedManager].configuration.showGizmos = true;
                    managers[selectedManager].configuration.showPaintGrid = false;
                }

                _paintModeButton.Toggle = false;
                _showGizmosButton.Toggle = true;
                _gizmosContainer.style.display = DisplayStyle.Flex;
                _paintContainer.style.display = DisplayStyle.None;
                // isPaintMode = false;
                // isGizmoMode = true;
                BuildPanel();
            };
          
            _toolbar.Add(_paintModeButton);
            _toolbar.Add(_showGizmosButton);

            root.Add(_toolbar);

            root.Add(_paintContainer);
            root.Add(_gizmosContainer);
            root.Add(_execute);

            var _lblSize = new Label("Brush Size");
            _lblSize.style.marginTop = 5;
            _lblSize.style.fontSize = 14;

            var _brushSize = new SliderInt();
            _brushSize.lowValue = 1;
            _brushSize.highValue = 10;
            _brushSize.showInputField = true;
            _brushSize.RegisterValueChangedCallback(evt => 
            {
                brushSize = (int)evt.newValue;
                managers[selectedManager].brushSize = brushSize;
            });

            _brushSize.value = brushSize;

            _paintContainer.Add(_lblSize);
            _paintContainer.Add(_brushSize);
            _paintContainer.Add(_blueprintLayersScrollView);

            _gizmosContainer.Add(_buildLayersScrollView);
          
            for (int i = 0; i < managers[selectedManager].configuration.blueprintLayerFolders.Count; i ++)
            {
                var _iIndex = i;
                var _foldout = new Foldout();
                _foldout.text = managers[selectedManager].configuration.blueprintLayerFolders[i].folderName;

                for (int j = 0; j < managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                {
                    if (managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers[j].lockFromPaint) continue;
                    
                    var _jIndex = j;
                    var _horizontalContainer = new VisualElement();
                    _horizontalContainer.style.marginBottom = 1;
                    _horizontalContainer.style.backgroundColor = colorDarkGrey;
                    // _horizontalContainer.SetBorder(1, Color.grey);
                    _horizontalContainer.style.flexDirection = FlexDirection.Row;

                    var _layerButton = new Button();
                    _layerButton.style.flexGrow = 1;
                    _layerButton.text = managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers[j].layerName;
                    _layerButton.name = managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers[j].guid;
                    _layerButton.clickable.clicked += () => // _layerButton.RegisterCallback<ClickEvent>(evt =>
                    {
                        if (managers[selectedManager].configuration.paintLayer ==  managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex] && managers[selectedManager].configuration.showPaintGrid)
                        {
                            managers[selectedManager].configuration.showPaintGrid = false;
                            _layerButton.SetBorder(0);
                        }
                        else
                        {
                            managers[selectedManager].configuration.showPaintGrid = true;
                            _layerButton.SetBorder(2, Color.white);
                        }
                        
                        selectedLayerGuid = managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex].guid;
                        managers[selectedManager].configuration.paintLayer = managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex];

                        managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex].SetAsset(managers[selectedManager].configuration);
                        

                        for (int i = 0; i < managers[selectedManager].configuration.blueprintLayerFolders.Count; i ++)
                        {
                            for (int j = 0; j < managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                            {
                                if (managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers[j].guid != selectedLayerGuid)
                                {
                                    root.Q<Button>(managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers[j].guid)?.SetBorder(0);
                                }
                            }
                        }
                        
                    };

                    var _clearButton = new Button();
                    _clearButton.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("clear.twc");
                    _clearButton.style.width = 24;
                    _clearButton.style.height = 24;
                    _clearButton.tooltip = "Clear layer";
                    _clearButton.clickable.clicked += () => //RegisterCallback<ClickEvent>(evt => 
                    {
                        selectedLayerGuid = managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex].guid;
                        managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex].SetAsset(managers[selectedManager].configuration);

                        managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex].ClearLayer();
                        managers[selectedManager].configuration.ExecuteBlueprintLayers(managers[selectedManager]);
                        managers[selectedManager].configuration.ExecuteBuildLayers(managers[selectedManager], true);
                    };

                     var _fillButton = new Button();
                    _fillButton.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("fill.twc");
                    _fillButton.style.width = 24;
                    _fillButton.style.height = 24;
                    _fillButton.tooltip = "Fill layer";
                    _fillButton.clickable.clicked += () => //RegisterCallback<ClickEvent>(evt => 
                    {
                        selectedLayerGuid = managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex].guid;
                        managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex].SetAsset(managers[selectedManager].configuration);

                        managers[selectedManager].configuration.blueprintLayerFolders[_iIndex].blueprintLayers[_jIndex].FillLayer();
                        managers[selectedManager].configuration.ExecuteBlueprintLayers(managers[selectedManager]);
                        managers[selectedManager].configuration.ExecuteBuildLayers(managers[selectedManager]);
                    };

                    _horizontalContainer.Add(_layerButton);    
                    _horizontalContainer.Add(_clearButton);
                    _horizontalContainer.Add(_fillButton);

                    _foldout.Add(_horizontalContainer);

                    
                }

                _blueprintLayersScrollView.Add(_foldout);
            }

            for (int i = 0; i < managers[selectedManager].configuration.buildLayerFolders.Count; i ++)
            {
                var _iIndex = i;
                var _foldout = new Foldout();
                _foldout.text = managers[selectedManager].configuration.buildLayerFolders[i].folderName;

                for (int j = 0; j < managers[selectedManager].configuration.buildLayerFolders[i].buildLayers.Count; j ++)
                {

                    var _jIndex = j;
                    var _layerButton = new Button();
                    _layerButton.style.flexGrow = 1;
                    _layerButton.style.height = 24;
                    _layerButton.text = managers[selectedManager].configuration.buildLayerFolders[i].buildLayers[j].layerName;
                    _layerButton.name = managers[selectedManager].configuration.buildLayerFolders[i].buildLayers[j].guid;
                    _layerButton.clickable.clicked += () => //.RegisterCallback<ClickEvent>(evt =>
                    {
                        _layerButton.SetBorder(2, Color.white);
                        managers[selectedManager].configuration.gizmoLayer = managers[selectedManager].configuration.buildLayerFolders[_iIndex].buildLayers[_jIndex] as BuildLayer;

                        for (int i = 0; i < managers[selectedManager].configuration.buildLayerFolders.Count; i ++)
                        {
                            for (int j = 0; j < managers[selectedManager].configuration.buildLayerFolders[i].buildLayers.Count; j ++)
                            {
                                if (managers[selectedManager].configuration.buildLayerFolders[i].buildLayers[j].guid != managers[selectedManager].configuration.gizmoLayer.guid)
                                {
                                    root.Q<Button>(managers[selectedManager].configuration.buildLayerFolders[i].buildLayers[j].guid)?.SetBorder(0);
                                }
                            }
                        }
                    };

                    _foldout.Add(_layerButton);
                }

                _buildLayersScrollView.Add(_foldout);
            }
        }

        public void OnSceneGUI(SceneView sceneView) 
        {
            if (managers == null) return;
            if (managers.Count == 0) return;
            if (managers[selectedManager] == null) return;
            if (managers[selectedManager].configuration == null) return;
            if (managers[selectedManager].configuration.showPaintGrid == false) return;


            Event _event = Event.current;

            if ((_event.type == EventType.MouseDown || _event.type == EventType.MouseDrag || _event.type == EventType.MouseUp) && _event.button == 2)
            {
                return;
            }

            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // do not paint when user is navigating    
#if UNITY_EDITOR_OSX
            if (_event.keyCode == KeyCode.LeftAlt)
            {
                altClick = true;
            }
#else
            if (_event.keyCode == KeyCode.LeftAlt || _event.keyCode == KeyCode.RightAlt)
            {
                altClick = true;
            }
#endif
          
           
            if (_event.type == EventType.MouseDown && !altClick)
                {
                    undoGroup = Undo.GetCurrentGroup();
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName("Paint Cells");

                    managers[selectedManager].paintedPositions.Clear(); // = new List<Vector2>();

                    if (_event.button == 0)
                    {
                        managers[selectedManager].paintState = true;
                    }
                    else if (_event.button == 1)
                    {
                        managers[selectedManager].paintState = false;
                    }

                    SetCellAt(_event.mousePosition);

                    _event.Use();
                }

            if (_event.type == EventType.MouseDrag && !altClick)
            {
                if (_event.button == 0)
                {
                    managers[selectedManager].paintState = true;
                }
                else
                {
                    managers[selectedManager].paintState = false;
                }

                SetCellAt(_event.mousePosition);

                _event.Use();
            }

            if (_event.type == EventType.MouseUp && !altClick)
            {
                SetCellsFinalize(_event.button == 0 ? true : false);

                Undo.CollapseUndoOperations(undoGroup);
                undoGroup = -1;

                altClick = false;              
            }


            if (_event.GetTypeForControl(controlID) == EventType.KeyUp)
            {
                #if UNITY_EDITOR_OSX
                if (_event.control)
                {
                    altClick = false;
                }
                #else
                if (_event.keyCode == KeyCode.LeftAlt || _event.keyCode == KeyCode.RightAlt)
                {
                    altClick = false;
                }
                #endif
            }
        }

        void SetCellAt(Vector2 _position)
        {
            var _mousePos = new Vector2(_position.x, _position.y );
            var _isMouseOverGrid = IsMouseOverGrid(_mousePos);
            if (_isMouseOverGrid)
            {
                var _wp = GetWorldPosition(_position);
                var _gridPos = GetGridPosition(_wp);
                int _halfSize = Mathf.CeilToInt(brushSize * 0.5f);
                float _radius = brushSize * 0.5f;
                
                Matrix4x4 _oldMatrix = Gizmos.matrix;

                Gizmos.matrix = Matrix4x4.TRS(managers[selectedManager].transform.position, managers[selectedManager].transform.rotation, Vector3.one);
                Gizmos.color = Color.yellow;

                for (int x = -_halfSize; x <= _halfSize; x ++)
                {
                    for (int y = -_halfSize; y <= _halfSize; y ++)
                    {
                        Vector2 _pos = new Vector2(_gridPos.x + x, _gridPos.y + y);

                        
                        if (Vector2.Distance(_gridPos, _pos) <= _radius)
                        {
                            // Vector2 localPos = new Vector2(_pos.x * managers[selectedManager].configuration.cellSize, _pos.y * managers[selectedManager].configuration.cellSize);
                            
                            if (_pos.x >= 0)
                            {
                                if (!managers[selectedManager].paintedPositions.Contains(_pos))
                                {
                                    managers[selectedManager].paintedPositions.Add(_pos);    
                                }
                            }
                        }
                    }
                }

                // Restore the previous Gizmos matrix
                Gizmos.matrix = _oldMatrix;
                Gizmos.color = Color.white;
            }
        }

        void SetCellsFinalize(bool _state)
        {
            var _layer = managers[selectedManager].configuration.GetBlueprintLayerByGuid(selectedLayerGuid);
            // blueprintLayer = _layer;

            if (_layer != null)
            {
                // Register Undo before making changes
                // blueprintLayerState = new BlueprintLayer.BlueprintLayerState(_layer);
                Undo.RegisterCompleteObjectUndo(_layer, _state ? "Paint Cells" : "Erase Cells");

                if (_state)
                {
                    _layer.AddCells(managers[selectedManager].paintedPositions);
                }
                else
                {
                    _layer.RemoveCells(managers[selectedManager].paintedPositions);
                }

                managers[selectedManager].OnBlueprintLayersReady -= BlueprintLayersReady;
                managers[selectedManager].OnBlueprintLayersReady += BlueprintLayersReady;
                managers[selectedManager].ExecuteBlueprintLayers();

            }
            else
            {
                Debug.Log("Select layer to paint cells");
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }

        void BlueprintLayersReady()
        {
            managers[selectedManager].ExecuteBuildLayers();
        }
        
        Vector2 GetGridPosition(Vector3 worldPos)
		{
			// Convert world position to local space relative to the rotated grid
			Vector3 localPos = managers[selectedManager].transform.InverseTransformPoint(worldPos);

			// Snap to the closest grid cell
			int gridX = Mathf.RoundToInt(localPos.x / managers[selectedManager].configuration.cellSize);
			int gridY = Mathf.RoundToInt(localPos.z / managers[selectedManager].configuration.cellSize);

			return new Vector2(gridX, gridY);
		}

        Vector3 GetWorldPosition(Vector2 _mousePos)
		{
			Ray _ray = HandleUtility.GUIPointToWorldRay(_mousePos);

            // Create a plane that fully aligns with the grid's rotation
            var _managerPosition = managers[selectedManager].transform.position;
            var _layer = managers[selectedManager].configuration.paintLayer;
            
			Plane gridPlane = new Plane(managers[selectedManager].transform.rotation * Vector3.up, new Vector3(_managerPosition.x, _managerPosition.y + _layer.defaultLayerHeight, _managerPosition.z));

			float _dist;
			if (gridPlane.Raycast(_ray, out _dist))
			{
				return _ray.GetPoint(_dist);
			}

			return Vector3.zero; 
        }

        bool IsMouseOverGrid(Vector2 _mousePos)
        {
            bool _return = false;

            var _wp = GetWorldPosition(_mousePos);
            var _gridPos = GetGridPosition(_wp);

         
            // var _cellSize = managers[selectedManager].configuration.cellSize;
            if (_gridPos.x >= 0 && _gridPos.y >= 0 && _gridPos.x < managers[selectedManager].configuration.width &&
            _gridPos.y < managers[selectedManager].configuration.height)
            {
                _return = true;
            }

            return _return;
        }
    }
}
#endif