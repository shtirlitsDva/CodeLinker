﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeRecyclerGui
{
  /// <summary> A project to recycle. </summary>
  class ProjectToRecycle
  {
      /// <summary> Source project.  Full Path with file extension.</summary>
      public string SourceProject { get; set; } 

      /// <summary> Destination project - just the file name, no path. Extension should be the same as the Source.</summary>
      public string DestinationProjectName { get; set; } 
  }
}