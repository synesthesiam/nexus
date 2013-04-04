using System;
using System.IO;
using System.Text.RegularExpressions;

using Gtk;

namespace Nexus
{
  public static class Markdown
  {
    public static void LoadIntoBuffer(TextBuffer buffer, TextReader reader)
    {
      buffer.TagTable.Add(new TextTag("header1")
      {        
        Weight = Pango.Weight.Bold,
        SizePoints = 18
      });

      buffer.TagTable.Add(new TextTag("header2")
      {        
        Weight = Pango.Weight.Bold,
        SizePoints = 14
      });

      buffer.TagTable.Add(new TextTag("header3")
      {        
        Weight = Pango.Weight.Bold
      });

      var iter = buffer.StartIter;
      var line = reader.ReadLine();

      while (line != null)
      {
        var textToInsert = string.Format("{0}{1}", line, Environment.NewLine);

        if (Regex.IsMatch(line, "^#[^#]"))
        {          
          buffer.InsertWithTagsByName(ref iter, textToInsert.Substring(2), "header1");
        }
        else if (Regex.IsMatch(line, "^##[^#]"))
        {          
          buffer.InsertWithTagsByName(ref iter, textToInsert.Substring(3), "header2");
        }
        else if (Regex.IsMatch(line, "^###[^#]"))
        {          
          buffer.InsertWithTagsByName(ref iter, textToInsert.Substring(4), "header3");
        }
        else
        {
          buffer.Insert(ref iter, textToInsert);
        }

        line = reader.ReadLine();
      }
    }
    
  } // Markdown

}