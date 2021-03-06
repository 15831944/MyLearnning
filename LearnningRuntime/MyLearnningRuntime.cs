﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcGi = Autodesk.AutoCAD.GraphicsInterface;


[assembly:ExtensionApplication(typeof(LearnningRuntime.MyLearnningRuntime))]
[assembly:CommandClass(typeof(LearnningRuntime.MyLearnningRuntime))]
namespace LearnningRuntime
{



    public class MyLearnningRuntime : IExtensionApplication
    {

        class PlaneOverrule : AcGi.DrawableOverrule
        {
            public DBObjectCollection copyCollection = new DBObjectCollection();
            /*重写该方法*/
            public override bool WorldDraw(AcGi.Drawable drawable, AcGi.WorldDraw wd)
            {
                Matrix3d matrix = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);

                Entity ent1 = drawable as Entity;
                if (ent1.ObjectId != ObjectId.Null)
                {
                    Entity copyEnt = ent1.GetTransformedCopy(matrix);
                    copyEnt.WorldDraw(wd);
                    copyCollection.Add(copyEnt);
                }

                return base.WorldDraw(drawable, wd);
            }

        }

        public class MyWallDrawRule : AcGi.DrawableOverrule
        {

            public override bool WorldDraw(AcGi.Drawable drawable, AcGi.WorldDraw wd)
            {

                var line = drawable as Line;

                if (line != null)
                {
                    //delta,这条直线所代表的三维向量
                    var vec = line.Delta.RotateBy(Math.PI / 2, Vector3d.ZAxis).GetNormal();
                    var pts = new Point3dCollection()
                    {
                        line.StartPoint+vec,line.EndPoint+vec,
                        line.EndPoint-vec,line.StartPoint-vec,
                    };
                    wd.Geometry.Polygon(pts);
                    

                    var hatch = new Hatch();
                    var pts2d = new Point2dCollection();
                    var bulge = new DoubleCollection();

                    foreach (Point3d pt3d in pts)
                    {
                        pts2d.Add(new Point2d(pt3d.X, pt3d.Y));
                        bulge.Add(0);
                    }

                    hatch.AppendLoop(HatchLoopTypes.Default, pts2d, bulge);
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANGLE");

                    hatch.WorldDraw(wd);

                }

                return base.WorldDraw(drawable, wd);
            }

        }

        [CommandMethod("cmd1")]
        public void Test()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            Editor acEd = acDoc.Editor;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                var blkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                var mdlSpc = acTrans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;


                var myWallRule = new MyWallDrawRule();

                myWallRule.SetExtensionDictionaryEntryFilter("MyWallType");

                Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), myWallRule, false);
                Overrule.Overruling = true;


                var line = new Line(Point3d.Origin, Point3d.Origin + Vector3d.XAxis.RotateBy(Math.PI / 6, Vector3d.ZAxis) * 10);

                mdlSpc.AppendEntity(line);

                acTrans.AddNewlyCreatedDBObject(line, true);

                if (!line.ExtensionDictionary.IsValid)
                {

                    line.CreateExtensionDictionary();
                    var dict = acTrans.GetObject(line.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
                    dict.SetAt("MyWallType", new DataTable());
                }

                acTrans.Commit();

            }
        }

        private static PlaneOverrule overrule;
        [CommandMethod("otdp")]
        public static void TestOverrideToDifferentPlane()
        {
            if (overrule == null)
            {
                overrule = new PlaneOverrule();
                Overrule.AddOverrule(RXObject.GetClass(typeof(Entity)), overrule, false);
            }
            else
            {
                foreach (DBObject obj in overrule.copyCollection)
                {
                    obj.Dispose();
                }
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Entity)), overrule);
                overrule.Dispose();
                overrule = null;
            }
            Overrule.Overruling = !Overrule.Overruling;
            Application.DocumentManager.MdiActiveDocument.Editor.Regen();
        }
        public void Initialize()
        {
            
        }

        public void Terminate()
        {
           
        }
    }
}
