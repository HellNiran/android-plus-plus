﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Win32;
using Microsoft.Build.Utilities;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public static class GccUtilities
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public const int CommandLineLength = 8191;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertPathWindowsToPosix (string path)
    {
      // 
      // Convert Windows path in to a Cygwin path suitable for passing to GCC command line.
      // 

      string rtn = path.Replace ('\\', '/');

      return QuoteIfNeeded (rtn);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertPathWindowsToGccDependency (string path)
    {
      string rtn = path.Replace ('\\', '/');

      return GccUtilities.Escape (rtn);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertPathPosixToWindows (string path)
    {
      // 
      // Convert a Cygwin path in to a Windows path.
      // 

      StringBuilder workingBuffer = new StringBuilder (path);

      workingBuffer.Replace ('/', '\\');

      workingBuffer.Replace ("\\\\", "\\");

      return workingBuffer.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string QuoteIfNeeded (string arg)
    {
      // 
      // Add quotes around a string, if they are needed.
      // 

      if (arg.StartsWith ("\""))
      {
        return arg;
      }

      var match = arg.IndexOfAny (new char [] { ' ', '\t', ';', '&' }) != -1;

      if (!match)
      {
        return arg;
      }

      return "\"" + arg + "\"";
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string Escape (string input)
    {
      StringBuilder escapedStringBuilder = new StringBuilder (input);

      escapedStringBuilder.Replace (@"\", @"\\");

      escapedStringBuilder.Replace (@" ", @"\ ");

      return escapedStringBuilder.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string Unescape (string input)
    {
      StringBuilder unescapedStringBuilder = new StringBuilder (input);

      unescapedStringBuilder.Replace (@"\\", @"\");

      unescapedStringBuilder.Replace (@"\ ", @" ");

      return unescapedStringBuilder.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string ConvertGccOutputToVS (string line)
    {
      // 
      // Parse and reformat GCC error and warning output into a Visual Studio 'jump to line' style.
      // 
      //    CppSource/demo.c:51: error: conflicting types for 'seedRandom'
      // becomes:
      //    c:\Projects\san-angeles\CppSource\demo.c(51) error: conflicting types for 'seedRandom'
      // 

      string [] GCC_REGEX_ERROR_MATCH = 
      {
        @"^\s*In file included from (.?.?[^:]*.*?):([1-9]\d*):(.*$)",   // "In file included from CppSource/demo.c:32:"
        @"^\s*(.?.?[^:]*.*?):([1-9]\d*):([1-9]\d*):(.*$)",              // "CppSource/importgl.c:25:17: error: new.h: No such file or directory"
        @"^\s*(.?.?[^:]*.*?):([1-9]\d*):(.*$)",                         // "CppSource/demo.c:51: error: conflicting types for 'seedRandom'"
        @"^\s*(.?.?[^:]*.*?):(.?.?[^:]*.*?):([1-9]\d*):(.*$)",          // "Android/Debug/app-android.o:C:\Projects\vs-android_samples\san-angeles/CppSource/app-android.c:38: first defined here"
      };

      string [] GCC_REGEX_FILENAME_GROUP = 
      {
        @"$1",
        @"$1",
        @"$1",
        @"$2",
      };

      string [] GCC_REGEX_ERROR_TO_VS_REPLACE = 
      {
        @"($2): includes this header: $3",
        @"($2,$3): $4",
        @"($2): $3",
        @"($3): '$1' $4",
      };

      for (int i = 0; i < GCC_REGEX_ERROR_MATCH.Length; ++i)
      {
        Regex regExMatcher = new Regex (GCC_REGEX_ERROR_MATCH [i]);

        if (regExMatcher.IsMatch (line))
        {
          string filename = regExMatcher.Replace (line, GCC_REGEX_FILENAME_GROUP [i]);

          filename = ConvertPathPosixToWindows (filename);

          string description = regExMatcher.Replace (line, GCC_REGEX_ERROR_TO_VS_REPLACE [i]);

          try
          {
            filename = Path.GetFullPath (filename);
          }
          catch (Exception)
          {
            // Not really concerned if this fails at the moment.
          }

          return filename + description;
        }
      }

      return line;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class DependencyParser
    {

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      private ITaskItem m_outputFile = new TaskItem ();

      private List<ITaskItem> m_dependencies = new List<ITaskItem> ();

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      public DependencyParser (string dependencyFile)
      {
        Parse (dependencyFile);
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      public ITaskItem OutputFile
      {
        get
        {
          return m_outputFile;
        }
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      public List<ITaskItem> Dependencies
      {
        get
        {
          return m_dependencies;
        }
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      private void Parse (string dependencyFile)
      {
        // 
        // Parse GCC and Java-style dependency files.
        // 
        // GCC:
        //  AndroidMT/Debug/native-media-jni.obj: \
        //   C:/Users/Justin/documents/visual\ studio\ 2010/Projects/native-media/native-media/jni/native-media-jni.c \
        // 
        // JAVA:
        //  AndroidMT/Debug/native-media-jni.class \
        //   : C:/Users/Justin/documents/visual\ studio\ 2010/Projects/native-media/native-media/jni/native-media-jni.java \
        // 

        // 
        // To reading variable ':' placement headers easier, we read the entire file and reconstitute it around ': ' and ' \'.
        // 

        string fileContents = File.ReadAllText (dependencyFile);

        fileContents = fileContents.Replace (System.Environment.NewLine, "");

        string [] dependencyEntries = fileContents.Split (new string [] { ": \\", " \\", ": " }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < dependencyEntries.Length; ++i)
        {
          string line = dependencyEntries [i].Replace (": ", "").Replace (" \\", "").Trim ();

          if (!string.IsNullOrWhiteSpace (line))
          {
            if (i == 0)
            {
              string outputFilePath = GccUtilities.Unescape (line);

              m_outputFile = new TaskItem (outputFilePath);
            }
            else
            {
              ParseDependencyLine (line);
            }
          }
        }
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      private void ParseDependencyLine (string line)
      {
        if (string.IsNullOrWhiteSpace (line))
        {
          return;
        }

        // 
        // Remove a trailing '\' character used to signify the list continues on the next line.
        // 

        if (line.EndsWith (@"\"))
        {
          line = line.Substring (0, line.Length - 1);
        }

        line.Trim ();

        // 
        // Now iterate through the line which can contain zero or more dependencies.
        // Although they are seperated by spaces we can't use Split() here since filenames
        // can also contain spaces that are escaped using backslash.  For some reason only
        // spaces are escaped.  Even literal backslash chars don't need escaping.
        // 

        while ((line.Length > 0) && (!string.IsNullOrWhiteSpace (line)))
        {
          int end = FindEndOfFilename (line);

          string filename = line.Substring (0, end);

          if (!string.IsNullOrWhiteSpace (filename))
          {
            // 
            // Files with spaces in look like this:
            //  C:\Program\ Files\ (x86)\ARM\Mali\ Developer\ Tools\OpenGL\ ES\ 1.1\ Emulator\ v1.0\include/GLES2/gl2ext.h \
            // 

            filename = GccUtilities.Unescape (filename);

            filename = GccUtilities.ConvertPathPosixToWindows (filename);

            m_dependencies.Add (new TaskItem (filename));
          }

          if (end == line.Length)
          {
            break;
          }

          line = line.Substring (end + 1).Trim ();
        }
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      private static int FindEndOfFilename (string line)
      {
        // 
        // Search line for an unescaped space character (which represents the end of file), or EOF.
        // 

        int i;

        bool escapedSequence = false;

        for (i = 0; i < line.Length; ++i)
        {
          if (line [i] == '\\')
          {
            escapedSequence = true;
          }
          else if ((line [i] == ' ') && !escapedSequence)
          {
            break;
          }
          else if (escapedSequence)
          {
            escapedSequence = false;
          }
        }

        return i;
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      public static void ConvertJavaDependencyFileToGcc (string dependencyFile)
      {
        // 
        // Load a specified dependency file and convert any invalid (windows) paths, to escaped-posix.
        // 

        string dependencyFileTemp = dependencyFile + ".tmp";

        using (StreamReader reader = new StreamReader (dependencyFile))
        {
          using (StreamWriter writer = new StreamWriter (dependencyFileTemp))
          {
            StringBuilder builder = new StringBuilder ();

            string line = reader.ReadLine ();

            while (!string.IsNullOrWhiteSpace (line))
            {
              builder.Length = 0;

              builder.Append (line);

              // 
              // Remove a trailing '\' character used to signify the list continues on the next line.
              // 

              if (line.EndsWith (@"\"))
              {
                line = line.Substring (0, line.Length - 1);
              }

              line.Trim ();

              builder.Replace (line, ConvertPathWindowsToGccDependency (line));

              builder.Replace (@"\ \", @" \"); // patch line endings

              builder.Replace (@"\ :\ ", @" : "); // patch output directive

              writer.WriteLine (builder.ToString ());

              line = reader.ReadLine ();
            }
          }
        }

        File.Copy (dependencyFileTemp, dependencyFile, true);

        File.Delete (dependencyFileTemp);
      }

      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
