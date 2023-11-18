using System;
using ImGuiNET;
using System.Numerics;
using System.Collections.Generic;
using ABEngine.ABERuntime;
using System.Linq;
using System.IO;
using Halak;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABEditor
{
    // Dummy
    class Node
    {
        public int ID;
        public string Name;
        public Vector2 Pos, Size;
        public AnimationState AnimState;
        public int InputsCount, OutputsCount;

        public EditorSprite pvTex;
        public IntPtr pvTexPtr;

        public int OccupiedInput;
        public int OccupiedOutput;
        public bool isEntry;

        public Node(int id, string name, Vector2 pos, AnimationState animState, int inputs_count, int outputs_count)
        {
            ID = id;
            Name = name;
            Pos = pos;
            AnimState = animState;
            InputsCount = inputs_count;
            OutputsCount = outputs_count;
            OccupiedInput = OccupiedOutput = 0;

            pvTexPtr = IntPtr.Zero;
        }

        public Vector2 GetInputSlotPos(int slot_no, float scale) { return new Vector2(Pos.X * scale, Pos.Y * scale + Size.Y * ((float) slot_no + 1) / ((float) InputsCount + 1)); }
        public Vector2 GetOutputSlotPos(int slot_no, float scale)  { return new Vector2(Pos.X * scale + Size.X, Pos.Y * scale + Size.Y * ((float)slot_no + 1) / ((float)OutputsCount + 1)); }
    }

    class NodeLink
    {
        public int InputId, InputSlot, OutputId, OutputSlot;

        public AnimationTransition transition { get; set; }

        public NodeLink(int input_idx, int input_slot, int output_idx, int output_slot, AnimationTransition animTrans) { InputId = input_idx; InputSlot = input_slot; OutputId = output_idx; OutputSlot = output_slot; transition = animTrans; }
    }

    public class AnimGraphEditor
    {
        public static bool isActive;

        static List<Node> nodes;
        static List<NodeLink> links;
        static bool inited = false;
        static bool show_grid = true;
        static int node_selected = -1;
        static int link_selected = -1;

        static Vector2 scrolling = Vector2.Zero;
        static ImGuiIOPtr io;

        static uint bgCol, GRID_COLOR, lineCol, lineHoverCol, lineSelCol, nodeSelBgCol, nodeDefBgCol, nodeRectCol, nodeSlotCol, entryNodeCol, entrySelNodeCol;

        // Constants
        static float NODE_SLOT_RADIUS = 4.0f;
        static Vector2 NODE_WINDOW_PADDING = new Vector2(8.0f, 8.0f);

        // Vars
        static int node_hovered_in_list = -1;
        static int node_hovered_in_scene = -1;
        static int link_hovered_in_scene = -1;

        static bool open_context_menu = false;
        static bool contextNode = false;

        // Link drawing
        static Node linkStartNode;
        static int linkStartSlot;
        static bool drawingLink = false;

        static List<string> comparerCombo = new List<string>() { "=", ">", "<" };

        static float scale = 1f;

        static string savePath = "";

        public static void Init()
        {
            io = ImGui.GetIO();

            animator = new Animator();
            nodes = new List<Node>();
            links = new List<NodeLink>();

            // Params
            animator.SetParameter("Speed", 0);

            //SpriteClip idleClip = new SpriteClip("Sprites/HeroKnight_Idle.abanim2d");
            //AnimationState idleState = new AnimationState()
            //{
            //    name = "Idle",
            //    clip = idleClip
            //};
            //var idleNode = new Node(0, idleState.name, new Vector2(40, 50), idleState, 1, 1);

            //EditorSprite idlePv = new EditorSprite(idleClip.imgPath, _rs);
            //idleNode.pvTex = idlePv;
            //idleNode.pvTexPtr = Editor.GetImGuiTexture(idlePv.frameView);

            //nodes.Add(idleNode);

            //SpriteClip walkclip = new SpriteClip("Sprites/HeroKnight_Walk.abanim2d");
            //AnimationState walkState = new AnimationState()
            //{
            //    name = "Walk",
            //    clip = walkclip
            //};
            //var walkNode = new Node(1, walkState.name, new Vector2(200, 200), walkState, 1, 1);

            //EditorSprite walkPv = new EditorSprite(walkclip.imgPath, _rs);
            //walkNode.pvTex = walkPv;
            //walkNode.pvTexPtr = Editor.GetImGuiTexture(walkPv.frameView);

            //nodes.Add(walkNode);

            //var idleToWalk = new AnimationTransition(AnimTransCompareType.Greater)
            //{
            //    startState = idleState,
            //    endState = walkState,
            //    parameterKey = "Speed",
            //    targetValue = 0
            //};


            //NodeLink link = new NodeLink(0, 0, 1, 0, idleToWalk);
            //nodes[link.InputIdx].OccupiedOutput++;
            //nodes[link.OutputIdx].OccupiedInput++;

            //links.Add(link);

            //links.Add(new NodeLink(1, 0, 2, 1));

            
            // Imgui vars
            bgCol = ImGui.GetColorU32(new Vector4(60 / 255f, 60 / 255f, 70 / 255f, 200 / 255f));
            GRID_COLOR = ImGui.GetColorU32(new Vector4(200 / 255f, 200 / 255f, 200 / 255f, 40 / 255f));
            lineCol = ImGui.GetColorU32(new Vector4(200 / 255f, 200 / 255f, 100 / 255f, 255 / 255f));
            nodeSelBgCol = ImGui.GetColorU32(new Vector4(75 / 255f, 75 / 255f, 75 / 255f, 255 / 255f));
            nodeDefBgCol = ImGui.GetColorU32(new Vector4(60 / 255f, 60 / 255f, 60 / 255f, 255 / 255f));
            nodeRectCol = ImGui.GetColorU32(new Vector4(100 / 255f, 100 / 255f, 100 / 255f, 255 / 255f));
            nodeSlotCol = ImGui.GetColorU32(new Vector4(150 / 255f, 150 / 255f, 150 / 255f, 150 / 255f));
            lineHoverCol = ImGui.GetColorU32(new Vector4(1f, 0.7f, 0f, 1f));
            lineSelCol = ImGui.GetColorU32(new Vector4(0f, 1f, 0f, 1f));
            entryNodeCol = ImGui.GetColorU32(new Vector4(109 / 255f, 82 / 255f, 209 / 255f, 255 / 255f));
            entrySelNodeCol = ImGui.GetColorU32(new Vector4(109 / 255f, 82 / 255f, 230 / 255f, 255 / 255f));


            
        }

        static Animator animator;
        public static void SetAnimGraph(string graphPath)
        {
            animator = new Animator();
            nodes = new List<Node>();
            links = new List<NodeLink>();

            string fullPath = Editor.AssetPath + graphPath;
            if(File.Exists(fullPath))
            {
                savePath = fullPath;
                string graphText = File.ReadAllText(savePath);

                // TODO Read anim graph
                if (!string.IsNullOrEmpty(graphText))
                {
                    animator = new Animator(graphPath);

                    JValue animData = JValue.Parse(graphText);

                    List<AnimationState> states = new List<AnimationState>();

                    // States
                    foreach (var stateKV in animData["States"].IndexedArray())
                    {
                        AnimationState newState = new AnimationState();
                        newState.Deserialize(stateKV.Value.ToString());
                        states.Add(newState);
                        //Node stateNode = nodes[stateKV.Key];
                        //stateNode.AnimState = newState;
                    }

                    // Nodes
                    foreach (var nodeKV in animData["Nodes"].IndexedArray())
                    {
                        JValue nodeData = nodeKV.Value;
                        float posX = nodeData["PosX"];
                        float posY = nodeData["PosY"]; ;
                        int inputC = nodeData["InpCount"];
                        int outputC = nodeData["OutCount"];

                        Node newNode = new Node(nodeData["ID"], nodeData["Name"], new Vector2(posX, posY), null, inputC, outputC);
                        newNode.OccupiedInput = nodeData["OccInpCount"];
                        newNode.OccupiedOutput = nodeData["OccOutCount"];

                        Guid stateUID = Guid.Parse(nodeData["StateUID"]);
                        AnimationState newState = states.FirstOrDefault(s => s.stateUID.Equals(stateUID));
                        newNode.AnimState = newState;

                        // Preview
                        var exNode = nodes.FirstOrDefault(n => n.AnimState.stateUID.Equals(stateUID));
                        if (exNode == null)
                        {
                            EditorSprite statePV = new EditorSprite(AssetCache.CreateTexture2D(newState.clip.imgPath));
                            newNode.pvTex = statePV;
                            newNode.pvTexPtr = Editor.GetImGuiTexture(statePV.frameView);
                        }
                        else
                        {
                            newNode.pvTex = exNode.pvTex;
                            newNode.pvTexPtr = exNode.pvTexPtr;
                        }

                        if (nodeKV.Key == 0)
                            newNode.isEntry = true;

                        nodes.Add(newNode);
                    }

                    nodes = nodes.OrderBy(n => n.ID).ToList();

                    // Links
                    foreach (var linkKV in animData["Links"].IndexedArray())
                    {
                        var linkData = linkKV.Value;
                        NodeLink link = new NodeLink(linkData["InpIdx"], linkData["InpSlot"], linkData["OutIdx"], linkData["OutSlot"], null);
                        links.Add(link);
                    }

                    // Transitions
                    foreach (var transKV in animData["Transitions"].IndexedArray())
                    {
                        AnimationTransition newTrans = new AnimationTransition(AnimTransCondition.Default());
                        newTrans.SetAnimator(animator);
                        newTrans.Deserialize(transKV.Value.ToString());

                        NodeLink transLink = links[transKV.Key];
                        transLink.transition = newTrans;
                      
                    }
                }

                isActive = true;
            }
        }

        static void SerializeNode(Node node, JsonArrayBuilder statesJArr, JsonArrayBuilder nodesJArr, List<AnimationState> exStates)
        {
            if (!exStates.Contains(node.AnimState))
            {
                node.AnimState.name = node.Name;
                statesJArr.Push(node.AnimState.Serialize());
                exStates.Add(node.AnimState);
            }

            JsonObjectBuilder nodeJ = new JsonObjectBuilder(200);
            nodeJ.Put("ID", node.ID);
            nodeJ.Put("StateUID", node.AnimState.stateUID.ToString());
            nodeJ.Put("Name", string.IsNullOrEmpty(node.Name) ? "" : node.Name);
            nodeJ.Put("PosX", node.Pos.X);
            nodeJ.Put("PosY", node.Pos.Y);

            nodeJ.Put("InpCount", node.InputsCount);
            nodeJ.Put("OutCount", node.OutputsCount);
            nodeJ.Put("OccInpCount", node.OccupiedInput);
            nodeJ.Put("OccOutCount", node.OccupiedOutput);

            nodesJArr.Push(nodeJ.Build());
        }

        static void SerializeLink(NodeLink link, JsonArrayBuilder transesJArr, JsonArrayBuilder linksJArr)
        {
            transesJArr.Push(link.transition.Serialize());

            JsonObjectBuilder linkJ = new JsonObjectBuilder(100);
            linkJ.Put("InpIdx", link.InputId);
            linkJ.Put("InpSlot", link.InputSlot);
            linkJ.Put("OutIdx", link.OutputId);
            linkJ.Put("OutSlot", link.OutputSlot);

            linksJArr.Push(linkJ.Build());
        }

        static void SaveGraph()
        {
            if (string.IsNullOrEmpty(savePath) || nodes.Count == 0)
                return;

            JsonObjectBuilder animJ = new JsonObjectBuilder(3000);


            JsonArrayBuilder paramsJArr = new JsonArrayBuilder(500);

            // Params
            foreach (var paramKV in animator.parameters)
            {
                JsonObjectBuilder paramJ = new JsonObjectBuilder(60);
                paramJ.Put("Key", paramKV.Key);
                paramJ.Put("Value", paramKV.Value);

                paramsJArr.Push(paramJ.Build());
            }

            JsonArrayBuilder statesJArr = new JsonArrayBuilder(1000);
            JsonArrayBuilder nodesJArr = new JsonArrayBuilder(1000);

            List<AnimationState> exStates = new List<AnimationState>();
            var entryNode = nodes.FirstOrDefault(n => n.isEntry);
            SerializeNode(entryNode, statesJArr, nodesJArr, exStates);
            foreach (var node in nodes)
            {
                if (node == entryNode)
                    continue;
                SerializeNode(node, statesJArr, nodesJArr, exStates);
            }

            JsonArrayBuilder transesJArr = new JsonArrayBuilder(500);
            JsonArrayBuilder linksJArr = new JsonArrayBuilder(500);

            foreach (var link in links)
            {
                SerializeLink(link, transesJArr, linksJArr);
            }

            animJ.Put("Params", paramsJArr.Build());
            animJ.Put("States", statesJArr.Build());
            animJ.Put("Transitions", transesJArr.Build());
            animJ.Put("Nodes", nodesJArr.Build());
            animJ.Put("Links", linksJArr.Build());

            File.WriteAllText(savePath, animJ.Build().Serialize());
        }

        static unsafe void CheckClipDrop(Vector2 windowPos, Vector2 offset)
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("ClipFileInd");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    var clipFilePath = AssetsFolderView.files[srcIndex];
                    Node exNode = nodes.FirstOrDefault(n => n.AnimState.clip.clipAssetPath.Equals(clipFilePath));

                    if(exNode == null)
                    {
                        SpriteClip newClip = new SpriteClip(clipFilePath);
                        AnimationState newState = new AnimationState(newClip);
                        Node newNode = new Node(nodes.Count, newState.name, ImGui.GetMousePos() / scale - offset / scale, newState, 1, 1);
                        EditorSprite newPv = new EditorSprite(AssetCache.CreateTexture2D(newClip.imgPath));
                        newNode.pvTex = newPv;
                        newNode.pvTexPtr = Editor.GetImGuiTexture(newPv.frameView);

                        if (nodes.Count == 0)
                            newNode.isEntry = true;

                        nodes.Add(newNode);
                    }
                    else
                    {
                        Node newNode = new Node(nodes.Count, exNode.Name, ImGui.GetMousePos() / scale - offset / scale, exNode.AnimState, 1, 1);
                        newNode.pvTex = exNode.pvTex;
                        newNode.pvTexPtr = exNode.pvTexPtr;
                        nodes.Add(newNode);
                    }
                }


                ImGui.EndDragDropTarget();
            }
        }

        static string renameParamKey;

        public static void Draw(float gameTime)
        {
            if (!isActive)
                return;

            node_hovered_in_list = -1;
            node_hovered_in_scene = -1;
            link_hovered_in_scene = -1;
            open_context_menu = false;
     
            ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);
            ImGui.Begin("Animation Graph Editor");

            ImGui.BeginChild("param_list", new Vector2(200, 0));
            ImGui.Text("Parameters");
            ImGui.Separator();

            int editInd = -1;
            int curInd = 0;
            bool newKey = false;
            string editKey = "";
            float editVal = 0;
            foreach (var paramPair in animator.parameters)
            {
                if (paramPair.Key.Equals(renameParamKey))
                {
                    string newParamName = paramPair.Key;
                    if (ImGui.InputText("##paramName", ref newParamName, 100, ImGuiInputTextFlags.CharsNoBlank | ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        if (!string.IsNullOrEmpty(newParamName) && !animator.parameters.ContainsKey(newParamName))
                        {
                            editInd = curInd;
                            newKey = true;
                            editKey = newParamName;
                            editVal = paramPair.Value;

                            // Change transitions keys
                            //var changeLinks = links.Where(l => !string.IsNullOrEmpty(l.transition.parameterKey) && l.transition.parameterKey.Equals(paramPair.Key));
                            //foreach (var link in changeLinks)
                            //{
                            //    link.transition.parameterKey = newParamName;
                            //}
                        }
              
                        renameParamKey = "";
                    }
                }
                else
                {
                    ImGui.Text(paramPair.Key);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        renameParamKey = paramPair.Key;
                }
                ImGui.SameLine();
                float val = paramPair.Value;
                if (ImGui.InputFloat("##" + paramPair.Key, ref val))
                {
                    editInd = curInd;
                    editKey = paramPair.Key;
                    editVal = val;
                }                   
                    

                curInd++;
            }

            if(editInd != -1)
            {
                if (newKey)
                {
                    string oldKey = animator.parameters.ElementAt(editInd).Key;
                    animator.parameters.Remove(oldKey);
                    animator.parameters.Add(editKey, editVal);
                }
                else
                    animator.parameters[editKey] = editVal;
            }

            if(ImGui.Button("New Parameter"))
            {
                if(!animator.parameters.ContainsKey("NewParam"))
                {
                    animator.SetParameter("NewParam", 0);
                }
            }

            float spaceH = ImGui.GetWindowHeight() - ImGui.GetCursorPosY();
            ImGui.Dummy(Vector2.UnitY * (spaceH - 50));
            ImGui.Separator();
            ImGui.Spacing();
            if(ImGui.Button("Save & Close"))
            {
                SaveGraph();
                isActive = false;
            }

            ImGui.EndChild();
            if (ImGui.IsItemClicked())
                renameParamKey = "";
            ImGui.SameLine();

            // Graph area
            ImGui.BeginGroup();
            
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.One);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, bgCol);
            ImGui.BeginChild("scrolling_region", Vector2.Zero, true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            ImGui.PopStyleVar(); // WindowPadding
            ImGui.PushItemWidth(120.0f);
            
            Vector2 offset = ImGui.GetCursorScreenPos() + scrolling * scale;
            ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

            // Draw grid
            float GRID_SZ = 64.0f * scale;
            Vector2 win_pos = ImGui.GetCursorScreenPos();
            Vector2 canvas_sz = ImGui.GetWindowSize();
            for (float x = scrolling.X % GRID_SZ; x < canvas_sz.X; x += GRID_SZ)
                draw_list.AddLine(new Vector2(x, 0.0f) + win_pos, new Vector2(x, canvas_sz.Y) + win_pos, GRID_COLOR);
            for (float y = scrolling.Y % GRID_SZ; y < canvas_sz.Y; y += GRID_SZ)
                draw_list.AddLine(new Vector2(0.0f, y) + win_pos, new Vector2(canvas_sz.X, y) + win_pos, GRID_COLOR);

            Vector2 mouseGraphPos = ImGui.GetMousePos();

            // Display links
            draw_list.ChannelsSplit(2);
            draw_list.ChannelsSetCurrent(0); // Background
            for (int link_idx = 0; link_idx < links.Count; link_idx++)
            {
                NodeLink link = links[link_idx];
                Node node_out = nodes.First(n => n.ID == link.InputId);
                Node node_inp = nodes.First(n => n.ID == link.OutputId);
                Vector2 p1 = offset + node_out.GetOutputSlotPos(link.InputSlot, scale);
                Vector2 p2 = offset + node_inp.GetInputSlotPos(link.OutputSlot, scale);
                //draw_list.AddBezierCurve(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, lineCol, 3.0f);

                float minX = p1.X, maxX = p2.X, minY = p1.Y, maxY = p2.Y;
                if(p1.X > p2.X)
                {
                    minX = p2.X;
                    maxX = p1.X;
                }

                if(p1.Y > p2.Y)
                {
                    minY = p2.Y;
                    maxY = p1.Y;
                }

                bool selected = link_idx == link_selected;
                bool hovered = mouseGraphPos.X > minX && mouseGraphPos.X < maxX && mouseGraphPos.Y > minY && mouseGraphPos.Y < maxY;
                if(link_hovered_in_scene == -1 && hovered)
                {
                    link_hovered_in_scene = link_idx;
                }

                if(selected)
                    draw_list.AddBezierCubic(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, lineSelCol, 3.0f);
                else if(link_hovered_in_scene == link_idx)
                    draw_list.AddBezierCubic(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, lineHoverCol, 3.0f);
                else
                    draw_list.AddBezierCubic(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, lineCol, 3.0f);

            }

            // Draw current link
            if (drawingLink)
            {
                Vector2 mousePos = ImGui.GetMousePos();
                draw_list.AddBezierCubic(offset + linkStartNode.GetOutputSlotPos(linkStartSlot, scale), offset + linkStartNode.GetOutputSlotPos(linkStartSlot, scale) + new Vector2(+50, 0), mousePos + new Vector2(-50, 0), mousePos, lineCol, 3.0f);
            }

            // Display nodes
            for (int node_idx = 0; node_idx < nodes.Count; node_idx++)
            {
                
                Node node = nodes[node_idx];
                ImGui.PushID(node.ID);
                Vector2 node_rect_min = offset + node.Pos * scale;

                // Draw pv offscreen
                if (node.pvTex != null)
                {
                    var state = node.AnimState;
                    var clip = state.clip;
                    _cl.SetFramebuffer(node.pvTex.spriteFB);
                    _cl.SetFullViewports();
                    _cl.ClearColorTarget(0, RgbaFloat.Black);
                    if ((gameTime - state.sampleFreq) > state.lastFrameTime)
                    {
                        state.curFrame++;
                        if (state.curFrame >= clip.frameCount)
                            state.curFrame = 0;
                        state.lastFrameTime = gameTime;

                        node.pvTex.SetUVPosScale(clip.uvPoses[state.curFrame], clip.uvScales[state.curFrame]);
                    }
                    node.pvTex.DrawEditor();
                }

                // Display node contents first
                draw_list.ChannelsSetCurrent(1); // Foreground
                bool old_any_active = ImGui.IsAnyItemActive();
                ImGui.SetCursorScreenPos(node_rect_min + NODE_WINDOW_PADDING * scale);

                Vector2 pvSize = new Vector2(100, 100) * scale;
                ImGui.BeginGroup(); // Lock horizontal position

                ImGui.SetNextItemWidth(pvSize.X);
                ImGui.Text(node.Name);

                if (node.pvTexPtr != IntPtr.Zero)
                    ImGui.Image(node.pvTexPtr, pvSize);
                //ImGui.SliderFloat("##value", ref node.Value, 0.0f, 1.0f, "Alpha %.2f");
                //ImGui.ColorEdit3("##color", ref node.Color.X);
                ImGui.EndGroup();

                // Save the size of what we have emitted and whether any of the widgets are being used
                bool node_widgets_active = (!old_any_active && ImGui.IsAnyItemActive());
                Vector2 boxSize = ImGui.GetItemRectSize();
                boxSize.X = pvSize.X;
                node.Size =  boxSize + NODE_WINDOW_PADDING * 2f * scale;
                Vector2 node_rect_max = node_rect_min + node.Size;

                // Display node box
                draw_list.ChannelsSetCurrent(0); // Background
                ImGui.SetCursorScreenPos(node_rect_min);
                ImGui.InvisibleButton("node", node.Size);
                if (ImGui.IsItemHovered())
                {
                    node_hovered_in_scene = node.ID;
                    //open_context_menu |= ImGui.IsMouseClicked(1);
                }
                bool node_moving_active = ImGui.IsItemActive();
                if (node_widgets_active || node_moving_active)
                    node_selected = node.ID;
                if (node_moving_active && ImGui.IsMouseDragging(0))
                    node.Pos = node.Pos + io.MouseDelta / scale;

                uint node_bg_color = (node_hovered_in_list == node.ID || node_hovered_in_scene == node.ID || (node_hovered_in_list == -1 && node_selected == node.ID)) ? (node.isEntry ? entrySelNodeCol : nodeSelBgCol ) : (node.isEntry ? entryNodeCol : nodeDefBgCol);
                draw_list.AddRectFilled(node_rect_min, node_rect_max, node_bg_color, 4.0f);
                draw_list.AddRect(node_rect_min, node_rect_max, nodeRectCol, 4.0f);
                for (int slot_idx = 0; slot_idx < node.InputsCount; slot_idx++)
                    draw_list.AddCircleFilled(offset + node.GetInputSlotPos(slot_idx, scale), NODE_SLOT_RADIUS, nodeSlotCol);
                for (int slot_idx = 0; slot_idx < node.OutputsCount; slot_idx++)
                    draw_list.AddCircleFilled(offset + node.GetOutputSlotPos(slot_idx, scale), NODE_SLOT_RADIUS, nodeSlotCol);

                ImGui.PopID();
            }
            draw_list.ChannelsMerge();

            // Empty Frame
            ImGui.SetCursorScreenPos(win_pos);
            ImGui.Dummy(canvas_sz);

            // Drag drop clip
            CheckClipDrop(win_pos, offset);

            // Open context menu
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                if (node_hovered_in_scene != -1)
                {
                    contextNode = true;
                    open_context_menu = true;
                }
                else
                {
                    contextNode = false;
                    open_context_menu = false;
                }
            }
            else if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                open_context_menu = false;

                if(drawingLink)
                {
                    if (node_hovered_in_scene != -1)
                    {
                        Node connectNode = nodes.First(n => n.ID == node_hovered_in_scene);
                        if(linkStartNode != connectNode)
                        {
                            if(connectNode.InputsCount - connectNode.OccupiedInput == 0)
                                connectNode.InputsCount++;
                            connectNode.OccupiedInput++;
                            linkStartNode.OccupiedOutput++;

                            var trans = new AnimationTransition(AnimTransCondition.Default());
                            trans.startState = linkStartNode.AnimState;
                            trans.endState = connectNode.AnimState;
                            trans.SetAnimator(animator);

                            NodeLink link = new NodeLink(linkStartNode.ID, linkStartSlot, connectNode.ID, connectNode.InputsCount - 1, trans);
                            links.Add(link);
                        }
                    }

                    drawingLink = false;

                    linkStartNode = null;
                    linkStartSlot = -1;
                }

                if (!ImGui.IsPopupOpen("context_menu") && !ImGui.IsPopupOpen("link_edit"))
                {
                    if (link_hovered_in_scene != -1)
                    {
                        link_selected = link_hovered_in_scene;
                        ImGui.OpenPopup("link_edit");
                    }
                    else
                        link_selected = -1;
                }
                
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Backspace) && link_selected != -1)
            {
                NodeLink link = links[link_selected];
                DeleteLink(link);
            }

            if (open_context_menu)
            {
                ImGui.OpenPopup("context_menu");
                //if (node_hovered_in_list != -1)
                //    node_selected = node_hovered_in_list;
                if (node_hovered_in_scene != -1)
                    node_selected = node_hovered_in_scene;
            }

            // Link tooltip
            if (link_hovered_in_scene != -1)
            {
                ImGui.BeginTooltip();
                AnimationTransition trans = links[link_hovered_in_scene].transition;
                foreach (var cond in trans.conditions)
                {
                    ImGui.Text(cond.GetTransitionText());

                }
                ImGui.EndTooltip();
            }

            // Link edit
            if (ImGui.BeginPopup("link_edit"))
            {
                if (link_selected != -1)
                {
                    AnimationTransition trans = links[link_selected].transition;

                    for (int i = 0; i < trans.conditions.Length; i++)
                    {
                        AnimTransCondition cond = trans.conditions[i];
                        ImGui.PushItemWidth(100);
                        if (ImGui.BeginCombo("##paramCmb", cond.parameterKey))
                        {
                            foreach (var paramPair in animator.parameters)
                            {
                                bool selected = paramPair.Key.Equals(cond.parameterKey);
                                if (ImGui.Selectable(paramPair.Key, selected))
                                    cond.parameterKey = paramPair.Key;
                                if (selected)
                                    ImGui.SetItemDefaultFocus();
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.PopItemWidth();
                        ImGui.SameLine();
                        ImGui.PushItemWidth(40);
                        if (ImGui.BeginCombo("##condCmb", comparerCombo[(int)cond.paramCompareType]))
                        {
                            for (int j = 0; j < comparerCombo.Count; j++)
                            {
                                bool selected = (int)cond.paramCompareType == j;
                                if (ImGui.Selectable(comparerCombo[j], selected))
                                    cond.paramCompareType = (AnimTransCompareType)j;
                                if (selected)
                                    ImGui.SetItemDefaultFocus();
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.PopItemWidth();
                        ImGui.SameLine();
                        ImGui.PushItemWidth(80);
                        float target = cond.targetValue;
                        if (ImGui.InputFloat("##targetFlt", ref target, 0f, 0f, "%.2f"))
                            cond.targetValue = target;
                        ImGui.PopItemWidth();

                        ImGui.Text("Exit Time: ");
                        float exitTime = trans.exitTime;
                        if (ImGui.InputFloat("##exitFltInp", ref exitTime, 0f, 0f, "%.2f"))
                            trans.exitTime = exitTime;
                    }
                   
                }

                ImGui.EndPopup();
            }


            // Draw context menu
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            if (ImGui.BeginPopup("context_menu"))
            {
                Node node = node_selected != -1 ? nodes.First(n => n.ID == node_selected) : null;
                Vector2 scene_pos = ImGui.GetMousePosOnOpeningCurrentPopup() - offset;
                if (contextNode && node != null)
                {
                    ImGui.InputText("", ref node.Name, 100);
                    ImGui.Separator();
                    if(ImGui.MenuItem("Create Link"))
                    {
                        // Needs new output
                        if(node.OutputsCount - node.OccupiedOutput == 0)
                        {
                            node.OutputsCount++;
                        }

                        linkStartNode = node;
                        linkStartSlot = node.OutputsCount - 1;
                        drawingLink = true;
                    }

                    if (!node.isEntry)
                    {
                        if (ImGui.MenuItem("Make Entry", null, false, true))
                        {
                            nodes.FirstOrDefault(n => n.isEntry).isEntry = false;
                            node.isEntry = true;
                        }

                        if (ImGui.MenuItem("Delete", null, false, true))
                        {
                            node_selected = node_hovered_in_scene = -1;

                            var linksToDelete = new List<NodeLink>();

                            for (int l = 0; l < links.Count; l++)
                            {
                                NodeLink link = links[l];
                                if (link.InputId == node.ID || link.OutputId == node.ID)
                                    linksToDelete.Add(link);
                            }

    
                            foreach (var delLink in linksToDelete)
                                DeleteLink(delLink);

                            //for (int n = node.ID + 1; n < nodes.Count; n++)
                            //    nodes[n].ID--;
                            nodes.Remove(node);
                        }
                    }
                   
                }
                else
                {
                    if (ImGui.MenuItem("Add")) { nodes.Add(new Node(nodes.Count, "New State", scene_pos, new AnimationState() , 1, 1)); }
                }
                ImGui.EndPopup();
            }
            ImGui.PopStyleVar();

            scale = Math.Clamp(MathF.Round(scale + io.MouseWheel * 0.1f, 1), 0.3f, 1f);

            // Scrolling
            if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Middle, 0f))
                scrolling = scrolling + io.MouseDelta;

            ImGui.PopItemWidth();
            ImGui.EndChild();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.EndGroup();

            ImGui.End();
        }

        static void DeleteLink(NodeLink link)
        {
            Node outNode = nodes.First(n => n.ID == link.InputId);
            Node inNode = nodes.First(n => n.ID == link.OutputId);

            if (outNode.OutputsCount > 1)
                outNode.OutputsCount--;
            outNode.OccupiedOutput--;

            if (inNode.InputsCount > 1)
                inNode.InputsCount--;
            inNode.OccupiedInput--;

            links.Remove(link);

            // Shift following links
            var followOutputs = links.Where(l => l.InputId == link.InputId && l.InputSlot > link.InputSlot);
            foreach (var fLink in followOutputs)
            {
                fLink.InputSlot--;
            }

            var followInputs = links.Where(l => l.OutputId == link.OutputId && l.OutputSlot > link.OutputSlot);
            foreach (var fLink in followInputs)
            {
                fLink.OutputSlot--;
            }

            link_selected = -1;
            link_hovered_in_scene = -1;
        }
    }
}
