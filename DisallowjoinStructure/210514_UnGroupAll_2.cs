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
                        foreach (Group group in groupType.Groups)
                        {
                            ICollection<ElementId> elemset_old = group.UngroupMembers();

                            ICollection<Element> elems = null;

                            foreach (ElementId elid in elemset_old)
                            {
                                Element el = doc.GetElement(elid);

                                if (docCollector.Contains(el))
                                    DisconnectElements(doc, el);
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