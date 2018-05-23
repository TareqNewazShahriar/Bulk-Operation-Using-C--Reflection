using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CsvGenerator
{
	public class BulkOperation<T>
	{
		#region Fields

		private List<T> list = null;
		private List<PropertyInfo> properties = null;
		private List<object> processedItems = null;

		Func<List<object>, object> _forEachItem;
		Func<List<object>, List<PropertyInfo>, dynamic> _atEnd;

		#endregion Fields

		#region Constructors

		public BulkOperation(List<T> list, Func<List<object>, List<PropertyInfo>, dynamic> AtEnd, Func<List<object>, object> EachValue = null)
		{
			this._forEachItem = EachValue;
			this._atEnd = AtEnd;

			this.list = list;
			properties = new List<PropertyInfo>();
			processedItems = new List<object>();

			InitPropObject(typeof(T));
		}

		#endregion Constructors

		#region Public Methods

		public dynamic Process()
		{
			ForEachItem(list);

			return _atEnd?.Invoke(processedItems, properties);
		}

		#endregion Public Methods

		#region Private Methdos

		private void InitPropObject(Type _type)
		{
			// take only native and public proeperties
			properties = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
		}

		private void ForEachItem(List<T> list)
		{
			foreach (var item in list)
			{
				if (_forEachItem != null)
					processedItems.Add(_forEachItem(GetValues(item)));
				else
					processedItems.Add(GetValues(item));
			}
		}

		private List<object> GetValues(T obj)
		{
			object val = null;
			var valueList = new List<object>();
			// get values from the model
			foreach (var s in properties)
			{
				try
				{
					val = s.GetValue(obj) ?? DBNull.Value;
				}
				catch
				{
					val = DBNull.Value;
				}

				valueList.Add(val);
			}

			return valueList;
		}

		#endregion Private Methods
	}

}
