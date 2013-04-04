using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace Nexus.Shared
{
	public static class CLIObject
	{
		public static string ToString(object obj, params string[] propertyNames)
		{
			var text = new StringBuilder();
			var objectType = obj.GetType();

			text.Append("[");
			text.Append(string.Join(",", propertyNames.Select(n => objectType.GetProperty(n))
				.Select(p => Convert.ToString(p.GetValue(obj, null)))
				.ToArray())
			);
			                        
			text.Append("]");

			return (text.ToString());
		}

		public static IEnumerable<T> FromString<T>(string text, params string[] propertyNames)
			where T : class, new()
		{
      if (text.Length < 3)
      {
        return (Enumerable.Empty<T>());
      }

			var objectType = typeof(T);
			var properties = propertyNames.Select(n => objectType.GetProperty(n)).ToList();

			var objectList = new List<T>();
			T currentObject = null;
			var propertyValues = new List<string>();
			var valueText = new StringBuilder();

			foreach (char currentChar in text)
			{
				switch (currentChar)
				{
					case '[':
						currentObject = new T();
						break;

					case ']':
						propertyValues.Add(valueText.ToString());
						valueText = new StringBuilder();
						
						for (int propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
						{
							var propInfo = properties[propertyIndex];

              if (propInfo.PropertyType.IsEnum)
              {
                propInfo.SetValue(currentObject, Enum.Parse(propInfo.PropertyType,
                                                            propertyValues[propertyIndex]), null);
              }
              else
              {
                propInfo.SetValue(currentObject, Convert.ChangeType(propertyValues[propertyIndex],
                                                                    propInfo.PropertyType), null);
              }
						}
						
						objectList.Add(currentObject);
						currentObject = null;
						propertyValues.Clear();
						break;

					case ',':
						propertyValues.Add(valueText.ToString());
						valueText = new StringBuilder();
						break;

					default:
						valueText.Append(currentChar);
						break;
				}
			}

			return (objectList);
		}

		public static string ToArrayString<T>(IEnumerable<T> items)
		{
      var text = new StringBuilder();

			text.Append("[");
			text.Append(string.Join(",",
				items.Select(x => Convert.ToString(x)).ToArray())
			);
			                        
			text.Append("]");

			return (text.ToString());
		}

    public static string ToMultiArrayString<T>(IEnumerable<IEnumerable<T>> items)
		{
      var text = new StringBuilder();

			text.Append(string.Join(string.Empty,
                              items.Select(i => ToArrayString(i)).ToArray()));
			                        
			return (text.ToString());
		}

		public static IEnumerable<T> FromArrayString<T>(string text)
		{
      if (text.Length < 3)
      {
        return (Enumerable.Empty<T>());
      }

			return (text.Substring(1, text.Length - 2)
				.Split(',').Select(s => (T)Convert.ChangeType(s, typeof(T))));
		}

		public static IEnumerable<IEnumerable<T>> FromMultiArrayString<T>(string text)
		{
      int startIndex = 0, endIndex = text.IndexOf(']');
      var outerList = new List<IEnumerable<T>>();

      while (endIndex > 0)
      {
        outerList.Add(FromArrayString<T>(text.Substring(startIndex, endIndex - startIndex + 1)));
        startIndex = endIndex + 1;
        endIndex = text.IndexOf(']', startIndex);
      }

      return (outerList);
		}
	}
}
