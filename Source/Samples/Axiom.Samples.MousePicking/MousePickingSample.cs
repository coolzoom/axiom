﻿#region MIT/X11 License

//Copyright (c) 2009 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

using Axiom.Animating;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using System.IO;
using System.Collections.Generic;
using Axiom.Components.RTShaderSystem;
using Axiom.Core.Collections;
using Microsoft.Office.Interop.Excel;
using System.Windows.Forms;
using System;
using System.Diagnostics;
using SIS = SharpInputSystem;

namespace Axiom.Samples.MousePicking
{
    /// <summary>
    /// Sample of selecting objects with a mouse
    /// </summary>
    public class MousePickingSample : SdkSample
    {
        private enum ShaderSystemLightingModel
        {
            PerVertexLighting,
            PerPixelLighting,
            NormalMapLightingTangentSpace,
            NormalMapLightingObjectSpace
        }
        private ShaderSystemLightingModel curLightingModel;
        private SubRenderState reflectionMapSubRS;
        private bool reflectionMapEnable;
        private Slider modifierValueSlider;
        private Slider reflectionPowerSlider;
        private const string ReflectionMapPowerSlider = "ReflectionPowerSlider";
        private const string ModifierValueSlider = "ModifierValueSlider";
        private const string MainEntityMesh = "sphere.mesh";
        private const string MainEntityName = "MainEntity";
        
        /// <summary>
        /// safety check for mouse picking sample calls
        /// </summary>
        private bool initialized = false;

        /// <summary>
        /// MouseSelector object
        /// </summary>
        private MouseSelector _MouseSelector = null;

        /// <summary>
        /// Sample menu GUI for setting selection mode
        /// </summary>
        private SelectMenu selectionModeMenu;

        /// <summary>
        /// sample label for displaying mouse coordinates
        /// </summary>
        protected Label MouseLocationLabel;

        private Dictionary<string, SceneNode> dictnode;
        private Dictionary<string, Entity> dictentity;
        private Dictionary<string, Vector3> dictOriginalPosition;
        private Slider SampleSliderX;
        private Slider SampleSliderY;
        private Slider SampleSliderZ;
        private Slider SampleSliderScale;
        public Label targetObjName;
        public Label StatusLabel;
        private string modelName = "sphere.mesh";
        private Mesh modelMesh ;
        private Vector3 modelSize;
        private EntityList targetEntities;
        private Entity layeredBlendingEntity;
        private Button SampleSaveButton;
        private System.Data.DataSet ds;
        private System.Data.DataTable dtRaw;
        /// <summary>
        /// Sample initialization
        /// </summary>
        public MousePickingSample()
        {
            this.layeredBlendingEntity = null;
            Metadata["Title"] = "Mouse Picking";
            Metadata["Description"] = "Demonstrates selecting a node with a mouse.";
            Metadata["Thumbnail"] = "thumb_picking.png";
            Metadata["Category"] = "Interaction";
            Metadata["Help"] = "Proof that Axiom is just the hottest thing. Bleh. So there. ^_^";
        }

        /// <summary>
        /// mouse pressed event override from SdkSample, calls MouseSelector's MousePressed method to start selection
        /// </summary>
        /// <param name="evt">MouseEventArgs</param>
        /// <param name="id">MouseButtonID</param>
        /// <returns>bool</returns>
        public override bool MousePressed(SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id)
        {
            if (this.initialized)
            {
                this._MouseSelector.MousePressed(evt, id);
            }
            return base.MousePressed(evt, id);
        }

        /// <summary>
        /// mouse moved event override from SdkSample, calls the MouseSelector's MouseMoved event for SelectionBox
        /// </summary>
        /// <param name="evt">MouseEventArgs</param>
        /// <returns>bool</returns>
        public override bool MouseMoved(SharpInputSystem.MouseEventArgs evt)
        {
            if (this.initialized)
            {
                this._MouseSelector.MouseMoved(evt);
                this.MouseLocationLabel.Caption = "(x:y) " + (evt.State.X.Absolute / (float)Camera.Viewport.ActualWidth).ToString() +
                                                  ":" +
                                                  (evt.State.Y.Absolute / (float)Camera.Viewport.ActualHeight).ToString();
            }
            return base.MouseMoved(evt);
        }

        /// <summary>
        /// mouse released event override from SdkSample, calls MouseSelector's MouseReleased method to end selection
        /// </summary>
        /// <param name="evt">MouseEventArgs</param>
        /// <param name="id">MouseButtonID</param>
        /// <returns>bool</returns>
        public override bool MouseReleased(SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id)
        {
            if (this.initialized)
            {
                this._MouseSelector.MouseReleased(evt, id);
            }
            //show selected items
            //targetObjName.Caption = _MouseSelector.Selection.ToString();
            string selectedobjs = "";
            foreach (var o in _MouseSelector.Selection)
            {
                selectedobjs += o.Name + " ";
            }
            targetObjName.Caption = selectedobjs;

            return base.MouseReleased(evt, id);
        }

        /// <summary>
        /// key pressed selection event to have the ability to keep the current selection, sets the MouseSelector.KeepPreviousSelection
        /// if the right or left control key is pressed
        /// </summary>
        /// <param name="evt">KeyEventArgs</param>
        /// <returns>bool</returns>
        public override bool KeyPressed(SharpInputSystem.KeyEventArgs evt)
        {
            if (this.initialized)
            {
                if (evt.Key == SharpInputSystem.KeyCode.Key_LCONTROL || evt.Key == SharpInputSystem.KeyCode.Key_RCONTROL)
                {
                    this._MouseSelector.KeepPreviousSelection = true;
                }
            }
            return base.KeyPressed(evt);
        }

        /// <summary>
        /// key pressed selection event to have the ability to keep the current selection, sets the MouseSelector.KeepPreviousSelection
        /// if the right or left control key is pressed
        /// </summary>
        /// <param name="evt">KeyEventArgs</param>
        /// <returns>bool</returns>
        public override bool KeyReleased(SharpInputSystem.KeyEventArgs evt)
        {
            if (this.initialized)
            {
                this._MouseSelector.KeepPreviousSelection = false;
            }
            return base.KeyReleased(evt);
        }

        /// <summary>
        /// Sets up the samples content
        /// </summary>
        protected override void SetupContent()
        {
            // set some ambient light
            SceneManager.AmbientLight = new ColorEx(1.0f, 0.2f, 0.2f, 0.2f);

            // create a skydome
            //SceneManager.SetSkyDome(true, "Examples/CloudySky", 5, 8);

            curLightingModel = ShaderSystemLightingModel.PerVertexLighting;
            targetEntities = new EntityList();
            targetEntities.Clear();
            reflectionMapSubRS = null;
            reflectionMapEnable = false;

            SceneManager.SetSkyBox(true, "Examples/NebulaSkyBox", 500);

            // setup some basic lighting for our scene
            //SceneManager.AmbientLight = new ColorEx(0.3f, 0.3f, 0.3f);
            SceneManager.CreateLight("ParticleSampleLight").Position = new Vector3(0, 0, 0);

            //// create a simple default point light
            //Light light = SceneManager.CreateLight("MainLight");
            //light.Position = new Vector3(20, 80, 50);

            // dim orange ambient and two bright orange lights to match the skybox
            SceneManager.AmbientLight = new ColorEx(0.3f, 0.2f, 0.0f);
            Light light = SceneManager.CreateLight("LightA");
            light.Position = new Vector3(2000, 1000, -1000);
            light.Diffuse = new ColorEx(1.0f, 0.5f, 0.0f);
            light = SceneManager.CreateLight("LightB");
            light.Position = new Vector3(2000, 1000, 1000);
            light.Diffuse = new ColorEx(1.0f, 0.5f, 0.0f);

            ////Create texture layer blending demonstration entity
            //this.layeredBlendingEntity = SceneManager.CreateEntity("LayeredBlendingMaterialEntity", MainEntityMesh);
            //this.layeredBlendingEntity.MaterialName = "RTSS/LayeredBlending";
            //this.layeredBlendingEntity.GetSubEntity(0).SetCustomParameter(2, Vector4.Zero);
            //childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
            //childNode.Position = new Vector3(300, 200, -200);
            //childNode.AttachObject(this.layeredBlendingEntity);
            //// create a plane for the plane mesh
            //var plane = new Plane();
            //plane.Normal = Vector3.UnitY;
            //plane.D = 200;

            //// create a plane mesh
            //MeshManager.Instance.CreatePlane("FloorPlane", ResourceGroupManager.DefaultResourceGroupName, plane, 200000, 200000,
            //                                  20, 20, true, 1, 50, 50, Vector3.UnitZ);

            //// create an entity to reference this mesh
            //Entity planeEntity = SceneManager.CreateEntity("Floor", "FloorPlane");
            //planeEntity.MaterialName = "Examples/RustySteel";
            //SceneManager.RootSceneNode.CreateChildSceneNode().AttachObject(planeEntity);

            // create an entity to have follow the path
            Entity ogreHead = SceneManager.CreateEntity("OgreHead", "ogrehead.mesh");

            // Load muiltiple heads for box select and demonstrating single select with stacked objects
            // create a scene node for each entity and attach the entity
            SceneNode ogreHead1Node;
            ogreHead1Node = SceneManager.RootSceneNode.CreateChildSceneNode("OgreHeadNode", Vector3.Zero, Quaternion.Identity);
            ogreHead1Node.AttachObject(ogreHead);

            SceneNode ogreHead2Node;
            Entity ogreHead2 = SceneManager.CreateEntity("OgreHead2", "ogrehead.mesh");
            ogreHead2Node = SceneManager.RootSceneNode.CreateChildSceneNode("OgreHead2Node", new Vector3(-100, 0, 0),
                                                                             Quaternion.Identity);
            ogreHead2Node.AttachObject(ogreHead2);

            SceneNode ogreHead3Node;
            Entity ogreHead3 = SceneManager.CreateEntity("OgreHead3", "ogrehead.mesh");
            ogreHead3Node = SceneManager.RootSceneNode.CreateChildSceneNode("OgreHead3Node", new Vector3(+100, 0, 0),
                                                                             Quaternion.Identity);
            ogreHead3Node.AttachObject(ogreHead3);


            Entity entity;
            SceneNode childNode;

            //Create the main entity and mark it as the current target object
            entity = SceneManager.CreateEntity(MainEntityName, MainEntityMesh);
            this.targetEntities.Add(entity);
            childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
            childNode.AttachObject(entity);
            //this.targetObj = entity;
            childNode.ShowBoundingBox = true;

            //Create reflection entity that will show the exported material.
            string mainExportedMaterial = SceneManager.GetEntity(MainEntityName).GetSubEntity(0).MaterialName;// + "_RTSS_Export";

            var matMainEnt = (Material)MaterialManager.Instance.GetByName(mainExportedMaterial);

            entity = SceneManager.CreateEntity("ExportedMaterialEntity", MainEntityMesh);
            entity.GetSubEntity(0).Material = matMainEnt;
            childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
            childNode.Position = new Vector3(0, 200, -200);
            childNode.AttachObject(entity);

            //Create texture layer blending demonstration entity
            this.layeredBlendingEntity = SceneManager.CreateEntity("LayeredBlendingMaterialEntity", MainEntityMesh);
            this.layeredBlendingEntity.MaterialName = "RTSS/LayeredBlending";
            this.layeredBlendingEntity.GetSubEntity(0).SetCustomParameter(2, Vector4.Zero);
            childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
            childNode.Position = new Vector3(300, 200, -200);
            childNode.AttachObject(this.layeredBlendingEntity);

            //Create per pixel lighting demo entity
            entity = SceneManager.CreateEntity("PerPixelEntity", "ogrehead.mesh");
            entity.MaterialName = "RTSS/PerPixel_SinglePass";
            childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
            childNode.Position = new Vector3(300, 100, -100);
            childNode.AttachObject(entity);

            //Create normal map lighting demo entity
            entity = SceneManager.CreateEntity("NormalMapEntity", "ogrehead.mesh");
            entity.MaterialName = "RTSS/NormalMapping_SinglePass";
            childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
            childNode.Position = new Vector3(-300, 100, -100);
            childNode.AttachObject(entity);


            //temp entity to get meshsize 
            Entity headsub = SceneManager.CreateEntity("Head", modelName);
            modelMesh = headsub.Mesh;
            modelSize = modelMesh.BoundingBox.Size;

            // make sure the camera tracks this node
            // set initial camera position
            //CameraManager.setStyle(CameraStyle.FreeLook);
            //Camera.Position = new Vector3(0, 0, 500);
            //Camera.SetAutoTracking(true, ogreHead1Node, Vector3.Zero);


            // set our camera to orbit around the origin and show cursor
            CameraManager.setStyle(CameraStyle.Orbit);
            CameraManager.SetYawPitchDist(0, 15, 250);
            Camera.SetAutoTracking(true, ogreHead1Node, Vector3.Zero);


            // create a scene node to attach the camera to
            SceneNode cameraNode = SceneManager.RootSceneNode.CreateChildSceneNode("CameraNode");
            cameraNode.AttachObject(Camera);

            // turn on some fog
            SceneManager.SetFog(FogMode.Exp, ColorEx.White, 0.0002f);

            this._MouseSelector = new MouseSelector("MouseSelector", Camera, Window);
            this._MouseSelector.SelectionMode = MouseSelector.SelectionModeType.None;

            SetupGUI();
            SetupSlider();

            ds = new System.Data.DataSet();
            
            SetupScatterPoint();
            this.initialized = true;
            base.SetupContent();

            UpdateSystemShaders();
        }


        private void SetupScatterPoint()
        {
            //if not empty
            if (dictnode != null)
            {
                foreach (var n in dictnode)
                {
                    SceneManager.DestroySceneNode(n.Value);
                }
            }
            if (dictentity != null)
            {
                foreach (var e in dictentity)
                {
                    SceneManager.RemoveEntity(e.Value);
                }
            }

            dtRaw = new System.Data.DataTable();
            dtRaw.TableName = "RAW";
            dtRaw.Columns.Add("id");
            dtRaw.Columns.Add("x");
            dtRaw.Columns.Add("y");
            dtRaw.Columns.Add("z");
            string[] content = File.ReadAllLines("C:\\Users\\Administrator\\Desktop\\data3.csv");

            dictnode = new Dictionary<string, SceneNode> { };
            dictentity = new Dictionary<string, Entity> { };
            dictOriginalPosition = new Dictionary<string, Vector3>();
            for (int i = 1; i < content.Length; i++)
            {
                string[] l = content[i].Split(',');
                if (l.Length == 3)
                {

                    float x = float.Parse(l[0]) * 100;
                    float y = float.Parse(l[1]) * 100;
                    float z = float.Parse(l[2]) * 10000;
                    dtRaw.Rows.Add(i, l[0], l[1], l[2]);
                    StatusLabel.Caption = "Loading " + i.ToString() + " of " + content.Length.ToString() + " items";
                    Entity headsub = SceneManager.CreateEntity("Head" + i.ToString(), modelName);
                    headsub.MaterialName = "Examples/GreenSkin";
                    targetEntities.Add(headsub);
                    Vector3 v = new Vector3(y, z, x);

                    // setup some basic lighting for our scene
                    //SceneManager.AmbientLight = new ColorEx(0.3f, 0.3f, 0.3f);
                    //SceneManager.CreateLight("Light" + i.ToString()).Position = v;

                    dictOriginalPosition.Add("node" + i.ToString(), v);
                    SceneNode headNodesub = SceneManager.RootSceneNode.CreateChildSceneNode();
                    headNodesub.AttachObject(headsub);
                    //headNodesub.Translate = new Vector3(200, 0, 0);
                    headNodesub.Scale = new Vector3(SampleSliderScale.Value, SampleSliderScale.Value, SampleSliderScale.Value);
                    //headNodesub.ScaleBy(new Vector3(1 / modelSize.x * SampleSliderScale.Value,
                    //                                1 / modelSize.y * SampleSliderScale.Value,
                    //                                1 / modelSize.z * SampleSliderScale.Value));
                    
                    headNodesub.Position = dictOriginalPosition["node" + i.ToString()];

                    dictnode.Add("node" + i.ToString(), headNodesub);
                    dictentity.Add("node" + i.ToString(), headsub);

                }
            }

            ds.Tables.Add(dtRaw);
        }


        public void ExportToExcel(System.Data.DataSet dataSet, string outputPath)
        {
            // Create the Excel Application object
            Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();
            // Create a new Excel Workbook
            Workbook excelWorkbook = excelApp.Workbooks.Add(Type.Missing);
            int sheetIndex = 0;
            int col, row;
            Worksheet excelSheet;
            // Copy each DataTable as a new Sheet
            foreach (System.Data.DataTable dt in dataSet.Tables)
            {
                sheetIndex += 1;
                // Copy the DataTable to an object array
                object[,] rawData = new object[dt.Rows.Count + 1, dt.Columns.Count - 1 + 1];
                // Copy the column names to the first row of the object array
                for (col = 0; col <= dt.Columns.Count - 1; col++)
                    rawData[0, col] = dt.Columns[col].ColumnName;
                // Copy the values to the object array
                for (col = 0; col <= dt.Columns.Count - 1; col++)
                {
                    for (row = 0; row <= dt.Rows.Count - 1; row++)
                        rawData[row + 1, col] = dt.Rows[row].ItemArray[col];
                }
                // Calculate the final column letter
                string finalColLetter = string.Empty;
                string colCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                int colCharsetLen = colCharset.Length;
                if (dt.Columns.Count > colCharsetLen)
                    finalColLetter = colCharset.Substring((dt.Columns.Count - 1) / colCharsetLen - 1, 1);
                finalColLetter += colCharset.Substring((dt.Columns.Count - 1) % colCharsetLen, 1);
                // Create a new Sheet
                excelSheet = (Worksheet)excelWorkbook.Sheets.Add(excelWorkbook.Sheets[sheetIndex], Type.Missing, 1, XlSheetType.xlWorksheet);


                // Fast data export to Excel
                string excelRange = string.Format("A1:{0}{1}", finalColLetter, dt.Rows.Count + 1);
                // set all to text format
                // excelSheet.Range(excelRange, Type.Missing).Cells.NumberFormatLocal = "@"
                excelSheet.Range[excelRange].Value2 = rawData;

                // Mark the first row as BOLD
                //excelSheet.Rows[1].Font.Bold = true;

                excelSheet.Activate();
                excelSheet.Application.ActiveWindow.SplitRow = 1;
                excelSheet.Application.ActiveWindow.FreezePanes = true;

                excelSheet.Name = dt.TableName;

                excelSheet = null/* TODO Change to default(_) if this is not a reference type */;
            }
            // Save and Close the Workbook
            excelWorkbook.SaveAs(outputPath, XlFileFormat.xlOpenXMLWorkbook, Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            excelWorkbook.Close(true, Type.Missing, Type.Missing);
            excelWorkbook = null/* TODO Change to default(_) if this is not a reference type */;
            // Release the Application object

            excelApp.Quit();
            // excelApp = Nothing
            // Collect the unreferenced objects
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

     
        //private void AddModelToScene(string modelName)
        //{
        //    this.numberOfModelsAdded++;
        //    for (int i = 0; i < 8; i++)
        //    {
        //        float scaleFactor = 30;
        //        Entity entity;
        //        SceneNode childNode;
        //        entity = SceneManager.CreateEntity("createdEnts" + this.numberOfModelsAdded.ToString(), modelName);
        //        this.lotsOfModelsEntitites.Add(entity);
        //        childNode = SceneManager.RootSceneNode.CreateChildSceneNode();
        //        this.lotsOfModelsNodes.Add(childNode);
        //        childNode.Position = new Vector3(this.numberOfModelsAdded * scaleFactor, 15, i * scaleFactor);
        //        childNode.AttachObject(entity);
        //        var modelMesh = (Mesh)MeshManager.Instance.GetByName(modelName);
        //        Vector3 modelSize = modelMesh.BoundingBox.Size;
        //        childNode.ScaleBy(new Vector3(1 / modelSize.x * scaleFactor,
        //                                        1 / modelSize.y * scaleFactor,
        //                                        1 / modelSize.z * scaleFactor));
        //    }
        //}

        /// <summary>
        /// Creates and initializes all the scene's GUI elements not defined in SdkSample
        /// </summary>
        protected void SetupGUI()
        {
            this.selectionModeMenu = TrayManager.CreateLongSelectMenu(TrayLocation.TopRight, "SelectionModeMenu",
                                                                       "Selection Mode",
                                                                       300, 150, 3);
            this.selectionModeMenu.AddItem("None");
            this.selectionModeMenu.AddItem("Mouse Select");
            this.selectionModeMenu.AddItem("Selection Box");
            this.selectionModeMenu.SelectItem(0);
            this.selectionModeMenu.SelectedIndexChanged += selectionModeMenu_SelectedIndexChanged;

            this.MouseLocationLabel = TrayManager.CreateLabel(TrayLocation.TopLeft, "Mouse Location", "", 350);

            TrayManager.ShowCursor();
        }
        private void UpdateSystemShaders()
        {
            foreach (var it in this.targetEntities)
            {
                GenerateShaders(it);
            }
        }
        private ShaderSystemLightingModel CurrentLightingModel
        {
            get
            {
                return this.curLightingModel;
            }
            set
            {
                if (this.curLightingModel != value)
                {
                    this.curLightingModel = value;

                    foreach (var it in this.targetEntities)
                    {
                        GenerateShaders(it);
                    }
                }
            }
        }

        private void GenerateShaders(Entity entity)
        {
            for (int i = 0; i < entity.SubEntityCount; i++)
            {
                SubEntity curSubEntity = entity.GetSubEntity(i);
                string curMaterialName = curSubEntity.MaterialName;
                bool success;

                //Create the shader based technique of this material.
                success = ShaderGenerator.Instance.CreateShaderBasedTechnique(curMaterialName, MaterialManager.DefaultSchemeName,
                                                                               ShaderGenerator.DefaultSchemeName, false);

                //Setup custmo shader sub render states according to current setup.
                if (success)
                {
                    var curMaterial = (Material)MaterialManager.Instance.GetByName(curMaterialName);
                    Pass curPass = curMaterial.GetTechnique(0).GetPass(0);

                    if (true)
                    {
                        curPass.Specular = ColorEx.White;
                        curPass.Shininess = 32;
                    }
                    else
                    {
                        curPass.Specular = ColorEx.Beige;
                        curPass.Shininess = 0;
                    }
                    // Grab the first pass render state. 
                    // NOTE: For more complicated samples iterate over the passes and build each one of them as desired.
                    RenderState renderState = ShaderGenerator.Instance.GetRenderState(ShaderGenerator.DefaultSchemeName,
                                                                                       curMaterialName, 0);

                    //Remove all sub render states
                    renderState.Reset();

                    if (this.curLightingModel == ShaderSystemLightingModel.PerVertexLighting)
                    {
                        SubRenderState perPerVertexLightModel = ShaderGenerator.Instance.CreateSubRenderState(FFPLighting.FFPType);
                        renderState.AddTemplateSubRenderState(perPerVertexLightModel);
                    }
                    else if (this.curLightingModel == ShaderSystemLightingModel.PerVertexLighting)
                    {
                        SubRenderState perPixelLightModel = ShaderGenerator.Instance.CreateSubRenderState(PerPixelLighting.SGXType);
                        renderState.AddTemplateSubRenderState(perPixelLightModel);
                    }
                    else if (this.curLightingModel == ShaderSystemLightingModel.NormalMapLightingTangentSpace)
                    {
                        ////Apply normal map only on main entity.
                        //if (entity.Name == MainEntityName)
                        //{
                        //    SubRenderState subRenderState = ShaderGenerator.Instance.CreateSubRenderState(NormalMapLighting.SGXType);
                        //    var normalMapSubRS = subRenderState as NormalMapLighting;

                        //    normalMapSubRS.NormalMapSpace = NormalMapSpace.Tangent;
                        //    normalMapSubRS.NormalMapTextureName = "Panels_Normal_Tangent.png";
                        //    renderState.AddTemplateSubRenderState(normalMapSubRS);
                        //}
                        ////It is secondary entity -> use simple per pixel lighting
                        //else
                        {
                            SubRenderState perPixelLightModel = ShaderGenerator.Instance.CreateSubRenderState(PerPixelLighting.SGXType);
                            renderState.AddTemplateSubRenderState(perPixelLightModel);
                        }
                    }
                    else if (this.curLightingModel == ShaderSystemLightingModel.NormalMapLightingObjectSpace)
                    {
                        ////Apply normal map only on main entity
                        //if (entity.Name == MainEntityName)
                        //{
                        //    SubRenderState subRenderState = ShaderGenerator.Instance.CreateSubRenderState(NormalMapLighting.SGXType);
                        //    var normalMapSubRS = subRenderState as NormalMapLighting;

                        //    normalMapSubRS.NormalMapSpace = NormalMapSpace.Object;
                        //    normalMapSubRS.NormalMapTextureName = "Panels_Normal_Obj.png";

                        //    renderState.AddTemplateSubRenderState(normalMapSubRS);
                        //}

                        ////It is secondary entity -> use simple per pixel lighting.
                        //else
                        {
                            SubRenderState perPixelLightModel = ShaderGenerator.Instance.CreateSubRenderState(PerPixelLighting.SGXType);
                            renderState.AddTemplateSubRenderState(perPixelLightModel);
                        }
                    }

                    if (this.reflectionMapEnable)
                    {
                        SubRenderState subRenderState = ShaderGenerator.Instance.CreateSubRenderState(ReflectionMap.SGXType);
                        var reflectMapSubRs = subRenderState as ReflectionMap;

                        reflectMapSubRs.ReflectionMapType = TextureType.CubeMap;
                        reflectMapSubRs.ReflectionPower = this.reflectionPowerSlider.Value;

                        //Setup the textures needed by the reflection effect
                        reflectMapSubRs.MaskMapTextureName = "Panels_refmask.png";
                        reflectMapSubRs.ReflectionMapTextureName = "cubescene.jpg";

                        renderState.AddTemplateSubRenderState(subRenderState);
                        this.reflectionMapSubRS = subRenderState;
                    }
                    else
                    {
                        this.reflectionMapSubRS = null;
                    }
                    //Invalidate this material in order to regen its shaders
                    ShaderGenerator.Instance.InvalidateMaterial(ShaderGenerator.DefaultSchemeName, curMaterialName);
                }
            }
        }
        public void SliderMoved(object sender, Slider slider)
        {
            if (slider.Name == ReflectionMapPowerSlider)
            {
                Real reflectionPower = slider.Value;

                if (this.reflectionMapSubRS != null)
                {
                    var reflectMapSubRS = this.reflectionMapSubRS as ReflectionMap;

                    // Since RTSS export caps based on the template sub render states we have to update the template reflection sub render state.
                    reflectMapSubRS.ReflectionPower = reflectionPower;

                    // Grab the instances set and update them with the new reflection power value.
                    // The instances are the actual sub render states that have been assembled to create the final shaders.
                    // Every time that the shaders have to be re-generated (light changes, fog changes etc..) a new set of sub render states 
                    // based on the template sub render states assembled for each pass.
                    // From that set of instances a CPU program is generated and afterward a GPU program finally generated.

                    foreach (var it in this.reflectionMapSubRS.TemplateSubRenderStateList)
                    {
                        var reflectionMapInstance = it as ReflectionMap;
                        reflectionMapInstance.ReflectionPower = reflectionPower;
                    }
                }
            }

            //if (slider.Name == ModifierValueSlider)
            //{
            //    if (this.layeredBlendingEntity != null)
            //    {
            //        Real val = this.modifierValueSlider.Value;
            //        this.layeredBlendingEntity.GetSubEntity(0).SetCustomParameter(2, new Vector4(val, val, val, 0));
            //    }
            //}
        }
        /// <summary>
        /// Event for when the menu changes, sets the MouseSelectors SelectionMode
        /// </summary>
        /// <param name="sender">SelectMenu object</param>
        /// <param name="e">EventArgs</param>
        private void selectionModeMenu_SelectedIndexChanged(SelectMenu sender)
        {
            if (sender != null)
            {
                this._MouseSelector.SelectionMode = (MouseSelector.SelectionModeType)sender.SelectionIndex;
            }
        }

        [OgreVersion(1, 7, 2)]
        protected void SetupSlider()
        {
            SampleSliderScale = TrayManager.CreateThickSlider(TrayLocation.TopLeft, "modelscale", "modelscale", 250, 80,
                                                0.1, 10, 100);
            SampleSliderScale.SetValue(0.3, false);
            SampleSliderScale.SliderMoved += new SliderMovedHandler(_slidermoved);

            SampleSliderX = TrayManager.CreateThickSlider(TrayLocation.TopLeft, "zoomX", "zoomX", 250, 80,
                                                            1, 100, 100);
            SampleSliderX.SetValue(1, false);
            SampleSliderX.SliderMoved += new SliderMovedHandler(_slidermoved);
            SampleSliderY = TrayManager.CreateThickSlider(TrayLocation.TopLeft, "zoomY", "zoomY", 250, 80,
                                                            1, 100, 100);
            SampleSliderY.SetValue(1, false);
            SampleSliderY.SliderMoved += new SliderMovedHandler(_slidermoved);
            SampleSliderZ = TrayManager.CreateThickSlider(TrayLocation.TopLeft, "zoomZ", "zoomZ", 250, 80,
                                                          1, 100, 100);
            SampleSliderZ.SetValue(1, false);
            SampleSliderZ.SliderMoved += new SliderMovedHandler(_slidermoved);

            targetObjName = TrayManager.CreateLabel(TrayLocation.TopLeft, "TargetObjName", string.Empty, 220);

            StatusLabel = TrayManager.CreateLabel(TrayLocation.Bottom, "Status", string.Empty, 220);
            //this.modifierValueSlider = TrayManager.CreateThickSlider(TrayLocation.Right, ModifierValueSlider, "Modifier", 240,
            //                                                        80, 0,
            //                                                        1, 100);
            //this.modifierValueSlider.SetValue(0.0f, false);
            //this.modifierValueSlider.SliderMoved += new SliderMovedHandler(SliderMoved);

            //this.reflectionPowerSlider = TrayManager.CreateThickSlider(TrayLocation.Bottom, ReflectionMapPowerSlider,
            //                                                "Reflection Power", 240, 80, 0, 1, 100);
            //this.reflectionPowerSlider.SetValue(0.5f, false);
            //this.reflectionPowerSlider.SliderMoved += new SliderMovedHandler(SliderMoved);


            SampleSaveButton = TrayManager.CreateButton(TrayLocation.TopLeft, "SaveExcel", "Save");
            SampleSaveButton.CursorPressed += new CursorPressedHandler(buttonpress);
        }

        private void buttonpress(object sender, Vector2 cursorPosition)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Please select output file path";
            saveFileDialog.Filter = "Excel(*.xlsx)|*.xlsx";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            //设置是否允许多选

            //按下确定选择的按钮
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //获得文件路径
                string localFilePath = saveFileDialog.FileName.ToString();
                ExportToExcel(ds, localFilePath);
                System.Windows.Forms.MessageBox.Show("Done!");
                //获取文件路径，不带文件名
                FileInfo fi = new FileInfo(localFilePath);
                Process.Start(fi.DirectoryName);
                ////获取文件名，带后缀名，不带路径
                //string fileNameWithSuffix = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1);
                ////去除文件后缀名
                //string fileNameWithoutSuffix = fileNameWithSuffix.Substring(0, fileNameWithSuffix.LastIndexOf("."));
                ////在文件名前加上时间
                //string fileNameWithTime = DateTime.Now.ToString("yyyy-MM-dd ") + fileNameExt;
                ////在文件名里加字符
                //string newFileName = localFilePath.Insert(1, "Tets");
            }
        }

            private void _slidermoved(object sender, Slider slider)
        {
            //slider.ValueCaption = slider.Value.ToString();

            foreach (var n in dictnode)
            {
                Vector3 np = new Vector3(dictOriginalPosition[n.Key].x * SampleSliderX.Value, dictOriginalPosition[n.Key].y * SampleSliderY.Value, dictOriginalPosition[n.Key].z * SampleSliderZ.Value);

                n.Value.Position = np;
                n.Value.Scale = new Vector3(SampleSliderScale.Value, SampleSliderScale.Value, SampleSliderScale.Value);
                //n.Value.ScaleBy(new Vector3(1 / modelSize.x * SampleSliderScale.Value,
                //                                1 / modelSize.y * SampleSliderScale.Value,
                //                                1 / modelSize.z * SampleSliderScale.Value));
            }

        }
    }
}
