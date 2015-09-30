﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


namespace CodeCloner
{
  /// <summary> Destination <c>CSPROJ</c> Parser and Cloner. </summary>
  internal class DestinationCsProjParser
  {
    static List<string> ItemElementsToSkip = new List<string> {"reference","projectreference","bootstrapperpackage"};
    static List<string> ItemElementsDoNotLink = new List<string> {"folder"};
    

    /// <summary> Absolute pathname of the destination <c>CSPROJ</c> including file name. </summary>
    internal  string DestCsProjAbsolutePath { get; }
    /// <summary> Absolute Directory of the destination <c>CSPROJ</c>. NO file name. </summary>
    internal  string DestCsProjDirectory { get; }
    private XDocument destCsProjXdoc;
    private XComment  startPlaceHolder;
    private XComment  endPlaceHolder;
    
    /// <summary> Source <c>CSPROJ</c>s defined in the Destination <c>CSPROJ</c> placeholder. 
    ///           Can be zero, can be lots.</summary>
    internal List<string> SourceCsProjList { get; }

    /// <summary> Code Files to be excluded from the clone. </summary>
    internal List<string> ExclusionsList { get; }

    /// <summary> Source <c>CSPROJ</c> is specified in the destination <c>CSPROJ</c> XML comment placeholder. </summary>
    /// <param name="destCsProj"> Absolute path of destination <c>CSPROJ</c>. </param>
    internal  DestinationCsProjParser(string destCsProj)
    {
      DestCsProjAbsolutePath = PathMaker.MakeAbsolutePathFromPossibleRelativePathOrDieTrying(null, destCsProj);
      DestCsProjDirectory = Path.GetDirectoryName(DestCsProjAbsolutePath)?? "";

      try { destCsProjXdoc = XDocument.Load(DestCsProjAbsolutePath); }
      catch (Exception e) { Program.Crash(e); }

      
      SourceCsProjList = new List<string>();
      ExclusionsList   = new List<string>();
      startPlaceHolder = FindCommentOrCrash("CodeCloner");
      endPlaceHolder   = FindCommentOrCrash("EndCodeCloner");
      foreach (string line in startPlaceHolder.Value.Split(new[] { "\r\n", "\n", Environment.NewLine }, StringSplitOptions.None).ToList())
      {
        
        if (line.ToLower().Trim().StartsWith("source:"))
        {
          string sourceInXml = line.ToLower().Replace("source:", "").Trim();
          string absoluteSource = PathMaker.MakeAbsolutePathFromPossibleRelativePathOrDieTrying(DestCsProjDirectory, sourceInXml);
          SourceCsProjList.Add(absoluteSource);
        }

        if (line.ToLower().Trim().StartsWith("exclude:"))
          ExclusionsList.Add(line.Replace("exclude:", "").Trim());
      }

      CloneCode();
    }

    /// <summary> Source <c>CSPROJ</c> and destination <c>CSPROJ</c> specified here. </summary>
    /// <param name="sourceCsProj"> Absolute or Relative path of Source <c>CSPROJ</c>. </param>
    /// <param name="destCsProj"> Absolute or Relative path of Destination <c>CSPROJ</c>. </param>
    internal DestinationCsProjParser(string sourceCsProj, string destCsProj)
    {
      SourceCsProjList = new List<string> {PathMaker.MakeAbsolutePathFromPossibleRelativePathOrDieTrying(null, sourceCsProj)};
      ExclusionsList   = new List<string>();
      
      DestCsProjAbsolutePath = PathMaker.MakeAbsolutePathFromPossibleRelativePathOrDieTrying(null, destCsProj);
      DestCsProjDirectory = Path.GetDirectoryName(DestCsProjAbsolutePath);

      try { destCsProjXdoc = XDocument.Load(DestCsProjAbsolutePath); }
      catch (Exception e) { Program.Crash(e); }

      startPlaceHolder = FindCommentOrCrash("CodeCloner");
      endPlaceHolder   = FindCommentOrCrash("EndCodeCloner");

      CloneCode();
    }




    /// <summary> Clones the source code from the source <c>CSPROJ</c> file to the destination <c>CSPROJ</c> file. 
    ///           Tweaks the file paths so the project can find them. 
    ///           Adds a <c>&lt;Link&gt;</c> so you can edit within the destination project.</summary>
    internal void CloneCode()
    {
      if (string.IsNullOrEmpty(DestCsProjAbsolutePath)) Program.Crash("ERROR: No destCsProjFileAbsolutePath. That's a bug.");
      if (!destCsProjXdoc.Root.Elements().Any()) Program.Crash("ERROR: No Destination CSPROJ file at " + DestCsProjAbsolutePath);

      RemoveOldDestCsProjClonedCode();

      foreach (string sourcePath in SourceCsProjList)
      {
        
        string sourceProjAbsolutePath = (PathMaker.IsAbsolutePath(sourcePath)) ? sourcePath : Path.Combine(DestCsProjDirectory, sourcePath);
        string sourceProjAbsoluteDirectory = Path.GetDirectoryName(sourceProjAbsolutePath);

        SourceCsProjParser sourceProjParser = new SourceCsProjParser(sourceProjAbsolutePath);

        endPlaceHolder.AddBeforeSelf(new XComment("Cloned from "+ sourcePath));

        string destRelativePathPrefix = PathMaker.MakeRelativePath(sourceProjAbsoluteDirectory, DestCsProjDirectory);

        foreach (XElement sourceItemGroup in sourceProjParser.ItemGroups)
        {
          XElement destItemGroup = new XElement("ItemGroup");

          foreach (XElement sourceItem in sourceItemGroup.Elements())
          {
            string elementName = sourceItem.Name.LocalName;
            if (ItemElementsToSkip.Contains(elementName.ToLower())) continue;

            XAttribute attrib = sourceItem.Attribute("Include") ?? sourceItem.Attribute("Exclude");

            if (attrib != null)
            {
              string originalPath = attrib.Value;
              if (ExclusionsList.Any(x => originalPath.Contains(x)))
              {
                Log.WriteLine("Excluded " + originalPath + " from " + sourceProjAbsolutePath + " clone to " + DestCsProjAbsolutePath
                  + " because you said Exclude: " + ExclusionsList.FirstOrDefault(x => originalPath.Contains(x)) );
                continue;
              }
              if (!PathMaker.IsAbsolutePath(originalPath)) { attrib.Value = destRelativePathPrefix + originalPath; }

              if (!sourceItem.Descendants("Link").Any() || !ItemElementsDoNotLink.Contains(elementName.ToLower())) {
                sourceItem.Add(XElement.Parse("<Link>" + originalPath + "</Link>"));
              }
              destItemGroup.Add(sourceItem);
            }
          }
          endPlaceHolder.AddBeforeSelf(destItemGroup);
        }
        endPlaceHolder.AddBeforeSelf(new XComment("End Clone from "+ sourcePath));
        Log.WriteLine("End Clone from "+ sourcePath);
      }
    }



    private XComment FindCommentOrCrash(string commentStartsWith)
    {
      IEnumerable<XComment> comments = from node in destCsProjXdoc.Elements().DescendantNodesAndSelf()
                                       where node.NodeType == XmlNodeType.Comment
                                       select node as XComment;

      List<XComment> placeholders  = comments.Where(c => c.Value.ToLower().Trim().StartsWith(commentStartsWith.ToLower())).ToList();

      if (placeholders.Count != 1)
        Program.Crash("ERROR: " +DestCsProjAbsolutePath+ " has " +placeholders.Count+" XML comments with " + commentStartsWith);

      return placeholders.First();
    }

    
    private void RemoveOldDestCsProjClonedCode()
    {
      if (startPlaceHolder != null && endPlaceHolder != null && startPlaceHolder.IsBefore(endPlaceHolder))
      {
        XNode startNode = startPlaceHolder;
        while (startNode.NextNode != endPlaceHolder) { startNode.NextNode.Remove(); }
      }
      else Program.Crash("Error: cannot remove old Cloned Code from " + DestCsProjAbsolutePath);
    }
   


  }
}
