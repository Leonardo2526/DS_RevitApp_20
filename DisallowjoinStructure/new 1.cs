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

                    foreach (ElementId eid in newgrpElementIds)
                    {
                        Element elem = doc.GetElement(eid);

                        //setTypes(doc, elem);
                        //strOldID += "\n" + elem.Id.IntegerValue.ToString();
                    }
                    GroupType oldgrType = null;

                    //set the group name to the former group name
                    grpNew.GroupType.Name = gr.Name;

                    FilteredElementCollector coll
                    = new FilteredElementCollector(doc)
                    .OfClass(typeof(GroupType));

                    foreach
                    (
                    GroupType grpTypes in new FilteredElementCollector(doc)
                    .OfClass(typeof(GroupType))
                    .Cast<GroupType>()
                    .Where(g => g.Name == gr.Name)
                    )

                    {
                        oldgrType = grpTypes;
                        break;
                    }

                    oldgrType = grpNew.GroupType;
                }