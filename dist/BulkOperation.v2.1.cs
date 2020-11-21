using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Bulk Operaion v2.1
/// Apache License 2.0
/// Repo: github.com/TareqNewazShahriar/Bulk-Operation-Using-CSharp-Reflection
/// </summary>
namespace BulkOperation
{
	/// <summary>
	/// A class to traverse and to do operations on a collection of complex objects.
	/// It will prepare the property information and values of each property using reflexion
	/// for the operation.
	/// </summary>
	/// <typeparam name="T">Complex type to process</typeparam>
	/// <typeparam name="TResult">Resultant type</typeparam>
	public class BulkOperation<T, TResult>
		where T : class
		where TResult : class
	{
		#region Fields

		private List<T> list = null;
		private List<PropertyInfo> properties = null;
		private List<KeyValuePair<string, Type>> propsNameAndType = null;
		private List<TResult> processedItems = null;

		Func<List<object>, List<KeyValuePair<string, Type>>, TResult> _ProcessEachItem;
		Func<List<TResult>, List<KeyValuePair<string, Type>>, TResult> _Finally;

		#endregion Fields

		#region Constructor

		public BulkOperation(List<T> list, 
			Func<List<object>, List<KeyValuePair<string, Type>>, TResult> processEachItems, 
			Func<List<TResult>, List<KeyValuePair<string, Type>>, TResult> AtEnd)
		{
			this._ProcessEachItem = processEachItems;
			this._Finally = AtEnd;

			this.list = list;
			properties = new List<PropertyInfo>();
			processedItems = new List<TResult>();

			InitPropObject();
		}

		#endregion Constructor

		#region Public Methods

		public TResult Process()
		{
			foreach (var item in list)
			{
				processedItems.Add(_ProcessEachItem(GetValues(item), propsNameAndType));
			}

			return _Finally.Invoke(processedItems, propsNameAndType);
		}

		#endregion Public Methods

		#region Private Methdos

		private void InitPropObject()
		{
			var type = typeof(T);
			// take only native and public proeperties
			properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
			propsNameAndType = properties.Select(x => new KeyValuePair<string, Type>(x.Name, x.PropertyType)).ToList();
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
