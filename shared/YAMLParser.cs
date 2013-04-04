using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Nexus
{
  public class YAMLParser
  {

    #region Fields

    protected IDictionary<string, IList<Action<string>>> pathHandlers =
      new Dictionary<string, IList<Action<string>>>();

    protected int currentLevel = 0;
    protected IList<string> pathComponents = new List<string>();

    protected Regex singleNode = new Regex("([a-z ]+):(.+)$", RegexOptions.IgnoreCase),
              multiNode = new Regex("([a-z ]+):$", RegexOptions.IgnoreCase),
              indent = new Regex("^(\\s+)");

    #endregion

    #region Protected Methods

    protected void HandlePathValue(string value)
    {
      var filteredPath = pathComponents.Take(currentLevel + 1).Where(p => !string.IsNullOrEmpty(p));
      var currentPath = string.Format("/{0}", string.Join("/", filteredPath.ToArray()));

      if (pathHandlers.ContainsKey(currentPath))
      {
        foreach (var handler in pathHandlers[currentPath])
        {
          handler(value);
        }
      }
    }

    #endregion

    #region Public Methods

    public void HandlePath(string path, Action<string> handler)
    {
      if (!pathHandlers.ContainsKey(path)) {
        pathHandlers[path] = new List<Action<string>>();
      }

      pathHandlers[path].Add(handler);
    }

    public void Parse(Stream inputStream)
    {
      currentLevel = 0;
      pathComponents.Clear();

      using (var reader = new StreamReader(inputStream))
      {
        var line = reader.ReadLine();

        while (line != null)
        {
          // Determine path level
          var match = indent.Match(line);

          if (match.Success)
          {
            currentLevel = match.Groups[1].Value.Length;
          }
          else
          {
            currentLevel = 0;
          }

          // Make sure there are enough path components
          while (pathComponents.Count < (currentLevel + 1))
          {
            pathComponents.Add(string.Empty);
          }

          line = line.Trim();

          if (line.StartsWith("#"))
          {
            // Skip comments
          }
          else
          {
            // Start matching node types
            match = singleNode.Match(line);

            if (match.Success)
            {
              // Single node with value
              pathComponents[currentLevel] = match.Groups[1].Value.Trim();
              HandlePathValue(match.Groups[2].Value.Trim());
            }
            else
            {
              match = multiNode.Match(line);

              if (match.Success)
              {
                // Multi-node with children
                pathComponents[currentLevel] = match.Groups[1].Value.Trim();
              }
              else if (line.StartsWith("-"))
              {
                // Item in current path
                HandlePathValue(line.Substring(1).Trim());

              } // if item

            } // if not single node

          } // if not comment

          line = reader.ReadLine();

        } // line != null

      } // reader = StreamReader

    } // method Parse

    #endregion

  } // class YAMLParser

} // namespace Nexus

