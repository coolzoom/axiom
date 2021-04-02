#region MIT/X11 License

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

namespace Axiom.Samples.MousePicking
{
    /// <summary>
    /// Sample of selecting objects with a mouse
    /// </summary>
    public class MousePickingSample : SdkSample
    {

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
        private string modelName = "sphere.mesh";
        private Dictionary<string, SceneNode> dictnode;
        private Dictionary<string, Entity> dictentity;
        private Dictionary<string, Vector3> dictOriginalPosition;
        private Slider SampleSliderX;
        private Slider SampleSliderY;
        private Slider SampleSliderZ;
        private Slider SampleSliderScale;
        public Label targetObjName;
        public Label StatusLabel;

        private Button SampleLoadButton;
        private Button SampleSaveButton;
        private TextBox SampleH12Limit;
        private System.Data.DataSet ds;
        private System.Data.DataTable dtRaw;
        /// <summary>
        /// Sample initialization
        /// </summary>
        public MousePickingSample()
        {
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
            //SceneManager.AmbientLight = new ColorEx(1.0f, 0.2f, 0.2f, 0.2f);
            // setup some basic lighting for our scene
            SceneManager.AmbientLight = new ColorEx(0.3f, 0.3f, 0.3f);
            SceneManager.CreateLight("CameraTrackLight").Position = new Vector3(20, 80, 50);


            // create a skydome
            //SceneManager.SetSkyDome(true, "Examples/CloudySky", 5, 8);

            SceneManager.SetSkyBox(true, "Examples/NebulaSkyBox", 500); //Examples/MorningSkyBox

            // create an entity to have follow the path
            Entity ogreHead = SceneManager.CreateEntity("OgreHead", "ogrehead.mesh");

            // Load muiltiple heads for box select and demonstrating single select with stacked objects
            // create a scene node for each entity and attach the entity
            SceneNode ogreHead1Node;
            ogreHead1Node = SceneManager.RootSceneNode.CreateChildSceneNode("OgreHeadNode", Vector3.Zero, Quaternion.Identity);
            ogreHead1Node.AttachObject(ogreHead);

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
            
            //default data
            SetupScatterPoint("C:\\Users\\Administrator\\Desktop\\data.csv");
            this.initialized = true;
            base.SetupContent();

        }


        private void SetupScatterPoint(string filename)
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
            ds = new System.Data.DataSet();
            dtRaw = new System.Data.DataTable();
            dtRaw.TableName = "RAW";
            dtRaw.Columns.Add("id");
            dtRaw.Columns.Add("x");
            dtRaw.Columns.Add("y");
            dtRaw.Columns.Add("z");
            string[] content = File.ReadAllLines(filename); //File.ReadAllLines("C:\\Users\\Administrator\\Desktop\\data3.csv")

            dictnode = new Dictionary<string, SceneNode> { };
            dictentity = new Dictionary<string, Entity> { };
            dictOriginalPosition = new Dictionary<string, Vector3>();
            for (int i = 1; i < content.Length; i++)
            {
                string[] l = content[i].Split(',');
                if (l.Length == 3)
                {

                    float x = float.Parse(l[0]) * 10;
                    float y = float.Parse(l[1]) * 10;
                    float z = float.Parse(l[2]) * 1000;
                    dtRaw.Rows.Add(i, l[0], l[1], l[2]);
                    StatusLabel.Caption = "Loading " + i.ToString() + " of " + content.Length.ToString() + " items";
                    Entity headsub = SceneManager.CreateEntity("node" + i.ToString(), modelName);
                    headsub.MaterialName = "Examples/GreenSkin";

                    Vector3 v = new Vector3(y, z, x);

                   //setup some basic lighting for our scene
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
                                                0.01, 1, 1000);
            SampleSliderScale.SetValue(0.1, false);
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
            SampleLoadButton = TrayManager.CreateButton(TrayLocation.TopLeft, "LoadCSV", "Load");
            SampleLoadButton.CursorPressed += new CursorPressedHandler(buttonload);

            SampleSaveButton = TrayManager.CreateButton(TrayLocation.TopLeft, "SaveExcel", "Save");
            SampleSaveButton.CursorPressed += new CursorPressedHandler(buttonpress);

           

        }

        private void buttonload(object sender, Vector2 cursorPosition)
        {

            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "csv|*.csv";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;

            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string fName = openFileDialog.FileName;
                if (File.Exists(fName))
                {
                    SetupScatterPoint(fName);
                }
            }
        }

        private void buttonpress(object sender, Vector2 cursorPosition)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Please select output file path";
            saveFileDialog.Filter = "Excel(*.xlsx)|*.xlsx";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
 
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                string localFilePath = saveFileDialog.FileName.ToString();
                ExportToExcel(ds, localFilePath);
                System.Windows.Forms.MessageBox.Show("Done!");

                FileInfo fi = new FileInfo(localFilePath);
                Process.Start(fi.DirectoryName);

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
