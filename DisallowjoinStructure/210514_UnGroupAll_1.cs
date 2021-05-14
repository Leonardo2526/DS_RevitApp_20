public void UnGroupAll(UIApplication uiapp)
        {
            Document doc = uiapp.ActiveUIDocument.Document;

            List<string> names = new List<string>();

            using (Transaction t = new Transaction(doc, "Ungroup All Groups"))
            {
                t.Start();
                foreach (GroupType groupType in new FilteredElementCollector(doc).OfClass(typeof(GroupType))
             .Cast<GroupType>()
             .Where(gt => gt.Name == "Группа1" && gt.Groups.Size > 0))
                {
                  
                    foreach (Group group in groupType.Groups)
                    {         
                        ICollection<ElementId> elemset_old = group.UngroupMembers();

                        //Create new group
                        Group grpNew = doc.Create.NewGroup(elemset_old);

                        names.Add(group.Name);
                        //set the group name to the former group name
                        grpNew.GroupType.Name = group.Name;
                       
                    }
                }

                t.Commit();
            }
        }