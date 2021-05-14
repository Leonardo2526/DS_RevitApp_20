using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DS_SystemTools;
using System.IO;
using System.Collections;

namespace DisallowjoinStructure
{

    [Transaction(TransactionMode.Manual)]
    public class EntryCommand : IExternalCommand
    {
        //Get current date and time    
        readonly string CurDate = DateTime.Now.ToString("yyMMdd");
        readonly string CurDateTime = DateTime.Now.ToString("yyMMdd_HHmmss");

        public Result Execute(ExternalCommandData revit,
         ref string message, ElementSet elements)
        {
            UIApplication uiapp = revit.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            try
            {
                UnGroupAll(uiapp);
                //GroupIt(doc);
                //TransactionCommit(uiapp);

                MessageBox.Show("Балочные конструкции отсоединены!");
               
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        public void UnGroupAll(UIApplication uiapp)
        {
            Document doc = uiapp.ActiveUIDocument.Document;

            List<string> names = new List<string>();

            FilteredElementCollector docCollector = GetStructuralElements(doc);

            int elcount = docCollector.GetElementCount();
            if (elcount == 0)
            {
                MessageBox.Show("Элементы для разделения не найдены.");
                return;
            }


            using (Transaction t = new Transaction(doc, "Ungroup All Groups"))
            {
                t.Start();


                int groupCount = 0;
                foreach (GroupType groupType in new FilteredElementCollector(doc).OfClass(typeof(GroupType))
             .Cast<GroupType>()
             .Where(gt => gt.Groups.Size > 0))
                {
                        groupCount += 1;
                }

                int count = 0;
                foreach (GroupType groupType in new FilteredElementCollector(doc).OfClass(typeof(GroupType))
             .Cast<GroupType>()
             .Where(gt => gt.Groups.Size > 0))
                {
                    count += 1;
                    if (count <= groupCount)
                    {
                        IEnumerator ie = groupType.Groups.GetEnumerator();
                        
                        foreach (Group group in groupType.Groups)
                        {
                            ICollection<ElementId> elemset_old = group.UngroupMembers();

                            foreach (Element el in docCollector)
                            {
                                
                                foreach (ElementId elid in elemset_old)
                                {
                                    if (el.Id == elid)
                                        DisconnectElements(doc, el);
                                }
                            }

                            //Create new group
                            Group grpNew = doc.Create.NewGroup(elemset_old);
                            
                            names.Add(group.Name);
                            //set the group name to the former group name 
                            grpNew.GroupType.Name = group.Name;

                          
                        }
                    }
                    else
                        break;


                }

                t.Commit();
            }
        }


        private void TransactionCommit(UIApplication uiapp)
        {
            Document doc = uiapp.ActiveUIDocument.Document;

            using (Transaction transNew = new Transaction(doc, "Disallow join"))
            {
                try
                {
                    transNew.Start();
                    //DisconnectElements(doc);
                    GroupIt(doc);

                }

                catch (Exception e)
                {
                    transNew.RollBack();
                    MessageBox.Show(e.ToString());
                }

                transNew.Commit();
            }
        }

        private void DisconnectElements(Document doc, Element el)
        {
            string DirName = @"%USERPROFILE%\Desktop\Logs\";
            string ExpDirName = Environment.ExpandEnvironmentVariables(DirName);

            DS_Tools dS_Tools = new DS_Tools
            {
                DS_LogName = CurDateTime + "_Log.txt",
                DS_LogOutputPath = ExpDirName
            };


           
                Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd((FamilyInstance)el, 0);
                Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd((FamilyInstance)el, 1);
        }

        FilteredElementCollector GetStructuralElements(Document doc)
        {
            // what categories of family instances
            // are we interested in?
            BuiltInCategory[] bics = new BuiltInCategory[] {BuiltInCategory.OST_StructuralFraming}; 

            IList<ElementFilter> a
              = new List<ElementFilter>(bics.Count());

            foreach (BuiltInCategory bic in bics)
            {
                a.Add(new ElementCategoryFilter(bic));
            }

            LogicalOrFilter categoryFilter
              = new LogicalOrFilter(a);

            LogicalAndFilter familyInstanceFilter
              = new LogicalAndFilter(categoryFilter,
                new ElementClassFilter(
                  typeof(FamilyInstance)));

            IList<ElementFilter> b
              = new List<ElementFilter>(6)
              {
                  familyInstanceFilter
              };

            LogicalOrFilter classFilter
              = new LogicalOrFilter(b);

            FilteredElementCollector collector
              = new FilteredElementCollector(doc);

            collector.WherePasses(classFilter);

            return collector;
        }

        FilteredElementCollector GetGroups(Document doc)
        {
            // what categories of family instances
            // are we interested in?
            //BuiltInCategory[] bics = new BuiltInCategory[] {BuiltInCategory.OST_StructuralFraming}; 
            BuiltInCategory[] bics = new BuiltInCategory[] { BuiltInCategory.OST_IOSModelGroups};


            IList<ElementFilter> a
              = new List<ElementFilter>(bics.Count());

            foreach (BuiltInCategory bic in bics)
            {
                a.Add(new ElementCategoryFilter(bic));
            }

            LogicalOrFilter categoryFilter
              = new LogicalOrFilter(a);

            LogicalAndFilter familyInstanceFilter
              = new LogicalAndFilter(categoryFilter,
                new ElementClassFilter(
                  typeof(FamilyInstance)));

            IList<ElementFilter> b
              = new List<ElementFilter>(6)
              {
                  familyInstanceFilter
              };

            LogicalOrFilter classFilter
              = new LogicalOrFilter(a);

            FilteredElementCollector collector
              = new FilteredElementCollector(doc);

            collector.WherePasses(classFilter);

            return collector;
        }


        void GroupIt(Document doc)
        {
            FilteredElementCollector docCollector = GetGroups(doc);

            foreach (Group gr in docCollector)
            {
                ungroup(doc, gr);
            }
        }
       

        void ungroup(Document doc, Group gr)
        {
            //Create element set for new group
            List<ElementId> newgrpElementIds = new List<ElementId>();

            //Ungroup the group
            using (Transaction trans = new Transaction(doc, "Ungroup and delete"))
            {
                try
                {
                    trans.Start();

                    ICollection<ElementId> elemset_old = gr.UngroupMembers();

                    //build a new group
                    foreach (ElementId eid in elemset_old)
                    {
                        newgrpElementIds.Add(eid);
                    }

                    //Create new group
                    Group grpNew = doc.Create.NewGroup(newgrpElementIds);

                    //set the group name to the former group name
                    grpNew.GroupType.Name = gr.Name;

                }

                catch (Exception e)
                {
                    trans.RollBack();
                    MessageBox.Show(e.ToString());
                }

                trans.Commit();
            }

            

        }

    }

}
