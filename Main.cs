using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using CVL3Dv23LibraryVAA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _03_PPipeAlign
{
    public class Main
    {
        [CommandMethod("VAA", "PPipeAlign", CommandFlags.Modal)]
        public static void CommandRun()
        {

            List<PressurePipeNetwork> pressurePipeNetworkList = DocumentData.CurrentCivilDocument.GetPressurePipeNetworks();
            if (pressurePipeNetworkList.Count == 0)
                return;
            List<Network> networkList = DocumentData.CurrentCivilDocument.GetNetworks();


            using (Transaction ts = DocumentData.CurrentDatabase.TransactionManager.StartTransaction())
            {

                List<PressurePipe> pressurePipeList = new List<PressurePipe>();
                List<LineSegment3d> lineSegment3DList = new List<LineSegment3d>();
                foreach (PressurePipeNetwork pressurePipeNetwork in pressurePipeNetworkList)
                {
                    //pressurePipeList.AddRange(pressurePipeNetwork.GetPipesOfPressurePipeNetwork());
                    foreach (ObjectId pressurePipeID in pressurePipeNetwork.GetPipeIds())
                    {
                        PressurePipe pressurePipe = ts.GetObject(pressurePipeID, OpenMode.ForWrite, false, true) as PressurePipe;
                        if (pressurePipe.StyleName == "ASML_ЭН_201_Кабель" || pressurePipe.StyleName == "ASML_ЭС_201_Кабель")
                        {
                            pressurePipeList.Add(pressurePipe);
                        }
                        //else
                        //{
                        //    MessageBox.Show("Пропyщена сеть:", pressurePipeNetwork.StyleName.ToString());
                        //}
                    }
                }

                List<Pipe> pipeList = new List<Pipe>(); 
                foreach (Network network in networkList)
                {
                    //pipeList.AddRange(network.GetPipesOfNetwork());
                    foreach (ObjectId pipeId in network.GetPipeIds())
                    {
                        Pipe pipe = ts.GetObject(pipeId, OpenMode.ForWrite, false, true) as Pipe;
                        if (pipe.StyleName == "ASML_ЭН_201_Трубный блок" || pipe.StyleName == "ASML_ЭС_201_Трубный блок")
                        {
                            pipeList.Add(pipe);
                        }
                        //else
                        //{
                        //    MessageBox.Show("Пропущена сеть:", pipe.StyleName.ToString());
                        //}

                    }
                }
                int counter = 0;
                foreach (Pipe pipe in pipeList)
                {
                    PartDataRecord pdr = pipe.PartData;
                    double podh = (double)pdr.GetDataFieldBy("PODH").Value/1000;

                    //if (lineSegment3DList.Any( ))
                    foreach (PressurePipe pressurePipe in pressurePipeList)
                    {
                        LineSegment3d lineSegment3D = new LineSegment3d(PointExts.ChangeH(pressurePipe.StartPoint, 0),
                                                                        PointExts.ChangeH(pressurePipe.EndPoint, 0));

                        double pressurePipeOuterDiameter = pressurePipe.OuterDiameter;

                        Tolerance normalTolerance = new Tolerance(1, 0.01);
                        //Tolerance continuationTolerance = new Tolerance(0, 1);


                        bool isOnlineStartPoint = lineSegment3D.IsOn(PointExts.ChangeH(pipe.StartPoint, 0), normalTolerance);
                        bool isOnlineEndPoint = lineSegment3D.IsOn(PointExts.ChangeH(pipe.EndPoint, 0), normalTolerance);

                        //bool isContinuationOnlineStartPoint = lineSegment3D.IsOn(PointExts.ChangeH(pipe.StartPoint, 0), normalTolerance);
                        //bool isContinuationOnlineEndPoint = lineSegment3D.IsOn(PointExts.ChangeH(pipe.EndPoint, 0), normalTolerance);

                        if (isOnlineStartPoint == true && isOnlineEndPoint == true)
                        {
                            double pipeStartPointNewZ = PointExts.GetPointZValueOnPressurePipe(pressurePipe, pipe.StartPoint) + podh / 2 - pressurePipeOuterDiameter/2;
                            double pipeEndPointNewZ = PointExts.GetPointZValueOnPressurePipe(pressurePipe, pipe.EndPoint) + podh / 2 - pressurePipeOuterDiameter/2;

                            pipe.StartPoint = new Point3d(pipe.StartPoint.X, pipe.StartPoint.Y, pipeStartPointNewZ);
                            pipe.EndPoint = new Point3d(pipe.EndPoint.X, pipe.EndPoint.Y, pipeEndPointNewZ);
                            counter++;
                        }

                        //else 
                    }
                }
                MessageBox.Show($"Всего измененно: {counter.ToString()}","Выравнивание трубных блоков");

                ts.Commit();
            }
            
        }
    }
}
