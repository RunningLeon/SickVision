using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sick.EasyRanger;
using Sick.EasyRanger.Controls;
using System.Threading;
using ControlClassLibrary.Properties;
using System.IO;
using System.Collections.ObjectModel;

namespace ControlClassLibrary
{
    public partial class VisionControl : UserControl
    {
        public VisionControl()
        {
            InitializeComponent();

        }
        System.Windows.Media.Color red = System.Windows.Media.Color.FromRgb(255, 0, 0);
        System.Windows.Media.Color green = System.Windows.Media.Color.FromRgb(0, 255, 0);
        System.Windows.Media.Color blue = System.Windows.Media.Color.FromRgb(0, 0, 255);
        System.Windows.Media.Color yellow = System.Windows.Media.Color.FromRgb(255, 255, 0);
        System.Windows.Media.Color magenta = System.Windows.Media.Color.FromRgb(255, 0, 255);
        System.Windows.Media.Color cyan = System.Windows.Media.Color.FromRgb(0, 255, 255);
        System.Windows.Media.Color white = System.Windows.Media.Color.FromRgb(255, 255, 255);
        System.Windows.Media.Color black = System.Windows.Media.Color.FromRgb(0, 0, 0);
        ENVResolveClass envRes;
        public bool imageProcessStartFlag;
        ProcessingEnvironment easyRanger;
        IconDevice camera;
        bool threadRunFlag;
        View2DControl viewer2D;
        View3DControl viewer3D;
        XmlResolveClass xmlRes;
        string selectedStepProgramName;
        string selectedViewType;
        //UI 
        SynchronizationContext m_SyncContext = null;
        //Image Grab ID
        int imageCount = 0;
        Dictionary<string, int> stpNameDict = new Dictionary<string, int>();

        //string ENVFilePath = Resources.EZRPath + @"\test_Offline.env";
        string ENVFilePath = Resources.EZRPath + @"\test_Online.env";
        string ICXFilePath = Resources.CameraConfig + @"\config.icx";
        string LogFilePath = Resources.LogPath + @"\LogResult.csv";

        private void VisionControl_Load(object sender, EventArgs e)
        {
            //splitcontainer3.hide();
            //生成Tree View
            treeViewProgram.Hide();

            try
            {
                if (File.Exists(ENVFilePath))
                {
                    easyRanger = new ProcessingEnvironment();
                    easyRanger.Load(ENVFilePath);
                    updateLogMsg("Load Environment File from " + ENVFilePath);
                    envRes = new ENVResolveClass(easyRanger);
                    updateLogMsg("Load Environment File Successfully...");
                    InitStpNameDict(easyRanger);
                    InitGUIControls();
                    //InitTreeView();
                    InitViewer(selectedViewType);
                    updateLogMsg("Displaying  " + selectedViewType + "......");
                    imageProcessStartFlag = true;
                    if (InitialCamera())
                    {
                        updateLogMsg("Camera Initial successfully...");
                        imageProcessStartFlag = false;
                    }
                    else
                    {
                        updateLogMsg("Camera Initial failed...");
                    }
                    // default select index
                    m_SyncContext = SynchronizationContext.Current;
                    backgroundWorker = new BackgroundWorker();
                    backgroundWorker.DoWork += BackgroundWorker_DoWork;
                    backgroundWorker.WorkerSupportsCancellation = true;
                    threadRunFlag = true;
                    xmlRes = new XmlResolveClass();
                    List<string> doubleDataNames = xmlRes.getEnableVariables(XmlResolveClass.VariableType.Number);
                    int i = 0;
                    foreach (Control cnt in splitContainer4.Panel2.Controls)
                    {
                        cnt.Text = doubleDataNames[i].Substring(4);
                        cnt.Size = new System.Drawing.Size(54, 20);
                        cnt.BackColor = Color.FromKnownColor(KnownColor.LightGray);
                        i++;
                    }
                }
                else
                {
                    updateLogMsg(ENVFilePath + " do not exist!");
                }
            }
            catch (Exception ex)
            {
                updateLogMsg(ex.ToString());
            }
        }

        void InitStpNameDict(ProcessingEnvironment env)
        {
            int i = 0;
            foreach (StepProgram stpProg in env.Programs)
            {
                stpNameDict.Add(stpProg.Name, i);
                comboBoxProgram.Items.Add(stpProg.Name);
                i++;
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            try
            {
                RunContinue();
            }
            catch (Exception ex)
            {
                m_SyncContext.Post(delegate
                {
                    updateLogMsg(ex.ToString());
                }, null);
            }
        }

        void InitTreeView()
        {
            //first node
            List<string> programList = new List<string>();
            List<string> stepList = new List<string>();
            programList = envRes.ProgramNameList();

            for (int i = 0; i < programList.Count; i++)
            {
                treeViewProgram.Nodes.Add(programList[i]);
                comboBoxProgram.Items.Add(programList[i]);
                stepList = envRes.StepNameList(easyRanger.GetStepProgram(programList[i]));
                for (int j = 0; j < stepList.Count; j++)
                {
                    treeViewProgram.Nodes[i].Nodes.Add(stepList[j]);
                }
            }

        }

        void InitGUIControls()
        {
            comboBoxProgram.SelectedIndex = 0;
            comboBoxView.SelectedIndex = 0;
            selectedStepProgramName = (string)comboBoxProgram.SelectedItem;
            selectedViewType = "View2D";
            buttonStart.Enabled = false;
            buttonStop.Enabled = false;
            buttonReset.Enabled = true;
            buttonRunOnce.Enabled = true;
            comboBoxProgram.Enabled = true;
            comboBoxView.Enabled = true;

        }

        void InitViewer(string curViewType)
        {
            //2d viewer
            if (curViewType == "View2D")
            {
                viewer2D = new View2DControl
                {
                    TrueAspectRatio = true,
                    ShowClearButton = System.Windows.Visibility.Visible,
                    ShowEditControls = System.Windows.Visibility.Collapsed,
                    ShowOptions = System.Windows.Visibility.Visible,
                    LineScale = 1.2,
                    PointScale = 1.2
                };

                viewer2D.Environment = easyRanger;
                elementHostView.Child = viewer2D;
            }


            //3D viewer
            if (curViewType == "View3D")
            {
                viewer3D = new View3DControl
                {
                    // Set hybrid color mode (color is determined by height and intensity)
                    ColorMode = View3DControl.ColorModes.Height,
                    // Set wireframe mode
                    GeometryMode = View3DControl.GeometryModes.Wireframe
                };
                // lowest and highest Z value in the image,
                viewer3D.ColorMin = 0.8f;
                viewer3D.ColorMax = 1f;
                viewer3D.ShowOptions = System.Windows.Visibility.Visible;
                viewer3D.CameraDefaultDistance = 1.0f;
                viewer3D.CameraDefaultPitch = 45;
                viewer3D.CameraDefaultYaw = 34;
                viewer3D.SpecularAmplification = 0.5f;
                viewer3D.Environment = easyRanger;
                elementHostView.Child = viewer3D;
            }
        }

        //Init camera
        bool InitialCamera()
        {
            try
            {
                camera = new IconDevice(ICXFilePath);
                camera.Name = "Camera";
                easyRanger.AddCamera(camera);
                if (camera.IsConnected)
                {
                    camera.Disconnect();
                }
                camera.Connect(ICXFilePath);
                if (camera.IsConnected)
                {
                    updateLogMsg(DateTime.Now.ToString() + ": " + "Camera Connected...");
                    camera.Start();
                    if (camera.IsStarted)
                    {
                        updateLogMsg(DateTime.Now.ToString() + ": " + "Camera Started...");
                    }
                    return true;
                }
                else
                {
                    updateLogMsg(DateTime.Now.ToString() + ": " + "Camera Not Connected! Please check ICXFilePath:" + ICXFilePath);
                    return false;
                }

            }
            catch (Exception ex)
            {
                updateLogMsg("Error when connecting camera: " + ex.ToString());
                return false;
            }


        }

        void updateLogMsg(string msg)
        {
            loggingRichTextBox.Text = msg + Environment.NewLine + loggingRichTextBox.Text;
        }

        void RunContinue()
        {
            while (threadRunFlag)
            {

                if (backgroundWorker.CancellationPending)
                {
                    break;
                }
                Thread.Sleep(500);
                RunAllOnce();
            }

        }

        void RunAllOnce()
        {
            // 1. grab image
            if (GrabRun(stpNameDict["Grab"]))
            {
                if (MatchRun(stpNameDict["Match"]))
                {
                    if (MeasureRun(stpNameDict["Measure"]))
                    {
                        m_SyncContext.Post(delegate
                        {
                            viewResult(selectedViewType);
                            updateLogMsg(outputData());
                            updateLogMsg("Finish processing Image[" + imageCount + "]...");
                        }, null);
                        return;
                    }
                }
            }
            m_SyncContext.Post(delegate
            {
                updateLogMsg("Processing Image[" + imageCount + "] failed!");
            }, null);

        }

        void RunOnce(string stepProgramName)
        {
            RunOneStepProgram(stepProgramName);
        }

        void RunOnce(int stepProgramIndex)
        {
            RunOneStepProgram(stepProgramIndex);
        }

        bool RunOneStepProgram(string name)
        {
            try
            {
                StepProgram stpProgram = easyRanger.GetStepProgram(Name);
                stpProgram.RunFromBeginning();
                foreach (Step stp in stpProgram.StepList)
                {
                    if (stp.Enabled)
                    {
                        if (!stp.Success)
                        {
                            m_SyncContext.Post(delegate
                            {
                                updateLogMsg(stp.Name + " in step program [" + name + "] failed, error:" + easyRanger.GetLastErrorMessage());
                            }, null);
                            return false;
                        }
                    }
                }
                m_SyncContext.Post(delegate
                {
                    updateLogMsg("Step program [" + name + "] finished successfully!");
                }, null);
                return true;
            }
            catch (Exception ex)
            {
                m_SyncContext.Post(delegate
                {
                    updateLogMsg("Step program [" + name + "] failed, error:" + ex.ToString());
                }, null);
                return false;
            }
        }

        bool RunOneStepProgram(int stepProgramIndex)
        {
            StepProgram stpProgram = null;
            try
            {
                stpProgram = easyRanger.GetStepProgram(stepProgramIndex);
                stpProgram.RunFromBeginning();
                m_SyncContext.Post(delegate
                {
                    updateLogMsg("Step program [" + stpProgram.Name + "] finished successfully!");
                }, null);
                return true;
            }
            catch (Sick.EasyRanger.EasyRangerException ex)
            {
                foreach (Step stp in stpProgram.StepList)
                {
                    if (stp.Enabled)
                    {
                        if (!stp.Success)
                        {
                            m_SyncContext.Post(delegate
                            {
                                updateLogMsg("Step #" + stp.Index + " in step program [" + stpProgram.Name + "] failed, error:" + easyRanger.GetLastErrorMessage());
                            }, null);
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                m_SyncContext.Post(delegate
                {
                    updateLogMsg("Step program [" + stepProgramIndex + "] failed, error:" + ex.ToString());
                }, null);
                return false;
            }
        }

        bool GrabRun(int stpIndex = -1, string stpName = "Grab")
        {
            bool isSuccess = false;
            if (stpIndex != -1)
            {
                isSuccess = RunOneStepProgram(stpIndex);
            }
            else
            {
                isSuccess = RunOneStepProgram(stpName);
            }
            if (isSuccess)
            {
                imageCount++;
                m_SyncContext.Post(delegate
                {
                    //viewResult(selectedViewType);
                    updateLogMsg("Grabed Image[" + imageCount.ToString() + "] successfully!");
                }, null);
            }
            return isSuccess;
        }

        bool TeachRun(int stpIndex = -1, string stpName = "Teach")
        {
            if (stpIndex != -1)
            {
                return RunOneStepProgram(stpIndex);
            }
            else
            {
                return RunOneStepProgram(stpName);
            }
        }

        bool MatchRun(int stpIndex = -1, string stpName = "Match")
        {
            if (stpIndex != -1)
            {
                return RunOneStepProgram(stpIndex);
            }
            else
            {
                return RunOneStepProgram(stpName);
            }
        }

        bool MeasureRun(int stpIndex = -1, string stpName = "Teach")
        {
            bool isSuccess = false;
            if (stpIndex != -1)
            {
                isSuccess = RunOneStepProgram(stpIndex);
            }
            else
            {
                isSuccess = RunOneStepProgram(stpName);
            }
            if (isSuccess)
            {
                imageCount++;
                m_SyncContext.Post(delegate
                {
                    updateLogMsg("Finish measure Image[" + imageCount.ToString() + "] successfully!");
                }, null);
            }
            return isSuccess;
        }

        public void viewResult(string type)
        {
            if (type == "View2D")
            {
                elementHostView.Child = viewer2D;
                viewer2D.ClearAll();
                List<string> drawingsList = xmlRes.getEnableVariables(XmlResolveClass.VariableType.Drawing);
                foreach (string str in drawingsList)
                {
                    int index = str.IndexOf('@');
                    string subStr = str.Substring(0, index);
                    string displayName = str.Substring(index + 1);
                    switch (subStr)
                    {
                        case "Image":
                            viewer2D.DrawImage(displayName, SubComponent.Intensity);
                            break;
                        case "Fixture":
                            viewer2D.DrawFixture(displayName, cyan);
                            break;
                        case "Region":
                            viewer2D.DrawROI(displayName, -1, green);
                            break;
                        case "Line2D":
                            viewer2D.LineScale = 10f;
                            viewer2D.DrawLine(displayName, red);
                            break;
                        case "Double":

                            break;
                        default: break;

                    }

                }
            }
            if (type == "View3D")
            {
                elementHostView.Child = viewer3D;
                List<string> drawingsList = xmlRes.getEnableVariables(XmlResolveClass.VariableType.Drawing);
                foreach (string str in drawingsList)
                {
                    int index = str.IndexOf('@');
                    string subStr = str.Substring(0, index);
                    string displayName = str.Substring(index + 1);
                    switch (subStr)
                    {
                        case "Image":
                            viewer3D.Draw(displayName);
                            break;
                        //case "Fixture":
                        //    viewer2D.DrawFixture(displayName, cyan);
                        //    break;
                        //case "Region":
                        //    viewer2D.DrawROI(displayName, -1, green);
                        //    break;
                        //case "Line2D":
                        //    viewer2D.LineScale = 10f;
                        //    viewer2D.DrawLine(displayName, red);
                        //    break;
                        default: break;

                    }

                }
            }


        }

        public string outputData()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            string data = "";
            string title = "";
            string value = "";
            if (envRes.getValueList().Count > 0)
            {
                dict = envRes.getValueList();
                foreach (KeyValuePair<string, string> kvp in dict)
                {
                    data += kvp.Key + "__" + kvp.Value + Environment.NewLine;
                    title += kvp.Key + ",";
                    value += kvp.Value + ",";
                }
                //if Log file not exists
                if (!File.Exists(LogFilePath))
                {
                    // create log file and append result
                    File.AppendAllText(LogFilePath, title + Environment.NewLine);
                }
                File.AppendAllText(LogFilePath, value + Environment.NewLine);
                return data;
            }
            return null;
        }

        public bool releaseCamera()
        {

            try
            {
                camera.Stop();
                camera.Disconnect();
                easyRanger.RemoveAllCameras();
            }
            catch (Exception ex)
            {
                updateLogMsg(easyRanger.GetLastErrorMessage());
            }

            return true;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (imageProcessStartFlag)
            {
                comboBoxView.Enabled = false;
                comboBoxProgram.Enabled = false;
                buttonRunOnce.Enabled = false;
                buttonStart.Enabled = false;
                buttonStop.Enabled = true;
                buttonReset.Enabled = false;
                buttonRunOnce.Enabled = false;
                backgroundWorker.RunWorkerAsync();
            }


        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            comboBoxView.Enabled = true;
            comboBoxProgram.Enabled = true;
            buttonRunOnce.Enabled = true;
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            buttonReset.Enabled = true;
            buttonRunOnce.Enabled = true;
            backgroundWorker.CancelAsync();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            imageProcessStartFlag = true;
            InitGUIControls();
            buttonStart.Enabled = true;
            buttonStop.Enabled = true;
            buttonReset.Enabled = false;
            RunOneStepProgram(stpNameDict["Teach"]);
        }

        private void buttonRunOnce_Click(object sender, EventArgs e)
        {
            RunOneStepProgram(stpNameDict[selectedStepProgramName]);
            comboBoxView.Enabled = true;
            comboBoxProgram.Enabled = true;
            buttonRunOnce.Enabled = true;
            buttonStart.Enabled = true;
            buttonStop.Enabled = true;
            buttonReset.Enabled = true;
            buttonRunOnce.Enabled = true;
            m_SyncContext.Post(delegate
            {
                viewResult(selectedViewType);
                if (selectedStepProgramName == "Measure")
                {
                    updateLogMsg(outputData());
                    updateLogMsg("Finish processing Image[" + imageCount + "]...");
                }
            }, null);
        }

        private void comboBoxView_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedViewType = (string)comboBoxView.SelectedItem;
        }

        private void comboBoxProgram_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedStepProgramName = (string)comboBoxProgram.SelectedItem;
        }
    }
}
