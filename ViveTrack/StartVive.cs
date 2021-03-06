﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Valve.VR;
using ViveTrack.Properties;


namespace ViveTrack
{
    public class StartVive : GH_Component
    {
        public OpenvrWrapper Vive;
        public static Transform CalibrationTransform = Transform.Identity;
        public static GH_Plane CalibrationPlane;
        public string OutMsg;
        public bool AutoUpdate;
        /// <summary>
        /// Initializes a new instance of the StartVive class.
        /// </summary>
        public StartVive()
          : base("StartVive", "StartVive",
              "Start HTV Vive, make sure SteamVR is running",
              "ViveTrack", "ViveTrack")
        {
            Vive = new OpenvrWrapper();
            AutoUpdate = true;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("AutoUpdate", "AutoUpdate", "Set True for auto update, if False is set you need a timer to refresh it.", GH_ParamAccess.item,true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Msg", "Msg", "Running Information of your Vive", GH_ParamAccess.item);
            pManager.AddGenericParameter("Vive", "Vive", "The Core of Vive tracking, needs to be passed to following component", GH_ParamAccess.item);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData("AutoUpdate", ref AutoUpdate);
            if (!DetectSteamVR())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "SteamVR not running.Please run SteamVR first.");
                if(AutoUpdate) this.ExpireSolution(true);
                return;
            }
            Vive.Connect();
            Vive.Update();
            if (Vive.Success)
            {
                
                OutMsg = Vive.TrackedDevices.Summary();
                //DA.SetDataList("Index", Vive.TrackedDevices.Indexes);
                DA.SetData("Vive", Vive);
                Vive.TrackedDevices.UpdatePoses();
            }
            else
            {
                OutMsg = "Vive is not setup correctly!! Detailed Reason:\n" + Vive.errorMsg + "\nCheck online the error code for more information.";
                Vive.Connect();
            }
            DA.SetData("Msg", OutMsg);

            if (AutoUpdate)
            {
                this.OnPingDocument().ScheduleSolution(1, doc => {
                    this.ExpireSolution(false);
                });
            }
        }

        public bool DetectSteamVR()
        {
            Process[] vrServer = Process.GetProcessesByName("vrserver");
            Process[] vrMonitor = Process.GetProcessesByName("vrmonitor");
            if ((vrServer.Length == 0) || (vrMonitor.Length == 0)) return false;
            if (!OpenVR.IsRuntimeInstalled()) return false;
            
            return true;
        }

        public void RunningSteamVR()
        {
            Process myProcess = new Process();
            try
            {
                myProcess.StartInfo.FileName = @"C:\Program Files (x86)\Steam\steamapps\common\SteamVR\bin\win64\vrstartup.exe";
                myProcess.Start();
                return;
            }
            catch(Exception e)
            {
                string message = "Can not find SteamVR application, please run by yourself :/";
                string caption = "Oops...Oops...Oops...";
                MessageBoxButtons button = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, button, MessageBoxIcon.Warning);
            }

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.ViveStart;
            }
        }

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "RunSteamVR", Menu_RunSteamVR, true, false).ToolTipText = @"Click to reset the component.";
           

        }



        private void Menu_RunSteamVR(object sender, EventArgs e)
        {
            RunningSteamVR();
            this.ExpireSolution(true);
            
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{730315e3-b05e-4227-aed2-cba81450bce7}"); }
        }
    }


   public class Robot
    {
        public Plane CurrentPos { set; get; }
        public List<Curve> PathCurves { set; get; }
        public double Tolerance { set; get; }
        public List<Point3d> PathPoints { set; get; }
        public Point3d TargetPos { set; get; }
        public bool Running { set; get; }

        private double divideLength = 0.2;
        private int index = 0;
        private int pointsCount;
  
        

        public Robot(Plane currentPos, List<Curve> paths, double tolerance)
        {
            CurrentPos = currentPos;
            PathCurves = paths;
            Tolerance = tolerance;
            PathPoints = GetPathPoints();
            pointsCount = PathPoints.Count;
        }

        private List<Point3d> GetPathPoints()
        {
            List<Point3d> pathPoints = new List<Point3d>();
            foreach (var crv in PathCurves)
            {
                Point3d[] points;
                crv.DivideByLength(divideLength, true, out points);
                pathPoints.AddRange(points.ToList());
            }
            
            return pathPoints;
        }

    }

    
}