﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DS.ClassLib.VarUtils;
using DS.MEPCurveTraversability.Interactors;
using DS.MEPCurveTraversability.Presenters;
using DS.MEPCurveTraversability.UI;
using OLMP.RevitAPI.Tools;
using OLMP.RevitAPI.Tools.Creation.Transactions;
using OLMP.RevitAPI.Tools.Extensions;
using OLMP.RevitAPI.Tools.Various;
using Rhino;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnitSystem = Rhino.UnitSystem;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;
using DS.MEPCurveTraversability.Interactors.Settings;
using System.Runtime;

namespace DS.MEPCurveTraversability;

/// <inheritdoc />
[Transaction(TransactionMode.Manual)]
public class ExternalCommand : IExternalCommand
{
    private static readonly double _mmToFeet =
            RhinoMath.UnitScale(UnitSystem.Millimeters, UnitSystem.Feet);
    private IEnumerable<Document> _allDocs;

    /// <inheritdoc />
    public Result Execute(ExternalCommandData commandData,
        ref string message, ElementSet elements)
    {
        var uiApp = commandData.Application;
        var application = uiApp.Application;
        var uiDoc = uiApp.ActiveUIDocument;

        var doc = uiDoc.Document;
        var links = doc.GetLoadedLinks();    

        var logger = AppSettings.Logger;
        var messenger = AppSettings.Messenger;
        var trf = new ContextTransactionFactory(doc, RevitContextOption.Inside);
        var elementFilter = new ElementMutliFilter(doc, links);

        if (new ElementSelector(uiDoc).Pick() is not MEPCurve mEPCurve) { return Result.Failed; }

        var docAR = doc;
        var docKR = doc;
        var linksAR = links;
        var linksKR = links;

        var settingsAR = DocSettingsAR.GetInstance();
        var settingsKR = DocSettingsKR.GetInstance();

        var checkServiceAR = GetCheckService(
            uiDoc,
            doc,
            links,
            docAR,
            linksAR,
            logger,
            trf,
            messenger,
            elementFilter,
            settingsAR.WallIntersectionSettings);
        if (!checkServiceAR.Initiate(mEPCurve)) { return Result.Failed; }

        var checkServiceKR = GetCheckService(
            uiDoc,
            doc,
            links,
            docKR,
            linksKR,
            logger,
            trf,
            messenger,
            elementFilter,
            settingsKR.WallIntersectionSettings);
        checkServiceKR.Initiate(mEPCurve);
        if (!checkServiceKR.Initiate(mEPCurve)) { return Result.Failed; }

        return Result.Succeeded;
    }

    private static (Document, IEnumerable<RevitLinkInstance>) ToDocLinks(
          IEnumerable<Document> docs,
          IEnumerable<RevitLinkInstance> sourceLinks)
    {
        Document doc = docs.FirstOrDefault(d => !d.IsLinked);
        var links = sourceLinks.Where(l => docs.Any(d => d.Title == l.GetLinkDocument().Title));

        return (doc, links);
    }

    private (Document, IEnumerable<RevitLinkInstance>) GetDocLinks(
        (Document, IEnumerable<RevitLinkInstance>) availableDocLinks, string suffix = null)
    {
        (Document, IEnumerable<RevitLinkInstance>) docLinks = new();

        return docLinks;
    }

    private IEnumerable<string> GetNames((Document, IEnumerable<RevitLinkInstance>) docLinks)
    {
        var names = new List<string>();

        if (docLinks.Item1 == null && docLinks.Item2 == null)
        { return names; }

        if (docLinks.Item1 != null)
        { names.Add(docLinks.Item1.Title); }
        if (docLinks.Item2 != null)
        { names.AddRange(docLinks.Item2.Select(r => r.GetLinkDocument().Title)); }

        return names;
    }


    private static TraversabilityService GetCheckService(
        UIDocument uiDoc,
        Document doc,
        List<RevitLinkInstance> links,
        Document activeDocToCheck,
        List<RevitLinkInstance> linksToCheck,
        ILogger logger,
        ContextTransactionFactory trf,
        IWindowMessenger messenger,
        ElementMutliFilter elementFilter,
        WallIntersectionSettings intersectionSettings)
    {
        IElementMultiFilter serviceElementFilter;
        if (activeDocToCheck != null && linksToCheck != null)
        { serviceElementFilter = new ElementMutliFilter(activeDocToCheck, linksToCheck); }
        else if (linksToCheck != null)
        { serviceElementFilter = new ElementMutliFilter(linksToCheck); }
        else
        { serviceElementFilter = new ElementMutliFilter(activeDocToCheck); }

        var elementIntersectFactory = new SolidElementIntersectionFactory(doc, serviceElementFilter)
        { Logger = logger, TransactionFactory = null };

        return new TraversabilityService(uiDoc, links, elementFilter, elementIntersectFactory, intersectionSettings)
        {
            Logger = logger,
            TransactionFactory = trf,
            WindowMessenger = messenger
        };
    }
}