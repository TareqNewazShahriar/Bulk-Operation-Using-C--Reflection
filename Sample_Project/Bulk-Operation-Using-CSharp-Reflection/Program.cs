using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bulk_Operation_Using_CSharp_Reflection
{
	class Program
	{
		static void Main(string[] args)
		{
			var list = new List<Model>();
			for (int i = 0; i < 10; i++)
			{
				list.Add(new Model() { Id = i, Name = i.ToString(), DOB = DateTime.UtcNow });
			}

			var bulk = new BulkOperation<Model>(list, End, F);
			string resultantData = bulk.Process();

			Console.Write(resultantData);
		}

		static object F(List<object> objList)
		{
			string line = "";
			foreach (var item in objList)
			{
				try
				{
					if (item.GetType() == typeof(string))
						line += "\"" + item.ToString() + "\"";
					else
						line += (item ?? "").ToString();
				}
				catch { }
				finally { line += ','; }
			}
			return line.TrimEnd(',');
		}

		static string End(List<object> items, List<PropertyInfo> props)
		{
			StringBuilder csv = new StringBuilder();
			props.ForEach(x => csv.Append(x.Name + ","));
			csv.Remove(csv.Length - 1, 1);
			csv.AppendLine();

			foreach (var item in items)
			{
				csv.AppendLine(item.ToString());
			}

			return csv.ToString();
		}
	}

	class Model
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime DOB { get; set; }
	}
}

