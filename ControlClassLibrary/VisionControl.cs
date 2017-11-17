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
        bool MatchFinished;
        StepProgram grabProgram;
        StepProgram matchProgram;
        //UI 
        SynchronizationContext m_SyncContext = null;
        //Image Grab ID
        int count = 0;

        public enum ViewType
        {
            View2D,
            View3D
        };

        string ENVFilePath = Resources.EZRPath + @"\test_Offline.env";
        //string ENVFilePath = Resources.EZRPath + @"\test.env";
        string ICXFilePath = Resources.CameraConfig + @"\config.icx";
        string LogFilePath = Resources.LogPath + @"\LogResult.csv";

        private void VisionControl_Load(object sender, EventArgs e)
        {
            //splitcontainer3.hide();
            treeViewProgram.Hide();
            //生成Tree View

            try
            {
                if (File.Exists(ENVFilePath))
                {
                    easyRanger = new ProcessingEnvironment();
                    easyRanger.Load(ENVFilePath);
                    updateLogMsg("Load Environment File from " + ENVFilePath);
                    envRes = new ENVResolveClass(easyRanger);
                    updateLogMsg("Load Environment File Successfully...");
                    InitTreeView();
                    InitViewer();
                    updateLogMsg("Display try view");
                    //if (InitialCamera())
                    //{
                    //    updateLogMsg("Camera Initial successfully...");
                    //    imageProcessStartFlag = false;
                    //}
                    //else
                    //{
                    //    updateLogMsg("Camera Initial failed...");
                    //}
                    comboBoxProgram.SelectedIndex = 0;
                    m_SyncContext = SynchronizationContext.Current;
                    backgroundWorker1 = new BackgroundWorker();
                    backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
                    backgroundWorker1.WorkerSupportsCancellation = true;
                    threadRunFlag = true;
                    xmlRes = new XmlResolveClass();
                    MatchFinished = true;
                    grabProgram = easyRanger.GetStepProgram("Grab");
                    matchProgram = easyRanger.GetStepProgram("Match");
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

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            try
            {
                RunContinue();
            }
            catch (Exception ex)
            {
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

        void InitViewer()
        {
            viewer2D = new View2DControl
            {
                TrueAspectRatio = true,
                ShowClearButton = System.Windows.Visibility.Visible,
                ShowEditControls = System.Windows.Visibility.Collapsed,
                ShowOptions = System.Windows.Visibility.Visible,
                LineScale = 2,
                PointScale = 2
            };
            //3D View
            //viewer3D = new View3DControl
            //{
            //    // Set hybrid color mode (color is determined by height and intensity)
            //    ColorMode = View3DControl.ColorModes.Height,
            //    // Set wireframe mode
            //    GeometryMode = View3DControl.GeometryModes.Wireframe
            //};
            //// lowest and highest Z value in the image,
            //viewer3D.ColorMin = 0.8f;
            //viewer3D.ColorMax = 1f;
            //viewer3D.ShowOptions = System.Windows.Visibility.Visible;
            //viewer3D.CameraDefaultDistance = 1.0f;
            //viewer3D.CameraDefaultPitch = 45;
            //viewer3D.CameraDefaultYaw = 34;
            //viewer3D.SpecularAmplification = 0.5f;
            //elementHostView.Child = viewer3D;
            //viewer3D.Environment = easyRanger;
            //
            viewer2D.Environment = easyRanger;
            elementHostView.Child = viewer2D;
        }

        bool InitialCamera()
        {
            //Add Camera to EZR
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
                updateLogMsg(ex.ToString());
                return false;
            }


        }

        void updateLogMsg(string msg)
        {
            richTextBox1.Text = msg + Environment.NewLine + richTextBox1.Text;

        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (imageProcessStartFlag)
            {
                MatchFinished = true;
                backgroundWorker1.RunWorkerAsync();
            }


        }

        object lockObj = new object();
        /// <summary>
        /// 
        /// </summary>
        void RunContinue()
        {
            while (threadRunFlag)
            {

                if (backgroundWorker1.CancellationPending)
                {
                    break;
                }
                Thread.Sleep(500);
                if (MatchFinished)
                {
                    RunOnce();
                }

            }

        }

        bool isGrabLock = false;
        /// <summary>
        /// 
        /// </summary>
        void RunOnce()
        {
            // 1. grab imag
            bool fStatus = false;
            if (!isGrabLock)
            {
                fStatus = GrabRun();
            }
            if (fStatus)
            {
                if (MatchRun())
                {
                    isGrabLock = true;
                    m_SyncContext.Post(delegate
                    {
                        
                        viewResult(ViewType.View2D);
                        updateLogMsg(outputData());
                        isGrabLock = false;

                    }, null);

                }
                else
                {
                    isGrabLock = true;
                    m_SyncContext.Post(delegate
                    {
                        viewResult(ViewType.View2D);
                        updateLogMsg(outputData());
                        isGrabLock = false;
                    }, null);

                }

            }
        }
        /// <summary>
        /// Run Grab stepProgram in EasyRanger
        /// </summary>
        /// <returns></returns>
        bool GrabRun()
        {
            try
            {
                grabProgram.RunFromBeginning();
            }
            catch (Exception ex)
            {
                if (easyRanger.GetLastErrorMessage().Contains("timeout"))
                {
                    //
                    m_SyncContext.Post(delegate
                    {
                        updateLogMsg("Camera is busy...");
                    }, null);
                    return false;
                }
                else
                {
                    m_SyncContext.Post(delegate
                    {
                        updateLogMsg(ex.ToString());
                    }, null);
                    return false;
                }
            }
            count++;
            m_SyncContext.Post(delegate
            {
                updateLogMsg("Image ID: " + count.ToString() + Environment.NewLine);
            }, null);
            return true;
        }
        /// <summary>
        /// Run Teach stepProgram in EasyRanger
        /// </summary>
        /// <returns></returns>
        bool TeachRun()
        {
            bool teachRunFlag = true;
            try
            {
                StepProgram teachProgram = easyRanger.GetStepProgram("Teach");
                teachProgram.RunFromBeginning();
                foreach (Step stp in teachProgram.StepList)
                {
                    if (stp.Enabled)
                    {
                        if (!stp.Success)
                        {
                            teachRunFlag = false;
                            m_SyncContext.Post(delegate
                            {
                                updateLogMsg(easyRanger.GetLastErrorMessage());
                            }, null);
                            return teachRunFlag;
                        }
                    }
                }
                return teachRunFlag;
            }
            catch (Exception ex)
            {
                m_SyncContext.Post(delegate
                {
                    updateLogMsg(ex.ToString());
                }, null);
                return false;
            }
        }
        /// <summary>
        /// Run Match stepProgram in EasyRanger
        /// </summary>
        /// <returns></returns>
        bool MatchRun()
        {
            try
            {
                matchProgram.RunFromBeginning();
                foreach (Step stp in matchProgram.StepList)
                {
                    if (stp.Enabled)
                    {
                        if (!stp.Success)
                        {
                            m_SyncContext.Post(delegate
                            {
                                updateLogMsg(easyRanger.GetLastErrorMessage());

                            }, null);
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                m_SyncContext.Post(delegate
                {
                    updateLogMsg(easyRanger.GetLastErrorMessage());
                }, null);
                return false;
            }
        }

        /// <summary>
        /// choose viewer in 2D or 3D
        /// </summary>
        /// <param name="type"></param>
        public void viewResult(ViewType type)
        {
            if (type == ViewType.View2D)
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
            else
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
                    File.AppendAllText(LogFilePath, title + Environment.NewLine );
                }
                File.AppendAllText(LogFilePath, value + Environment.NewLine);
                return data;
            }
            return null;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        private void buttonRunOnce_Click(object sender, EventArgs e)
        {
            RunOnce();
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

        private void buttonReset_Click(object sender, EventArgs e)
        {
            imageProcessStartFlag = true;
        }

        private void comboBoxView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
