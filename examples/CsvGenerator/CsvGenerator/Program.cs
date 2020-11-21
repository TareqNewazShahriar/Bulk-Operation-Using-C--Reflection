using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BulkOperation
{
	class Program
	{
		static void Main(string[] args)
		{
			var list = new List<Model>();
			for (int i = 0; i < 10; i++)
			{
				list.Add(new Model() { Id = i, Name = "Name-" + i.ToString(), DOB = DateTime.UtcNow });
			}

			var bulk = new BulkOperation<Model, string>(list, CreateCsvRow, GenerateCsvWithHeader);
			string resultantData = bulk.Process();

			Console.Write(resultantData);
		}

		static string CreateCsvRow(List<object> valueList, List<PropertyInfo> props)
		{
			string line = "";
			foreach (var item in valueList)
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

		static string GenerateCsvWithHeader(List<string> processedList, List<PropertyInfo> props)
		{
			StringBuilder stringBuilder = new StringBuilder();
			props.ForEach(x => stringBuilder.Append(x.Name + ","));
			stringBuilder.Remove(stringBuilder.Length - 1, 1);
			stringBuilder.AppendLine();

			foreach (var item in processedList)
			{
				stringBuilder.AppendLine(item.ToString());
			}

			return stringBuilder.ToString();
		}
	}

	class Model
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime DOB { get; set; }
	}
}
